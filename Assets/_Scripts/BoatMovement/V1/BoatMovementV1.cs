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

    private void Update()
    {
        // Get input
        _throttle = Input.GetAxis("Vertical");  // W/S or Up/Down Arrow
        //_rudderInput = Input.GetAxis("Horizontal");    // A/D or Left/Right Arrow
        _rudderInput = Input.Ke(KeyCode.A);    // A/D or Left/Right Arrow
    }


    void FixedUpdate()
    {
        // Smoothly rotate rudder toward input direction
        float targetAngle = _rudderInput * maxRudderAngle;
        currentRudderAngle = Mathf.MoveTowards(currentRudderAngle, targetAngle, rudderTurnSpeed * Time.fixedDeltaTime);


        // Apply forward propulsion
        Vector3 propulsion = transform.forward * _throttle * throttleForce * Time.fixedDeltaTime;
        _rb.AddForce(propulsion);

        // Clamp speed
        if (_rb.linearVelocity.magnitude > maxSpeed)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * maxSpeed;
        }


        // Simulate rudder steering
        Vector3 rudderForce = Quaternion.Euler(0f, currentRudderAngle, 0f) * transform.forward;
        rudderForce *= _throttle * turnForceMultiplier;
       
        _rb.AddForce(rudderForce, ForceMode.Force);

        // Align the boat's facing direction with velocity
        //if (_rb.linearVelocity.sqrMagnitude > 0.1f)
        //{
            Quaternion targetRotation = Quaternion.LookRotation(_rb.linearVelocity.normalized, Vector3.forward);
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, turnAlignSpeed * Time.fixedDeltaTime));
        //}
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, Vector3.forward * 25f);
    }

}
