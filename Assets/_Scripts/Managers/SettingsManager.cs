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

    private Resolution[] availableResolutions;

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

        // initialize the controls without firing callbacks
        graphicsPresetOptionList.SetOption(savedPreset);
        resolutionDropdown.value    = savedRes;
        windowModeOptionList.SetOption(savedMode);

        // now hook up their change-listeners
        graphicsPresetOptionList.onChangeOption.AddListener(OnGraphicsPresetChanged);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        windowModeOptionList.onChangeOption.AddListener(OnWindowModeChanged);
    }

    private IEnumerator Start()
    {
        // wait one frame so Unity's UI / InputSystems are fully ready
        yield return null;

        // apply whatever the player had last time
        OnGraphicsPresetChanged(graphicsPresetOptionList.optionIndex);
        OnResolutionChanged(resolutionDropdown.value);
        OnWindowModeChanged(windowModeOptionList.optionIndex);
        
        // set the initial state to avoid sound at the game start
        settingsInitialized = true;
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
        int maxQ  = QualitySettings.names.Length - 1;
        int count = graphicsPresetOptionList.options.Count;
        // Map index from 0 (lowest) to maxQ (highest)
        int level = count > 1 
            ? Mathf.RoundToInt(Mathf.Lerp(0, maxQ, (float)idx / (count - 1)))
            : maxQ;

        QualitySettings.SetQualityLevel(level, true);
        PlayerPrefs.SetInt(GraphicsPresetKey, idx);
        PlayerPrefs.Save();

        if (settingsInitialized)
            ClickSound();
    }

    public void OnResolutionChanged(int idx)
    {
        if (idx < 0 || idx >= availableResolutions.Length)
            return;

        var r = availableResolutions[idx];
        // preserve the current fullscreen mode
        Screen.SetResolution(r.width, r.height,
                             Screen.fullScreenMode,
                             r.refreshRate);

        PlayerPrefs.SetInt(ResolutionKey, idx);
        PlayerPrefs.Save();
        
        if (settingsInitialized)
            ClickSound();
    }

    public void OnWindowModeChanged(int idx)
    {
        var mode = (WindowMode)idx;
        var fs   = mode switch
        {
            WindowMode.Fullscreen => FullScreenMode.ExclusiveFullScreen,
            WindowMode.Borderless => FullScreenMode.FullScreenWindow,
            WindowMode.Windowed   => FullScreenMode.Windowed,
            _                     => FullScreenMode.Windowed
        };

        // reapply the chosen resolution under the new mode
        int resIdx = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionKey, 0),
                                 0, availableResolutions.Length - 1);
        var r = availableResolutions[resIdx];
        Screen.SetResolution(r.width, r.height, fs, r.refreshRate);

        PlayerPrefs.SetInt(WindowModeKey, idx);
        PlayerPrefs.Save();
        
        if (settingsInitialized)
            ClickSound();
    }
}
