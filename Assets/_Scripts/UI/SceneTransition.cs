using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using Unity.Netcode;

public class SceneTransition : MonoBehaviour
{
    // Keep existing properties and singleton setup
    public static SceneTransition Instance { get; private set; }

    [SerializeField] private float _defaultFadeDuration = 1f;
    [SerializeField] private Image _fadeImage;
    [SerializeField] private Image _loadingImage;
    [SerializeField] private bool _debugMessages = true; // Enable debugging by default

    private Material _transitionMaterial;
    private bool _isInitialized = false;
    private bool _isNetworkTransitionInProgress = false;
    private bool _clientIsFadingOut = false;
    private bool _isFadingIn = false;

    // Add a flag to track if client has seen the initial fade
    private bool _clientHasFadedIn = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFadeCanvas();
            _isInitialized = true;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (_isInitialized)
        {
            // Ensure we start fully faded out and then fade in
            if (_transitionMaterial != null)
            {
                _transitionMaterial.SetFloat("_Fade", 1f);
                _clientHasFadedIn = false; // Reset this flag on start
                _fadeImage.enabled = true; // Enable before initial fade
                FadeIn(_defaultFadeDuration);
            }
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Listen for network manager spawn events to register handlers when network is ready
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            RegisterNetworkHandlers();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // Re-register when client connects to ensure we have handlers
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            RegisterNetworkHandlers();
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            
            if (NetworkManager.Singleton.CustomMessagingManager != null &&
                NetworkManager.Singleton.IsListening)
            {
                UnregisterNetworkHandlers();
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DebugLog($"Scene loaded: {scene.name}, NetworkTransition: {_isNetworkTransitionInProgress}, ClientFading: {_clientIsFadingOut}");

        if (_isNetworkTransitionInProgress && !NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost)
        {
            // Client-specific handling after scene loads during a networked transition
            // Don't automatically fade in - wait for the message from host
            if (_transitionMaterial != null)
            {
                _transitionMaterial.SetFloat("_Fade", 1f); // Ensure we're fully faded out
            }
        }
        else if (!_isNetworkTransitionInProgress)
        {
            // For local scene loads, do a fade in
            if (_transitionMaterial != null && !_isFadingIn)
            {
                _transitionMaterial.SetFloat("_Fade", 1f); // Force fully faded out
                StartCoroutine(DelayedFadeIn(_defaultFadeDuration));
            }
        }
    }

    private void RegisterNetworkHandlers()
    {
        if (NetworkManager.Singleton.CustomMessagingManager == null) return;

        DebugLog($"Registering network handlers. IsClient: {NetworkManager.Singleton.IsClient}, IsHost: {NetworkManager.Singleton.IsHost}");

        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
            "SceneTransitionStart",
            (senderClientId, reader) =>
            {
                string sceneName;
                reader.ReadValueSafe(out sceneName);
                DebugLog($"Client received fade out message for scene: {sceneName}");
                
                _clientIsFadingOut = true;
                _isNetworkTransitionInProgress = true;
                
                // Force immediate material setup
                if (_transitionMaterial == null)
                {
                    InitializeFadeCanvas();
                    DebugLog("Material reinitialized on client fade start");
                }
                
                // Start with partially visible screen to make sure fade is visible
                if (_transitionMaterial != null)
                {
                    _transitionMaterial.SetFloat("_Fade", 0f);
                }
                
                // Start fade out on main thread
                MainThreadDispatcher.RunOnMainThread(() => {
                    StartCoroutine(ClientFadeOut(_defaultFadeDuration));
                });
            });

        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
            "SceneTransitionComplete",
            (senderClientId, reader) =>
            {
                DebugLog("Client received fade in message");
                _clientIsFadingOut = false;
                
                if (_transitionMaterial == null)
                {
                    InitializeFadeCanvas();
                    DebugLog("Material reinitialized on client fade complete");
                }
                
                // Ensure we're fully faded out
                if (_transitionMaterial != null)
                {
                    _transitionMaterial.SetFloat("_Fade", 1f);
                }
                
                // Start fade in on main thread
                MainThreadDispatcher.RunOnMainThread(() => {
                    StartCoroutine(DelayedFadeIn(_defaultFadeDuration));
                });
            });
        
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
            "ShowLoadingImage",
            (senderClientId, reader) => {
                DebugLog("Client received show loading image command");
        
                MainThreadDispatcher.RunOnMainThread(() => {
                    // First check if we're still fading the main transition image
                    if (_clientIsFadingOut) {
                        // We need to wait until the fade out is complete before showing loading
                        StartCoroutine(ShowLoadingImageWhenFadeComplete());
                    } else {
                        // Main fade is already complete, show loading image now
                        ShowLoadingImageImmediate();
                    }
                });
            });
    }
    
    private IEnumerator ShowLoadingImageWhenFadeComplete()
    {
        // Wait until the fade out flag is cleared or maximum of 2 seconds
        float timeout = 2f;
        float elapsed = 0f;
    
        while (_clientIsFadingOut && elapsed < timeout) {
            yield return null;
            elapsed += Time.deltaTime;
        }
    
        // Now show the loading image
        ShowLoadingImageImmediate();
    }

    private void ShowLoadingImageImmediate()
    {
        if (_loadingImage != null && _loadingImage.material != null) {
            _loadingImage.enabled = true;
            _loadingImage.material.SetFloat("_Fade", 0f);
        
            // Animate loading image fade-in
            _loadingImage.material.DOFloat(1f, "_Fade", _defaultFadeDuration * 0.5f)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true);
        
            DebugLog("Client showing loading image");
        }
        else {
            DebugLog("Client loading image or material is null!");
        }
    }

    private void UnregisterNetworkHandlers()
    {
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("SceneTransitionStart");
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("SceneTransitionComplete");
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("ShowLoadingImage");
    }

    private void InitializeFadeCanvas()
    {
        if (_fadeImage == null)
        {
            _fadeImage = GetComponentInChildren<Image>(true);
        }

        if (_fadeImage != null)
        {
            if (_fadeImage.material != null)
            {
                _fadeImage.material = new Material(_fadeImage.material);
                _transitionMaterial = _fadeImage.material;
                _transitionMaterial.SetFloat("_Fade", 0f);  // Start transparent
                _fadeImage.enabled = false;  // Start disabled
                DebugLog("Transition material initialized");
            }
            else
            {
                Debug.LogError("SceneTransition: Fade image material is null!");
            }
        }
        else
        {
            Debug.LogError("SceneTransition: Child canvas must contain an Image component!");
        }

        // Initialize loading image
        if (_loadingImage != null)
        {
            if (_loadingImage.material != null)
            {
                _loadingImage.material = new Material(_loadingImage.material);
                _loadingImage.material.SetFloat("_Fade", 0f);  // Start transparent
                _loadingImage.enabled = false;  // Start disabled
                DebugLog("Loading image material initialized");
            }
            else
            {
                Debug.LogError("SceneTransition: Loading image material is null!");
            }
        }
    }

    public void LoadScene(string sceneName, float fadeSpeed = -1f)
    {
        if (_transitionMaterial == null)
        {
            Debug.LogError("SceneTransition: Transition material is missing!");
            return;
        }

        float duration = (fadeSpeed > 0) ? fadeSpeed : _defaultFadeDuration;
        StartCoroutine(FadeAndLoadScene(sceneName, duration));
    }

    public void LoadNetworkedSceneForAllClients(string sceneName, float fadeSpeed = -1f)
    {
        if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Only the host can trigger a networked scene transition for all clients");
            return;
        }

        _isNetworkTransitionInProgress = true;
        DebugLog($"Host starting network scene transition to {sceneName}");

        // Register for scene load completion
        NetworkManager.Singleton.SceneManager.OnLoadComplete += HandleSceneLoaded;

        // Send message to all clients to start fading
        if (NetworkManager.Singleton.CustomMessagingManager != null)
        {
            using FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp);
            writer.WriteValueSafe(sceneName);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("SceneTransitionStart", writer);
            DebugLog("Sent fade out message to all clients");
        }
        else
        {
            Debug.LogError("CustomMessagingManager is null, cannot send fade message");
        }

        // Host also fades out and loads scene
        StartCoroutine(HostFadeAndLoad(sceneName, fadeSpeed));
    }

    private IEnumerator HostFadeAndLoad(string sceneName, float fadeSpeed)
    {
        // Fade out on host
        float duration = fadeSpeed > 0 ? fadeSpeed : _defaultFadeDuration;
    
        if (_transitionMaterial == null)
        {
            InitializeFadeCanvas();
        }
    
        // Start with clear fade
        if (_transitionMaterial != null)
        {
            _transitionMaterial.SetFloat("_Fade", 0f);
        }
    
        // Complete the full fade out sequence
        yield return FadeOut(duration);
    
        // Now tell all clients to show the loading image
        if (NetworkManager.Singleton.CustomMessagingManager != null)
        {
            using FastBufferWriter writer = new FastBufferWriter(4, Unity.Collections.Allocator.Temp);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("ShowLoadingImage", writer);
            DebugLog("Sent show loading image command to all clients");
        }
    
        // Wait to ensure clients have received the message
        yield return new WaitForSeconds(0.2f);
    
        DebugLog($"Host loading scene: {sceneName}");
    
        // Load the scene
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private void HandleSceneLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (_isNetworkTransitionInProgress)
        {
            DebugLog($"Scene loaded for client {clientId}");

            // Host sends fade-in command to all clients
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                // Wait a moment to make sure all clients have loaded the scene
                StartCoroutine(DelayedFadeInCommand());
            }
        }
    }
    
    private IEnumerator DelayedFadeInCommand()
    {
        // Give clients time to fully load the scene before fading in
        yield return new WaitForSeconds(1.0f);
        
        DebugLog("Host sending fade in command to all clients");

        if (NetworkManager.Singleton.CustomMessagingManager != null)
        {
            using FastBufferWriter writer = new FastBufferWriter(4, Unity.Collections.Allocator.Temp);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("SceneTransitionComplete", writer);
        }
        else
        {
            Debug.LogError("CustomMessagingManager is null for fade in");
        }

        // Host also fades in
        StartCoroutine(DelayedFadeIn(_defaultFadeDuration));

        // Clean up event listener
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= HandleSceneLoaded;
    }

    private IEnumerator ClientFadeOut(float fadeSpeed)
    {
        DebugLog("Client starting fade out");

        // Reinitialize the material if needed
        if (_transitionMaterial == null || _fadeImage == null)
        {
            InitializeFadeCanvas();
            if (_transitionMaterial == null)
            {
                Debug.LogError("Client fade out: Failed to initialize transition material!");
                yield break;
            }
        }

        // Make sure canvas is on top of everything
        Canvas canvas = _fadeImage.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 9999;
            DebugLog("Set canvas sorting order to maximum");
        }

        // Start with clear screen for visible fade
        _transitionMaterial.SetFloat("_Fade", 0f);
        
        // Wait one frame to ensure material is applied
        yield return null;

        // Do the fade animation
        yield return FadeOut(fadeSpeed);

        // Ensure we end fully faded
        _transitionMaterial.SetFloat("_Fade", 1f);
        _clientIsFadingOut = false; // Mark fade as complete
        DebugLog("Client fade out complete");
    }

    private IEnumerator DelayedFadeIn(float fadeSpeed)
    {
        // Prevent multiple fade-ins from starting
        if (_isFadingIn)
        {
            DebugLog("Skipping fade-in as one is already in progress");
            yield break;
        }

        _isFadingIn = true;
    
        // Wait briefly to ensure scene is rendered
        yield return new WaitForSeconds(0.5f);

        DebugLog("Starting fade in");

        // Ensure material state before animating
        if (_transitionMaterial == null)
        {
            InitializeFadeCanvas();
        }

        if (_transitionMaterial != null)
        {
            // Ensure we start fully faded
            _transitionMaterial.SetFloat("_Fade", 1f);

            // Fade back in
            yield return FadeIn(fadeSpeed);

            // Now we've seen our first fade-in
            _clientHasFadedIn = true;
        }
        else
        {
            Debug.LogError("Fade in: Transition material is null!");
        }

        // Reset network transition flag
        _isNetworkTransitionInProgress = false;
        _isFadingIn = false;
    }

    private IEnumerator FadeAndLoadScene(string sceneName, float fadeSpeed)
    {
        DebugLog($"Starting local fade and load to {sceneName}");

        // 1. Fade Out completely
        yield return FadeOut(fadeSpeed);

        // 2. Ensure material is fully faded
        if (_transitionMaterial != null)
        {
            _transitionMaterial.SetFloat("_Fade", 1f);
        }

        // 3. Load Scene
        SceneManager.LoadScene(sceneName);

        // 4. The OnSceneLoaded handler will take care of fade in
    }

    private IEnumerator FadeOut(float fadeSpeed)
    {
        if (_transitionMaterial == null)
        {
            Debug.LogError("Fade Out: Transition material is null!");
            yield break;
        }

        // 1. Ensure loading image is fully hidden and disabled first
        if (_loadingImage != null)
        {
            _loadingImage.material.SetFloat("_Fade", 0f);
            _loadingImage.enabled = false;
        }
    
        // 2. Enable and fade out the main transition image
        _fadeImage.enabled = true;
        DOTween.Kill(_transitionMaterial);

        float startValue = _transitionMaterial.GetFloat("_Fade");
        DebugLog($"Starting fade out animation from {startValue}, duration: {fadeSpeed}s");

        yield return _transitionMaterial.DOFloat(1f, "_Fade", fadeSpeed)
            .SetEase(Ease.InOutQuad)
            .SetUpdate(true)
            .WaitForCompletion();

        DebugLog("Fade out animation complete");
    
        // 3. Small delay before showing loading image
        yield return new WaitForSeconds(0.2f);

        // 4. Now fade in the loading image
        if (_loadingImage != null && _loadingImage.material != null)
        {
            _loadingImage.enabled = true;
            _loadingImage.material.SetFloat("_Fade", 0f);  // Start fully transparent

            DebugLog("Starting loading image fade in");

            yield return _loadingImage.material.DOFloat(1f, "_Fade", fadeSpeed * 0.5f)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true)
                .WaitForCompletion();

            DebugLog("Loading image fade-in complete");
        }
    }

    private IEnumerator FadeIn(float fadeSpeed)
    {
        // First fade out the loading image if visible
        if (_loadingImage != null && _loadingImage.material != null && _loadingImage.enabled)
        {
            DebugLog("Starting loading image fade out");
        
            yield return _loadingImage.material.DOFloat(0f, "_Fade", fadeSpeed * 0.5f)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true)
                .WaitForCompletion();
            
            _loadingImage.enabled = false;
            DebugLog("Loading image fade-out complete");
        }

        // Then fade out the main transition image
        if (_transitionMaterial == null)
        {
            Debug.LogError("Fade In: Transition material is null!");
            yield break;
        }

        _fadeImage.enabled = true;
        DOTween.Kill(_transitionMaterial);

        float startValue = _transitionMaterial.GetFloat("_Fade");
        DebugLog($"Starting fade in animation from {startValue}, duration: {fadeSpeed}s");

        yield return _transitionMaterial.DOFloat(0f, "_Fade", fadeSpeed)
            .SetEase(Ease.InOutQuad)
            .SetUpdate(true)
            .WaitForCompletion();

        _fadeImage.enabled = false;
        DebugLog("Fade in animation complete");
    }

    private void DebugLog(string message)
    {
        if (_debugMessages)
        {
            Debug.Log($"[SceneTransition] {message}");
        }
    }
}

// Add this helper class to run actions on the main thread
public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    public static void RunOnMainThread(Action action)
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("MainThreadDispatcher");
            _instance = go.AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}