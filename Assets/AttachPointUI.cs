using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class AttachPointUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject uiContainer;
    [SerializeField] private RectTransform crosshairImage;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private Ease fadeInEase = Ease.OutBack;
    [SerializeField] private Ease fadeOutEase = Ease.InQuad;
    
    [Header("Size Settings")]
    [SerializeField] private float normalSize = 1f;
    [SerializeField] private float highlightedSize = 2f;
    
    private Camera mainCamera;
    private Tween currentTween;
    
    void Start()
    {
        // Find main camera
        mainCamera = Camera.main;
        
        // Initialize UI as hidden
        if (uiContainer != null)
        {
            uiContainer.SetActive(false);
        }
        
        if (crosshairImage != null)
        {
            crosshairImage.localScale = Vector3.zero;
        }
    }
    
    void LateUpdate()
    {
        // Billboard effect - make UI always face the camera
        if (mainCamera != null && uiContainer != null && uiContainer.activeInHierarchy)
        {
            uiContainer.transform.LookAt(
                uiContainer.transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up);
        }
    }
    
    public void Show(bool highlighted = false)
    {
        if (uiContainer == null || crosshairImage == null) return;
        
        // Kill any active animation
        if (currentTween != null) currentTween.Kill();
        
        uiContainer.SetActive(true);
        
        // Animate from 0 to target size based on highlight state
        float targetSize = highlighted ? highlightedSize : normalSize;
        currentTween = crosshairImage.DOScale(Vector3.one * targetSize, fadeInDuration)
            .From(Vector3.zero)
            .SetEase(fadeInEase);
    }
    
    public void SetHighlighted(bool highlighted)
    {
        if (uiContainer == null || crosshairImage == null || !uiContainer.activeInHierarchy) return;
        
        // Kill any active animation
        if (currentTween != null) currentTween.Kill();
        
        // Animate to appropriate size
        float targetSize = highlighted ? highlightedSize : normalSize;
        currentTween = crosshairImage.DOScale(Vector3.one * targetSize, fadeInDuration * 0.5f)
            .SetEase(fadeInEase);
    }
    
    public void Hide()
    {
        if (uiContainer == null || crosshairImage == null) return;
        
        // Kill any active animation
        if (currentTween != null) currentTween.Kill();
        
        // Animate to zero scale and then disable
        currentTween = crosshairImage.DOScale(Vector3.zero, fadeOutDuration)
            .SetEase(fadeOutEase)
            .OnComplete(() => uiContainer.SetActive(false));
    }
}