using UnityEngine;
using DG.Tweening;

public class UIBlurController : MonoBehaviour
{
    [Header("Blur Material Settings")]
    [Tooltip("Reference to the material with the blur shader.")]
    [SerializeField] private Material blurMaterial;

    [Tooltip("Shader property name controlling the blur amount.")]
    [SerializeField] private string blurProperty = "_Amount";

    [Tooltip("Starting blur amount (unpaused state).")]
    [SerializeField] private float blurStart = 0f;

    [Tooltip("Ending blur amount (paused state).")]
    [SerializeField] private float blurEnd = 3f;

    [Tooltip("Duration of the blur tween.")]
    [SerializeField] private float blurTweenDuration = 0.5f;

    [Header("Pause Blur Tweens")]
    [Tooltip("Delay before starting the pause blur tween.")]
    [SerializeField] private float pauseBlurDelay = 0f;

    [Tooltip("Easing for applying the pause blur tween.")]
    [SerializeField] private Ease pauseBlurEase = Ease.OutCubic;

    [Header("Resume Blur Tweens")]
    [Tooltip("Delay before starting the resume blur tween.")]
    [SerializeField] private float removeBlurDelay = 0f;

    [Tooltip("Easing for removing the blur tween.")]
    [SerializeField] private Ease removeBlurEase = Ease.OutCubic;

    [Tooltip("Speed multiplier for the removal blur tween. Increasing this will make it faster.")]
    [SerializeField] private float removeBlurSpeedMultiplier = 1f;

    private Tween currentTween;

    /// <summary>
    /// Tween blur from 0 to the paused value.
    /// </summary>
    public void ApplyPauseBlur()
    {
        currentTween?.Kill();

        currentTween = DOTween.To(() => blurMaterial.GetFloat(blurProperty),
                                    value => blurMaterial.SetFloat(blurProperty, value),
                                    blurEnd,
                                    blurTweenDuration)
                                .SetEase(pauseBlurEase)
                                .SetDelay(pauseBlurDelay);
    }

    /// <summary>
    /// Tween blur from the paused value back to 0.
    /// </summary>
    public void RemovePauseBlur()
    {
        currentTween?.Kill();

        float adjustedDuration = blurTweenDuration / removeBlurSpeedMultiplier;
        currentTween = DOTween.To(() => blurMaterial.GetFloat(blurProperty),
                                    value => blurMaterial.SetFloat(blurProperty, value),
                                    blurStart,
                                    adjustedDuration)
                                .SetEase(removeBlurEase)
                                .SetDelay(removeBlurDelay);
    }
}