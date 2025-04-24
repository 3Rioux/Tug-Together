using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening; // Import DOTween

/// <summary>
/// Script that handles transition and scene changes 
/// </summary>
public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; } // SceneController Instance

    [SerializeField] private GameObject _fadeCanvasPrefab; // Prefab with Canvas & UI Image
    [SerializeField] private float _defaultFadeDuration = 1f; // Default fade resistence

    private Material _transitionMaterial; //store current _transitionMaterial
    private GameObject _fadeCanvasInstance; // store the fadeCanvas Prefab instance 
    private bool _isInitialized = false; // used to have a transition at the game start 

    /// <summary>
    /// First method called: Creates an Instance of this script and Initialises the Fade State + Canvas 
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFadeCanvas();
            _isInitialized = true;
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate
        }
    }

    /// <summary>
    /// Method called in Awake to Initialise the Fade Canvas prefabs for the transition s
    /// </summary>
    private void InitializeFadeCanvas()
    {
        if (_fadeCanvasPrefab != null)
        {
            //Instantiate fade Canvas 
            _fadeCanvasInstance = Instantiate(_fadeCanvasPrefab);
            DontDestroyOnLoad(_fadeCanvasInstance); //move it to dontDestroyon load

            //access the _fadeCanvasInstance Image component 
            Image fadeImage = _fadeCanvasInstance.GetComponentInChildren<Image>();
            if (fadeImage != null)
            {
                _transitionMaterial = fadeImage.material;
                _transitionMaterial.SetFloat("_Fade", 0); // Start fully visible
            }
            else
            {
                Debug.LogError("SceneController: Fade Canvas Prefab must contain an Image component!");
            }
        }
        else
        {
            Debug.LogError("SceneController: Fade Canvas Prefab is not assigned!");
        }
    }

    /// <summary>
    /// Method called after Awake to start the Transition fade in 
    /// </summary>
    private void Start()
    {
        if (_isInitialized)
        {
            FadeIn(_defaultFadeDuration);
        }
    }

    /// <summary>
    /// Public method to Load a scene with a fade transition.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    /// <param name="fadeSpeed">Optional: Custom fade duration.</param>
    public void LoadScene(string sceneName, float fadeSpeed = -1f)
    {
        if (_transitionMaterial == null)
        {
            Debug.LogError("SceneController: Transition material is missing!");
            return;
        }

        //calculate fade duration
        float duration = (fadeSpeed > 0) ? fadeSpeed : _defaultFadeDuration;
        StartCoroutine(FadeAndLoadScene(sceneName, duration));
    }

    /// <summary>
    /// Method That Loads Scene with Transition:
    ///     starts the Fade out Transition ->
    ///     Loads Scene (if scene we transition to is == "GameplayScene" Call the StartGame method in the GameManager)  ->
    ///     start Fade In Transition
    /// </summary>
    /// <param name="sceneName"></param>
    /// <param name="fadeSpeed"></param>
    /// <returns></returns>
    private IEnumerator FadeAndLoadScene(string sceneName, float fadeSpeed)
    {
        // 1. Fade Out completely
        yield return FadeOut(fadeSpeed);

        // 2. Ensure material is fully faded before loading new scene
        _transitionMaterial.SetFloat("_Fade", 1);

        // 3. Load Scene
        SceneManager.LoadScene(sceneName);

        // 4. Short wait after scene loads to avoid flickering
        yield return new WaitForSeconds(0.3f);

        // 5. Fade In to reveal new scene
        yield return FadeIn(fadeSpeed);
    }

    /// <summary>
    /// Fades out the screen completely before scene load.
    /// </summary>
    /// <param name="fadeSpeed">Duration of the fade effect.</param>
    private IEnumerator FadeOut(float fadeSpeed)
    {
        yield return _transitionMaterial.DOFloat(1f, "_Fade", fadeSpeed)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();
    }

    /// <summary>
    /// Fades in the screen after scene load.
    /// </summary>
    /// <param name="fadeSpeed">Duration of the fade effect.</param>
    private IEnumerator FadeIn(float fadeSpeed)
    {
        yield return _transitionMaterial.DOFloat(0f, "_Fade", fadeSpeed)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();
    }
}
