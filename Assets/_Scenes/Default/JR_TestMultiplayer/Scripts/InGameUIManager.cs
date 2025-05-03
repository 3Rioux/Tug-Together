using Unity.Netcode;
using UnityEngine;

public class InGameUIManager : MonoBehaviour
{

    private void OnDestroy()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
            LobbyManager.Instance.LeaveLobby(); // make sure the lobby knows you left 
        }
    }

    private void OnApplicationQuit()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
            LobbyManager.Instance.LeaveLobby(); // make sure the lobby knows you left 
        }
    }

    public void OnLeaveGamePressed()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
            LobbyManager.Instance.LeaveLobby(); // make sure the lobby knows you left 
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("JR_MainMenu");
    }
}
