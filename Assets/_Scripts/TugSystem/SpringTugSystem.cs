using GogoGaga.OptimizedRopesAndCables;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;


public class SpringTugSystem : NetworkBehaviour
{

    #region Variables
    public bool isDead = false; // stop tuw logic especially auto aim if player is dead;
    
    [SerializeField] private TextMeshProUGUI distanceText;

   
    [SerializeField] private SpringJoint springJoint;
    [SerializeField] private Rope visualRope;
    [SerializeField] private GameObject boatHook;
    public Rope VisualRope { get => visualRope; set => visualRope = value; }

    [Header("Tow Settings")]
    [SerializeField] private Rigidbody towedObject;
    [SerializeField] private Transform towAnchor;
    [SerializeField] private Transform targetAttachPoint;
    [SerializeField] private Transform ropeAttachPoint;
    [SerializeField] private float springAmount = 35f;
    [SerializeField] private float springMaxDistance = 5f;
    [SerializeField] private float maxTowDistance = 25f;
    [SerializeField] private float distanceToTowedObject;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Aim Settings ")]
    [SerializeField] private CinemachineCamera freeLookCamera; // FreeLook Camera
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraFollowPoint;
    [SerializeField] private Transform cameraAimFollowPoint;
    [SerializeField] private LayerMask aimColliderLayerMask = new LayerMask();//the layer the player will hit (i want to ignore the player layer) (Cant hit itself with a ray)
    [SerializeField] private bool isAimMode = false; // bool to stop the auto target closest hook code
    [SerializeField] private List<Transform> visibleHooks = new List<Transform>();
    [SerializeField] private List<Transform> hookPoints;
    [SerializeField] private float hookSwitchCooldown = 1f; // Cooldown in seconds
    [SerializeField] private CinemachineCamera aimCamera; // aim Camera 
    [SerializeField] private float cameraBlendTime = 0.2f;
    private Quaternion lastFreeLookRotation;
    private bool isTransitioning = false;
    private float lastSwitchTime;
    private Vector3 lastAimingEulerAngles;
    private Vector3 lastFreeLookEulerAngles;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject crosshair;
    [SerializeField] private RectTransform crosshairRect;
    [SerializeField] private UnityEngine.UI.Image crosshairImage;  // Reference to the Image component
    [SerializeField] private Sprite normalCrosshairSprite;  // Normal state sprite
    [SerializeField] private Sprite attachedCrosshairSprite;  // Attached state sprite
    [SerializeField] private float crosshairFadeDuration = 0.3f;  // Animation duration
    [SerializeField] private float crosshairTransitionDuration = 0.4f;  // Transition animation duration
    [SerializeField] private Ease crosshairShowEase = Ease.OutBack;
    [SerializeField] private Ease crosshairHideEase = Ease.InQuad;
    [SerializeField] private Ease crosshairGrowEase = Ease.OutElastic;  // For transition animation
    [SerializeField] private Ease crosshairShrinkEase = Ease.InOutBack;  // For transition animation
    private Tween crosshairTween;  // To track and kill active animations
    //Stuff To Disable when Aiming:
    //[SerializeField] private GameObject sails;
    //[SerializeField] private GameObject flags;
    //[SerializeField] private GameObject face; // all good i hand the LayerMask as max distance lol

    [Tooltip("How far in degrees can you move the aim camera up")]
    [SerializeField] private float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the aim camera down")]
    [SerializeField] private float BottomClamp = -30.0f;

    [Tooltip("Additional degress to override the aim camera. Useful for fine tuning camera position when locked")]
    [SerializeField] private float CameraAngleOverride = 0.0f;

    public float Sensitivity = 1.0f;//----------------reduce the mouse sensitivity when aiming--------------------------------------------------------------------------------------
    private const float _threshold = 0.01f;
    private float yaw;
    private float pitch;
    private float lastHookSwitchTime = 0f; // Time when the last switch happened
    private Vector2 lookVector;

    private BoatInputActions controls;
    internal bool isAttached = false; // allow re-attaching and detattaching

    //Visualise attach points 
    private Transform currentClosestAttachPoint;
    public int selectedHookIndex; // store teh index of the closes point !!! dont like this !!!
    private Transform lastClosestAttachPoint;

    // Different material for highlight
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material defaultMaterial;
    
    private TugboatMovementWFloat boatMovement;
    
    private Dictionary<Transform, AttachPointUI> hookPointUIs = new Dictionary<Transform, AttachPointUI>();




    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return controls.Boat.Look.activeControl.Equals("KeyboardMouse");
#else
				return false;
#endif
        }
    }

    public float DistanceToTowedObject { get => distanceToTowedObject; set => distanceToTowedObject = value; }
    #endregion


    private void Awake()
    {
        // Make sure the towedObject rigidbody is properly set up
        if (towedObject != null)
        {
            // Freeze rotation on X and Z axes, allowing only Y rotation
            towedObject.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        if(boatHook != null)
        {
            boatHook.SetActive(true); // make sure it active 
        }
    }


    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        
        if (crosshair != null)
        {
            crosshair.SetActive(false);
            if (crosshairRect == null)
                crosshairRect = crosshair.GetComponent<RectTransform>();
            if (crosshairRect != null)
                crosshairRect.localScale = Vector3.zero;
        }
        
        InitializeAttachPointUIs(); // shit doesnt work for now

        //visualRope.gameObject.GetComponent<NetworkObject>().Spawn();
        visualRope.gameObject.SetActive(false); //needs to create the network object before being deactivated 

        if (towedObject == null)
        {
            towedObject = LevelVariableManager.Instance.GlobalBargeRigidBody;
            hookPoints = towedObject.GetComponent<TowableObjectController>().TowPointList;
        }

        boatMovement = GetComponent<TugboatMovementWFloat>();
        // springJoint.autoConfigureConnectedAnchor = false;
        //springJoint.connectedAnchor = ropeAttachPoint.position;
    }

    private void OnEnable()
    {
        // //if (!IsOwner) return;
        //
        // controls = new BoatInputActions();
        // controls.Boat.Enable();
        //
        // // Register event handlers
        // controls.Boat.Hook.performed += OnHookTriggered;
        // controls.Boat.AimHook.performed += OnAimTriggered;
        // controls.Boat.AimHook.performed += ctx => isAimMode = true; //set to true when pressed 
        // controls.Boat.AimHook.canceled += ctx => isAimMode = false; // set to false when cancelled 
        //
        // //cam look 
        // controls.Boat.Look.performed += ctx => lookVector = ctx.ReadValue<Vector2>();
        // controls.Boat.Look.canceled += _ => lookVector = Vector2.zero; // remove this to enable camera drift 
        // //}else
        // //{
        // //    // Unregister event handlers
        // //    //controls.Boat.Hook.performed -= OnHookTriggered;
        // //    //controls.Boat.AimHook.performed -= OnHookTriggered;
        //
        // //    lookVector = Vector2.zero;
        // //}
        //
        //
        // //set aim cam to off by default 
        // //aimCamera.gameObject.SetActive(false);// off by default 
        // isAimMode = false;
        
        controls = new BoatInputActions();
        controls.Boat.Enable();

        // Register aim events
        controls.Boat.AimHook.performed += OnAimModeEnter;
        controls.Boat.AimHook.canceled += OnAimModeExit;
        
        controls.Boat.Hook.performed += OnHookTriggered;
    
        // Look input
        controls.Boat.Look.performed += ctx => lookVector = ctx.ReadValue<Vector2>();
        controls.Boat.Look.canceled += _ => lookVector = Vector2.zero;
    
        // Set initial priorities but don't disable
        aimCamera.Priority = freeLookCamera.Priority - 10; // Lower than third-person by default
        isAimMode = false;

        if (towedObject == null)
        {
            towedObject = LevelVariableManager.Instance.GlobalBargeRigidBody;
            hookPoints = towedObject.GetComponent<TowableObjectController>().TowPointList;
        }
    }
    
    private void OnDisable()
    {
        //if (!IsOwner) return;
        // Unregister event handlers
        controls.Boat.Hook.performed -= OnHookTriggered;
        controls.Boat.AimHook.performed -= OnHookTriggered;

        // Disable the action map
        controls.Boat.Disable();
    }

    
    private void OnAimModeEnter(InputAction.CallbackContext ctx)
    {
        isAimMode = true;
        lastSwitchTime = Time.time;

        ShowCrosshair();
    
        // Get the ACTUAL current view direction instead of camera euler angles
        Vector3 currentViewDirection = Camera.main.transform.forward;
    
        // Set aim camera to look in exactly the same direction
        cameraAimFollowPoint.rotation = Quaternion.LookRotation(currentViewDirection);
    
        // Store these initial values for aim camera reference
        yaw = cameraAimFollowPoint.eulerAngles.y;
        pitch = cameraAimFollowPoint.eulerAngles.x;
        if (pitch > 180) pitch -= 360f; // Convert to -180 to 180 range

        // Switch camera priority
        aimCamera.Priority = freeLookCamera.Priority + 10;

        // Only show UIs if not currently attached
        if (!isAttached)
        {
            FindVisibleHooks();
            SelectClosestVisibleHook();
        }
    }

    private void OnAimModeExit(InputAction.CallbackContext ctx)
    {
        isAimMode = false;
        lastSwitchTime = Time.time;
        
        HideCrosshair();
    
        // Store the aim camera rotation BEFORE changing priority
        if (Camera.main != null)
            lastAimingEulerAngles = Camera.main.transform.eulerAngles;
    
        // Hide hook UIs
        foreach (var ui in hookPointUIs.Values)
        {
            ui.Hide();
        }
    
        // Apply aim rotation to freelook camera
        CinemachineOrbitalFollow orbitalFollow = freeLookCamera.GetComponent<CinemachineOrbitalFollow>();
        if (orbitalFollow != null)
        {
            // Set horizontal rotation directly from stored aim angles
            orbitalFollow.HorizontalAxis.Value = lastAimingEulerAngles.y;
        
            // Convert pitch to normalized 0-1 value
            float verticalAngle = lastAimingEulerAngles.x;
            if (verticalAngle > 180) verticalAngle -= 360; // Convert to -180 to 180 range
        
            // Clamp and normalize
            verticalAngle = Mathf.Clamp(verticalAngle, BottomClamp, TopClamp);
            orbitalFollow.VerticalAxis.Value = Mathf.InverseLerp(BottomClamp, TopClamp, verticalAngle);
        }
    
        // Switch camera priority
        aimCamera.Priority = freeLookCamera.Priority - 10;
    }
    
    private void ShowCrosshair()
{
    if (crosshair == null) return;

    // Kill any active animation
    if (crosshairTween != null) crosshairTween.Kill();

    // Make sure crosshair is active and has correct image
    crosshair.SetActive(true);
    crosshairImage.sprite = isAttached ? attachedCrosshairSprite : normalCrosshairSprite;

    // Set initial scale to zero
    crosshairRect.localScale = Vector3.zero;

    // Animate to full scale
    crosshairTween = crosshairRect.DOScale(Vector3.one, crosshairFadeDuration)
        .SetEase(crosshairShowEase);
}

private void HideCrosshair()
{
    if (crosshair == null) return;

    // Kill any active animation
    if (crosshairTween != null) crosshairTween.Kill();

    // Animate to zero scale
    crosshairTween = crosshairRect.DOScale(Vector3.zero, crosshairFadeDuration)
        .SetEase(crosshairHideEase)
        .OnComplete(() => crosshair.SetActive(false));  // Hide after animation completes
}

private void TransitionCrosshair(bool toAttached)
{
    if (crosshair == null) return;

    // Kill any active animation
    if (crosshairTween != null) crosshairTween.Kill();

    // Create a sequence for smooth transition
    Sequence sequence = DOTween.Sequence();

    // Grow the crosshair
    sequence.Append(crosshairRect.DOScale(Vector3.one * 1.25f, crosshairTransitionDuration * 0.4f)
        .SetEase(crosshairGrowEase));

    // Shrink to zero
    sequence.Append(crosshairRect.DOScale(Vector3.zero, crosshairTransitionDuration * 0.3f)
        .SetEase(crosshairHideEase)
        .OnComplete(() => {
            // Change sprite when fully scaled down
            crosshairImage.sprite = toAttached ? attachedCrosshairSprite : normalCrosshairSprite;
        }));

    // Grow back with new image
    sequence.Append(crosshairRect.DOScale(Vector3.one * 1.25f, crosshairTransitionDuration * 0.3f)
        .SetEase(crosshairShowEase));

    // Shrink to normal size
    sequence.Append(crosshairRect.DOScale(Vector3.one, crosshairTransitionDuration * 0.3f)
        .SetEase(crosshairShrinkEase));

    crosshairTween = sequence;
}


    private void Update()
    {
        // *** Always update this even if its just a copy on another client ***
        //Only update the visual rope length if we are attached
        //if (isAttached)
        //{
        //    //set ropeLength to max lenght to simulate tention in the rope.
        //    // * if not here it will make the rope length == the the distance between the boat & cargo when attached *

        //    //Check if we are passed the max tow distance 
        //    if (distanceToTowedObject >= springMaxDistance)
        //    {
        //        visualRope.ropeLength = springMaxDistance;
        //    }
        //    else
        //    {
        //       // visualRope.ropeLength = distanceToTowedObject;
        //    }
        //}
        // *** Always update this even if its just a copy on another client ***

        if (isDead)// -----Do nothing if dead -----
        {
            //reset attached
            lineRenderer.enabled = false;
            return; 
        }

        if (towedObject == null || !IsOwner)
        {
           
            //Debug.LogError("towedObject is not assigned. Please assign it in the inspector.");
            return;
        }
        
        if (Time.time < lastSwitchTime + cameraBlendTime)
        {
            // Block camera input during transitions to avoid desync
            lookVector = Vector2.zero;
        }
        
        
        //Visual Rope Length
        DistanceToTowedObject = Vector3.Distance(transform.position, towedObject.position + new Vector3(0,0,12f)); // + 12 z because the center is not the center of the boat 

        //Display the distance between the Player and the Barge (For tweeking sprint settings)
        if (distanceText != null) distanceText.text = DistanceToTowedObject.ToString() + " m";

        // ====== 
        // ===Hooking Mechanic Start=== 
        // ====== 

        // ---Aim Mode ---
        //if (isAimMode && visibleHooks.Count > 0) //<-- Only allow aim if hookpoints are in view 
        Vector3 cameraForward = Camera.main.transform.forward;
        //cameraForward.y = 0f;
        //cameraForward.Normalize();

        //Quaternion newRot = Quaternion.LookRotation(cameraForward);

        // //cameraAimFollowPoint.rotation = Quaternion.LookRotation(cameraForward); // instant snap
        // cameraAimFollowPoint.forward = Camera.main.transform.forward;
        //
        // cameraAimFollowPoint.rotation = Quaternion.LookRotation(cameraForward); // instant snap

        if (isAimMode)
        {//can always aim 


            // turn on aim cam when aim button pressed
            //aimCamera.gameObject.SetActive(true);

            // Camera snap to look direction        
            //Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
            //Vector3 worldAimTarget = new Vector3(Screen.width / 2f, 0, Screen.height / 2f);
            //Vector3 worldAimTarget = aimCamera.gameObject.transform.forward;
            //worldAimTarget.y = targetAttachPoint.position.y; // 
            //Vector3 aimDirection = (worldAimTarget - transform.position).normalized;


            ////rotate the player
            //targetAttachPoint.forward = Camera.main.transform.position;//  aimDirection - Camera.main.transform.position;// Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 20.0f);


            if (DistanceToTowedObject <= maxTowDistance && !isAttached)
            {
                //Logic to determine the closest hook to the camera center 
                FindVisibleHooks();

                //lock to camera to the hooks and when the player moves they mouse it will lock onto the next hook in that direction 
                SelectClosestVisibleHook();


                if (!isAttached && visibleHooks.Count > 0 && IsOwner)
                {
                    //draw a line 
                    lineRenderer.enabled = true;
                    lineRenderer.SetPosition(0, visualRope.StartPoint.position);
                    lineRenderer.SetPosition(1, currentClosestAttachPoint.position);
                }
                else
                {
                    //reset attached
                    lineRenderer.enabled = false;
                }

                //Switch aim hook after cooldown
                if (lookVector.magnitude > 2f && Time.time >= lastHookSwitchTime + hookSwitchCooldown)
                {
                    // Example: if mouse moved right, select next hook
                    if (lookVector.x > 2f)
                    {
                        SelectNextHook(1);
                        lastHookSwitchTime = Time.time; // Reset cooldown
                    }
                    else if (lookVector.x < -2f)
                    {
                        SelectNextHook(-1);
                        lastHookSwitchTime = Time.time; // Reset cooldown
                    }
                }



            }
            else
            {

            }//end if close enough to hook 
            

        }//end if aimMode 
        else
        {
            //aimCamera.gameObject.SetActive(false);// turn off aim when not aiming  
            // Hide all UIs when not in aim mode
            foreach (var ui in hookPointUIs.Values)
            {
                ui.Hide();
            }
        }

        //Auto Mode (Only active while close)
        if (DistanceToTowedObject <= maxTowDistance) 
        {

            
            if (towedObject != null && !isAimMode)
            { //if we are close enough to hook show clossest hook point && that we are NOT in Aim mode 
                UpdateClosestAttachPoint();

               
                //aimCamera.gameObject.SetActive(false);// turn off aim when not aiming  

                //only draw the line if not already attached
                if (!isAttached && IsOwner)
                {
                    lineRenderer.enabled = true;
                    //draw a line 
                    lineRenderer.SetPosition(0, visualRope.StartPoint.position);
                    lineRenderer.SetPosition(1, currentClosestAttachPoint.position);
                }
                else
                {
                    //reset attached
                    lineRenderer.enabled = false;
                }

            }

        }
        else
        {
            //reset when out of range 
            lineRenderer.enabled = false;
            //add check for the start of the game whenre we dont have a hook point 
            if (currentClosestAttachPoint != null)
            {
                SetAttachPointHighlight(currentClosestAttachPoint, false);
            }
            //aimCamera.gameObject.SetActive(false);// turn off aim when out of range  aiming  
        }


        // ====== 
        // ===Hooking Mechanic End=== 
        // ====== 

        // Add rotation sync only during transitions
        if (isTransitioning)
        {
            if (isAimMode)
            {
                // Keep aim point synced with current camera view during transition
                cameraAimFollowPoint.rotation = Camera.main.transform.rotation;
            }
            else
            {
                // Keep freelook direction when transitioning back
                if (cameraFollowPoint != null)
                {
                    cameraFollowPoint.rotation = Camera.main.transform.rotation;
                }
            }
        }

    }//end update 

    private void LateUpdate()
    {
        //if (!IsOwner)
        //{
        //    return;
        //}
        //if (towedObject == null)
        //{
        //    towedObject = LevelVariableManager.Instance.GlobalBargeRigidBody;
        //    hookPoints = towedObject.GetComponent<TowableObjectController>().TowPointList;
        //}

        CameraRotation();
    }
    
    private void FixedUpdate()
    {
        // Only apply if we're attached and towing
        if (IsHost && isAttached && towedObject != null)
        {
            // Get the current rotation
            Quaternion currentRotation = towedObject.transform.rotation;
        
            // Create a corrected rotation that only preserves Y rotation
            Vector3 eulerAngles = currentRotation.eulerAngles;
            Quaternion targetRotation = Quaternion.Euler(0, eulerAngles.y, 0);
        
            // Apply the corrected rotation
            towedObject.MoveRotation(targetRotation);

            // Alternative approach - reset angular velocity on unwanted axes
            Vector3 angularVelocity = towedObject.angularVelocity;
            towedObject.angularVelocity = new Vector3(0, angularVelocity.y, 0);
        }
    }

   


    #region AimCameraMotion

    private void CameraRotation()
    {
        // if there is an input
        if (lookVector.sqrMagnitude >= _threshold)
        {
            //Don't multiply mouse input by Time.deltaTime;
            //float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
            float deltaTimeMultiplier = Time.deltaTime;

            yaw += lookVector.x * deltaTimeMultiplier * Sensitivity;
            pitch += -lookVector.y * deltaTimeMultiplier * Sensitivity;
        }

        // clamp our rotations so our values are limited 360 degrees
        yaw = ClampAngle(yaw, float.MinValue, float.MaxValue);
        pitch = ClampAngle(pitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        cameraAimFollowPoint.transform.rotation = Quaternion.Euler(pitch + CameraAngleOverride,
            yaw, 0.0f);

    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    #endregion


    #region AimMethods

    private void InitializeAttachPointUIs()
    {
        hookPointUIs.Clear();
    
        if (hookPoints != null)
        {
            foreach (Transform point in hookPoints)
            {
                AttachPointUI ui = point.GetComponentInChildren<AttachPointUI>();
                if (ui != null)
                {
                    hookPointUIs[point] = ui;
                }
            }
        }
    }
    
    /// <summary>
    /// Event/Action triggered method to switch to the aim camera.
    /// Now -> just toggles the aim camera + runs an initial visible hook check 
    /// Used to -> aim the camera towards the closest hookable object and allow the user to aim at the Hook they want to connect to 
    /// </summary>
    /// <param name="context"></param>
    private void OnAimTriggered(InputAction.CallbackContext context)
    {
        //check if we are close enough to hook to the barge 
        if (DistanceToTowedObject > maxTowDistance)
        {
            //aimCamera.enabled = false;
            isAimMode = false; //jsut to be safe 
            return;
        }
     

        isAimMode = true;

        //toggle the Aim Camera:
        //aimCamera.gameObject.SetActive(true);// turn off aim when not aiming  

        //Logic to determine the closest hook to the camera center 
        FindVisibleHooks();

        //lock to camera to the hooks and when the player moves they mouse it will lock onto the next hook in that direction 
        SelectClosestVisibleHook();
    }



    /// <summary>
    /// Finds and saves the hooks the player can "see" thus hook onto. Saves into List visibleHooks that is used by the Aim. (For now)
    /// </summary>
    private void FindVisibleHooks()
    {
        // Hide all UIs first
        foreach (var ui in hookPointUIs.Values)
        {
            ui.Hide();
        }
        
        visibleHooks.Clear();

        // Use the provided camera, or fall back to Camera.main if none set.
        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        Vector3 camPosition = cam.transform.position;

        foreach (Transform hook in towedObject.GetComponent<TowableObjectController>().TowPointList)
        {
            // Convert the hookpoint's position to viewport space.
            Vector3 viewportPos = cam.WorldToViewportPoint(hook.position);

            // Check if the hookpoint is in front of the camera and inside the viewport.
            bool inViewport = viewportPos.z > 0 &&
                              viewportPos.x > 0 && viewportPos.x < 1 &&
                              viewportPos.y > 0 && viewportPos.y < 1;

            if (!inViewport)
                continue;

            // Perform a raycast from the camera to the hookpoint.
            //Vector3 direction = hook.position - inViewRayPointOrigin.position;
            Vector3 direction = hook.position - camPosition;
            RaycastHit hit;

            // Draw the ray in red with a duration of 0.5 seconds.
            //Debug.DrawRay(camPosition, direction, Color.red, 1f);

            // if(Physics.Linecast(camPosition, direction, out hit))
            if (Physics.Raycast(camPosition, direction, out hit, (maxTowDistance * 2f), aimColliderLayerMask))
            {
                Debug.DrawRay(camPosition, direction, Color.magenta, 1f);
                if (hit.collider != null)
                {
                    if(hit.collider.gameObject.CompareTag("AttachPoint"))
                    {
                        // Draw the ray in red with a duration of 0.5 seconds.
                        Debug.DrawRay(camPosition, direction, Color.green, 1f);
                        visibleHooks.Add(hook);
                        
                        // Show UI for visible hook
                        if (hookPointUIs.TryGetValue(hook, out AttachPointUI ui))
                        {
                            ui.Show();
                        }
                    }else
                    {
                        // Debug.Log($"Failled collision = {hit.collider.tag}");
                        // Debug.DrawRay(camPosition, direction, Color.red, 1f);
                    }
                }else
                {
                    // Debug.Log($"Failled collision = {hit.collider.tag}");
                }
            }
        }//end foreach

    }

    /// <summary>
    /// Changes the currently selected hook point based on if its in view of the player & 
    /// the proximity to the center of the screen.
    /// </summary>
    private void SelectClosestVisibleHook()
    {
        if (visibleHooks.Count == 0)
        {
            //isAimMode = false; i can now allways aim 
            return;
        }

        // Use the provided camera, or fall back to Camera.main if none set.
        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        Vector3 camPosition = cam.transform.position;
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);

        float closestDistance = Mathf.Infinity;
        Transform closestHook = null;

        foreach (Transform hook in visibleHooks)
        {
            Vector3 viewportPos = cam.WorldToViewportPoint(hook.position);// get teh position of the hook 
            Vector2 viewport2D = new Vector2(viewportPos.x, viewportPos.y);

            float distance = Vector2.Distance(screenCenter, viewport2D);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestHook = hook;
            }
        }

        currentClosestAttachPoint = closestHook;
        HighlightSelectedHook();
    }


    /// <summary>
    /// Changes the current aimed at hook point to the next one in the visible hook points list when the player moves the mouse. (Depreciated instead use SelectClosestVisibleHook())
    /// (This method was used when the aim was hard locking to the hook points) (Depreciated but keeping incase we give the setting to turn this back on)
    /// </summary>
    /// <param name="direction"></param>
    private void SelectNextHook(int direction)
    {
        if (visibleHooks.Count == 0)
            return;

        selectedHookIndex += direction;

        if (selectedHookIndex >= visibleHooks.Count)
            selectedHookIndex = 0;
        if (selectedHookIndex < 0)
            selectedHookIndex = visibleHooks.Count - 1;

        currentClosestAttachPoint = visibleHooks[selectedHookIndex];
        //aimCamera.LookAt = visibleHooks[selectedHookIndex].gameObject.transform;

        HighlightSelectedHook();
    }

    #endregion

   
    /// <summary>
    /// sets the color of all hook point by looping through them. (
    /// If the hook == the Current selected hookpoint on the towable object (barge) 
    /// change green else change to orange.
    /// </summary>
    private void HighlightSelectedHook()
    {
        //foreach (Transform hook in visibleHooks) // <-- had an error where if the selected hook point became out of view before changing the point in would remain green
        foreach (Transform hook in hookPoints)
        {
            bool isSelected = hook == currentClosestAttachPoint;
            SetAttachPointHighlight(hook, isSelected);
        
            // Update UI highlight state
            if (hookPointUIs.TryGetValue(hook, out AttachPointUI ui))
            {
                ui.SetHighlighted(isSelected);
            }
        }
    }

    #region RPCMethods

    //=================================================================================================================================== Make Network SpringJoint Work

    // Called on the client to request a joint
    void RequestSpringJoint(GameObject player, GameObject targetObject)
    {
        var playerNetObj = player.GetComponent<NetworkObject>();
        var targetNetObj = targetObject.GetComponent<NetworkObject>();
        AttachSpringJointServerRpc(playerNetObj, targetNetObj);
    }

    [ServerRpc]
    void AttachSpringJointServerRpc(NetworkObjectReference playerRef, NetworkObjectReference targetRef)
    {
        // Resolve references on the server
        if (playerRef.TryGet(out NetworkObject playerNet) && targetRef.TryGet(out NetworkObject targetNet))
        {
            print("Attaching joint");
            var playerBody = playerNet.GetComponent<Rigidbody>();
            //var spring = playerNet.GetComponent<SpringJoint>() ?? playerNet.gameObject.AddComponent<SpringJoint>();
            var spring = playerNet.gameObject.AddComponent<SpringJoint>();
            spring.connectedBody = targetNet.GetComponent<Rigidbody>();
            // Configure spring settings as needed...
            //set Spring properties 
            spring.connectedBody = towedObject;
            spring.spring = springAmount;
            spring.damper = 1;
            spring.maxDistance = springMaxDistance;
            spring.enableCollision = true;
        }
    }

    //Detach the joint!
    void RequestDetach(GameObject player)
    {
        var playerNetObj = player.GetComponent<NetworkObject>();

        DetachSpringJointServerRpc(playerNetObj);
    }

    [ServerRpc]
    void DetachSpringJointServerRpc(NetworkObjectReference playerRef)
    {
        // Resolve references on the server
        if (playerRef.TryGet(out NetworkObject playerNet))
        {
            print("Detach joint on server");
            var playerBody = playerNet.GetComponent<Rigidbody>();
            //var spring = playerNet.GetComponent<SpringJoint>() ?? playerNet.gameObject.AddComponent<SpringJoint>();
            var spring = playerNet.GetComponent<SpringJoint>();

            //Destroy the spring on the server copy 
            Destroy(spring);

            playerNet.GetComponent<SpringTugSystem>().Detach();
            playerNet.GetComponent<SpringTugSystem>().visualRope.gameObject.SetActive(false);
        }
    }

    //=================================================================================================================================== Make Network SpringJoint Work

    //===================================================START SHOW ROPE ON ALL CLIENT INSTANCES================================================================================

    [ServerRpc]
    public void AttachRopeServerRpc(NetworkObjectReference ropeOwnerRef, int attachPositionIndex)
    {
        ShowRopeClientRpc(ropeOwnerRef, attachPositionIndex);
    }

    //Make the Rope Spawn on all clients instances of this players game object: 
    [ClientRpc]
    void ShowRopeClientRpc(ulong clientId, int attachPositionIndex)
    {
        var playerNet = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        SpringTugSystem pTugSystem = playerNet.gameObject.GetComponent<SpringTugSystem>();

        var towPoints = towedObject.gameObject.GetComponent<TowableObjectController>().TowPointList;

       
        //get the tow point at the given index 
        pTugSystem.VisualRope.EndPoint = towPoints[attachPositionIndex];
        pTugSystem.VisualRope.gameObject.SetActive(true);
    }

    [ClientRpc]
    void ShowRopeClientRpc(NetworkObjectReference ropeOwnerRef, int attachPositionIndex)
    {
        Debug.Log($"==={ropeOwnerRef}=== Attach Rope Client");

        if (ropeOwnerRef.TryGet(out NetworkObject playerNet))
        {
            SpringTugSystem pTugSystem = playerNet.GetComponent<SpringTugSystem>();

            var towPoints = towedObject.GetComponent<TowableObjectController>().TowPointList;

            // Set endpoint and activate the rope
            pTugSystem.VisualRope.EndPoint = towPoints[attachPositionIndex];
            pTugSystem.VisualRope.gameObject.SetActive(true);
        }
    }

    //===================================================END SHOW ROPE ON ALL CLIENT INSTANCES================================================================================

    //===================================================START HIDE ROPE ON ALL CLIENT INSTANCES================================================================================
    [ServerRpc]
    public void DetachRopeServerRpc(NetworkObjectReference ropeOwnerRef)
    {
        HideRopeClientRpc(ropeOwnerRef);
    }

    [ClientRpc]
    void HideRopeClientRpc(NetworkObjectReference ropeOwnerRef)
    {
        Debug.Log($"==={ropeOwnerRef}=== Attach Rope Client");

        if (ropeOwnerRef.TryGet(out NetworkObject playerNet))
        {
            SpringTugSystem pTugSystem = playerNet.GetComponent<SpringTugSystem>();

            var towPoints = towedObject.GetComponent<TowableObjectController>().TowPointList;

            //// Set endpoint and activate the rope
            //pTugSystem.VisualRope.EndPoint = towPoints[attachPositionIndex];
            pTugSystem.VisualRope.gameObject.SetActive(false);
        }
    }

    //===================================================END HIDE ROPE ON ALL CLIENT INSTANCES================================================================================

    #endregion

    #region Hook-Attach/Detach

    /// <summary>
    /// Method triggered my Hook Action ("R") that will attach and deattach the player from the barge 
    /// </summary>
    /// <param name="context"></param>
    private void OnHookTriggered(InputAction.CallbackContext context)
    {
        Debug.Log($"===Hook Triggered  isAttached == {isAttached}===");
        if (isAttached)
        {
            Detach();
        }
        else
        {
            Attach();
        }
    }



    /// <summary>
    /// Will add a Spring joint to the boat and connect it to the target object(Barge) 
    /// as well as connect the rope end point to the closest attachpoint on the target object
    /// and display the rope 
    /// </summary>
    public void Attach()
    {
        //Check everything needed is properly set
        if (towedObject == null || currentClosestAttachPoint == null)
            return;

        //if there is still a spring joint attached for some reason then remove it 
        if (springJoint != null)
            Destroy(springJoint);

        //Check if the boat is close enough to attach to the towedObject:
        if (DistanceToTowedObject <= maxTowDistance)
        {
            
            TransitionCrosshair(true);  // Transition to attached crosshair


            //Find closest attach point 
            //targetAttachPoint = FindClosestAttachPoint();
            targetAttachPoint = currentClosestAttachPoint;

            if (targetAttachPoint == null)
            {
                Debug.LogWarning("No attach point found on the towed object!");
                return;
            }

            ////Move the rope endPoint to the attachpoint 
            //visualRope.EndPoint = targetAttachPoint;

            //// display the rope:
            //visualRope.gameObject.SetActive(true);

            // StartCoroutine("AttachRopeEffect");
            AttachRopeWEffect();

             var towPoints = towedObject.gameObject.GetComponent<TowableObjectController>().TowPointList;

            int attachPointIndex = towPoints.IndexOf(targetAttachPoint);


            //AttachRopeServerRpc(OwnerClientId, index);
            AttachRopeServerRpc(NetworkObject, attachPointIndex);


            //only the host can derectly attach the spring joint 
            if (!IsServer)
            {
                //request to attach a joint on the server side copy of the client game object 
                RequestSpringJoint(this.gameObject, towedObject.gameObject);
            }

            //ALWAYS create a spring locally as well:

            //Create and attach the Spring Joint 
            springJoint = gameObject.AddComponent<SpringJoint>(); // attach a spring joint to this gameobject plus set it as springJoint

            //set Spring properties 
            springJoint.connectedBody = towedObject;
            springJoint.spring = springAmount;
            springJoint.damper = 1;
            springJoint.maxDistance = springMaxDistance;
            springJoint.enableCollision = true;


            //Set attached state 
            isAttached = true;
        
            // Hide all UIs when attached
            foreach (var ui in hookPointUIs.Values)
            {
                ui.Hide();
            }

        }else
        {
            // attach failled VFX + SFX due to boat being to far from target 
            Debug.LogWarning("Attach failed: too far from target.");
        }
        
        // Hide all UIs when attached
        foreach (var ui in hookPointUIs.Values)
        {
            ui.Hide();
        }
    }

    /// <summary>
    /// Will Detach the boat from the barge by removing the Spring joint and hidding the rope.
    /// </summary>
    public void Detach()
    {
        TransitionCrosshair(false);  // Transition to normal crosshair
        
        if (springJoint != null)
        {
            //Disconnect local Joint
            Destroy(springJoint);

            //Hide Rope Mesh:
            //visualRope.gameObject.GetComponent<RopeMesh>().enabled = false;
            //or just the gameobject itself 
            //visualRope.ropeLength = 5f; // this should create a cool effect 
            //visualRope.gameObject.SetActive(false);
            
            DetatchRopeWEffect();

            
            //Make sure the Spring joint is destroyed on the Server side copy of the player as well 
            if (!IsServer)
            {
                //request to attach a joint on the server side copy of the client game object 
                RequestDetach(this.gameObject);
            }

        }

        isAttached = false; // no longer attached 
        
        // If still in aim mode, show UIs again
        if (isAimMode)
        {
            FindVisibleHooks();
            SelectClosestVisibleHook();
        }
    }


    private void AttachRopeWEffect()
    {
        // Add screen shake when hooking
        if (boatMovement != null)
        {
            boatMovement.GenerateScreenShake(0.4f);
        }
        
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.HookShoot, transform.position);

        
        visualRope.gameObject.SetActive(true);

        visualRope.EndPoint = boatHook.transform; // this should create a cool effect when next attaching 

        //Move the rope endPoint to the attachpoint 
        visualRope.EndPoint = targetAttachPoint;

        visualRope.ropeLength = springMaxDistance; // this should create a cool effect when next attaching 
       
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.HookAttach, targetAttachPoint.position);
        AudioManager.Instance.StartLoop(FMODEvents.Instance.HookIdle, visualRope.gameObject);

       //wait for a second before disableing the hook?

        boatHook.SetActive(false); // make sure it off when hooked in 
    }


    private void DetatchRopeWEffect()
    {
        visualRope.ropeLength = springMaxDistance; // this should create a cool effect when next attaching 
        visualRope.EndPoint = boatHook.transform; // this should create a cool effect when next attaching 
        visualRope.gameObject.SetActive(false);
        
        // Play detach sound and stop idle loop
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.HookDetach, transform.position);
        AudioManager.Instance.StopLoop();

        boatHook.SetActive(true); // make sure it active 

        
        DetachRopeServerRpc(NetworkObject);
    }

    #endregion

    #region AutoHookConnection

    /// <summary>
    /// Finds the closest attach point (Transform) on the towed object.
    /// Assumes that attach points are children with a specific tag or are grouped.
    /// </summary>
    private Transform FindClosestAttachPoint()
    {
        Transform closestPoint = null;
        float closestDistance = Mathf.Infinity;

        var towPoints = towedObject.gameObject.GetComponent<TowableObjectController>().TowPointList;

        //foreach (Transform towPoint in towedObject.gameObject.GetComponent<TowableObjectController>().TowPointList)
        foreach (Transform towPoint in towPoints)
        {
            if (towPoint.CompareTag("AttachPoint"))
            {
                //Find Distance 
                float distance = Vector3.Distance(transform.position, towPoint.position);
                //set closest if closer 
                if (distance < closestDistance)
                {  
                    closestDistance = distance;
                    closestPoint = towPoint;
                    selectedHookIndex = towedObject.gameObject.GetComponent<TowableObjectController>().TowPointList.IndexOf(closestPoint);
                }
            }
        }

        return closestPoint;
    }

    /// <summary>
    /// will set the current closes tow point and reset old closest point 
    /// </summary>
    private void UpdateClosestAttachPoint()
    {
        if (towedObject != null)
        {
            Transform closest = FindClosestAttachPoint();

            if (closest != currentClosestAttachPoint)
            {
                // Unhighlight last one
                if (currentClosestAttachPoint != null)
                {
                    SetAttachPointHighlight(currentClosestAttachPoint, false);
                }

                // Highlight new one
                if (closest != null)
                {
                    SetAttachPointHighlight(closest, true);
                }

                currentClosestAttachPoint = closest;
            }
        }
    }

    /// <summary>
    /// Changes the material of the attachpoint base on bool 
    /// </summary>
    /// <param name="attachPoint"></param>
    /// <param name="highlight"></param>
    private void SetAttachPointHighlight(Transform attachPoint, bool highlight)
    {
        // Assume the attach points have a MeshRenderer
        MeshRenderer renderer = attachPoint.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = highlight ? highlightMaterial : defaultMaterial;
        }
    }

    #endregion

    #region Debuging

    private void OnDrawGizmos()
    {
        if (springJoint != null)
        {
            Gizmos.color = Color.black;
            //Gizmos.DrawSphere(transform.position, maxTowDistance);

            //foreach (Transform hook in towedObject.GetComponent<TowableObjectController>().TowPointList)
            //{              
            //    Gizmos.DrawLine(transform.position, hook.position);
            //}

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, currentClosestAttachPoint.position);


            Gizmos.DrawSphere(currentClosestAttachPoint.position, 0.1f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(springJoint.connectedAnchor, 5f);


        }
    }

    #endregion

}
