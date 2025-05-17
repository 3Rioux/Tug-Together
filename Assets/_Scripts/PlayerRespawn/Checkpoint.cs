using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private List<Transform> checkpointSpawnPoints;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var netObj = other.GetComponent<NetworkObject>();

        if (netObj != null && netObj.IsOwner)
        {
            // Tell server to set this as the new respawn point
            var player = other.GetComponent<PlayerRespawn>();
            //Set Spawn Point to one of the random Spawn points 
            player.UpdateSpawnPoint(checkpointSpawnPoints[Random.Range(0, checkpointSpawnPoints.Count)]);
        }
    }
}
