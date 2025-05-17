using UnityEngine;
using TMPro;

public class InteractionPromptManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject promptCanvas;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Camera playerCamera;
    
    [Header("Settings")]
    [SerializeField] private float detectRadius = 15f; // How close player must be to see prompt
    [SerializeField] private LayerMask towableLayer; // Layer for towable objects
    [SerializeField] private float showChanceReduction = 20f; // Percent reduction each time
    
    private SpringTugSystem tugSystem;
    private GameObject currentTowable;
    private int interactionCount = 0;
    private float currentShowChance = 100f;
    
    private void Start()
    {
        tugSystem = GetComponent<SpringTugSystem>();
        
        if (playerCamera == null)
            playerCamera = Camera.main;
            
        if (promptCanvas != null)
            promptCanvas.SetActive(false);
    }
    
    private void Update()
    {
        // Don't show prompts if already towing something
        if (tugSystem != null && tugSystem.isAttached)
        {
            HidePrompt();
            return;
        }
        
        // Check for nearby towable objects
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectRadius, towableLayer);
        
        if (hitColliders.Length > 0)
        {
            // Found a towable object
            GameObject towable = hitColliders[0].gameObject;
            
            // If this is a new towable object
            if (towable != currentTowable)
            {
                currentTowable = towable;
                
                // Apply chance-based system
                if (Random.Range(0f, 100f) <= currentShowChance)
                {
                    ShowPrompt("Press <b>[R]</b> to Tow");
                    
                    // Reduce chance for next time
                    currentShowChance = Mathf.Max(0, currentShowChance - showChanceReduction);
                    interactionCount++;
                }
            }
        }
        else
        {
            // No towable objects nearby
            HidePrompt();
            currentTowable = null;
        }
        
        // // Make canvas face camera (billboard effect)
        // if (promptCanvas.activeSelf && playerCamera != null)
        // {
        //     promptCanvas.transform.rotation = playerCamera.transform.rotation;
        // }
    }
    
    private void ShowPrompt(string message)
    {
        if (promptCanvas != null)
        {
            promptText.text = message;
            promptCanvas.SetActive(true);
        }
    }
    
    private void HidePrompt()
    {
        if (promptCanvas != null && promptCanvas.activeSelf)
        {
            promptCanvas.SetActive(false);
        }
    }
    
    // Reset the chance when player starts a new game or level
    public void ResetPromptChance()
    {
        currentShowChance = 100f;
        interactionCount = 0;
    }
}