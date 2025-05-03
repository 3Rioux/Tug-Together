using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;
using Unity.Services.Lobbies;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task<string> SetupRelayHostAsync(int maxConnections = 4)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log($"Relay Host Join Code: {joinCode}");

            // Store in lobby so clients can retrieve
            await LobbyService.Instance.UpdateLobbyAsync(LobbyManager.Instance.CurrentLobby.Id, new UpdateLobbyOptions
            {
                Data = new System.Collections.Generic.Dictionary<string, Unity.Services.Lobbies.Models.DataObject>
                {
                    { "RelayJoinCode", new Unity.Services.Lobbies.Models.DataObject(Unity.Services.Lobbies.Models.DataObject.VisibilityOptions.Member, joinCode) }
                }
            });

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            //transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay host setup failed: {e.Message}");
            return null;
        }
    }

    public async Task<bool> JoinRelayAsync(string joinCode)
    {
        try
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
           // transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay client join failed: {e.Message}");
            return false;
        }
    }
}