using System;
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
        //set the default throttle force Multiplier to the currently set throttleForceMultiplier
        defaultThrottleForceMultiplier = throttleForceMultiplier;

        // If targetSurface is not set, find the Ocean game object and get its WaterSurface component ALWAYS FOR ALL PLAYERS 
        if (targetSurface == null)
        {
            targetSurface = JR_NetBoatRequiredComponentsSource.Instance.GlobalWaterSurface; // get the water surface 
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
        if (!IsOwner) return;

        if (debugSpeedText == null)
        {
            //Debug.LogWarning("debugSpeedText is not assigned in the inspector.");
            return;
        }


        float speed = rb.linearVelocity.magnitude - 10f;
        if (speed <= 0.4f) speed = 0f;
        debugSpeedText.text = $"Speed: {speed:0}m/s";


        ApplyMovementEffects(speed);

        ////look Controls:
        //if (lookVector != Vector2.zero)
        //{
        //    yaw += lookVector.x * lookSensitivity * Time.deltaTime;
        //    pitch -= lookVector.y * lookSensitivity * Time.deltaTime;
        //    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        //    cameraRig.rotation = Quaternion.Euler(pitch, yaw, 0f);
        //}

    }

    /// <summary>
    /// Apply settings to the boats visual effects based on the boats speed 
    /// </summary>
    /// <param name="currentSpeed"></param>
    private void ApplyMovementEffects(float currentSpeed)
    {


        // Normalize speed to a value between 0 and 1
        float normalizedSpeed = Mathf.InverseLerp(0, maxSpeed - 10f, currentSpeed);

         // Smoothly transition amplitude between 0 and 2 based on normalized speed
        bowWaveDecal.amplitude = Mathf.Lerp(0, 3, normalizedSpeed);
        //floatingOffset = new float3(0, Mathf.Lerp(0, -0.5f, normalizedSpeed), 0);

        if (currentSpeed <= 1f)
        {
            //bowWaveDecal.gameObject.GetComponent<Material>().shader.Get .FindPropertyIndex("")
            //bowWaveMaterial.SetFloat("_Elevation", 0f);
            //bowWaveDecal.amplitude = 0;
            bowWaveDecal.regionSize = new Vector2(0, 25);

            //Conter the Bow wave Ampliture surface float:
            floatingOffset = new float3(0, 0, 0);

            //VFX
            rearSplashVFX.gameObject.SetActive(false);

        }
        else if (currentSpeed <= 10f)
        {
            //bowWaveMaterial.SetFloat("_Elevation", 0.5f);
           // bowWaveDecal.amplitude = 1f;
            bowWaveDecal.regionSize = new Vector2(11, 25);

            //Conter the Bow wave Ampliture surface float:
            //floatingOffset = new float3(0, -0.5f, 0);

            //VFX
            rearSplashVFX.gameObject.SetActive(true);
        }
        else if (currentSpeed > 10f)
        {
            // bowWaveMaterial.SetFloat("_Elevation", 1f);
           // bowWaveDecal.amplitude = 2f;
            bowWaveDecal.regionSize = new Vector2(17, 25);

            //Conter the Bow wave Ampliture surface float:
            //floatingOffset = new float3(0, -1f, 0);

            //VFX
            rearSplashVFX.gameObject.SetActive(true);
        }


    }
    [SerializeField] float surfaceStickLerpAmount = 1f;

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
            gameObject.transform.position = searchResult.projectedPositionWS;
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

    private void OnApplicationFocus(bool focus)
    {
        if(SceneManager.GetActiveScene().name != "MainMenu")
        Cursor.lockState = CursorLockMode.Locked;
        
    }


   
}
