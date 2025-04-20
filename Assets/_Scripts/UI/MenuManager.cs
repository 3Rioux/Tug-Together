// C#
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

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

    private void Awake()
    {
        // Set up credits button listener
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
        mainMenu.SetActive(true);
        hostMenu.SetActive(false);
        joinMenu.SetActive(false);
        optionsMenu.SetActive(false);
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
        hostMenu.SetActive(false);
        joinMenu.SetActive(false);
        optionsMenu.SetActive(false);
        mainMenu.SetActive(true);
    }
    
    public void OnExitButtonClicked()
    {
        Application.Quit();
    }

    // New method to handle credits button click
    private void OnCreditsButtonClicked()
    {
        if (CreditsController.Instance != null)
            CreditsController.Instance.ShowCredits();
        else
            Debug.LogError("CreditsController instance not found");
    }
}