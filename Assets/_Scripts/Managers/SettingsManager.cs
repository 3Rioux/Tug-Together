// Language: csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.HighDefinition;
using TMPro;
using QuantumTek.QuantumUI;

public enum DLSSQualityMode { Quality, Balanced, Performance, UltraPerformance }
public enum Fsr2QualityMode { Quality, Balanced, Performance, UltraPerformance }

/// <summary>
/// Manages HDRP display settings including overall graphics presets (via QUI_OptionList)
/// and runtime upscaler selection (DLSS/FSR2). Stabilized to avoid crashes when switching scalers.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public enum UpscaleOption { DLSS, FSR }

    [Header("Camera & HDRP Data")]
    [SerializeField] private Camera mainCamera;
    private HDAdditionalCameraData hdCamData;

    [Header("UI References")]
    [SerializeField] private QUI_OptionList graphicsPresetOptionList;
    [SerializeField] private Toggle upscalerToggle;
    [SerializeField] private GameObject upscalerPanel;
    [SerializeField] private TMP_Dropdown upscalerDropdown;
    [SerializeField] private TMP_Dropdown upscalerQualityDropdown;
    [SerializeField] private Slider upscalerSharpnessSlider;
    [SerializeField] private TMP_Text sharpnessPercentageText;

    private UpscaleOption currentUpscaler;
    private Coroutine switchCoroutine;

    private DLSSQualityMode[] dlssModes;
    private Fsr2QualityMode[] fsr2Modes;

    private const string GraphicsPresetKey = "GraphicsPreset";
    private const string UpscaleEnabledKey = "UpscaleEnabled";
    private const string UpscalerIndexKey = "UpscalerIndex";
    private const string UpscalerQualityKey = "UpscalerQuality";
    private const string UpscalerSharpnessKey = "UpscalerSharpness";

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (mainCamera != null)
            hdCamData = mainCamera.GetComponent<HDAdditionalCameraData>();

        dlssModes = (DLSSQualityMode[])System.Enum.GetValues(typeof(DLSSQualityMode));
        fsr2Modes = (Fsr2QualityMode[])System.Enum.GetValues(typeof(Fsr2QualityMode));

        // Use custom quality names instead of QualitySettings.names.
        // Mapping: Ultra -> highest, Med -> middle, Low -> lowest.
        if (graphicsPresetOptionList != null)
        {
            var customQualityNames = new List<string> {"Low", "Med", "Ultra" };
            graphicsPresetOptionList.options = customQualityNames;
            int savedPreset = PlayerPrefs.GetInt(GraphicsPresetKey, 0);
            savedPreset = Mathf.Clamp(savedPreset, 0, customQualityNames.Count - 1);
            graphicsPresetOptionList.SetOption(savedPreset);
            graphicsPresetOptionList.onChangeOption.AddListener(OnGraphicsPresetChanged);
        }

        // Upscaler UI callbacks.
        upscalerToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(UpscaleEnabledKey, 0) == 1);
        upscalerToggle.onValueChanged.AddListener(OnToggleUpscaler);
        upscalerDropdown.onValueChanged.AddListener(OnUpscalerChanged);
        upscalerQualityDropdown.onValueChanged.AddListener(OnUpscalerQualityChanged);
        float savedSharp = PlayerPrefs.GetFloat(UpscalerSharpnessKey, upscalerSharpnessSlider.value);
        upscalerSharpnessSlider.SetValueWithoutNotify(savedSharp);
        upscalerSharpnessSlider.onValueChanged.AddListener(OnUpscalerSharpnessChanged);
        UpdateSharpnessText(savedSharp);

        upscalerPanel.SetActive(upscalerToggle.isOn);
        if (upscalerToggle.isOn)
            PopulateUpscalerDropdown();
    }

    private void OnGraphicsPresetChanged(int idx)
    {
        // Map custom preset to a quality level
        // Ultra: highest quality, Med: middle, Low: lowest.
        int qualityLevel = 0;
        int maxQuality = QualitySettings.names.Length - 1;
        if (idx == 0) // Ultra
            qualityLevel = maxQuality;
        else if (idx == 1) // Med
            qualityLevel = maxQuality / 2;
        else if (idx == 2) // Low
            qualityLevel = 0;
        
        QualitySettings.SetQualityLevel(qualityLevel, true);
        PlayerPrefs.SetInt(GraphicsPresetKey, idx);
        PlayerPrefs.Save();
    }

    private void OnToggleUpscaler(bool isOn)
    {
        PlayerPrefs.SetInt(UpscaleEnabledKey, isOn ? 1 : 0);
        PlayerPrefs.Save();
        upscalerPanel.SetActive(isOn);
        if (isOn)
            PopulateUpscalerDropdown();
        else if (hdCamData != null)
            DisableAllUpscalers();
    }

    private void PopulateUpscalerDropdown()
    {
        var opts = new List<string>();
        if (SystemInfo.graphicsDeviceName.ToLower().Contains("rtx"))
            opts.Add("DLSS");
        opts.Add("FSR");

        upscalerDropdown.ClearOptions();
        upscalerDropdown.AddOptions(opts);

        int saved = PlayerPrefs.GetInt(UpscalerIndexKey, 0);
        saved = Mathf.Clamp(saved, 0, opts.Count - 1);
        upscalerDropdown.SetValueWithoutNotify(saved);
        upscalerDropdown.RefreshShownValue();

        if (switchCoroutine != null) StopCoroutine(switchCoroutine);
        switchCoroutine = StartCoroutine(SwitchUpscalerRoutine(saved));
    }

    public void OnUpscalerChanged(int idx)
    {
        if (switchCoroutine != null) StopCoroutine(switchCoroutine);
        switchCoroutine = StartCoroutine(SwitchUpscalerRoutine(idx));
    }

    private IEnumerator SwitchUpscalerRoutine(int idx)
    {
        if (hdCamData == null || idx < 0 || idx >= upscalerDropdown.options.Count)
            yield break;

        DisableAllUpscalers();
        hdCamData.allowDynamicResolution = false;
        yield return null;

        string sel = upscalerDropdown.options[idx].text;
        currentUpscaler = sel == "DLSS" ? UpscaleOption.DLSS : UpscaleOption.FSR;
        PlayerPrefs.SetInt(UpscalerIndexKey, idx);
        PlayerPrefs.Save();

        hdCamData.allowDynamicResolution = true;
        if (currentUpscaler == UpscaleOption.DLSS)
            hdCamData.allowDeepLearningSuperSampling = true;
        else
            hdCamData.allowFidelityFX2SuperResolution = true;

        PopulateQualityDropdown(currentUpscaler);
        OnUpscalerQualityChanged(upscalerQualityDropdown.value);
        OnUpscalerSharpnessChanged(upscalerSharpnessSlider.value);

        switchCoroutine = null;
    }

    private void PopulateQualityDropdown(UpscaleOption opt)
    {
        upscalerQualityDropdown.ClearOptions();
        if (opt == UpscaleOption.DLSS)
            upscalerQualityDropdown.AddOptions(new List<string>(System.Enum.GetNames(typeof(DLSSQualityMode))));
        else
            upscalerQualityDropdown.AddOptions(new List<string>(System.Enum.GetNames(typeof(Fsr2QualityMode))));

        int saved = PlayerPrefs.GetInt(UpscalerQualityKey, 0);
        saved = Mathf.Clamp(saved, 0, upscalerQualityDropdown.options.Count - 1);
        upscalerQualityDropdown.SetValueWithoutNotify(saved);
        upscalerQualityDropdown.RefreshShownValue();
    }

    private void OnUpscalerQualityChanged(int idx)
    {
        if (hdCamData == null) return;
        idx = Mathf.Clamp(idx, 0, upscalerQualityDropdown.options.Count - 1);
        PlayerPrefs.SetInt(UpscalerQualityKey, idx);
        PlayerPrefs.Save();

        if (currentUpscaler == UpscaleOption.DLSS)
        {
            hdCamData.deepLearningSuperSamplingUseCustomQualitySettings = true;
            hdCamData.deepLearningSuperSamplingQuality = (uint)dlssModes[idx];
        }
        else
        {
            hdCamData.fidelityFX2SuperResolutionUseCustomQualitySettings = true;
            hdCamData.fidelityFX2SuperResolutionQuality = (uint)fsr2Modes[idx];
        }
    }

    private void OnUpscalerSharpnessChanged(float value)
    {
        if (hdCamData == null) return;
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(UpscalerSharpnessKey, value);
        PlayerPrefs.Save();
        UpdateSharpnessText(value);

        if (currentUpscaler == UpscaleOption.DLSS)
        {
            hdCamData.deepLearningSuperSamplingUseCustomAttributes = true;
            hdCamData.deepLearningSuperSamplingSharpening = value;
        }
        else
        {
            hdCamData.fsrOverrideSharpness = true;
            hdCamData.fsrSharpness = value;
        }
    }

    private void UpdateSharpnessText(float v)
    {
        if (sharpnessPercentageText != null)
            sharpnessPercentageText.text = Mathf.RoundToInt(v * 100) + "%";
    }

    private void DisableAllUpscalers()
    {
        if (hdCamData == null) return;
        hdCamData.allowDeepLearningSuperSampling = false;
        hdCamData.deepLearningSuperSamplingUseCustomQualitySettings = false;
        hdCamData.deepLearningSuperSamplingUseCustomAttributes = false;

        hdCamData.allowFidelityFX2SuperResolution = false;
        hdCamData.fidelityFX2SuperResolutionUseCustomQualitySettings = false;
        hdCamData.fsrOverrideSharpness = false;

        hdCamData.allowDynamicResolution = false;
    }
}