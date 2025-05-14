using System;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;


/// <summary>
/// Tugboat controller using Rigidbody, HDRP Custom Water, and reverse-compatible thrust.
/// Suitable for towing heavy vessels.
/// https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/water-deform-a-water-surface.html#bow-wave
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class TugboatMovementWFloat : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI debugSpeedText;

    [Header("Thrust & Speed")]
    public float maxSpeed = 8f;
    //public float boostMaxSpeed = 100f;
    public float throttleForce = 2000f;
    public float throttleForceMultiplier = 10f;
    [HideInInspector] public float defaultThrottleForceMultiplier = 10f;
    public float reverseThrottleFactor = 0.5f;    // Less power in reverse
    public float accelerationSmoothing = 2f;

    [Header("Movement Effects")]
    [SerializeField] WaterDecal bowWaveDecal; 
    [SerializeField] Material bowWaveMaterial; 
    [SerializeField] WaterFoamGenerator foamGenerator; 
    [SerializeField] VisualEffect rearSplashVFX;
    [SerializeField] private Material speedLinesMaterial;
    
    [Header("Ship Sails")]
    [SerializeField] private GameObject[] sailObjects; // Changed from Material[] sailMaterials
    private Material[] _sailMaterials; // Will store extracted materials
    private float _currentSailStrength = 0.2f;
    

    private Vector3 _dampVelocity = Vector3.zero;

    [Header("Turning")]
    public float turnTorque = 1500f;
    public float turnResponsiveness = 3f;

    [Header("Camera Settings")]
    [SerializeField] private Transform cameraRig; // Drag your CameraRig here in Inspector
    [SerializeField] private float lookSensitivity = 1.5f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Physics")]
    public float dragWhenNotMoving = 2f;
    public float angularDrag = 0.5f;

    //flaoting/movement Damping
    [Tooltip("How long (in seconds) it takes to catch up to the true water height.")]
    [SerializeField] float dampingTime = 0.2f;

    [Space(10)]
    [Header("Float")]
    [SerializeField] private float3 floatingOffset;

    public WaterSurface targetSurface = null;
    public bool includeDeformation = true;
    public bool excludeSimulation = false;

    [Header("Orientation")]
    public bool alignToWaterNormal = true;
    [Range(0f, 10f)] public float orientationSmoothSpeed = 5f;

    private Vector3 previousPosition;
    [SerializeField] private float pitchAngleMultiplier = 2f;
    [SerializeField] private float rollAngleMultiplier = 3f;
    // Internal search params
    WaterSearchParameters searchParameters = new WaterSearchParameters();
    WaterSearchResult searchResult = new WaterSearchResult();


    private Rigidbody rb;
    private Vector2 moveVector;
    private Vector2 lookVector;
    private float yaw;
    private float pitch;
    private float targetThrottle;
    private float currentThrottle;

    private BoatInputActions controls;
    
    [SerializeField] private CinemachineInputAxisController inputProvider;


    private readonly NetworkVariable<float> _syncedSpeed = new NetworkVariable<float>(0f, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.angularDamping = angularDrag;

        controls = new BoatInputActions();
        //Movement input
        controls.Boat.Move.performed += ctx => moveVector = ctx.ReadValue<Vector2>();
        controls.Boat.Move.canceled += _ => moveVector = Vector2.zero; // remove this to enable Toggle speed 
        
        //Look input
        controls.Boat.Look.performed += ctx => lookVector = ctx.ReadValue<Vector2>();
        controls.Boat.Look.canceled += _ => lookVector = Vector2.zero; // remove this to enable camera drift 
    }

    private void Start()
    {
        // Existing code
        defaultThrottleForceMultiplier = throttleForceMultiplier;

        // If targetSurface is not set, find the Ocean game object and get its WaterSurface component
        if (targetSurface == null)
        {
            targetSurface = JR_NetBoatRequiredComponentsSource.Instance.GlobalWaterSurface;
        }
    
        // Extract materials from sail GameObjects
        if (sailObjects != null && sailObjects.Length > 0)
        {
            List<Material> materials = new List<Material>();
            foreach (GameObject sail in sailObjects)
            {
                if (sail != null)
                {
                    Renderer renderer = sail.GetComponent<Renderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        materials.Add(renderer.material);
                    }
                }
            }
            _sailMaterials = materials.ToArray();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (targetSurface == null)
        {
            InitializeWaterTarget();
        }
    }

    void OnEnable()
    {
        if (targetSurface == null)
        {
            targetSurface = JR_NetBoatRequiredComponentsSource.Instance.GlobalWaterSurface; // get the water surface 
        }
        controls.Enable();
    }


    void OnDisable()
    {
        controls.Disable();
    }
    
    public void SetControlEnabled(bool enabled)
    {
        if (enabled)
        {
            controls.Enable();
        
            // Enable Cinemachine input
            if (inputProvider != null)
                inputProvider.enabled = true;
        }
        else
        {
            controls.Disable();
            moveVector = Vector2.zero;
            lookVector = Vector2.zero;
        
            // Disable Cinemachine input
            if (inputProvider != null)
                inputProvider.enabled = false;
        }
    }

    private void Update()
    {
        // Local player logic
        if (IsOwner)
        {
            float speed = rb.linearVelocity.magnitude - 10f;
            if (speed <= 0.4f) speed = 0f;
        
            // Update network variable with current speed
            _syncedSpeed.Value = speed;
        
            // Display speed in UI
            if (debugSpeedText != null)
                debugSpeedText.text = $"Speed: {speed:0}m/s";
            
            // Apply visual effects for local player using local calculations
            ApplyMovementEffects(speed);
        }
        // Remote player logic
        else
        {
            // Apply visual effects for remote player using network-synced speed
            ApplyMovementEffects(_syncedSpeed.Value);
        }
    }
    
    private void ApplyMovementEffects(float currentSpeed)
    {
        // Normalize speed to a value between 0 and 1
        float normalizedSpeed = Mathf.InverseLerp(0, maxSpeed - 10f, currentSpeed);

        // == Wave Bow Effect ==
        
        // Smoothly transition amplitude between 0 and 3 based on normalized speed
        bowWaveDecal.amplitude = Mathf.Lerp(0, 3, normalizedSpeed);
        
        // Smoothly transition bow wave decal region size
        Vector2 minSize = new(30.5f, 43f);
        Vector2 maxSize = new(45f, 55f);
        bowWaveDecal.regionSize = Vector2.Lerp(minSize, maxSize, normalizedSpeed);

        // == Water Splash Effect ==
        if (currentSpeed > 1.0f && targetSurface != null)
        {
            // Get current position of the splash effect
            Vector3 splashPosition = rearSplashVFX.transform.position;
    
            // Create search parameters just for the splash
            WaterSearchParameters splashParams = new WaterSearchParameters
            {
                startPositionWS = splashPosition + Vector3.up * 2f,
                targetPositionWS = splashPosition,
                includeDeformation = includeDeformation,
                excludeSimulation = excludeSimulation,
                error = 0.01f,
                maxIterations = 4
            };

            // Find water height at splash position
            if (targetSurface.ProjectPointOnWaterSurface(splashParams, out WaterSearchResult splashResult))
            {
                // Only update the Y position to match water height
                splashPosition.y = splashResult.projectedPositionWS.y;
                rearSplashVFX.transform.position = splashPosition;
            }
    
            // Set VFX size based on speed
            float splashSize = Mathf.Lerp(0.3f, 2.5f, normalizedSpeed);
            rearSplashVFX.SetFloat("Size", splashSize);
            rearSplashVFX.gameObject.SetActive(true);
        }
        else
        {
            rearSplashVFX.gameObject.SetActive(false);
        }

        // == Sails Effect ==
        
        // Calculate sail strength but round to 2 decimal places
        float rawStrength = Mathf.Lerp(0.2f, 0.8f, normalizedSpeed);
        float roundedStrength = Mathf.Round(rawStrength * 100) / 100f; // Round to 2 decimal places (0.XX)
    
        // Only update if the rounded value actually changed
        if (Mathf.Abs(_currentSailStrength - roundedStrength) > 0.001f)
        {
            _currentSailStrength = roundedStrength;
        
            if (_sailMaterials != null)
            {
                foreach (Material sail in _sailMaterials)
                {
                    if (sail != null)
                    {
                        sail.SetFloat("_Strength", _currentSailStrength);
                    }
                }
            }
        }
        
        // == Speed Lines Effect ==
        
        // Handle speed lines material
        if (speedLinesMaterial != null)
        {
            // Calculate density directly from sail strength rather than separately from speed
            float linesDensity = Mathf.InverseLerp(0.2f, 0.8f, _currentSailStrength) * 0.33f;
            speedLinesMaterial.SetFloat("_Lines_Density", linesDensity);
        }
    }

    void FixedUpdate()
    {

        if (!IsOwner)
        {
            return;
        }

        HandleThrottle();
        HandleTurning();
        //HandleDrag();


        if (targetSurface == null)
            return;


        // Build the search parameters
        searchParameters.startPositionWS = searchResult.candidateLocationWS;
        searchParameters.targetPositionWS = gameObject.transform.position;
        searchParameters.error = 0.01f;
        searchParameters.maxIterations = 8;
        searchParameters.includeDeformation = includeDeformation;
        searchParameters.excludeSimulation = excludeSimulation;

        // Do the search
        if (targetSurface.ProjectPointOnWaterSurface(searchParameters, out searchResult))
        {
            //Attempt Smoother movement
            //Vector3 currentPositon = transform.position;
            //gameObject.transform.position = new Vector3(currentPositon.x, Mathf.Lerp(transform.position.y, searchResult.projectedPositionWS.y, surfaceStickLerpAmount), currentPositon.z); //
            // OLD WAY --->>>
            //gameObject.transform.position = searchResult.projectedPositionWS;
            
            //damping fit to surface to filter small waves 
            transform.position = Vector3.SmoothDamp(
                      transform.position,                   // current position
                      searchResult.projectedPositionWS,     // target on-water position
                      ref _dampVelocity,                    // velocity state
                      dampingTime);                         // smoothing time
        }

        //Align the boat to the water 
        if (alignToWaterNormal)
        {
            AlignToWaterNormal(searchResult.normalWS);
        }
    }

    /// <summary>
    /// Get the water surface when spawned 
    /// </summary>
    private void InitializeWaterTarget()
    {
        if (targetSurface == null)
        {
            //Get teh Scenes Water Surface 
            targetSurface = JR_NetBoatRequiredComponentsSource.Instance.GlobalWaterSurface; 

            // Log an error if still not found.
            if (targetSurface == null)
            {

                Debug.LogError("WaterSurface component not found. Ensure an object named Ocean or a WaterSurface instance exists in the scene.", this);
            }
        }
    }


    private void HandleThrottle()
    {
        float vertical = moveVector.y;
        targetThrottle = vertical;

        // Smoothing for gradual thrust changes (prevents jerky trailer movement)
        currentThrottle = Mathf.MoveTowards(currentThrottle, targetThrottle, accelerationSmoothing * Time.fixedDeltaTime);

        // Throttle adjusted for direction
        float adjustedForce = throttleForce * currentThrottle * throttleForceMultiplier;
        if (currentThrottle < 0)
            adjustedForce *= reverseThrottleFactor;

        Vector3 force = transform.forward * adjustedForce * Time.fixedDeltaTime;
        rb.AddForce(force, ForceMode.Force);

        // Limit max resistence (for tugboat realism)
        Vector3 flatVel = rb.linearVelocity;
        flatVel.y = 0f;
        if (flatVel.magnitude > maxSpeed)
        {
            Vector3 limited = flatVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(limited.x, rb.linearVelocity.y, limited.z);
        }
    }

    private void HandleTurning()
    {
        float turnInput = moveVector.x;

        // Only allow turn input when there is forward/reverse motion
        if (Mathf.Abs(currentThrottle) > 0.05f)
        {
            float torqueAmount = turnInput * turnTorque * Time.fixedDeltaTime;
            rb.AddTorque(Vector3.up * torqueAmount, ForceMode.Force);
        }
    }

    private void HandleDrag()
    {
        // Apply higher drag if throttle is idle to resist drifting when towing
        if (Mathf.Approximately(currentThrottle, 0f))
            rb.linearDamping = dragWhenNotMoving;
        else
            rb.linearDamping = 0f;
    }


    /// <summary>
    /// method to align the gameobject to the water surface with damping 
    /// </summary>
    /// <param name="waterNormal"></param>
    private void AlignToWaterNormal(float3 waterNormal)
    {
        //Vector3 velocity = (transform.position - previousPosition) / Time.deltaTime;
        //previousPosition = transform.position;


        // Flatten for horizontal velocity
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        float forwardSpeed = localVelocity.z;
        float sideSpeed = localVelocity.x;

        // Simulate pitch (nose up/down from forward acceleration)
        float pitchAngle = -forwardSpeed * pitchAngleMultiplier;

        // Simulate roll (lean into turns / side drift)
        float rollAngle = sideSpeed * rollAngleMultiplier;

        Quaternion targetTilt = Quaternion.Euler(pitchAngle, 0f, rollAngle);

        // Base upright rotation from water normal
        Vector3 forwardProjected = Vector3.ProjectOnPlane(transform.forward, waterNormal).normalized;
        Quaternion waterAligned = Quaternion.LookRotation(forwardProjected, waterNormal);

        // Combine boat tilt + wave normal
        Quaternion targetRotation = waterAligned * targetTilt;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, orientationSmoothSpeed * Time.deltaTime);

    }

    // private void OnApplicationFocus(bool focus)
    // {
    //     if(SceneManager.GetActiveScene().name != "MainMenu")
    //     Cursor.lockState = CursorLockMode.Locked;
    //     
    // }


   
}
