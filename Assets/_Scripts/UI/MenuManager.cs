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

    [Header("Options Submenus")]
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject videoMenu;
    [SerializeField] private GameObject audioMenu;

    [Header("Buttons")]
    [SerializeField] private Button creditsButton;

    private enum MenuState
    {
        Main,
        Host,
        Join,
        Options,
        VideoSettings,
        AudioSettings,
        Tutorial,
        Create
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

    private void OnEnable()
    {
        SwitchToState(MenuState.Main);
    }

    // Keep your existing FMOD sound methods
    private void ClickSound()
    {
        // FMOD sound trigger (do not modify)
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
    }

    private void BackSound()
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
        optionsContainer.SetActive(false);
        if (tutorialMenu != null) tutorialMenu.SetActive(false);

        // For options submenus, ensure they're all hidden initially
        if (settingsMenu != null) settingsMenu.SetActive(false);
        if (videoMenu != null) videoMenu.SetActive(false);
        if (audioMenu != null) audioMenu.SetActive(false);

        // Activate panels based on new state
        switch (newState)
        {
            case MenuState.Main:
                mainMenu.SetActive(true);
                break;

            case MenuState.Host:
                hostMenu.SetActive(true);
                if (createMenu != null) createMenu.SetActive(true);
                if (tutorialMenu != null) tutorialMenu.SetActive(false);
                break;

            case MenuState.Join:
                joinMenu.SetActive(true);
                break;

            case MenuState.Options:
                optionsContainer.SetActive(true);
                if (settingsMenu != null) settingsMenu.SetActive(true);
                break;

            case MenuState.VideoSettings:
                optionsContainer.SetActive(true);
                if (videoMenu != null) videoMenu.SetActive(true);
                break;

            case MenuState.AudioSettings:
                optionsContainer.SetActive(true);
                if (audioMenu != null) audioMenu.SetActive(true);
                break;

            case MenuState.Tutorial:
                hostMenu.SetActive(true);
                if (tutorialMenu != null) tutorialMenu.SetActive(true);
                if (createMenu != null) createMenu.SetActive(false);
                break;
            
            case MenuState.Create:
                hostMenu.SetActive(true);
                if (createMenu != null) createMenu.SetActive(true);
                if (tutorialMenu != null) tutorialMenu.SetActive(false);
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
        SwitchToState(MenuState.Tutorial);
    }
    
    public void OnTutorialYesButtonClick()
    {
        ClickSound();
        DOTween.KillAll();
        NetworkManager.Singleton.SceneManager.LoadScene("_Scenes/Tutorial", LoadSceneMode.Single);
    }

    public void OnTutorialNoButtonClick()
    {
        ClickSound();
        DOTween.KillAll();
        NetworkManager.Singleton.SceneManager.LoadScene("_Scenes/Level1", LoadSceneMode.Single);
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
        SwitchToState(MenuState.Host);
    }

    public void OnJoinButtonClicked()
    {
        ClickSound();
        SwitchToState(MenuState.Join);
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
}