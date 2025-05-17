using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using TMPro;
using UnityEngine.UI;
using QuantumTek.QuantumUI;

public class SettingsManager : MonoBehaviour
{
    public enum WindowMode { Fullscreen, Borderless, Windowed }

    [Header("UI References (assign in Inspector)")]
    [SerializeField] private QUI_OptionList graphicsPresetOptionList;
    [SerializeField] private TMP_Dropdown       resolutionDropdown;
    [SerializeField] private QUI_OptionList     windowModeOptionList;
    
    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button backButton;  // Add this line


    private Resolution[] availableResolutions;
    
    // Add these fields to store pending changes
    private int pendingGraphicsPreset;
    private int pendingResolutionIndex;
    private int pendingWindowMode;
    private bool hasPendingChanges = false;

    // PlayerPrefs keys
    private const string GraphicsPresetKey = "GraphicsPreset";
    private const string ResolutionKey     = "ResolutionIndex";
    private const string WindowModeKey     = "WindowMode";
    
    private bool settingsInitialized = false;

    private void Awake()
    {
        // build the dropdown list once
        BuildResolutionDropdown();

        // restore saved indices (clamped to valid range)
        int savedPreset = PlayerPrefs.GetInt(GraphicsPresetKey, 0);
        int savedRes    = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionKey, 0), 0, availableResolutions.Length - 1);
        int savedMode   = Mathf.Clamp(PlayerPrefs.GetInt(WindowModeKey, 0), 0, windowModeOptionList.options.Count - 1);

        // initialize pending values
        pendingGraphicsPreset = savedPreset;
        pendingResolutionIndex = savedRes;
        pendingWindowMode = savedMode;

        // initialize the controls without firing callbacks
        graphicsPresetOptionList.SetOption(savedPreset);
        resolutionDropdown.value = savedRes;
        windowModeOptionList.SetOption(savedMode);

        // now hook up their change-listeners
        graphicsPresetOptionList.onChangeOption.AddListener(OnGraphicsPresetChanged);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        windowModeOptionList.onChangeOption.AddListener(OnWindowModeChanged);
    
        // hook up apply button
        if (applyButton != null)
            applyButton.onClick.AddListener(ApplySettings);
        
        // hook up back button
        if (backButton != null)
            backButton.onClick.AddListener(RevertSettings);
    }

    private IEnumerator Start()
    {
        // wait one frame so Unity's UI / InputSystems are fully ready
        yield return null;

        // apply settings on game start
        ApplySettings();

        // set the initial state to avoid sound at the game start
        settingsInitialized = true;
    }
    
    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.F4))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }
    }
    
    private void ClickSound()
    {
        // FMOD sound trigger (do not modify)
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIClick, transform.position);
    }

    private void BuildResolutionDropdown()
    {
        // collect one entry per (width×height) at the highest refresh rate
        var unique = new Dictionary<(int, int), Resolution>();
        foreach (var r in Screen.resolutions)
        {
            var key = (r.width, r.height);
            if (!unique.ContainsKey(key) || r.refreshRate > unique[key].refreshRate)
                unique[key] = r;
        }

        // sort descending by total pixels
        availableResolutions = unique.Values
                                      .OrderByDescending(r => r.width * r.height)
                                      .ToArray();

        // fill the TMP dropdown
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(
            availableResolutions
              .Select(r => new TMP_Dropdown.OptionData($"{r.width} x {r.height}"))
              .ToList()
        );
    }

    public void OnGraphicsPresetChanged(int idx)
    {
        pendingGraphicsPreset = idx;
        hasPendingChanges = true;
    
        if (settingsInitialized)
            ClickSound();
    }

    public void OnResolutionChanged(int idx)
    {
        if (idx < 0 || idx >= availableResolutions.Length)
            return;
        
        pendingResolutionIndex = idx;
        hasPendingChanges = true;
    
        if (settingsInitialized)
            ClickSound();
    }

    public void OnWindowModeChanged(int idx)
    {
        pendingWindowMode = idx;
        hasPendingChanges = true;
    
        if (settingsInitialized)
            ClickSound();
    }
    
    public void ApplySettings()
    {
        // Apply Graphics Preset
        int maxQ = QualitySettings.names.Length - 1;
        int count = graphicsPresetOptionList.options.Count;
        int level = count > 1
            ? Mathf.RoundToInt(Mathf.Lerp(0, maxQ, (float)pendingGraphicsPreset / (count - 1)))
            : maxQ;

        QualitySettings.SetQualityLevel(level, true);
        PlayerPrefs.SetInt(GraphicsPresetKey, pendingGraphicsPreset);

        // Apply Window Mode and Resolution together
        var mode = (WindowMode)pendingWindowMode;
        var fs = mode switch
        {
            WindowMode.Fullscreen => FullScreenMode.ExclusiveFullScreen,
            WindowMode.Borderless => FullScreenMode.FullScreenWindow,
            WindowMode.Windowed => FullScreenMode.Windowed,
            _ => FullScreenMode.Windowed
        };

        // Apply resolution with the selected window mode
        var r = availableResolutions[pendingResolutionIndex];
        Screen.SetResolution(r.width, r.height, fs, r.refreshRate);
    
        PlayerPrefs.SetInt(ResolutionKey, pendingResolutionIndex);
        PlayerPrefs.SetInt(WindowModeKey, pendingWindowMode);
        PlayerPrefs.Save();
    
        hasPendingChanges = false;
    
        if (settingsInitialized)
            ClickSound();
    }
    
    // Add this method to revert UI to the saved settings
    public void RevertSettings()
    {
        // Get the currently saved values
        int savedPreset = PlayerPrefs.GetInt(GraphicsPresetKey, 0);
        int savedRes = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionKey, 0), 0, availableResolutions.Length - 1);
        int savedMode = Mathf.Clamp(PlayerPrefs.GetInt(WindowModeKey, 0), 0, windowModeOptionList.options.Count - 1);

        // Reset pending values to saved values
        pendingGraphicsPreset = savedPreset;
        pendingResolutionIndex = savedRes;
        pendingWindowMode = savedMode;

        // Update UI without triggering callbacks
        graphicsPresetOptionList.SetOption(savedPreset);
        resolutionDropdown.value = savedRes;
        windowModeOptionList.SetOption(savedMode);

        // Reset pending changes flag
        hasPendingChanges = false;
    }
}
