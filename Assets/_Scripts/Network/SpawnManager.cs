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

    //track spawn player Name objects
    private List<PlayerNameDisplay> _spawnPlayerNameList = new List<PlayerNameDisplay>();
    
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

            ////set all names locally after Spawning is Done
            //AllPlayerNamesSetLocal();
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
        go.tag = "Player";
        
        var boatMovement = go.GetComponent<TugboatMovementWFloat>();
        if (boatMovement == null)
        {
            Debug.LogWarning("TugboatMovementWFloat component not found, adding it.", this);
            boatMovement = go.AddComponent<TugboatMovementWFloat>();
        }

        NetworkObject netObj = go.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId);
        _spawnedClients.Add(clientId);

        _spawnPlayerNameList.Add(go.GetComponent<PlayerNameDisplay>());

        //Add Player to Server + added Client RPC to add players to Clients as well
        int maxHealth = go.GetComponent<UnitHealthController>().MaxHealth; // get the units max health 
        AddPlayerToClientsServerRpc(clientId, maxHealth);

        //_spawnPlayerNameList[(int)clientId].GetPlayerName()

        //PlayerListUI.Instance?.UpdatePlayerNames(clientId, go.GetComponent<PlayerNameDisplay>().GetPlayerName(), netObj.IsLocalPlayer);
    }


    #region AddPlayerToTrackingList

    ///// <summary>
    ///// Get the other player names for late joinning players 
    ///// </summary>
    ///// <returns></returns>
    //private void AllPlayerNamesSetLocal()
    //{
    //    foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
    //    {
    //        PlayerListUI.Instance?.UpdatePlayerNames(clientId, _spawnPlayerNameList[(int)clientId].GetPlayerName(), _spawnPlayerNameList[(int)clientId].gameObject.GetComponent<NetworkObject>().IsLocalPlayer);
    //    }
    //}


    [ServerRpc(RequireOwnership = false)]
    public void AddPlayerToClientsServerRpc(ulong clientId, int playerMaxHealth, ServerRpcParams rpcParams = default)
    {
        if (PlayerListUI.Instance?.AddPlayerToList(clientId, playerMaxHealth) == true)
        {
            BroadcastAddPlayerClientIDClientRpc(clientId, playerMaxHealth);
            print("Added Player Server");
        }
        else
        {
            print("failed to add player to UI");
        }
    }

    [ClientRpc]
    void BroadcastAddPlayerClientIDClientRpc(ulong playerClientId, int playerMaxHealth)
    {
        //Loop through already spawned players to make sure the other players are also added 
        foreach (ulong playerID in NetworkManager.Singleton?.ConnectedClientsIds)
        {
            if (PlayerListUI.Instance?.AddPlayerToList(playerID, playerMaxHealth) == true)
            {
                print("Added Player Client");

                //Debug.Log($"{playerClientId} score is now: {score}");

                //// Store or update score
                //PlayerListUI.Instance?.UpdatePlayerScore(playerClientId, score);
            }
            else
            {
                print("failed to add Player On Client");
            }
        }
    }
    #endregion


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