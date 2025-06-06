using System;
using System.Collections;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject hostMenu;
    [SerializeField] private GameObject joinMenu;
    [SerializeField] private GameObject tutorialMenu;
    [SerializeField] private GameObject createMenu;
    [SerializeField] private GameObject optionsContainer;
    [SerializeField] private GameObject loadingMenu;
    [SerializeField] private GameObject lobbyMenu;
    [SerializeField] private GameObject roomCodeMenu;

    [Header("Options Submenus")]
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject videoMenu;
    [SerializeField] private GameObject audioMenu;

    [Header("Buttons")]
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button proceedButton;
    
    [Header("Button Animation")]
    [SerializeField] private float buttonAnimationDuration = 0.3f;
    [SerializeField] private Ease buttonShrinkEase = Ease.InBack;
    [SerializeField] private Ease buttonExpandEase = Ease.OutBack;
    
    [Header("Session Management")]
    [SerializeField] private SessionEventBridge sessionEventBridge;
    
    [Header("UI Animation")]
    [SerializeField] private float fadeAnimationDuration = 0.5f;
    private CanvasGroup loadingMenuCanvasGroup;
    
    private Unity.Services.Multiplayer.ISession currentSession;
    private bool isJoiningPlayer = false;
    
    private enum MenuState
    {
        Main,
        Host,
        Join,
        Options,
        VideoSettings,
        AudioSettings,
        Tutorial,
        Create,
        Loading,
        Lobby,
        RoomCode
    }

    private MenuState currentState = MenuState.Main;

    private void Awake()
    {
        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveAllListeners();
            creditsButton.onClick.AddListener(OnCreditsButtonClicked);
        }
        else
        {
            Debug.LogError("Credits button reference not assigned in the inspector.");
        }
    }

    private void Start()
    {
        AudioManager.Instance.PlayAmbience(FMODEvents.Instance.MainMenu);

        if (sessionEventBridge != null)
        {
            sessionEventBridge.OnJoiningSession.AddListener(OnPlayerJoining);
            sessionEventBridge.OnJoinedSession.AddListener(OnPlayerJoined);
            sessionEventBridge.OnFailedToJoinSession.AddListener(OnJoinFailed);
        }
        else
        {
            Debug.LogWarning("Session event bridge not found");
        }

        if (proceedButton != null)
        {
            proceedButton.gameObject.SetActive(false);
        }

        // Get or add CanvasGroup to loadingMenu
        loadingMenuCanvasGroup = loadingMenu.GetComponent<CanvasGroup>();
        
        Cursor.lockState = CursorLockMode.None;
    
        // Ensure initial state
        loadingMenuCanvasGroup.alpha = 0f;
        loadingMenu.SetActive(false);
    }
    
public void AnimateButtonDisable()
{
    if (proceedButton != null)
    {
        proceedButton.interactable = false;
        
        // Get canvas group component or add one
        CanvasGroup canvasGroup = proceedButton.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = proceedButton.gameObject.AddComponent<CanvasGroup>();
            
        // Create sequence for fade and scale
        Sequence disableSequence = DOTween.Sequence();
        
        // Fade out
        disableSequence.Append(canvasGroup.DOFade(0f, buttonAnimationDuration * 0.7f));
        
        // Scale down
        disableSequence.Join(proceedButton.transform.DOScale(Vector3.zero, buttonAnimationDuration)
            .SetEase(buttonShrinkEase));
            
        // Deactivate after complete
        disableSequence.OnComplete(() => {
            proceedButton.gameObject.SetActive(false);
            Debug.Log("Proceed button disabled and animated out");
        });
    }
}

private void AnimateButtonEnable()
{
    if (proceedButton != null)
    {
        // Activate button
        proceedButton.gameObject.SetActive(true);
        
        // Get canvas group component or add one
        CanvasGroup canvasGroup = proceedButton.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = proceedButton.gameObject.AddComponent<CanvasGroup>();
            
        // Set initial state
        canvasGroup.alpha = 0f;
        proceedButton.transform.localScale = Vector3.zero;
        
        // Create sequence for scale and fade
        Sequence enableSequence = DOTween.Sequence();
        
        // Scale up
        enableSequence.Append(proceedButton.transform.DOScale(Vector3.one, buttonAnimationDuration)
            .SetEase(buttonExpandEase));
            
        // Fade in
        enableSequence.Join(canvasGroup.DOFade(1f, buttonAnimationDuration));
        
        // Reset animator's initial scale and enable interaction
        enableSequence.OnComplete(() => {
            proceedButton.interactable = true;
            
            // Reset the ButtonAnimatorController's initial scale
            ButtonAnimatorController animator = proceedButton.GetComponent<ButtonAnimatorController>();
            if (animator != null)
                animator.ResetInitialScale(Vector3.one);
                
            Debug.Log("Proceed button re-enabled after animation");
        });
    }
}
    
// Add these methods to handle session events
    private void OnPlayerJoining()
    {
        // First show loading screen when joining starts
        SwitchToState(MenuState.Loading);
    
        // Disable button while joining with animation
        if (proceedButton != null)
        {
            //AnimateButtonDisable();
        }
    }

    private void OnPlayerJoined(Unity.Services.Multiplayer.ISession session)
    {
        // Store reference to the session
        currentSession = session;
        Debug.Log("Player joined with session: " + session.Id);
    
        // Subscribe to the PlayerJoined event on the session
        currentSession.PlayerJoined += OnRemotePlayerJoined;
    
        // Different menus based on whether player is hosting or joining
        if (isJoiningPlayer)
        {
            // Player joining with code goes to lobby
            SwitchToState(MenuState.Lobby);
        }
        else
        {
            // Host player stays on host menu or goes to create menu
            SwitchToState(MenuState.Create);
        }
    
        // Handle button animations
        if (proceedButton != null)
        {
            proceedButton.interactable = false;
            //AnimateButtonDisable();
            StartCoroutine(DelayedButtonEnable());
        }
    }
    
    private void OnRemotePlayerJoined(string playerId)
    {
        Debug.Log("Remote player joined: " + playerId);
        
        // Disable the button
        if (proceedButton != null)
        {
            proceedButton.interactable = false;
            AnimateButtonDisable();
            StartCoroutine(DelayedButtonEnable());
        }
    }

    private IEnumerator DelayedButtonEnable()
    {
        // Wait for 2 seconds before re-enabling button
        yield return new WaitForSeconds(3f);

        if (proceedButton != null)
        {
            AnimateButtonEnable();
        }
    }

    private void OnJoinFailed(Unity.Services.Multiplayer.SessionException exception)
    {
        // Handle join failure
        Debug.LogError("Failed to join session: " + exception.Message);
    
        // Return to the join menu state
        SwitchToState(MenuState.Join);
    
        // Re-enable proceed button if needed
        if (proceedButton != null)
            AnimateButtonEnable();
    }

    private void OnDestroy()
    {
        // Clean up event listeners
        if (sessionEventBridge != null)
        {
            sessionEventBridge.OnJoiningSession.RemoveListener(OnPlayerJoining);
            sessionEventBridge.OnJoinedSession.RemoveListener(OnPlayerJoined);
            sessionEventBridge.OnFailedToJoinSession.RemoveListener(OnJoinFailed);
        }
        
        if (currentSession != null)
        {
            currentSession.PlayerJoined -= OnRemotePlayerJoined;
        }
    }

    private void OnEnable()
    {
        SwitchToState(MenuState.Main);
    }
    
    public void ClickSound()
    {
        // FMOD sound trigger (do not modify)
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
    }

    public void BackSound()
    {
        // FMOD sound trigger (do not modify)
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIBack, transform.position);
    }

    private void SwitchToState(MenuState newState)
    {
        // Hide all panels first
        mainMenu.SetActive(false);
        hostMenu.SetActive(false);
        joinMenu.SetActive(false);
        //loadingMenu.SetActive(false);
        createMenu.SetActive(false);
        optionsContainer.SetActive(false);
        tutorialMenu.SetActive(false);
        lobbyMenu.SetActive(false);
        roomCodeMenu.SetActive(false);

        // For options submenus, ensure they're all hidden initially
        settingsMenu.SetActive(false);
        videoMenu.SetActive(false);
        audioMenu.SetActive(false);
        
        // If we're switching away from loading state, hide it
        if (currentState == MenuState.Loading && newState != MenuState.Loading)
            HideLoadingMenu();

        // Activate panels based on new state
        switch (newState)
        {
            case MenuState.Main:
                mainMenu.SetActive(true);
                break;

            case MenuState.Host:
                hostMenu.SetActive(true);
                createMenu.SetActive(true);
                break;

            case MenuState.Join:
                joinMenu.SetActive(true);
                roomCodeMenu.SetActive(true);
                break;

            case MenuState.Options:
                optionsContainer.SetActive(true);
                settingsMenu.SetActive(true);
                break;

            case MenuState.VideoSettings:
                optionsContainer.SetActive(true);
                videoMenu.SetActive(true);
                break;

            case MenuState.AudioSettings:
                optionsContainer.SetActive(true);
                audioMenu.SetActive(true);
                break;

            case MenuState.Tutorial:
                hostMenu.SetActive(true);
                tutorialMenu.SetActive(true);
                break;
            
            case MenuState.Create:
                hostMenu.SetActive(true);
                createMenu.SetActive(true);
                break;
            
            case MenuState.Loading:
                ShowLoadingMenu();
                break;
            
            case MenuState.Lobby:
                joinMenu.SetActive(true);
                lobbyMenu.SetActive(true);
                break;
        }

        currentState = newState;
    }

    public void OnBackButtonClick()
    {
        ExecuteBackNavigation();
    }

    public void OnProceedButtonClick()
    {
        ClickSound();
        //SceneTransition.Instance.LoadNetworkedSceneForAllClients("_Scenes/Complete_Level_1");
        //SceneTransition.Instance.LoadNetworkedSceneForAllClients("_Scenes/NEWLevelONE");
        SwitchToState(MenuState.Tutorial);
    }
    
    public void LevelONE()
    {
        ClickSound();
        SceneTransition.Instance.LoadNetworkedSceneForAllClients("_Scenes/LevelONEONE");
    }
    
    public void Lobby()
    {

        SwitchToState(MenuState.Lobby);
    }

    public void Loading()
    {
        SwitchToState(MenuState.Loading);
    }
    
    public void OnTutorialYesButtonClick()
    {
        ClickSound();
        DOTween.KillAll();
        //NetworkManager.Singleton.SceneManager.LoadScene("_Scenes/Tutorial", LoadSceneMode.Single);
        //SceneTransition.Instance.LoadNetworkedScene("_Scenes/Tutorial");
        SceneTransition.Instance.LoadNetworkedSceneForAllClients("_Scenes/LevelONEONE");

    }

    public void OnTutorialNoButtonClick()
    {
        ClickSound();
        DOTween.KillAll();
        //NetworkManager.Singleton.SceneManager.LoadScene("_Scenes/Level1", LoadSceneMode.Single);
        //NetworkManager.Singleton.SceneManager.LoadScene("_Scenes/Complete_Level_1", LoadSceneMode.Single);
        SceneTransition.Instance.LoadNetworkedSceneForAllClients("_Scenes/Complete_Level_1");
    }

    private void ExecuteBackNavigation()
    {
        if (currentState != MenuState.Main)
        {
            BackSound();
        }

        switch (currentState)
        {
            case MenuState.VideoSettings:
            case MenuState.AudioSettings:
                SwitchToState(MenuState.Options);
                break;

            case MenuState.Options:
            case MenuState.Host:
            case MenuState.Join:
            case MenuState.Lobby:
            case MenuState.Create:
                SwitchToState(MenuState.Main);
                break;
            case MenuState.Tutorial:
                SwitchToState(MenuState.Create);
                break;
        }
    }

    public void OnHostButtonClicked()
    {
        ClickSound();
        //SwitchToState(MenuState.Host);
        isJoiningPlayer = false; // Mark as host player
    }

    public void OnJoinButtonClicked()
    {
        ClickSound();
        SwitchToState(MenuState.Join);
        isJoiningPlayer = true;  // Mark as joining player
    }

    public void OnOptionsButtonClicked()
    {
        ClickSound();
        SwitchToState(MenuState.Options);
    }

    public void OnVideoButtonClicked()
    {
        ClickSound();
        SwitchToState(MenuState.VideoSettings);
    }

    public void OnAudioButtonClicked()
    {
        ClickSound();
        SwitchToState(MenuState.AudioSettings);
    }

    public void OnExitButtonClicked()
    {
        ClickSound();
        Application.Quit();
    }

    private void OnCreditsButtonClicked()
    {
        ClickSound();
        if (CreditsController.Instance != null)
            CreditsController.Instance.ShowCredits();
        else
            Debug.LogError("CreditsController instance not found");
    }
    
    // Method to show loading menu with animation
    public void ShowLoadingMenu()
    {
        // Make sure we have the canvas group
        if (loadingMenuCanvasGroup == null)
            loadingMenuCanvasGroup = loadingMenu.GetComponent<CanvasGroup>();
        
        // Set initial state
        loadingMenuCanvasGroup.alpha = 0f;
        loadingMenu.SetActive(true);
    
        // Animate fade in
        loadingMenuCanvasGroup.DOFade(1f, fadeAnimationDuration);
    }

// Method to hide loading menu with animation
    public void HideLoadingMenu()
    {
        if (loadingMenuCanvasGroup == null || !loadingMenu.activeSelf)
            return;
        
        // Animate fade out and deactivate when done
        loadingMenuCanvasGroup.DOFade(0f, fadeAnimationDuration)
            .OnComplete(() => loadingMenu.SetActive(false));
    }
}