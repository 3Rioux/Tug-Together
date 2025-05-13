using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    [SerializeField] private GameObject[] _playerPrefabVariants; // Array of player prefab variants
    [SerializeField] private Transform[] _spawnPoints;
    private int _nextSpawnIndex = 0;
    private bool _sceneLoaded;

    // Track spawned client IDs
    private HashSet<ulong> _spawnedClients = new HashSet<ulong>();
    
    // Track which client has which prefab variant
    private Dictionary<ulong, int> _clientPrefabVariants = new Dictionary<ulong, int>();
    
    // Track which prefab variants are currently in use
    private HashSet<int> _usedPrefabVariants = new HashSet<int>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
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

    private void HandleClientDisconnect(ulong clientId)
    {
        _spawnedClients.Remove(clientId);
        
        // Don't remove from _clientPrefabVariants to preserve their variant when reconnecting
        if (_clientPrefabVariants.TryGetValue(clientId, out int prefabIndex))
        {
            _usedPrefabVariants.Remove(prefabIndex);
        }
    }

    private void SpawnForClient(ulong clientId)
    {
        if (_spawnedClients.Contains(clientId))
            return;

        if (!_sceneLoaded)
        {
            StartCoroutine(DelaySpawn(clientId));
            return;
        }

        Transform spawn = _spawnPoints[_nextSpawnIndex % _spawnPoints.Length];
        _nextSpawnIndex++;

        // Determine which prefab variant to use
        int prefabIndex;
        
        // Check if client is reconnecting
        if (_clientPrefabVariants.TryGetValue(clientId, out prefabIndex))
        {
            Debug.Log($"Client {clientId} reconnected, using previous variant {prefabIndex}");
            _usedPrefabVariants.Add(prefabIndex);
        }
        else
        {
            // Assign new random variant that's not in use
            prefabIndex = GetUnusedPrefabVariant();
            _clientPrefabVariants[clientId] = prefabIndex;
            _usedPrefabVariants.Add(prefabIndex);
            Debug.Log($"Assigned new variant {prefabIndex} to client {clientId}");
        }

        // Instantiate the selected prefab variant
        GameObject go = Instantiate(_playerPrefabVariants[prefabIndex], spawn.position, spawn.rotation);

        var boatMovement = go.GetComponent<TugboatMovementWFloat>();
        if (boatMovement == null)
        {
            Debug.LogWarning("TugboatMovementWFloat component not found, adding it.", this);
            boatMovement = go.AddComponent<TugboatMovementWFloat>();
        }

        go.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        _spawnedClients.Add(clientId);
    }

    private int GetUnusedPrefabVariant()
    {
        // If all variants are in use, find an available one
        if (_usedPrefabVariants.Count >= _playerPrefabVariants.Length)
        {
            Debug.Log("All variants in use, selecting least used variant");
            return 0; // Default to first variant as fallback
        }

        // Get available variants
        List<int> availableVariants = new List<int>();
        for (int i = 0; i < _playerPrefabVariants.Length; i++)
        {
            if (!_usedPrefabVariants.Contains(i))
                availableVariants.Add(i);
        }

        // Select random available variant
        return availableVariants[Random.Range(0, availableVariants.Count)];
    }

    private IEnumerator DelaySpawn(ulong clientId)
    {
        while (!_sceneLoaded)
        {
            yield return new WaitForSeconds(0.5f);
        }
        SpawnForClient(clientId);
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
    }
}