using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class HostDisconnectionHandler : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject disconnectionPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI messageText;
    
    [Header("Settings")]
    [SerializeField] private string mainMenuScene = "_Scenes/MainMenu";
    [SerializeField] private float disconnectionTimeoutSeconds = 5f;
    
    private bool isCheckingDisconnection = false;
    private bool isHost = false;
    private Coroutine disconnectionCheckCoroutine;

    private void Start()
    {
        // Initialize UI
        if (disconnectionPanel != null)
            disconnectionPanel.SetActive(false);
            
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);
            
        if (messageText != null)
            messageText.text = "Host disconnected. The session has ended.";
        
        // Register for network events
        RegisterNetworkEvents();
    }
    
    private void RegisterNetworkEvents()
    {
        // NetCode for GameObjects events
        if (NetworkManager.Singleton != null)
        {
            isHost = NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            NetworkManager.Singleton.OnTransportFailure += OnTransportFailure;
        }
        
        // Subscribe to session events if available
        var sessionEventBridge = FindObjectOfType<SessionEventBridge>();
        if (sessionEventBridge != null)
        {
            sessionEventBridge.OnFailedToJoinSession.AddListener(OnSessionFailure);
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.OnTransportFailure -= OnTransportFailure;
        }
        
        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            
        if (disconnectionCheckCoroutine != null)
            StopCoroutine(disconnectionCheckCoroutine);
    }
    
    private void OnClientDisconnect(ulong clientId)
    {
        // If we're not the host and the host (clientId 0) disconnected
        if (!isHost && clientId == 0 && !isCheckingDisconnection)
        {
            StartDisconnectionCheck();
        }
    }
    
    private void OnTransportFailure()
    {
        if (!isHost && !isCheckingDisconnection)
        {
            StartDisconnectionCheck();
        }
    }
    
    private void OnSessionFailure(Unity.Services.Multiplayer.SessionException exception)
    {
        if (!isCheckingDisconnection)
        {
            ShowDisconnectionPanel();
        }
    }
    
    private void StartDisconnectionCheck()
    {
        isCheckingDisconnection = true;
        disconnectionCheckCoroutine = StartCoroutine(CheckHostDisconnection());
    }
    
    private IEnumerator CheckHostDisconnection()
    {
        // Wait for timeout to confirm disconnection
        yield return new WaitForSeconds(disconnectionTimeoutSeconds);
        
        // If we're still connected to a server, false alarm
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            isCheckingDisconnection = false;
            yield break;
        }
        
        // Show disconnection panel
        ShowDisconnectionPanel();
    }
    
    private void ShowDisconnectionPanel()
    {
        if (disconnectionPanel != null)
        {
            disconnectionPanel.SetActive(true);
            
            // Ensure it's visible in the hierarchy
            Canvas canvas = disconnectionPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 100;
            }
        }
    }
    
    private void OnCloseButtonClicked()
    {
        // Hide panel first
        if (disconnectionPanel != null)
            disconnectionPanel.SetActive(false);
            
        // Shutdown network connections
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            NetworkManager.Singleton.Shutdown();
            
        // Return to main menu
        SceneManager.LoadScene(mainMenuScene);
    }
}