using UnityEngine;

/// <summary>
/// Tugboat physics controller with rudder-based steering, heading alignment, and max speed
///  buoyancy, and wave simulation
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BoatMovementV1 : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 10f;               // Max speed in meters/sec
    [SerializeField] private float throttleForce = 1000f;        // Forward/backward propulsion
    //[SerializeField] private float turnTorque = 200f;          // Left/right turning power
    [SerializeField] private float maxRudderAngle = 30f;         // Max rudder angle in degrees
    [SerializeField] private float rudderTurnSpeed = 45f;        // Degrees per second
    [SerializeField] private float turnForceMultiplier = 10f;    // How strongly the rudder affects the boat's direction
    [SerializeField] private float turnAlignSpeed = 2f;          // How quickly the boat aligns to its direction


    [Header("Buoyancy")]
    public float waterLevel = 0f;
    public float buoyancyStrength = 10f;
    public float buoyancyDamping = 0.5f;
    public float waveAmplitude = 1f;
    public float waveFrequency = 0.5f;
    public float waveSpeed = 1f;

    private Rigidbody _rb; // this rigid body component

    private float currentRudderAngle = 0f;      // Current rudder deflection
    private Vector2 inputVector = Vector2.zero;
    //private float _throttle;                    // current throttle amount
    //private float _rudderInput;                 // current turn amount 


    private BoatInputActions controls;

    void Awake()
    {
        controls = new BoatInputActions();
        controls.Boat.Move.performed += ctx => inputVector = ctx.ReadValue<Vector2>();
        controls.Boat.Move.canceled += _ => inputVector = Vector2.zero;
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    void Start()
    {
        _rb = GetComponent<Rigidbody>(); // get it from this gameobject
    }

    //private void Update()
    //{
    //    // Get input
    //    _throttle = Input.GetAxis("Vertical");  // W/S or Up/Down Arrow
    //    //_rudderInput = Input.GetAxis("Horizontal");    // A/D or Left/Right Arrow
    //    _rudderInput = Input.Ke(KeyCode.A);    // A/D or Left/Right Arrow
    //}


    private void FixedUpdate()
    {
        ApplyBuoyancy();
        ApplyThrustAndSteering();
        AlignWithlinearVelocity();
    }


    private void ApplyThrustAndSteering()
    {
        float throttle = inputVector.y;
        float rudderInput = inputVector.x;

        float targetRudder = rudderInput * maxRudderAngle;
        currentRudderAngle = Mathf.MoveTowards(currentRudderAngle, targetRudder, rudderTurnSpeed * Time.fixedDeltaTime);

        // Thrust
        Vector3 propulsion = transform.forward * throttle * throttleForce * Time.fixedDeltaTime;
        _rb.AddForce(propulsion);

        // Clamp max speed
        if (_rb.linearVelocity.magnitude > maxSpeed)
            _rb.linearVelocity = _rb.linearVelocity.normalized * maxSpeed;

        // Rudder direction
        Vector3 rudderForce = Quaternion.Euler(0f, currentRudderAngle, 0f) * transform.forward;
        rudderForce *= throttle * turnForceMultiplier;
        _rb.AddForce(rudderForce, ForceMode.Force);
    }

    private void ApplyBuoyancy()
    {
        float waveY = GetWaveHeight(transform.position.x, transform.position.z, Time.time);
        float submergedDepth = Mathf.Clamp01((waveY - transform.position.y));

        if (submergedDepth > 0f)
        {
            float upwardForce = buoyancyStrength * submergedDepth;
            Vector3 damping = -_rb.linearVelocity * buoyancyDamping;
            _rb.AddForce(Vector3.up * upwardForce + damping, ForceMode.Acceleration);
        }
    }


    private float GetWaveHeight(float x, float z, float t)
    {
        return waterLevel + Mathf.Sin((x + t * waveSpeed) * waveFrequency) * waveAmplitude;
    }

    private void AlignWithlinearVelocity()
    {
        Vector3 horizontallinearVelocity = _rb.linearVelocity;
        horizontallinearVelocity.y = 0f;

        if (horizontallinearVelocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontallinearVelocity.normalized, Vector3.up);
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, turnAlignSpeed * Time.fixedDeltaTime));
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, Vector3.forward * 25f);
    }

}
