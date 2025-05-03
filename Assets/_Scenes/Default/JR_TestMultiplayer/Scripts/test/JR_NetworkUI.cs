using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.UI;


public class JR_NetworkUI : MonoBehaviour
{

    //public Button ServerButton;
    //public Button HostButton;
    //public Button ClientButton;

    
    public void StartServerOnClick()
    {
        NetworkManager.Singleton.StartServer();
        Debug.Log("Server Started");
    }

    public void StartHostOnClick()
    {
        StartHostWithRelayAsync();
        Debug.Log("Host Started");
    }

    public void StartClientOnClick()
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("Client Started");
    }


    public async void StartHostWithRelayAsync()
    {
        try
        {
            // 1. Allocate a Relay server
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4); // max players

            // 2. Get join code to share with clients
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"Relay Join Code: {joinCode}");

            // 3. Configure Unity Transport with Relay data
            //UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            //transport.SetRelayServerData(new RelayServerData(allocation, RelayServerData.TransportProtocol.Udp));

            // 4. Start Host (after setting Relay server data)
            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay Exception: {e.Message}");
        }
    }

}
