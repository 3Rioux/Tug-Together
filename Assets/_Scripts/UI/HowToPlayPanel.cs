using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections.Generic;

// Static manager to handle multiple video panels
public static class VideoPanelManager
{
    public static int MaxConcurrentVideos = 2; // Limit to 2 active videos
    private static List<HowToPlayPanel> activePanels = new List<HowToPlayPanel>();

    public static bool RegisterPlayingPanel(HowToPlayPanel panel)
    {
        // If already registered, just keep it at the top of priority
        if (activePanels.Contains(panel))
        {
            activePanels.Remove(panel);
            activePanels.Add(panel); // Move to end (highest priority)
            return true;
        }

        // If we're at max capacity, pause the oldest one
        if (activePanels.Count >= MaxConcurrentVideos && activePanels.Count > 0)
        {
            HowToPlayPanel oldestPanel = activePanels[0];
            oldestPanel.ForcePauseVideo();
            activePanels.RemoveAt(0);
        }

        activePanels.Add(panel);
        return true;
    }

    public static void UnregisterPanel(HowToPlayPanel panel)
    {
        activePanels.Remove(panel);
    }
}

public class HowToPlayPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RawImage videoImage;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Image panelImage;

    [Header("Thumbnail")]
    [SerializeField] private Texture fallbackThumbnail;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private Ease fadeInEasing = Ease.OutQuad;
    [SerializeField] private Ease fadeOutEasing = Ease.InQuad;

    [Header("Scale Settings")]
    [SerializeField] private float hoverScaleFactor = 1.05f;
    [SerializeField] private float scaleInDuration = 0.3f;
    [SerializeField] private float scaleOutDuration = 0.3f;
    [SerializeField] private Ease scaleInEasing = Ease.OutBack;
    [SerializeField] private Ease scaleOutEasing = Ease.InBack;

    [Header("Brightness Settings")]
    [SerializeField] private float normalBrightness = 0.5f;
    [SerializeField] private float hoverBrightness = 1.0f;
    [SerializeField] private float brightnessDuration = 0.3f;
    [SerializeField] private Ease brightnessEasing = Ease.OutQuad;

    private Tween imageFadeTween;
    private Tween audioFadeTween;
    private Tween scaleTween;
    private Tween brightnessTween;
    private Vector3 originalScale;
    private bool videoReady = false;
    private bool isHovered = false;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (videoImage == null) videoImage = GetComponentInChildren<RawImage>();
        if (videoPlayer == null) videoPlayer = GetComponentInChildren<VideoPlayer>();
        if (panelImage == null) panelImage = GetComponent<Image>();

        if (panelImage != null)
        {
            Color color = panelImage.color;
            color.a = .3f;
            panelImage.color = color;
        }

        PrepareVideoWithThumbnail();
    }

    private void PrepareVideoWithThumbnail()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
            videoPlayer.errorReceived += OnVideoError;

            if (videoImage != null)
            {
                if (fallbackThumbnail != null)
                {
                    videoImage.texture = fallbackThumbnail;
                }
                else if (videoPlayer.targetTexture != null)
                {
                    videoImage.texture = videoPlayer.targetTexture;
                }
            }

            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.Prepare();
        }
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning($"Video error on {gameObject.name}: {message}");
        
        // Try to recover by preparing again
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.Prepare();
        }
    }

    private void Start()
    {
        if (videoImage != null)
        {
            Color color = videoImage.color;
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            videoImage.color = Color.HSVToRGB(h, s, normalBrightness);
        }
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        videoReady = true;
        source.frame = 0;
        source.Pause();

        if (videoImage != null && videoPlayer.targetTexture != null)
        {
            videoImage.texture = videoPlayer.targetTexture;
        }
        
        // If user is already hovering, start playback immediately
        if (isHovered && videoReady)
        {
            PlayVideo();
        }
    }

    private void OnDestroy()
    {
        ClearTweens();
        VideoPanelManager.UnregisterPanel(this);

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.Stop();
        }
    }

    private void ClearTweens()
    {
        imageFadeTween?.Kill();
        audioFadeTween?.Kill();
        scaleTween?.Kill();
        brightnessTween?.Kill();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        ClearTweens();

        if (panelImage != null)
        {
            imageFadeTween = panelImage.DOFade(1f, fadeInDuration)
                .SetEase(fadeInEasing);
        }

        scaleTween = transform.DOScale(originalScale * hoverScaleFactor, scaleInDuration)
            .SetEase(scaleInEasing);

        if (videoImage != null)
        {
            float h, s, v;
            Color.RGBToHSV(videoImage.color, out h, out s, out v);
            brightnessTween = DOTween.To(
                () => v,
                value => videoImage.color = Color.HSVToRGB(h, s, value),
                hoverBrightness,
                brightnessDuration
            ).SetEase(brightnessEasing);
        }

        // Only attempt to play if the video is ready
        if (videoPlayer != null && videoReady)
        {
            PlayVideo();
        }
    }

    private void PlayVideo()
    {
        // Register with manager first - this will pause other videos if needed
        if (VideoPanelManager.RegisterPlayingPanel(this))
        {
            videoPlayer.Play();

            if (videoPlayer.audioOutputMode != VideoAudioOutputMode.None)
            {
                audioFadeTween = DOTween.To(() => videoPlayer.GetDirectAudioVolume(0),
                    volume => videoPlayer.SetDirectAudioVolume(0, volume),
                    1f, fadeInDuration)
                    .SetEase(fadeInEasing);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        ClearTweens();

        if (panelImage != null)
        {
            imageFadeTween = panelImage.DOFade(.3f, fadeOutDuration)
                .SetEase(fadeOutEasing);
        }

        scaleTween = transform.DOScale(originalScale, scaleOutDuration)
            .SetEase(scaleOutEasing);

        if (videoImage != null)
        {
            float h, s, v;
            Color.RGBToHSV(videoImage.color, out h, out s, out v);
            brightnessTween = DOTween.To(
                () => v,
                value => videoImage.color = Color.HSVToRGB(h, s, value),
                normalBrightness,
                brightnessDuration
            ).SetEase(brightnessEasing);
        }

        PauseVideo();
        VideoPanelManager.UnregisterPanel(this);
    }

    private void PauseVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();

            if (videoPlayer.audioOutputMode != VideoAudioOutputMode.None)
            {
                audioFadeTween = DOTween.To(() => videoPlayer.GetDirectAudioVolume(0),
                    volume => videoPlayer.SetDirectAudioVolume(0, volume),
                    0f, fadeOutDuration)
                    .SetEase(fadeOutEasing);
            }
        }
    }

    // Called by the manager when this panel needs to stop
    public void ForcePauseVideo()
    {
        PauseVideo();
    }
}