using System;
using UnityEngine;
using UnityEngine.UI;

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
    
    private Slider _volumeSlider;

    private void Awake()
    {
        _volumeSlider = GetComponentInChildren<Slider>();
    }

    private void Update()
    {
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
                Debug.LogError("Invalid volume type" + volumeType);
                break;
        }
    }
    
    public void OnSliderValueChanged()
    {
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
                Debug.LogError("Invalid volume type" + volumeType);
                break;
        }
    }
}
