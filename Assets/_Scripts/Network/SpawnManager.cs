// C#
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

public class SpawnManager : NetworkBehaviour
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    private int _nextSpawnIndex = 0;
    private bool _sceneLoaded;

    // Track spawned client IDs to avoid duplicate spawns.
    private HashSet<ulong> _spawnedClients = new HashSet<ulong>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
        {
            _sceneLoaded = true;
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                SpawnForClient(clientId);
            }
        }
    }

    private void SpawnForClient(ulong clientId)
    {
        // Only spawn if this client hasn't been spawned yet.
        if (_spawnedClients.Contains(clientId))
            return;

        // If the scene isn't loaded, delay the spawn.
        if (!_sceneLoaded)
        {
            StartCoroutine(DelaySpawn(clientId));
            return;
        }

        Transform spawn = _spawnPoints[_nextSpawnIndex % _spawnPoints.Length];
        _nextSpawnIndex++;

        GameObject go = Instantiate(_playerPrefab, spawn.position, spawn.rotation);

        //Add Player to leaderboard:

        // Try to get the component, and add it if it doesn't exist
        var boatMovement = go.GetComponent<TugboatMovementWFloat>();
        if (boatMovement == null)
        {
            Debug.LogWarning("TugboatMovementWFloat component not found, attempting to add it.", this);
            boatMovement = go.AddComponent<TugboatMovementWFloat>();
        }
    
        // Initialize water surface
        if (boatMovement != null && boatMovement.targetSurface == null)
        {
            StartCoroutine(WaitForWaterSurface(boatMovement));
        }
    
        go.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        _spawnedClients.Add(clientId);

        // Update leaderboard now that the player is spawned
        LeaderboardManager.Instance?.UpdateLeaderboard();
    }

    private IEnumerator DelaySpawn(ulong clientId)
    {
        while (!_sceneLoaded)
        {
            yield return new WaitForSeconds(0.5f);
        }
        SpawnForClient(clientId);
    }

    private IEnumerator WaitForWaterSurface(TugboatMovementWFloat boatMovement)
    {
        GameObject ocean = GameObject.Find("Ocean");
        if (ocean != null)
        {
            boatMovement.targetSurface = ocean.GetComponent<WaterSurface>();
            if (boatMovement.targetSurface != null)
            {
                Debug.Log("WaterSurface found on Ocean object: " + ocean.name);
                yield break;
            }
        }
        boatMovement.targetSurface = FindObjectOfType<WaterSurface>();
        if (boatMovement.targetSurface != null)
        {
            Debug.Log("WaterSurface found via FindObjectOfType on: " + boatMovement.targetSurface.gameObject.name);
        }
        else
        {
            Debug.LogError("WaterSurface component not found in the scene.", this);
        }
        yield break;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
    }
}