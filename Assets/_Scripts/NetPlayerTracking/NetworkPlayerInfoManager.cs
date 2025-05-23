using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

/// <summary>
/// This helps with centralized tracking and menu rendering of the player info 
/// </summary>
public class NetworkPlayerInfoManager : MonoBehaviour 
{
    public static NetworkPlayerInfoManager Instance;

    public readonly Dictionary<ulong, NetworkPlayerInfo> playerInfos = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        //// Manually add the host's player
        //if (IsHost)
        //{
        //    var clientId = NetworkManager.Singleton.LocalClientId;
        //    var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        //    var info = playerObj.GetComponent<NetworkPlayerInfo>();
        //    if (info)
        //    {
        //        playerInfos[clientId] = info;
        //    }
        //    else
        //    {
        //        print("Failled to add Connected cliend to the playerInfos Dictionary");
        //    }
        //    //foreach (var client in NetworkManager.Singleton.ConnectedClients)
        //    //{
        //    //    AddPlayerInfo(client.Key, client.Value.PlayerObject);
        //    //}
        //}

        //NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        //NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    //public override void OnNetworkSpawn()
    //{
    //    NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    //    NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    //}


    private void OnClientConnected(ulong clientId)
    {
        var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        var info = playerObj.GetComponent<NetworkPlayerInfo>();
        if (info)
        {
            playerInfos[clientId] = info;
        }else
        {
            print("Failled to add Connected cliend to the playerInfos Dictionary");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (playerInfos.ContainsKey(clientId)) playerInfos.Remove(clientId);
    }


    public IReadOnlyDictionary<ulong, NetworkPlayerInfo> GetAllPlayerInfos() => playerInfos;


    ///// <summary>
    ///// Method to add the player info on the Host Manually.
    ///// </summary>
    ///// <param name="clientId"></param>
    ///// <param name="playerObject"></param>
    //private void AddPlayerInfo(ulong clientId, NetworkObject playerObject)
    //{
    //    if (!playerObject) return;

    //    var info = playerObject.GetComponent<NetworkPlayerInfo>();
    //    if (info && !playerInfos.ContainsKey(clientId))
    //    {
    //        playerInfos[clientId] = info;
    //    }
    //}

}
