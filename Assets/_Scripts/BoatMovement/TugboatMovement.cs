using UnityEngine;


/// <summary>
/// Tugboat controller using Rigidbody, HDRP Water Buoyancy (assumed), and reverse-compatible thrust.
/// Suitable for towing heavy vessels.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class TugboatMovement : MonoBehaviour
{
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

    void FixedUpdate()
    {
        HandleThrottle();
        HandleTurning();
        HandleDrag();
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

        // Limit max speed (for tugboat realism)
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







}
