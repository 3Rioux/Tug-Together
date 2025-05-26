using Unity.Netcode;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// This script will activate/Deactivate the Navigation components (Compass, Map...)
/// with smooth animations using DoTween and make them face the camera
/// </summary>
public class ActivateNavigation : NetworkBehaviour
{
    [SerializeField] private Transform navigationParent;
    [SerializeField] private Canvas worldSpaceCanvas;
    
    [Header("Billboard Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private bool lockYAxis = true;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private bool useSmoothing = true;
    [SerializeField] private bool useLateUpdate = true;

    [Header("Compass Components")]
    [SerializeField] private Transform compassBase;       // The base of the compass
    [SerializeField] private Transform[] compassArrows;   // Array of compass arrow elements

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float arrowDelay = 0.05f;    // Delay between arrow animations
    [SerializeField] private Ease appearEase = Ease.OutBack;
    [SerializeField] private Ease disappearEase = Ease.InBack;

    // Store original scales
    private Vector3 navigationOriginalScale;
    private Vector3 compassBaseOriginalScale;
    private Vector3[] compassArrowsOriginalScales;
    private Vector3 canvasOriginalScale;

    private BoatInputActions controls;
    private bool isMapActive = false;
    [SerializeField] private bool isToggle = true;
    private Sequence animationSequence;

    private void Awake()
    {
        // Find main camera if not assigned
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        // Store original scales before setting to zero
        navigationOriginalScale = navigationParent.localScale;
        if (compassBase != null)
            compassBaseOriginalScale = compassBase.localScale;

        if (compassArrows != null)
        {
            compassArrowsOriginalScales = new Vector3[compassArrows.Length];
            for (int i = 0; i < compassArrows.Length; i++)
            {
                if (compassArrows[i] != null)
                    compassArrowsOriginalScales[i] = compassArrows[i].localScale;
            }
        }

        if (worldSpaceCanvas != null)
            canvasOriginalScale = worldSpaceCanvas.transform.localScale;

        // Initialize all components to zero scale
        navigationParent.gameObject.SetActive(false);
        navigationParent.localScale = Vector3.zero;

        if (compassBase != null)
            compassBase.localScale = Vector3.zero;

        if (compassArrows != null)
        {
            foreach (var arrow in compassArrows)
            {
                if (arrow != null)
                    arrow.localScale = Vector3.zero;
            }
        }

        if (worldSpaceCanvas != null)
        {
            worldSpaceCanvas.transform.localScale = Vector3.zero;
            worldSpaceCanvas.gameObject.SetActive(false);
        }

        controls = new BoatInputActions();

        if (isToggle)
        {
            controls.Boat.ToggleMap.performed += ctx => ToggleNavigation();
        }
        else
        {
            controls.Boat.ToggleMap.performed += ctx => HoldNavigation(true);
            controls.Boat.ToggleMap.canceled += _ => HoldNavigation(false);
        }
    }

    private void Update()
    {
        // Only handle rotation here if not using LateUpdate
        if (!useLateUpdate && navigationParent.gameObject.activeInHierarchy && mainCamera != null)
        {
            RotateToFaceCamera();
        }
    }
    
    private void LateUpdate()
    {
        // Handle rotation in LateUpdate for smoother following
        if (useLateUpdate && navigationParent.gameObject.activeInHierarchy && mainCamera != null)
        {
            RotateToFaceCamera();
        }
    }

     /// <summary>
    /// Makes the navigation UI face the camera like a billboard
    /// </summary>
    private void RotateToFaceCamera()
    {
        if (lockYAxis)
        {
            // Y-axis only rotation (classic billboard)
            Vector3 directionToCamera = mainCamera.transform.position - navigationParent.position;
            directionToCamera.y = 0; // Remove vertical component to only rotate on Y axis

            if (directionToCamera != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera);
                
                if (useSmoothing)
                {
                    navigationParent.rotation = Quaternion.Slerp(
                        navigationParent.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime);
                }
                else
                {
                    // Direct rotation without smoothing
                    navigationParent.rotation = targetRotation;
                }
            }
        }
        else
        {
            // Full billboard rotation
            if (useSmoothing)
            {
                // Get direction to camera
                Vector3 directionToCamera = mainCamera.transform.position - navigationParent.position;
                if (directionToCamera != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera);
                    navigationParent.rotation = Quaternion.Slerp(
                        navigationParent.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime);
                }
            }
            else
            {
                // Direct look at camera
                navigationParent.forward = -(mainCamera.transform.position - navigationParent.position).normalized;
            }
        }
    }

    void OnEnable()
    {
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Disable();
    }

    /// <summary>
    /// Toggles the navigation with animations
    /// </summary>
    private void ToggleNavigation()
    {
        if (!IsOwner) return;

        isMapActive = !isMapActive;

        if (isMapActive)
        {
            ShowNavigationWithAnimation();
        }
        else
        {
            HideNavigationWithAnimation();
        }
    }

    /// <summary>
    /// Activate/Deactivate the Navigation with animations
    /// </summary>
    private void HoldNavigation(bool trigger)
    {
        if (!IsOwner) return;

        if (trigger)
        {
            ShowNavigationWithAnimation();
        }
        else
        {
            HideNavigationWithAnimation();
        }
    }

private void ShowNavigationWithAnimation()
{
    // Kill any running animations
    DOTween.Kill(navigationParent);
    if (animationSequence != null && animationSequence.IsActive())
        animationSequence.Kill();

    // Enable objects
    navigationParent.gameObject.SetActive(true);

    // Create animation sequence
    animationSequence = DOTween.Sequence();

    // 1. First animate the navigation parent
    navigationParent.localScale = Vector3.zero;
    animationSequence.Append(navigationParent.DOScale(navigationOriginalScale, animationDuration)
        .SetEase(appearEase));

    if (compassBase != null)
    {
        // 2. Start compass base animation
        compassBase.localScale = Vector3.zero;

        // Calculate base animation duration
        float baseAnimDuration = animationDuration * 1.2f;

        // Insert base animation at the start
        animationSequence.Insert(animationDuration * 0.2f,
            compassBase.DOScale(compassBaseOriginalScale, baseAnimDuration)
            .SetEase(appearEase));

        // 3. Calculate when to start arrows (when base is at ~80%)
        float arrowsStartTime = animationDuration * 0.2f + (baseAnimDuration * 0.7f);

        // 4. Then animate each arrow with overlap
        if (compassArrows != null && compassArrows.Length > 0)
        {
            float arrowAnimDuration = animationDuration * 0.7f;
            float totalArrowsTime = arrowAnimDuration + (compassArrows.Length - 1) * arrowDelay;

            for (int i = 0; i < compassArrows.Length; i++)
            {
                if (compassArrows[i] != null)
                {
                    float thisArrowStartTime = arrowsStartTime + (i * arrowDelay);
                    compassArrows[i].localScale = Vector3.zero;

                    animationSequence.Insert(thisArrowStartTime,
                        compassArrows[i].DOScale(compassArrowsOriginalScales[i], arrowAnimDuration)
                        .SetEase(appearEase));
                }
            }

            // 5. Start canvas animation when last arrow is at ~80% (if canvas exists)
            if (worldSpaceCanvas != null)
            {
                float canvasStartTime = arrowsStartTime + totalArrowsTime * 0.8f;

                worldSpaceCanvas.gameObject.SetActive(true);
                worldSpaceCanvas.transform.localScale = Vector3.zero;

                animationSequence.Insert(canvasStartTime,
                    worldSpaceCanvas.transform.DOScale(canvasOriginalScale, animationDuration)
                    .SetEase(appearEase));
            }
        }
        else if (worldSpaceCanvas != null) // If no arrows, start canvas after base
        {
            float canvasStartTime = arrowsStartTime;

            worldSpaceCanvas.gameObject.SetActive(true);
            worldSpaceCanvas.transform.localScale = Vector3.zero;

            animationSequence.Insert(canvasStartTime,
                worldSpaceCanvas.transform.DOScale(canvasOriginalScale, animationDuration)
                .SetEase(appearEase));
        }
    }
    else if (worldSpaceCanvas != null) // No compass base, show canvas after parent
    {
        worldSpaceCanvas.gameObject.SetActive(true);
        worldSpaceCanvas.transform.localScale = Vector3.zero;

        animationSequence.Insert(animationDuration * 0.8f,
            worldSpaceCanvas.transform.DOScale(canvasOriginalScale, animationDuration)
            .SetEase(appearEase));
    }
    
    // Immediately align to camera when shown
    if (mainCamera != null)
    {
        RotateToFaceCamera();
    }
}

private void HideNavigationWithAnimation()
{
    // Kill any running animations
    DOTween.Kill(navigationParent);
    if (animationSequence != null && animationSequence.IsActive())
        animationSequence.Kill();

    // Create reverse animation sequence with overlapping elements
    animationSequence = DOTween.Sequence();
    float totalDuration = 0f;

    // 1. First start hiding the canvas
    if (worldSpaceCanvas != null)
    {
        animationSequence.Append(worldSpaceCanvas.transform.DOScale(Vector3.zero, animationDuration * 0.8f)
            .SetEase(disappearEase));
        totalDuration += animationDuration * 0.4f; // Only wait for 50% completion
    }

    // 2. Start hiding arrows in reverse order with overlap
    if (compassArrows != null && compassArrows.Length > 0)
    {
        float arrowAnimDuration = animationDuration * 0.5f;

        for (int i = compassArrows.Length - 1; i >= 0; i--)
        {
            if (compassArrows[i] != null)
            {
                // Insert at position with some overlap
                animationSequence.Insert(totalDuration + (compassArrows.Length - 1 - i) * arrowDelay * 0.5f,
                    compassArrows[i].DOScale(Vector3.zero, arrowAnimDuration)
                    .SetEase(disappearEase));
            }
        }

        // Add time but account for overlap
        totalDuration += arrowDelay * 0.5f * (compassArrows.Length - 1) + arrowAnimDuration * 0.6f;
    }

    // 3. Hide compass base with slight overlap
    if (compassBase != null)
    {
        animationSequence.Insert(totalDuration,
            compassBase.DOScale(Vector3.zero, animationDuration)
            .SetEase(disappearEase));

        totalDuration += animationDuration * 0.6f;
    }

    // 4. Finally hide navigation parent and disable objects
    animationSequence.Insert(totalDuration,
        navigationParent.DOScale(Vector3.zero, animationDuration)
        .SetEase(disappearEase))
        .OnComplete(() => {
            navigationParent.gameObject.SetActive(false);
            if (worldSpaceCanvas != null)
                worldSpaceCanvas.gameObject.SetActive(false);
        });
}

    private void OnDestroy()
    {
        // Clean up any running animations
        if (animationSequence != null && animationSequence.IsActive())
            animationSequence.Kill();
    }
}