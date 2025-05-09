// C#

using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject gameMenuPanel; // Also used as pause panel
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject sessionPanel;
    [SerializeField] private GameObject quitPanel;

    [Header("Options Submenus")]
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject videoMenu;
    [SerializeField] private GameObject audioMenu;

    [Header("Input")]
    [SerializeField] private InputActionReference backActionReference;
    [SerializeField] private InputActionReference pauseActionReference; // Pause action binding

    [Header("Blur Effect")]
    [SerializeField] private UIBlurController blurController; // Reference to the UIBlurController

    // Duration for the close animation before deactivating the panel.
    [SerializeField] private float closeAnimationDuration = 0.5f;

    private LiquidBubblePanelAnimation _anim;
    private bool _isPaused = false;

    void Awake()
    {
        if (gameMenuPanel != null)
        {
            _anim = gameMenuPanel.GetComponent<LiquidBubblePanelAnimation>();
            if (_anim == null)
                Debug.LogError("Panel missing LiquidBubblePanelAnimation", this);
        }

        if (backActionReference != null)
            backActionReference.action.performed += OnBackActionPerformed;
        else
            Debug.LogError("Back action reference not assigned");

        if (pauseActionReference != null)
            pauseActionReference.action.performed += OnPausePerformed;
        else
            Debug.LogError("Pause action reference not assigned");
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void OnDestroy()
    {
        if (backActionReference != null)
            backActionReference.action.performed -= OnBackActionPerformed;
        if (pauseActionReference != null)
            pauseActionReference.action.performed -= OnPausePerformed;
    }

    void OnBackActionPerformed(InputAction.CallbackContext context)
    {
        ProcessBackNavigation();
    }

    public void OnBackButtonClicked()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIBack, transform.position);
        ProcessBackNavigation();
    }

    private void ProcessBackNavigation()
    {
        if (quitPanel.activeSelf)
        {
            OnQuitCancelButtonClicked();
            return;
        }
    
        if (sessionPanel.activeSelf)
        {
            OnSessionBackButtonClicked();
            return;
        }
        
        if (optionsPanel.activeSelf)
        {
            if (videoMenu != null && videoMenu.activeSelf)
            {
                videoMenu.SetActive(false);
                if (settingsMenu != null)
                    settingsMenu.SetActive(true);
                Debug.Log("Video menu closed, returning to settings.");
                return;
            }
            if (audioMenu != null && audioMenu.activeSelf)
            {
                audioMenu.SetActive(false);
                if (settingsMenu != null)
                    settingsMenu.SetActive(true);
                Debug.Log("Audio menu closed, returning to settings.");
                return;
            }
            if (settingsMenu != null && settingsMenu.activeSelf)
            {
                optionsPanel.SetActive(false);
                gameMenuPanel.SetActive(true);
                Debug.Log("Settings closed, returning to pause menu.");
                return;
            }
            if (sessionPanel != null && sessionPanel.activeSelf)
            {
                sessionPanel.SetActive(false);
                gameMenuPanel.SetActive(true);
                Debug.Log("session closed, returning to pause menu.");
                return;
            }
            if (quitPanel != null && quitPanel.activeSelf)
            {
                quitPanel.SetActive(false);
                gameMenuPanel.SetActive(true);
                Debug.Log("quit closed, returning to pause menu.");
                return;
            }
        }
    }

    void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (optionsPanel.activeSelf)
            return;
        TogglePause();
    }

    void TogglePause()
    {
        if (_anim != null && _anim.IsAnimating)
            return;

        if (_isPaused)
            StartCoroutine(ResumeGameCoroutine());
        else
            PauseGame();
    }

    public void PauseGame()
    {
        gameMenuPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        
        if (_isPaused)
            return;

        if (_anim != null)
            _anim.PlayOpenAnimation();

        // Call UIBlurController to apply blur on pause.
        if (blurController != null)
            blurController.ApplyPauseBlur();

        // Disable only the local player's boat controls
        DisableLocalPlayerControls();

        _isPaused = true;
        Debug.Log("Game paused.");
    }

    public IEnumerator ResumeGameCoroutine()
    {

        Cursor.lockState = CursorLockMode.Locked;
        
        if (_anim != null)
            _anim.PlayCloseAnimation();

        // Call UIBlurController to remove blur when resuming.
        if (blurController != null)
            blurController.RemovePauseBlur();
        
        // Re-enable only the local player's boat controls
        EnableLocalPlayerControls();
        
        yield return new WaitForSeconds(closeAnimationDuration);

        gameMenuPanel.SetActive(false);
        _isPaused = false;
        
        Debug.Log("Game resumed from pause menu.");
    }
    
    private void DisableLocalPlayerControls()
    {
        foreach (var boat in FindObjectsOfType<StrippedTubBoatMovement>())
        {
            if (boat.IsOwner) // Only disable controls for the local player's boat
            {
                boat.SetControlEnabled(false);
            }
        }
    }

    private void EnableLocalPlayerControls()
    {
        foreach (var boat in FindObjectsOfType<StrippedTubBoatMovement>())
        {
            if (boat.IsOwner) // Only enable controls for the local player's boat
            {
                boat.SetControlEnabled(true);
            }
        }
    }

    public void OnResumeButtonClicked()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
    
        if (_isPaused)
            StartCoroutine(ResumeGameCoroutine());
    }

    public void OnQuitButtonClicked()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
        if (gameMenuPanel != null) gameMenuPanel.SetActive(false);
        quitPanel.SetActive(true);
    }

    public void OnSessionButtonClicked()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
        if (gameMenuPanel != null) gameMenuPanel.SetActive(false);
        sessionPanel.SetActive(true);
    
        // Ensure SessionCodeConnector is working properly when the panel is opened
        RefreshSessionCodeConnector();
    }
    
    private void RefreshSessionCodeConnector()
    {
        // Find the SessionCodeConnector component in the session panel
        SessionCodeConnector connector = sessionPanel.GetComponentInChildren<SessionCodeConnector>(true);
    
        if (connector != null)
        {
            // Just call RefreshConnection instead of destroying and recreating
            connector.RefreshConnection();
            Debug.Log("SessionCodeConnector refreshed");
        }
        else
        {
            // If no connector exists, add one
            sessionPanel.AddComponent<SessionCodeConnector>();
            Debug.Log("SessionCodeConnector added");
        }
    }
    
    public void OnQuitCancelButtonClicked()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIBack, transform.position);
        quitPanel.SetActive(false);
        gameMenuPanel.SetActive(true);
    }
    
    public void OnSessionBackButtonClicked()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIBack, transform.position);
        sessionPanel.SetActive(false);
        gameMenuPanel.SetActive(true);
    }

    public void OnMainMenuButtonClicked()
    {
        DOTween.KillAll();
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
        SceneTransition.Instance.LoadScene("MainMenu");
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("Returning to main menu.");
    }
    
    // public void OnMainMenuButtonClicked()
    // {
    //     DOTween.KillAll();
    //     AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
    //
    //     // If we're in a networked game, use LoadNetworkedScene instead
    //     if (Unity.Netcode.NetworkManager.Singleton != null && 
    //         Unity.Netcode.NetworkManager.Singleton.IsListening)
    //     {
    //         if (Unity.Netcode.NetworkManager.Singleton.IsHost || Unity.Netcode.NetworkManager.Singleton.IsServer)
    //         {
    //             // Host/server can trigger network scene change
    //             SceneTransition.Instance.LoadNetworkedSceneForAllClients("MainMenu");
    //         }
    //         else
    //         {
    //             // Clients should just do a local scene change since they can't control the network
    //             SceneTransition.Instance.LoadScene("MainMenu");
    //         }
    //     }
    //     else
    //     {
    //         // Not in a networked game, use regular scene load
    //         SceneTransition.Instance.LoadScene("MainMenu");
    //     }
    //
    //     Cursor.lockState = CursorLockMode.None;
    //     Debug.Log("Returning to main menu.");
    // }

    public void OnOptionsButtonClicked()
    {
        gameMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
        if (settingsMenu != null) settingsMenu.SetActive(true);
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
        if (videoMenu != null) videoMenu.SetActive(false);
        if (audioMenu != null) audioMenu.SetActive(false);

        Debug.Log("Options opened from pause menu.");
    }

    public void OnCloseOptionsButtonClicked()
    {
        optionsPanel.SetActive(false);
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIBack, transform.position);
        gameMenuPanel.SetActive(true);
        Debug.Log("Options closed.");
    }

    public void OnVideoButtonClicked()
    {
        if (settingsMenu != null) settingsMenu.SetActive(false);
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
        if (videoMenu != null) videoMenu.SetActive(true);
        Debug.Log("Video menu opened.");
    }

    public void OnAudioButtonClicked()
    {
        if (settingsMenu != null) settingsMenu.SetActive(false);
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
        if (audioMenu != null) audioMenu.SetActive(true);
        Debug.Log("Audio menu opened.");
    }
}