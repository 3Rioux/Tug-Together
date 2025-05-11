using Unity.Netcode;
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
        NetworkManager.Singleton.StartHost();
        Debug.Log("Host Started");
    }

    public void StartClientOnClick()
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("Client Started");
    }

}
