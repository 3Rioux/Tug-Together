using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIPulseBreath : MonoBehaviour
{
    [Header("Pulse (Scale) Settings")]
    public float pulseSpeed = 1f;
    public float pulseAmount = 0.05f;


    [Header("Breath (Pixel Multiplier) Settings")]
    public float breathSpeed = 1f;
    public float breathAmount = 0.1f;
    public float basePixelMultiplier = 1f;


    private RectTransform rectTransform;
    private Image image;
    private Vector3 originalScale;
    private float time;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        originalScale = rectTransform.localScale;

        if (image.sprite == null || !image.sprite.packed)
        {
            Debug.LogWarning("Make sure the image sprite is not packed or uses 'Sprite (2D and UI)' with Read/Write enabled for pixel multiplier to have visible effects.");
        }
    }

    void Update()
    {
        time += Time.deltaTime;

        // Pulse Effect (Scale)
        float scalePulse = 1f + Mathf.Sin(time * pulseSpeed) * pulseAmount;
        rectTransform.localScale = originalScale * scalePulse;

        // Breath Effect (Pixel Per Unit Multiplier)
        float pixelPulse = basePixelMultiplier + Mathf.Sin(time * breathSpeed) * breathAmount;
        image.pixelsPerUnitMultiplier = pixelPulse;
    }
}
