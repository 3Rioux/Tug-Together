using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RespawnPointManager : NetworkBehaviour 
{
    public static RespawnPointManager Instance;

    [SerializeField] private PlayerRespawn playerRespawn;
    [SerializeField] private List<Transform> spawnPoints = new();
    private Dictionary<int, ulong> occupiedSpawns = new(); // Key: spawn index, Value: clientId


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    ///  Called by server to get a free spawn point and mark it as used
    /// </summary>
    /// <param name="clientId"></param>
    /// <returns></returns>
    public Transform GetAvailableSpawnPoint(ulong clientId)
    {
        if (!IsServer)
        {
            Debug.LogError("Only the server should assign spawn points.");
            return null;
        }

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (!occupiedSpawns.ContainsKey(i))
            {
                occupiedSpawns[i] = clientId;
                return spawnPoints[i];
            }
        }

        Debug.LogWarning("No available spawn points!");
        return null;
    }

    /// <summary>
    /// Frees a spawn point after a delay
    /// </summary>
    /// <param name="spawnIndex"></param>
    /// <param name="delay"></param>
    /// <returns></returns>
    private IEnumerator FreeSpawnPointAfterDelay(int spawnIndex, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (occupiedSpawns.ContainsKey(spawnIndex))
        {
            occupiedSpawns.Remove(spawnIndex);
        }
    }

    /// <summary>
    /// Manually release spawn point for a specific player
    /// </summary>
    /// <param name="clientId"></param>
    public void ReleaseSpawnPoint(ulong clientId)
    {
        foreach (var kvp in occupiedSpawns)
        {
            if (kvp.Value == clientId)
            {
                occupiedSpawns.Remove(kvp.Key);
                return;
            }
        }
    }

    public void ResetAllSpawnPoints()
    {
        occupiedSpawns.Clear();
    }





    public void RespawnPointRequest()
    {
        RequestRespawnServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc]
    private void RequestRespawnServerRpc(ulong clientId, ServerRpcParams rpcParams = default)
    {
        var spawn = GetAvailableSpawnPoint(clientId);
        if (spawn != null)
        {
            ApplyRespawnClientRpc(clientId, spawn.position);
        }
    }

    [ClientRpc]
    private void ApplyRespawnClientRpc(ulong clientId, Vector3 spawnPosition)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId)
            return;

        // Teleport and respawn locally
        playerRespawn.UpdateSpawnPoint(spawnPosition);
    }

}
