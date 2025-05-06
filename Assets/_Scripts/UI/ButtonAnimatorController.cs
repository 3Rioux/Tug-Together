using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAnimatorController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale & Duration")]
    [SerializeField] private float _holdScale = 1.2f;               // Multiplier when button is pressed
    [SerializeField] private float _releaseScale = 1.0f;            // Multiplier when button is released
    [SerializeField] private float _pressDuration = 0.15f;          // Time for press animation
    [SerializeField] private float _releaseDuration = 0.2f;         // Time for release animation
    [SerializeField] private Ease _pressEase = Ease.OutBack;        // Easing for press animation
    [SerializeField] private Ease _releaseEase = Ease.OutElastic;   // Easing for release animation
    
    [SerializeField] private Vector3 _defaultScale = Vector3.one;


    private Vector3 _initialScale;                // Cached initial scale for the button
    private Button _buttonComponent;              // Cached reference to Button component
    private Tween _scaleTween;                    // Reference to active scale tween
    private bool _hasInitialized = false;

    private void Awake()
    {
        // Only capture scale if the object is active and not zero
        if (gameObject.activeSelf && transform.localScale != Vector3.zero)
        {
            _initialScale = transform.localScale;
            _hasInitialized = true;
        }
        else
        {
            _initialScale = _defaultScale;
        }
    }
    
    private void OnEnable()
    {
        // If we weren't properly initialized in Awake, do it now when enabled
        if (!_hasInitialized && transform.localScale != Vector3.zero)
        {
            _initialScale = transform.localScale;
            _hasInitialized = true;
        }
    }
    
    public void ResetInitialScale(Vector3 newScale)
    {
        _initialScale = newScale;
        _hasInitialized = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!IsButtonInteractable()) return;

        _scaleTween?.Kill();
        // Animate to initial scale multiplied by hold scale
        _scaleTween = transform.DOScale(_initialScale * _holdScale, _pressDuration)
            .SetEase(_pressEase)
            .SetUpdate(true); // Ignore time scale
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!IsButtonInteractable()) return;

        _scaleTween?.Kill();
        // Animate back to initial scale multiplied by release scale
        _scaleTween = transform.DOScale(_initialScale * _releaseScale, _releaseDuration)
            .SetEase(_releaseEase)
            .SetUpdate(true); // Ignore time scale
    }

    private bool IsButtonInteractable()
    {
        if (_buttonComponent == null)
        {
            _buttonComponent = GetComponent<Button>();
        }
        return _buttonComponent == null || _buttonComponent.interactable;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            _scaleTween?.Kill();
            transform.localScale = _initialScale;
        }
    }
}