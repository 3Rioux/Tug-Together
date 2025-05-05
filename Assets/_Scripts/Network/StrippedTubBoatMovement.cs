using System;
using System.Collections;
using TMPro;
using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;


/// <summary>
/// Tugboat controller using Rigidbody, HDRP Custom Water, and reverse-compatible thrust.
/// Suitable for towing heavy vessels.
/// https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/water-deform-a-water-surface.html#bow-wave
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class StrippedTubBoatMovement : NetworkBehaviour
{
    // [Header("Camera")]
    // [SerializeField] private GameObject playerCamera;
    
    [Header("Thrust & Speed")]
    [Tooltip("Sets the capped forward speed in meters per second.")]
    public float maxSpeed = 8f;

    [Tooltip("Base force applied when moving forward.")]
    public float throttleForce = 2000f;

    [Tooltip("Multiplier to enhance responsiveness of throttle.")]
    public float throttleForceMultiplier = 10f;

    [Tooltip("Reduces throttle force when moving in reverse.")]
    public float reverseThrottleFactor = 0.5f;

    [Tooltip("Smoothing factor for gradual acceleration.")]
    public float accelerationSmoothing = 2f;

    [Header("Turning")]
    [Tooltip("Torque applied when turning.")]
    public float turnTorque = 1500f;

    [Tooltip("Responsiveness of the boat turning.")]
    public float turnResponsiveness = 3f;

    [Header("Camera Settings")]
    [Tooltip("Reference to the camera rig.")]
    [SerializeField] private Transform cameraRig;

    [Tooltip("Sensitivity for camera look adjustments.")]
    [SerializeField] private float lookSensitivity = 1.5f;

    [Tooltip("Minimum vertical angle for camera pitch.")]
    [SerializeField] private float minPitch = -30f;

    [Tooltip("Maximum vertical angle for camera pitch.")]
    [SerializeField] private float maxPitch = 60f;

    [Header("Physics")]
    [Tooltip("Drag force applied when the boat is not moving.")]
    public float dragWhenNotMoving = 2f;

    [Tooltip("Angular drag applied for rotational damping.")]
    public float angularDrag = 0.5f;

    [Space(10)]
    [Header("Float")]
    [Tooltip("Offset used for adjusting the boat's floating position.")]
    [SerializeField] private float3 floatingOffset;

    [Tooltip("Reference to the WaterSurface instance for water projection.")]
    public WaterSurface targetSurface = null;

    [Tooltip("Include water deformation effects during projection.")]
    public bool includeDeformation = true;

    [Tooltip("Exclude water simulation in projection calculations.")]
    public bool excludeSimulation = false;

    [Header("Orientation")]
    [Tooltip("Aligns the boat to the normal of the water surface.")]
    public bool alignToWaterNormal = true;

    [Tooltip("Smooth speed factor for rotating the boat to align with the water.")]
    [Range(0f, 10f)]
    public float orientationSmoothSpeed = 5f;

    [Tooltip("Stores the previous position for movement calculations.")]
    private Vector3 previousPosition;

    [Tooltip("Multiplier affecting the tilt (pitch) based on forward speed.")]
    [SerializeField] private float pitchAngleMultiplier = 2f;

    [Tooltip("Multiplier affecting the tilt (roll) during lateral movement.")]
    [SerializeField] private float rollAngleMultiplier = 3f;

    // Internal search params
    [Tooltip("Parameters for searching the water surface.")]
    WaterSearchParameters searchParameters = new WaterSearchParameters();

    [Tooltip("Result of the water surface projection search.")]
    WaterSearchResult searchResult = new WaterSearchResult();

    private Rigidbody rb;
    private Vector2 moveVector;
    private Vector2 lookVector;
    private float yaw;
    private float pitch;
    private float targetThrottle;
    private float currentThrottle;

    private BoatInputActions controls;



// Add this field to your class
    private CinemachineInputAxisController inputProvider;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.angularDamping = angularDrag;

        // Get the input provider from the camera rig
        if (cameraRig != null)
        {
            inputProvider = cameraRig.GetComponentInChildren<CinemachineInputAxisController>();
            if (inputProvider == null)
            {
                Debug.LogWarning("No CinemachineInputProvider found on camera rig", this);
            }
        }

        controls = new BoatInputActions();
        //Movement input
        controls.Boat.Move.performed += ctx => moveVector = ctx.ReadValue<Vector2>();
        controls.Boat.Move.canceled += _ => moveVector = Vector2.zero; 

        //Look input
        controls.Boat.Look.performed += ctx => lookVector = ctx.ReadValue<Vector2>();
        controls.Boat.Look.canceled += _ => lookVector = Vector2.zero;
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
    
    private void Start()
    {
        // Also verify on Start
        if (targetSurface == null)
        {
            InitializeWaterTarget();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (targetSurface == null)
        {
            InitializeWaterTarget();
        }
    }
    
    
    private void InitializeWaterTarget()
    {
        if (targetSurface == null)
        {
            // Try to find an object named Ocean.
            GameObject ocean = GameObject.Find("Ocean");
            if (ocean != null)
            {
                targetSurface = ocean.GetComponent<WaterSurface>();
                Debug.Log("WaterSurface found on Ocean object: " + ocean.name);
            }
        
            // Fallback: search for any WaterSurface instance in the scene.
            if (targetSurface == null)
            {
                targetSurface = FindObjectOfType<WaterSurface>();
                if (targetSurface != null)
                {
                    Debug.Log("WaterSurface found via FindObjectOfType on: " + targetSurface.gameObject.name);
                }
            }

            //targetSurface = JR_NetWaterSync.instance.GlobalWaterSurface; // get the water surface 

            // Log an error if still not found.
            if (targetSurface == null)
            {

                Debug.LogError("WaterSurface component not found. Ensure an object named Ocean or a WaterSurface instance exists in the scene.", this);
            }
        }
    }

    void OnEnable()
    {
        // if (targetSurface == null)
        // {
        //     targetSurface = JR_NetWaterSync.instance.GlobalWaterSurface; // get the water surface 
        // }
        controls.Enable();
    }

   
    void OnDisable() => controls.Disable();

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
            gameObject.transform.position = searchResult.projectedPositionWS;
        }

        //Align the boat to the water 
        if (alignToWaterNormal)
        {
            AlignToWaterNormal(searchResult.normalWS);
        }
    }

    // private void LateUpdate()
    // {
    //     if (targetSurface == null)
    //     {
    //         targetSurface = JR_NetWaterSync.instance.GlobalWaterSurface; // get the water surface 
    //     }
    // }


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

}
