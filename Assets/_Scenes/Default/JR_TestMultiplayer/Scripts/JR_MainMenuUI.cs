using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class JR_MainMenuUI : MonoBehaviour
{

    [SerializeField] Button hostStartGameButton; // button that is on the host that triggers the game to start for everyone 

    /// <summary>
    /// Host button Click 
    /// </summary>
    public async void HostGameButtonClicked()
    {
        var lobby = await LobbyManager.Instance.CreateLobbyAsync("MyLobby", 4, isPrivate: false);
        if (lobby == null) return;

        string relayCode = await RelayManager.Instance.SetupRelayHostAsync();
        if (string.IsNullOrEmpty(relayCode)) return;

        NetworkManager.Singleton.StartHost();

        // Enable "Start Game" Button for host
        hostStartGameButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Client Join Button click 
    /// </summary>
    /// <param name="code"></param>
    public async void JoinGameButtonClicked(string code)
    {
        var lobby = await LobbyManager.Instance.JoinLobbyByCodeAsync(code);
        if (lobby == null) return;

        string relayCode = lobby.Data["RelayJoinCode"].Value;

        bool joined = await RelayManager.Instance.JoinRelayAsync(relayCode);
        if (!joined) return;

        NetworkManager.Singleton.StartClient();
    }

    /// <summary>
    /// Start Game Button Click 
    /// </summary>
    public void OnHostStartGamePressed()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        // Tell all clients to load the gameplay scene
        NetworkManager.Singleton.SceneManager.LoadScene("GameplayScene", LoadSceneMode.Single);

       // NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(); // ----------------------------------------------------THE ANSWER
    }
}
