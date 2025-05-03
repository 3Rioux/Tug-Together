using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class JR_MainMenuUI1 : MonoBehaviour
{
    [Header("UI References")]
    public Button hostButton;
    public TMP_InputField joinCodeInput;
    public Button joinByCodeButton;
    public Button quickJoinButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI signInText;

    [SerializeField] GameObject MainMenu;
    [SerializeField] GameObject HostMenu;
    [SerializeField] Button hostStartGameButton; // button that is on the host that triggers the game to start for everyone 
    [SerializeField] TMP_InputField lobbyCodeText;


    private bool isPlayerIdTextSet = false;

    private void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinByCodeButton.onClick.AddListener(OnJoinByCodeClicked);
        quickJoinButton.onClick.AddListener(OnQuickJoinClicked);

        //Start Game HOST ONLY
        hostStartGameButton.onClick.AddListener(OnHostStartGamePressed);

        
        // Optional sign-in display
        if (AuthenticationManager.Instance != null && AuthenticationManager.Instance.IsSignedIn)
        {
            signInText.text = $"Signed in as: {Unity.Services.Authentication.AuthenticationService.Instance.PlayerId}";
        }
    }

    private void LateUpdate()
    {
        // Optional sign-in display
        if (AuthenticationManager.Instance != null && AuthenticationManager.Instance.IsSignedIn && !isPlayerIdTextSet)
        {
            signInText.text = $"Signed in as: {Unity.Services.Authentication.AuthenticationService.Instance.PlayerId}";
            isPlayerIdTextSet = true; // set once 
        }
    }


    //===Button Click Events===

    public async void OnHostClicked()
    {
        statusText.text = "Creating Lobby...";
        var lobby = await LobbyManager.Instance.CreateLobbyAsync("My Lobby", 4, false);
        if (lobby == null)
        {
            statusText.text = "Failed to create lobby.";
            return;
        }

        statusText.text = "Setting up Relay...";
        string joinCode = await RelayManager.Instance.SetupRelayHostAsync();
        if (string.IsNullOrEmpty(joinCode))
        {
            statusText.text = "Relay allocation failed.";
            return;
        }

        NetworkManager.Singleton.StartHost();
        statusText.text = "Hosting game...";

        //set the Lobby Code Text to the Current Lobby Code:
        lobbyCodeText.text = LobbyManager.Instance.GetLobbyCode();
    }

    public async void OnJoinByCodeClicked()
    {
        string code = joinCodeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(code))
        {
            statusText.text = "Enter a join code.";
            return;
        }

        statusText.text = "Joining lobby...";
        var lobby = await LobbyManager.Instance.JoinLobbyByCodeAsync(code);
        if (lobby == null)
        {
            statusText.text = "Failed to join lobby.";
            return;
        }

        string relayCode = lobby.Data["RelayJoinCode"].Value;
        bool joined = await RelayManager.Instance.JoinRelayAsync(relayCode);
        if (!joined)
        {
            statusText.text = "Relay join failed.";
            return;
        }

        NetworkManager.Singleton.StartClient();
        statusText.text = "Joined game.";
    }

    public async void OnQuickJoinClicked()
    {
        statusText.text = "Searching for available games...";
        var lobby = await LobbyManager.Instance.QuickJoinLobbyAsync();
        if (lobby == null)
        {
            statusText.text = "No lobbies found.";
            return;
        }

        string relayCode = lobby.Data["RelayJoinCode"].Value;
        bool joined = await RelayManager.Instance.JoinRelayAsync(relayCode);
        if (!joined)
        {
            statusText.text = "Relay join failed.";
            return;
        }

        NetworkManager.Singleton.StartClient();
        statusText.text = "Joined game.";
    }


    /// <summary>
    /// Start Game Button Click 
    /// </summary>
    public void OnHostStartGamePressed()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        // Tell all clients to load the gameplay scene
        NetworkManager.Singleton.SceneManager.LoadScene("JR_Gameplay", LoadSceneMode.Single);

        // NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(); // ----------------------------------------------------THE ANSWER
    }

}
