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

    [Header("Options Submenus")]
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject videoMenu;
    [SerializeField] private GameObject audioMenu;

    [Header("Input")]
    [SerializeField] private InputActionReference backActionReference;
    [SerializeField] private InputActionReference pauseActionReference; // Pause action binding

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
        ProcessBackNavigation();
    }

    private void ProcessBackNavigation()
    {
        if (!gameMenuPanel.activeSelf && _isPaused)
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIBack, transform.position);
        }

        
        // If options panel is visible, process the submenu navigation.
        if (optionsPanel.activeSelf)
        {
            // If video submenu is active, close it and show settings.
            if (videoMenu != null && videoMenu.activeSelf)
            {
                videoMenu.SetActive(false);
                if (settingsMenu != null)
                    settingsMenu.SetActive(true);
                Debug.Log("Video menu closed, returning to settings.");
                return;
            }
            // If audio submenu is active, close it and show settings.
            if (audioMenu != null && audioMenu.activeSelf)
            {
                audioMenu.SetActive(false);
                if (settingsMenu != null)
                    settingsMenu.SetActive(true);
                Debug.Log("Audio menu closed, returning to settings.");
                return;
            }
            // If settings submenu is active, close options and return to pause menu.
            if (settingsMenu != null && settingsMenu.activeSelf)
            {
                optionsPanel.SetActive(false);
                gameMenuPanel.SetActive(true);
                Debug.Log("Settings closed, returning to pause menu.");
                return;
            }
        }
    }

    void OnPausePerformed(InputAction.CallbackContext context)
    {
        // Prevent pause toggle if the options panel is active.
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
        if (_isPaused)
            return;
        if (_anim != null)
            _anim.PlayOpenAnimation();
        _isPaused = true;
        gameMenuPanel.SetActive(true);
        Debug.Log("Game paused.");
    }

    // Use coroutine to delay deactivation until the closing animation completes.
    public IEnumerator ResumeGameCoroutine()
    {
        if (_anim != null)
            _anim.PlayCloseAnimation();

        yield return new WaitForSeconds(closeAnimationDuration);

        _isPaused = false;
        gameMenuPanel.SetActive(false);
        Debug.Log("Game resumed from pause menu.");
    }

    public void OnResumeButtonClicked()
    {
        if (_isPaused)
            StartCoroutine(ResumeGameCoroutine());
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
    }

    public void OnMainMenuButtonClicked()
    {
        DOTween.KillAll();
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
        SceneManager.LoadScene("MainMenu");
        Debug.Log("Returning to main menu.");
    }

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