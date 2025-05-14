using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAnimatorController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale & Duration")]
    [SerializeField] private float _hoverScale = 1.1f;
    [SerializeField] private float _normalScale = 1.0f;
    [SerializeField] private float _hoverDuration = 0.2f;
    [SerializeField] private float _exitDuration = 0.2f;
    [SerializeField] private Ease _hoverEase = Ease.OutQuint;
    [SerializeField] private Ease _exitEase = Ease.OutQuint;

    [SerializeField] private TextMeshProUGUI underline;
    [SerializeField] private float underlineFadeDuration = 0.2f;

    [SerializeField] private Vector3 _defaultScale = Vector3.one;

    [Header("Behavior Toggle")]
    [SerializeField] private bool _useHoverBehavior = true;

    private Vector3 _initialScale;
    private Button _buttonComponent;
    private Tween _scaleTween;
    private bool _hasInitialized = false;
    private Tween _underlineTween;

    private void Awake()
    {
        if (gameObject.activeSelf && transform.localScale != Vector3.zero)
        {
            _initialScale = transform.localScale;
            _hasInitialized = true;
        }
        else
        {
            _initialScale = _defaultScale;
        }

        InitializeUnderline();
    }

    private void OnEnable()
    {
        if (!_hasInitialized && transform.localScale != Vector3.zero)
        {
            _initialScale = transform.localScale;
            _hasInitialized = true;
        }

        InitializeUnderline();
    }

    private void OnDisable()
    {
        transform.localScale = _initialScale;

        if (underline != null)
        {
            Color color = underline.color;
            color.a = 0f;
            underline.color = color;
        }
    }

    private void InitializeUnderline()
    {
        if (underline != null)
        {
            Color color = underline.color;
            color.a = 0f;
            underline.color = color;
        }
    }

    public void ResetInitialScale(Vector3 newScale)
    {
        _initialScale = newScale;
        _hasInitialized = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_useHoverBehavior && IsButtonInteractable())
        {
            AnimateScale(_hoverScale, _hoverDuration, _hoverEase);
            FadeUnderline(1f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_useHoverBehavior && IsButtonInteractable())
        {
            AnimateScale(_normalScale, _exitDuration, _exitEase);
            FadeUnderline(0f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_useHoverBehavior && IsButtonInteractable())
        {
            AnimateScale(_hoverScale, _hoverDuration, _hoverEase);
            FadeUnderline(1f);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_useHoverBehavior && IsButtonInteractable())
        {
            AnimateScale(_normalScale, _exitDuration, _exitEase);
            FadeUnderline(0f);
        }
    }

    private void AnimateScale(float targetScale, float duration, Ease ease)
    {
        _scaleTween?.Kill();
        _scaleTween = transform.DOScale(_initialScale * targetScale, duration)
            .SetEase(ease)
            .SetUpdate(true);
    }

    private void FadeUnderline(float targetAlpha)
    {
        if (underline != null)
        {
            _underlineTween?.Kill();
            _underlineTween = DOTween.To(() => underline.alpha, x => underline.alpha = x, targetAlpha, underlineFadeDuration);
        }
    }

    private bool IsButtonInteractable()
    {
        if (_buttonComponent == null)
        {
            _buttonComponent = GetComponent<Button>();
        }
        return _buttonComponent == null || _buttonComponent.interactable;
    }
}