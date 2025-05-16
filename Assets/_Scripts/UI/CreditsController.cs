using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;

public class CreditsController : MonoBehaviour
{
    public static CreditsController Instance { get; private set; }

    [Header("Prefab and Scene")]
    [SerializeField] private GameObject creditsCanvasPrefab;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Fade and Scroll")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float scrollSpeed = 50f;
    [Tooltip("How far below the panel to start (negative moves it down)")]
    [SerializeField] private float startYOffset = 0f;
    [Tooltip("How far beyond content top to stop")]
    [SerializeField] private float finishYOffset = 0f;

    [Header("Thank You Message")]
    [Tooltip("Fade duration for the thank-you text")]
    [SerializeField] private float thankYouFadeDuration = 1f;
    [Tooltip("How long the thank-you text stays fully visible")]
    [SerializeField] private float thankYouDisplayDuration = 3f;

    private GameObject canvasInstance;
    private CanvasGroup canvasGroup;
    private RectTransform creditsContent;
    private Button skipButton;
    private CanvasGroup thankYouCanvasGroup;
    private bool isSkipping;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void ShowCredits()
    {
        AudioManager.Instance.PlayAmbience(FMODEvents.Instance.Credits);
        // Ignore if creditsCanvas is already active.
        if (canvasInstance != null)
        {
            Debug.Log("Credits already active. Ignoring additional request.");
            return;
        }

        if (creditsCanvasPrefab == null)
        {
            Debug.LogError("Assign the CreditsCanvas prefab in the inspector");
            return;
        }

        canvasInstance = Instantiate(creditsCanvasPrefab);
        DontDestroyOnLoad(canvasInstance);

        Transform panel = canvasInstance.transform.Find("Panel");
        if (panel == null)
        {
            Debug.LogError("CreditsCanvas prefab needs a child named 'Panel'");
            return;
        }

        canvasGroup = panel.GetComponent<CanvasGroup>();
        creditsContent = panel.Find("CreditsContent").GetComponent<RectTransform>();
        skipButton = panel.Find("SkipButton").GetComponent<Button>();

        Transform thankTf = panel.Find("ThankYouText");
        if (thankTf != null)
        {
            thankYouCanvasGroup = thankTf.GetComponent<CanvasGroup>();
            if (thankYouCanvasGroup != null)
            {
                thankYouCanvasGroup.alpha = 0f;
                thankYouCanvasGroup.gameObject.SetActive(true);
            }
        }
        else
        {
            Debug.LogError("Panel needs a child named 'ThankYouText' with a CanvasGroup");
        }

        skipButton.onClick.RemoveAllListeners();
        skipButton.onClick.AddListener(() => isSkipping = true);

        canvasGroup.alpha = 0f;
        isSkipping = false;

        StartCoroutine(RunCredits());
    }

    private IEnumerator RunCredits()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(creditsContent);

        float panelH = canvasGroup.GetComponent<RectTransform>().rect.height;
        float contentH = creditsContent.rect.height;

        float startY = -panelH + startYOffset;
        float endY = contentH + finishYOffset;

        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            if (canvasGroup == null) yield break;
            canvasGroup.alpha = t / fadeDuration;
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        creditsContent.anchoredPosition = new Vector2(0f, startY);

        while (!isSkipping && creditsContent.anchoredPosition.y < endY)
        {
            creditsContent.anchoredPosition += Vector2.up * scrollSpeed * Time.unscaledDeltaTime;
            yield return null;
        }

        if (!isSkipping)
            creditsContent.anchoredPosition = new Vector2(0f, endY);

        // If skip was pressed, go directly to MainMenu.
        // Otherwise show the thank you message before transitioning.
        if (isSkipping)
            yield return StartCoroutine(LoadMenuAndFadeOut());
        else
            yield return StartCoroutine(ShowThankYouAndExit());
    }

    private IEnumerator ShowThankYouAndExit()
    {
        if (thankYouCanvasGroup != null)
        {
            for (float t = 0f; t < thankYouFadeDuration; t += Time.unscaledDeltaTime)
            {
                thankYouCanvasGroup.alpha = t / thankYouFadeDuration;
                yield return null;
            }
            thankYouCanvasGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(thankYouDisplayDuration);

            for (float t = 0f; t < thankYouFadeDuration; t += Time.unscaledDeltaTime)
            {
                thankYouCanvasGroup.alpha = 1f - (t / thankYouFadeDuration);
                yield return null;
            }
            thankYouCanvasGroup.alpha = 0f;
        }

        yield return StartCoroutine(LoadMenuAndFadeOut());
    }

    private IEnumerator LoadMenuAndFadeOut()
    {
        yield return SceneManager.LoadSceneAsync(mainMenuSceneName);

        if (canvasGroup == null)
            yield break;

        // Create a tween using DOTween.To with null checks in the getter and setter.
        var tween = DOTween.To(
            () => canvasGroup != null ? canvasGroup.alpha : 0f,
            x => { if (canvasGroup != null) canvasGroup.alpha = x; },
            0f,
            fadeDuration
        );

        yield return tween.WaitForCompletion();

        if (canvasInstance != null)
        {
            Destroy(canvasInstance);
            canvasInstance = null;
            canvasGroup = null;
            creditsContent = null;
            skipButton = null;
            thankYouCanvasGroup = null;
        }

        DOTween.KillAll();
    }
}