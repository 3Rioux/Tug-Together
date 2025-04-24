using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;


/// <summary>
/// Tugboat controller using Rigidbody, HDRP Custom Water, and reverse-compatible thrust.
/// Suitable for towing heavy vessels.
/// https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/water-deform-a-water-surface.html#bow-wave
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class TugboatMovementWFloat : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugSpeedText;

    [Header("Thrust & Speed")]
    public float maxSpeed = 8f;
    public float throttleForce = 2000f;
    public float reverseThrottleFactor = 0.5f;    // Less power in reverse
    public float accelerationSmoothing = 2f;

    [Header("Turning")]
    public float turnTorque = 1500f;
    public float turnResponsiveness = 3f;

    [Header("Physics")]
    public float dragWhenNotMoving = 2f;
    public float angularDrag = 2f;

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
    private Vector2 inputVector;
    private float targetThrottle;
    private float currentThrottle;

    private BoatInputActions controls;



    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.angularDamping = angularDrag;

        controls = new BoatInputActions();
        controls.Boat.Move.performed += ctx => inputVector = ctx.ReadValue<Vector2>();
        controls.Boat.Move.canceled += _ => inputVector = Vector2.zero;
    }


    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    private void Update()
    {
        debugSpeedText.text = $"Speed: {rb.linearVelocity.magnitude:0} m/s"; //display current resistence 
       
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
            gameObject.transform.position = searchResult.projectedPositionWS + floatingOffset;

            
        }
    }

    void FixedUpdate()
    {
        HandleThrottle();
        HandleTurning();
        //HandleDrag();

        //Align the boat to the water 
        if (alignToWaterNormal)
        {
            AlignToWaterNormal(searchResult.normalWS);
        }
    }


    private void HandleThrottle()
    {
        float vertical = inputVector.y;
        targetThrottle = vertical;

        // Smoothing for gradual thrust changes (prevents jerky trailer movement)
        currentThrottle = Mathf.MoveTowards(currentThrottle, targetThrottle, accelerationSmoothing * Time.fixedDeltaTime);

        // Throttle adjusted for direction
        float adjustedForce = throttleForce * currentThrottle;
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
        float turnInput = inputVector.x;

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
