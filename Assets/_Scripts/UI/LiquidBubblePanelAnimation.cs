using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum ContentTweenChoice { Fade, Scale, FadeAndScale }

public class LiquidBubblePanelAnimation : MonoBehaviour
{
    /* ─── Inspector fields (names unchanged) ─── */

    [Header("Background")]
    [SerializeField] private Image  backgroundImage;
    [SerializeField] private string bubblePropertyName = "_Fade";
    [SerializeField] private float  bubbleStartValue   = 0f;
    [SerializeField] private float  bubbleEndValue     = 1f;
    [SerializeField] private float  bubbleFadeDuration = 1f;

    [Header("Content Panel")]
    [SerializeField] private GameObject         contentPanel;
    [SerializeField] private float              contentTweenDuration = 0.5f;
    [SerializeField] private float              initialContentScale  = 0.8f;
    [SerializeField] private ContentTweenChoice contentTweenChoice   = ContentTweenChoice.Fade;

    [Header("Sequential Reveal")]
    [SerializeField] private bool  revealChildrenSequentially = false;
    [SerializeField] private float childRevealInterval        = 0.1f;

    /* ─── Internals ─── */

    private Material                 _bubbleMat;
    private CanvasGroup              _panelGroup;
    private readonly List<CanvasGroup> _childGroups = new();
    private bool _isAnimating;

    public bool IsAnimating => _isAnimating;      // exposes state for PauseController

    /* --------------------------------------------------------------------- */
// ─────────────────────────────────────────────────────────────
// Awake  (re‑issued so children are NOT pre‑scaled)
// ─────────────────────────────────────────────────────────────
    void Awake()
    {
        gameObject.SetActive(false);                                    // avoid one‑frame flash

        _bubbleMat  = backgroundImage ? backgroundImage.material : null;
        _panelGroup = GetOrAddCanvasGroup(contentPanel);

        _childGroups.Clear();
        foreach (Transform child in contentPanel.transform)             // include inactive
        {
            var cg = GetOrAddCanvasGroup(child.gameObject);
            cg.alpha = 0f;                                              // invisible at start
            _childGroups.Add(cg);
            // child.localScale left untouched (1,1,1)
        }

        _panelGroup.alpha = 0f;
        contentPanel.transform.localScale = Vector3.one * initialContentScale;
        contentPanel.SetActive(false);
        
        if (backgroundImage != null)
        {
            // Ensure you are working on the instance, not the shared material.
            _bubbleMat = backgroundImage.material;
            _bubbleMat.SetFloat(bubblePropertyName, bubbleStartValue);
        }
        else
        {
            Debug.LogError("Bubble material is not assigned in the inspector.");
        }
    }


    /* ======================== Public API ======================== */

    public void PlayOpenAnimation()
    {
        if (_isAnimating) return;
        _isAnimating = true;

        gameObject.SetActive(true);               // show root

        _bubbleMat.SetFloat(bubblePropertyName, bubbleStartValue);

        DOTween.To(() => _bubbleMat.GetFloat(bubblePropertyName),
                   v  => _bubbleMat.SetFloat(bubblePropertyName, v),
                   bubbleEndValue, bubbleFadeDuration)
               .SetEase(Ease.Linear)
               .SetUpdate(true)
               .OnComplete(StartContentReveal);
    }

    public void PlayCloseAnimation()
    {
        if (_isAnimating) return;
        _isAnimating = true;

        CloseContent()
            .OnComplete(() =>
            {
                DOTween.To(() => _bubbleMat.GetFloat(bubblePropertyName),
                           v  => _bubbleMat.SetFloat(bubblePropertyName, v),
                           bubbleStartValue, bubbleFadeDuration)
                       .SetEase(Ease.Linear)
                       .SetUpdate(true)
                       .OnComplete(() =>
                       {
                           gameObject.SetActive(false);
                           _isAnimating = false;
                       });
            });
    }

    /* ======================== Internal ========================= */

    void StartContentReveal()
    {
        contentPanel.SetActive(true);

        if (revealChildrenSequentially)
            RevealChildrenOneByOne();
        else
            RevealWholePanel();
    }
    
    // ─────────────────────────────────────────────────────────────
    // RevealWholePanel
    // ─────────────────────────────────────────────────────────────
    void RevealWholePanel()
    {
        // Children must be fully visible when we're not animating them individually
        foreach (var cg in _childGroups) cg.alpha = 1f;

        _panelGroup.alpha = (contentTweenChoice == ContentTweenChoice.Scale) ? 1f : 0f;

        contentPanel.transform.localScale = Vector3.one * initialContentScale;

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        if (contentTweenChoice == ContentTweenChoice.Fade ||
            contentTweenChoice == ContentTweenChoice.FadeAndScale)
        {
            seq.Join(
                DOTween.To(() => _panelGroup.alpha,
                        a  => _panelGroup.alpha = a,
                        1f,
                        contentTweenDuration)
                    .SetEase(Ease.OutCubic));
        }

        if (contentTweenChoice == ContentTweenChoice.Scale ||
            contentTweenChoice == ContentTweenChoice.FadeAndScale)
        {
            seq.Join(
                contentPanel.transform.DOScale(1f, contentTweenDuration)
                    .SetEase(Ease.OutBack, 1.3f));
        }

        seq.OnComplete(() => _isAnimating = false);
    }
    
    // ─────────────────────────────────────────────────────────────
// RevealChildrenOneByOne
// ─────────────────────────────────────────────────────────────
    void RevealChildrenOneByOne()
    {
        _panelGroup.alpha = 1f;                           // parent visible
        contentPanel.transform.localScale = Vector3.one;  // parent full size

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        for (int i = 0; i < _childGroups.Count; ++i)
        {
            float delay = i * childRevealInterval;
            CanvasGroup cg = _childGroups[i];
            Transform   tf = cg.transform;

            // Start values
            cg.alpha      = (contentTweenChoice == ContentTweenChoice.Scale) ? 1f : 0f;
            tf.localScale = Vector3.one * initialContentScale;

            // Fade tween
            if (contentTweenChoice != ContentTweenChoice.Scale)
            {
                seq.Insert(delay,
                    DOTween.To(() => cg.alpha,
                            a  => cg.alpha = a,
                            1f,
                            contentTweenDuration)
                        .SetEase(Ease.OutCubic));
            }

            // Scale tween
            if (contentTweenChoice == ContentTweenChoice.Scale ||
                contentTweenChoice == ContentTweenChoice.FadeAndScale)
            {
                seq.Insert(delay,
                    tf.DOScale(1f, contentTweenDuration)
                        .SetEase(Ease.OutBack, 1.3f));
            }
        }

        seq.OnComplete(() => _isAnimating = false);
    }




// ─────────────────────────────────────────────────────────────
// CloseContent  (unchanged except final alpha reset for Scale)
// ─────────────────────────────────────────────────────────────
    Sequence CloseContent()
    {
        Sequence seq = DOTween.Sequence().SetUpdate(true);

        if (contentTweenChoice == ContentTweenChoice.Fade ||
            contentTweenChoice == ContentTweenChoice.FadeAndScale)
        {
            seq.Join(
                DOTween.To(() => _panelGroup.alpha,
                        a  => _panelGroup.alpha = a,
                        0f,
                        contentTweenDuration)
                    .SetEase(Ease.OutCubic));
        }

        if (contentTweenChoice == ContentTweenChoice.Scale ||
            contentTweenChoice == ContentTweenChoice.FadeAndScale)
        {
            seq.Join(
                contentPanel.transform.DOScale(initialContentScale, contentTweenDuration)
                    .SetEase(Ease.InBack));
        }

        if (contentTweenChoice == ContentTweenChoice.Scale)
            seq.AppendCallback(() => _panelGroup.alpha = 0f);

        return seq;
    }



    static CanvasGroup GetOrAddCanvasGroup(GameObject go)
        => go.TryGetComponent(out CanvasGroup cg) ? cg : go.AddComponent<CanvasGroup>();
}
