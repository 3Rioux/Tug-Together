using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionCodeConnector : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text sessionCodeText;
    [SerializeField] private Button copyCodeButton;
    [SerializeField] private GameObject codeContainer;

    private Component showJoinCodeComponent;
    private bool isInitialized = false;

    private void OnEnable()
    {
        StartCoroutine(InitializeWhenReady());
    }

    private IEnumerator InitializeWhenReady()
    {
        // Wait a frame to ensure all components are initialized
        yield return null;

        // If already initialized, skip
        if (isInitialized)
            yield break;

        // Find UI components if not assigned
        if (sessionCodeText == null)
            sessionCodeText = GetComponentInChildren<TMP_Text>();

        if (copyCodeButton == null)
            copyCodeButton = GetComponentInChildren<Button>();

        // Add listener to copy button
        if (copyCodeButton != null)
        {
            copyCodeButton.onClick.RemoveAllListeners();
            copyCodeButton.onClick.AddListener(CopySessionCode);
        }

        // Find the ShowJoinCode component in the hierarchy (including inactive objects)
        showJoinCodeComponent = FindShowJoinCodeComponent();
        if (showJoinCodeComponent == null)
        {
            Debug.LogWarning("Could not find ShowJoinCode component, will retry later", this);
            sessionCodeText.text = "Waiting for session...";
            if (codeContainer != null)
                codeContainer.SetActive(true);
                
            // Try again in a second
            yield return new WaitForSeconds(1f);
            StartCoroutine(InitializeWhenReady());
            yield break;
        }

        // Setup completed
        isInitialized = true;

        // Update the UI with current session code if available
        UpdateSessionCodeDisplay();
    }

    private Component FindShowJoinCodeComponent()
    {
        // Search in all GameObjects including inactive ones
        foreach (var rootObj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!rootObj.scene.IsValid())
                continue; // Skip prefabs
                
            foreach (var component in rootObj.GetComponents<Component>())
            {
                if (component != null && component.GetType().FullName == "Unity.Multiplayer.Widgets.ShowJoinCode")
                {
                    return component;
                }
            }
        }
        return null;
    }

    public void UpdateSessionCodeDisplay()
    {
        if (!isInitialized || showJoinCodeComponent == null || sessionCodeText == null)
            return;

        // Try to get the session code from the ShowJoinCode component
        object sessionObj = GetSessionFromComponent();
        if (sessionObj == null)
        {
            sessionCodeText.text = "Waiting for code...";
            if (codeContainer != null)
                codeContainer.SetActive(true);
            return;
        }

        // Try to get the code from the session
        string code = GetCodeFromSession(sessionObj);
        if (string.IsNullOrEmpty(code))
        {
            sessionCodeText.text = "No code available";
            if (codeContainer != null)
                codeContainer.SetActive(true);
        }
        else
        {
            sessionCodeText.text = code;
            if (codeContainer != null)
                codeContainer.SetActive(true);
        }
    }

    private object GetSessionFromComponent()
    {
        if (showJoinCodeComponent == null)
            return null;

        try
        {
            var sessionProperty = showJoinCodeComponent.GetType().GetProperty("Session");
            return sessionProperty?.GetValue(showJoinCodeComponent);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error getting session: {ex.Message}", this);
            return null;
        }
    }

    private string GetCodeFromSession(object sessionObj)
    {
        if (sessionObj == null)
            return null;

        try
        {
            var codeProperty = sessionObj.GetType().GetProperty("Code");
            return codeProperty?.GetValue(sessionObj) as string;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error getting code: {ex.Message}", this);
            return null;
        }
    }

    public void CopySessionCode()
    {
        if (!isInitialized || sessionCodeText == null)
            return;

        string text = sessionCodeText.text;
        if (string.IsNullOrEmpty(text) || text == "Waiting for code..." || text == "No code available")
            return;

        // Remove any rich text tags before copying
        string plainText = Regex.Replace(text, "<.*?>", string.Empty);
        GUIUtility.systemCopyBuffer = plainText;

        // Visual feedback - could add animation or text change here
        Debug.Log($"Session code copied to clipboard: {plainText}");
    }

    public void RefreshConnection()
    {
        isInitialized = false;
        StartCoroutine(InitializeWhenReady());
    }
}