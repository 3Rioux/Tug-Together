using System.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine.SceneManagement;

public class HostDisconnectionHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SessionEventBridge sessionEventBridge;
    [SerializeField] private WidgetEventDispatcherBridge eventDispatcherBridge;
    [SerializeField] private Canvas disconnectionNoticeCanvas;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float returnToMenuDelay = 5f;
    
    private ISession currentSession;
    private string hostPlayerId;
    private bool isReturningToMenu = false;

    private void Start()
    {
        // Make sure required components are found
        // Then disable the canvas
        if (disconnectionNoticeCanvas != null)
        {
            disconnectionNoticeCanvas.gameObject.SetActive(false);
        }
    }
    
    private void OnEnable()
    {
        // Connect to network events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        }

        // Connect to session events
        if (sessionEventBridge != null)
        {
            sessionEventBridge.OnJoinedSession.AddListener(OnPlayerJoined);
        }

        // Connect to widget events if bridge exists
        if (eventDispatcherBridge != null)
        {
            eventDispatcherBridge.OnPlayerLeft.AddListener(OnRemotePlayerLeft);
            eventDispatcherBridge.OnRemovedFromSession.AddListener(OnRemovedFromSession);
            eventDispatcherBridge.OnSessionDeleted.AddListener(OnSessionDeleted);
        }
        
        // Initialize the disconnection canvas as hidden
        if (disconnectionNoticeCanvas != null)
        {
            disconnectionNoticeCanvas.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // Disconnect from network events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
        }

        // Disconnect from session events
        if (sessionEventBridge != null)
        {
            sessionEventBridge.OnJoinedSession.RemoveListener(OnPlayerJoined);
        }

        // Disconnect from widget events
        if (eventDispatcherBridge != null)
        {
            eventDispatcherBridge.OnPlayerLeft.RemoveListener(OnRemotePlayerLeft);
            eventDispatcherBridge.OnRemovedFromSession.RemoveListener(OnRemovedFromSession);
            eventDispatcherBridge.OnSessionDeleted.RemoveListener(OnSessionDeleted);
        }

        // Clean up session reference
        if (currentSession != null)
        {
            currentSession.PlayerJoined -= OnRemotePlayerJoined;
        }
    }
    
    private void OnPlayerJoined(ISession session)
    {
        Debug.Log("Connected to session: " + session.Id);
        currentSession = session;

        // Store the session object's ID as the host ID if you're not the host
        if (!NetworkManager.Singleton.IsHost)
        {
            try {
                // Use reflection to get the host ID if available
                var hostIdProperty = session.GetType().GetProperty("HostId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (hostIdProperty != null)
                {
                    hostPlayerId = hostIdProperty.GetValue(session) as string;
                    Debug.Log($"Found host ID via reflection: {hostPlayerId}");
                }
                else
                {
                    // Fallback: Store creator ID if available
                    var creatorIdProperty = session.GetType().GetProperty("CreatorId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (creatorIdProperty != null)
                    {
                        hostPlayerId = creatorIdProperty.GetValue(session) as string;
                        Debug.Log($"Using creator ID as host ID: {hostPlayerId}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error getting host ID: {ex.Message}");
            }
        }

        // Subscribe to the PlayerJoined event on the session
        currentSession.PlayerJoined += OnRemotePlayerJoined;
    }
    
    private void OnRemotePlayerJoined(string playerId)
    {
        Debug.Log($"Remote player joined: {playerId}");
    }
    
    private void OnClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            // Host doesn't handle its own disconnection here
            return;
        }
        
        // Handle when a client disconnects
        if (hostPlayerId != null && clientId.ToString() == hostPlayerId)
        {
            ShowDisconnectionNotice("Host has disconnected from the game.");
            StartCoroutine(ReturnToMenuAfterDelay(returnToMenuDelay));
        }
    }
    
    private void OnRemotePlayerLeft(string playerId)
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            // Host doesn't need special handling for other clients
            return;
        }
        
        if (playerId == hostPlayerId)
        {
            ShowDisconnectionNotice("Host has left the game.");
            StartCoroutine(ReturnToMenuAfterDelay(returnToMenuDelay));
        }
    }
    
    private void OnRemovedFromSession()
    {
        ShowDisconnectionNotice("You have been removed from the session.");
        StartCoroutine(ReturnToMenuAfterDelay(returnToMenuDelay));
    }
    
    private void OnSessionDeleted()
    {
        ShowDisconnectionNotice("The session has been deleted.");
        StartCoroutine(ReturnToMenuAfterDelay(returnToMenuDelay));
    }
    
    private void OnServerStopped(bool _)
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            // Host/server initiated the stop, normal flow
            return;
        }
        
        // For clients, this means the server stopped unexpectedly
        ShowDisconnectionNotice("Connection to host was lost.");
        StartCoroutine(ReturnToMenuAfterDelay(returnToMenuDelay));
    }

    private void ShowDisconnectionNotice(string message)
    {
        if (disconnectionNoticeCanvas != null)
        {
            disconnectionNoticeCanvas.gameObject.SetActive(true);

            // Find message text component that isn't the countdown text
            TextMeshProUGUI messageText = null;
            TextMeshProUGUI[] textComponents = disconnectionNoticeCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in textComponents)
            {
                if (text != countdownText)
                {
                    messageText = text;
                    break;
                }
            }

            // Update message text if found
            if (messageText != null)
            {
                messageText.text = message;
            }

            // Initialize the countdown text
            if (countdownText != null)
            {
                countdownText.text = returnToMenuDelay.ToString("0");
            }
        }
    }

    private IEnumerator ReturnToMenuAfterDelay(float delay)
    {
        isReturningToMenu = true;
        float timeRemaining = delay;

        while (timeRemaining > 0)
        {
            // Update countdown text
            if (countdownText != null)
            {
                countdownText.text = Mathf.CeilToInt(timeRemaining).ToString();
            }

            // Wait one second
            yield return new WaitForSeconds(1f);
            timeRemaining -= 1f;
        }

        // Final countdown display
        if (countdownText != null)
        {
            countdownText.text = "0";
        }

        ReturnToMainMenu();
    }
    
    private void ReturnToMainMenu()
    {
        // Clean up network connection
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
        
        // Load main menu scene
        SceneManager.LoadScene("_Scenes/MainMenu");
    }
}