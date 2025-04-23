using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject hostMenu;
    [SerializeField] private GameObject joinMenu;
    // Options container that holds the options menus
    [SerializeField] private GameObject optionsContainer;

    // Options submenu panels:
    [SerializeField] private GameObject settingsMenu; // Default options panel
    [SerializeField] private GameObject videoMenu;
    [SerializeField] private GameObject audioMenu;

    [Header("Buttons")]
    [SerializeField] private Button creditsButton;

    [Header("Input")]
    [SerializeField] private InputActionReference backActionReference;

    private void Awake()
    {
        if (backActionReference != null)
        {
            backActionReference.action.performed += OnBackActionPerformed;
        }
        else
        {
            Debug.LogError("Back action reference not assigned in the inspector.");
        }

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

    private void OnEnable()
    {
        backActionReference?.action.Enable();

        // Start with main menu visible. All others are hidden.
        mainMenu.SetActive(true);
        hostMenu.SetActive(false);
        joinMenu.SetActive(false);
        optionsContainer.SetActive(false);

        // Within the options container, default to the settings menu and hide submenus.
        if (settingsMenu != null) settingsMenu.SetActive(true);
        if (videoMenu != null) videoMenu.SetActive(false);
        if (audioMenu != null) audioMenu.SetActive(false);
    }

    private void OnDisable()
    {
        if (backActionReference != null)
        {
            backActionReference.action.performed -= OnBackActionPerformed;
            backActionReference.action.Disable();
        }
    }

    private void OnBackActionPerformed(InputAction.CallbackContext context)
    {
        ExecuteBackNavigation();
    }

    // This method can be assigned to UI back buttons.
    public void OnBackButtonClick()
    {
        ExecuteBackNavigation();
    }

    // Consolidates back navigation logic:
    private void ExecuteBackNavigation()
    {
        if (!mainMenu.activeSelf)
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIBack, transform.position);
        }
        
        // If video or audio panel is active, return to options default.
        if (videoMenu != null && videoMenu.activeSelf)
        {
            videoMenu.SetActive(false);
            if (settingsMenu != null) settingsMenu.SetActive(true);
            return;
        }
        if (audioMenu != null && audioMenu.activeSelf)
        {
            audioMenu.SetActive(false);
            if (settingsMenu != null) settingsMenu.SetActive(true);
            return;
        }

        // If options container (default settings) is active, go back to main menu.
        if (optionsContainer != null && optionsContainer.activeSelf)
        {
            optionsContainer.SetActive(false);
            if (mainMenu != null) mainMenu.SetActive(true);
            return;
        }

        // If host or join menu is active, go back to main menu.
        if ((hostMenu != null && hostMenu.activeSelf) || (joinMenu != null && joinMenu.activeSelf))
        {
            if (hostMenu != null) hostMenu.SetActive(false);
            if (joinMenu != null) joinMenu.SetActive(false);
            if (mainMenu != null) mainMenu.SetActive(true);
        }
    }

    public void OnHostButtonClicked()
    {
        mainMenu.SetActive(false);
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
        hostMenu.SetActive(true);
    }

    public void OnJoinButtonClicked()
    {
        mainMenu.SetActive(false);
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
        joinMenu.SetActive(true);
    }

    public void OnOptionsButtonClicked()
    {
        mainMenu.SetActive(false);
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
        optionsContainer.SetActive(true);
        // Default to settingsMenu active; hide video and audio panels.
        if (settingsMenu != null) settingsMenu.SetActive(true);
        if (videoMenu != null) videoMenu.SetActive(false);
        if (audioMenu != null) audioMenu.SetActive(false);
    }

    public void OnVideoButtonClicked()
    {
        if (settingsMenu != null) settingsMenu.SetActive(false);
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
        if (videoMenu != null) videoMenu.SetActive(true);
    }

    public void OnAudioButtonClicked()
    {
        if (settingsMenu != null) settingsMenu.SetActive(false);
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
        if (audioMenu != null) audioMenu.SetActive(true);
    }

    public void OnExitButtonClicked()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
        Application.Quit();
    }

    private void OnCreditsButtonClicked()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
        if (CreditsController.Instance != null)
            CreditsController.Instance.ShowCredits();
        else
            Debug.LogError("CreditsController instance not found");
    }
}