// C#
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class SpawnManager : NetworkBehaviour
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Transform[] _spawnPoints;

    private int _nextSpawnIndex = 0;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback += SpawnForClient;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnForClient(clientId);
        }
    }

    private void SpawnForClient(ulong clientId)
    {
        Transform spawn = _spawnPoints[_nextSpawnIndex % _spawnPoints.Length];
        _nextSpawnIndex++;

        GameObject go = Instantiate(_playerPrefab, spawn.position, spawn.rotation);

        var boatMovement = go.GetComponent<StrippedTubBoatMovement>();
        if (boatMovement != null && boatMovement.targetSurface == null)
        {
            StartCoroutine(WaitForWaterSurface(boatMovement));
        }
        else
        {
            Debug.LogError("BoatMovement component not found on spawned object.", this);
        }

        go.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }

    private IEnumerator WaitForWaterSurface(StrippedTubBoatMovement boatMovement)
    {
        while (boatMovement.targetSurface == null)
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
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= SpawnForClient;
    }
}