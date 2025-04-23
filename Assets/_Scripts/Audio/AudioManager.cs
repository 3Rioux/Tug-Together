using System;
using FMOD.Studio;
using UnityEngine;
using FMODUnity;

public class AudioManager : MonoBehaviour
{
    private EventInstance ambienceEventInstance;
    
    public static AudioManager Instance { get; private set; }
    
    [Header("Audio Settings")]
    [Range(0,1)] public float masterVolume = 1.0f;
    [Range(0,1)] public float musicVolume = 1.0f;
    [Range(0,1)] public float sfxVolume = 1.0f;
    [Range(0,1)] public float ambienceVolume = 1.0f;
    [Range(0,1)] public float uiVolume = 1.0f;

    private Bus masterBus;
    private Bus musicBus;
    private Bus sfxBus;
    private Bus ambienceBus;
    private Bus uiBus;
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        
        masterBus = RuntimeManager.GetBus("bus:/");
        musicBus = RuntimeManager.GetBus("bus:/Music");
        sfxBus = RuntimeManager.GetBus("bus:/SFX");
        ambienceBus = RuntimeManager.GetBus("bus:/Ambience");
        uiBus = RuntimeManager.GetBus("bus:/UI");
    }

    private void Start()
    {
        InitializeAmbience(FMODEvents.Instance.MainMenu);
    }

    private void Update()
    {
        masterBus.setVolume(masterVolume);
        musicBus.setVolume(musicVolume);
        sfxBus.setVolume(sfxVolume);
        ambienceBus.setVolume(ambienceVolume);
        uiBus.setVolume(uiVolume);
    }

    private void InitializeAmbience(EventReference ambienceEventReference)
    {
        if (!ambienceEventReference.IsNull)
        {
            ambienceEventInstance = RuntimeManager.CreateInstance(ambienceEventReference);
            ambienceEventInstance.start();
        }
        else
        {
            Debug.LogWarning("Ambience event reference is null or invalid.");
        }
    }


    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }
}