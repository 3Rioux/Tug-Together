using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VolumeSettings : MonoBehaviour
{
    private enum VolumeType
    {
        MASTER,
        MUSIC,
        SFX,
        AMBIENCE,
        UI
    }
    
    [Header("Volume Type")]
    [SerializeField] private VolumeType volumeType;
    
    [Header("UI References")]
    [SerializeField] private Slider _volumeSlider;
    [SerializeField] private TMP_Text volumeValueText; // displays the volume value
    
    private int _lastDisplayedValue = -1; // Initialize to invalid value to ensure first sound plays


    private void Awake()
    {
        if (_volumeSlider == null)
            _volumeSlider = GetComponentInChildren<Slider>();
        
        UpdateVolumeText(_volumeSlider.value);
    }

    private void Update()
    {
        // Update slider value from AudioManager and update text display
        switch (volumeType)
        {
            case VolumeType.MASTER:
                _volumeSlider.value = AudioManager.Instance.masterVolume;
                break;
            case VolumeType.MUSIC:
                _volumeSlider.value = AudioManager.Instance.musicVolume;
                break;
            case VolumeType.SFX:
                _volumeSlider.value = AudioManager.Instance.sfxVolume;
                break;
            case VolumeType.AMBIENCE:
                _volumeSlider.value = AudioManager.Instance.ambienceVolume;
                break;
            case VolumeType.UI:
                _volumeSlider.value = AudioManager.Instance.uiVolume;
                break;
            default:
                Debug.LogError("Invalid volume type: " + volumeType);
                break;
        }
        UpdateVolumeText(_volumeSlider.value);
    }
    
    public void OnSliderValueChanged()
    {
        // Update volume value on AudioManager
        switch (volumeType)
        {
            case VolumeType.MASTER:
                AudioManager.Instance.masterVolume = _volumeSlider.value;
                break;
            case VolumeType.MUSIC:
                AudioManager.Instance.musicVolume = _volumeSlider.value;
                break;
            case VolumeType.SFX:
                AudioManager.Instance.sfxVolume = _volumeSlider.value;
                break;
            case VolumeType.AMBIENCE:
                AudioManager.Instance.ambienceVolume = _volumeSlider.value;
                break;
            case VolumeType.UI:
                AudioManager.Instance.uiVolume = _volumeSlider.value;
                break;
            default:
                Debug.LogError("Invalid volume type: " + volumeType);
                break;
        }
    
        // Calculate the integer display value (0-100)
        int currentDisplayValue = Mathf.RoundToInt(_volumeSlider.value * 100);
    
        // Update the displayed text
        UpdateVolumeText(_volumeSlider.value);
    
        // If the displayed value has changed, play sound
        if (currentDisplayValue != _lastDisplayedValue)
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.UIScroll, transform.position);
            _lastDisplayedValue = currentDisplayValue;
        }
    }

    private void UpdateVolumeText(float value)
    {
        if (volumeValueText != null)
            volumeValueText.text = (value * 100).ToString("0");
    }
}