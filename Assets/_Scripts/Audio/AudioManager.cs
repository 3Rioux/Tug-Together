using System.Collections;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume   = 1f;
    [Range(0f, 1f)] public float musicVolume    = 1f;
    [Range(0f, 1f)] public float sfxVolume      = 1f;
    [Range(0f, 1f)] public float ambienceVolume = 1f;
    [Range(0f, 1f)] public float uiVolume       = 1f;

    private EventInstance _ambienceInstance;
    private EventInstance _loopInstance;

    private Bus _masterBus;
    private Bus _musicBus;
    private Bus _sfxBus;
    private Bus _ambienceBus;
    private Bus _uiBus;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _masterBus   = RuntimeManager.GetBus("bus:/");
        _musicBus    = RuntimeManager.GetBus("bus:/Music");
        _sfxBus      = RuntimeManager.GetBus("bus:/SFX");
        _ambienceBus = RuntimeManager.GetBus("bus:/Ambience");
        _uiBus       = RuntimeManager.GetBus("bus:/UI");
    }

    private void Update()
    {
        _masterBus.setVolume(masterVolume);
        _musicBus.setVolume(musicVolume);
        _sfxBus.setVolume(sfxVolume);
        _ambienceBus.setVolume(ambienceVolume);
        _uiBus.setVolume(uiVolume);
    }

    // play or swap ambience immediately
    public void PlayAmbience(EventReference ambience)
    {
        StopAmbience();

        if (ambience.IsNull)
        {
            Debug.LogWarning("Ambience reference is null");
            return;
        }

        _ambienceInstance = RuntimeManager.CreateInstance(ambience);
        _ambienceInstance.start();
    }

    // stop current ambience immediately
    public void StopAmbience()
    {
        if (_ambienceInstance.isValid())
        {
            _ambienceInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _ambienceInstance.release();
        }
    }

    // fade out ambience over duration then stop
    public void FadeOutAmbience(float duration)
    {
        StartCoroutine(FadeAmbienceCoroutine(duration));
    }

    private IEnumerator FadeAmbienceCoroutine(float duration)
    {
        float elapsed = 0f;
        _ambienceBus.getVolume(out float startVol);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _ambienceBus.setVolume(Mathf.Lerp(startVol, 0f, t));
            yield return null;
        }

        StopAmbience();
        _ambienceBus.setVolume(ambienceVolume);
    }

    // play sfx at world position
    public void PlayOneShot(EventReference sfx, Vector3 pos)
    {
        if (sfx.IsNull)
        {
            Debug.LogWarning("SFX reference is null");
            return;
        }
        RuntimeManager.PlayOneShot(sfx, pos);
    }

    // start a looping sound attached to an emitter
    public void StartLoop(EventReference loopRef, GameObject emitter)
    {
        StopLoop();

        if (loopRef.IsNull)
        {
            Debug.LogWarning("Loop reference is null");
            return;
        }

        _loopInstance = RuntimeManager.CreateInstance(loopRef);
        RuntimeManager.AttachInstanceToGameObject(_loopInstance, emitter);
        _loopInstance.start();
    }

    // stop looping sound immediately
    public void StopLoop()
    {
        if (_loopInstance.isValid())
        {
            _loopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _loopInstance.release();
        }
    }

    // fade out looping sound then stop
    public void FadeOutLoop(float duration)
    {
        StartCoroutine(fadeLoopCoroutine(duration));
    }

    private IEnumerator fadeLoopCoroutine(float duration)
    {
        float elapsed = 0f;
        _sfxBus.getVolume(out float startVol);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _sfxBus.setVolume(Mathf.Lerp(startVol, 0f, t));
            yield return null;
        }

        StopLoop();
        _sfxBus.setVolume(sfxVolume);
    }
}
