using System;
using FMOD.Studio;
using UnityEngine;
using FMODUnity;

public class AudioManager : MonoBehaviour
{
    private EventInstance ambienceEventInstance;
    
    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeAmbience(FMODEvents.Instance.MainMenu);
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