// Language: C#
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject hostMenu;
    [SerializeField] private GameObject joinMenu;
    [SerializeField] private GameObject optionsMenu;

    [Header("Scene Name")]
    [SerializeField] private string gameSceneName = "Gameplay";

    [Header("Credits Button")]
    [SerializeField] private Button creditsButton;

    [Header("Input")]
    [SerializeField] private InputActionReference backActionReference;

    private void Awake()
    {
        if (backActionReference != null)
        {
            backActionReference.action.performed += context => OnBackButtonClicked();
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

        mainMenu.SetActive(true);
        hostMenu.SetActive(false);
        joinMenu.SetActive(false);
        optionsMenu.SetActive(false);
    }

    private void OnDisable()
    {
        backActionReference?.action.Disable();
    }

    public void OnHostButtonClicked()
    {
        mainMenu.SetActive(false);
        hostMenu.SetActive(true);
    }

    public void OnJoinButtonClicked()
    {
        mainMenu.SetActive(false);
        joinMenu.SetActive(true);
    }

    public void OnOptionsButtonClicked()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }
    
    public void OnBackButtonClicked()
    {
        if (hostMenu != null) hostMenu.SetActive(false);
        if (joinMenu != null) joinMenu.SetActive(false);
        if (optionsMenu != null) optionsMenu.SetActive(false);
        if (mainMenu != null) mainMenu.SetActive(true);
    }

    public void OnExitButtonClicked()
    {
        Application.Quit();
    }

    private void OnCreditsButtonClicked()
    {
        if (CreditsController.Instance != null)
            CreditsController.Instance.ShowCredits();
        else
            Debug.LogError("CreditsController instance not found");
    }
}