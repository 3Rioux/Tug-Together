using UnityEngine;

/// <summary>
/// Simple tugboat movement controller using Unity Rigidbody physics
/// Tugboat movement using rudder steering instead of direct torque
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BoatMovementV1 : MonoBehaviour
{
    [SerializeField] private float throttleForce = 1000f;       // Forward/backward propulsion
    //[SerializeField] private float turnTorque = 200f;         // Left/right turning power
    [SerializeField] private float maxRudderAngle = 30f;        // Max rudder angle in degrees
    [SerializeField] private float rudderTurnSpeed = 45f;       // Degrees per second
    [SerializeField] private float turnForceMultiplier = 10f;   // How strongly the rudder affects the boat's direction
    

   
    private Rigidbody _rb; // this rigid body component

    private float currentRudderAngle = 0f;    // Current rudder deflection

    private float _throttle; // current throttle amount
    private float _rudderInput; // current turn amount 


    void Start()
    {
        _rb = GetComponent<Rigidbody>(); // get it from this gameobject
    }

    private void Update()
    {
        // Get input
        _throttle = Input.GetAxis("Vertical");  // W/S or Up/Down Arrow
        _rudderInput = Input.GetAxis("Horizontal");    // A/D or Left/Right Arrow
    }

    void FixedUpdate()
    {
        // Smoothly rotate rudder toward input direction
        float targetAngle = _rudderInput * maxRudderAngle;
        currentRudderAngle = Mathf.MoveTowards(currentRudderAngle, targetAngle, rudderTurnSpeed * Time.fixedDeltaTime);


        // Apply forward force
        Vector3 propulsion = transform.forward * _throttle * throttleForce * Time.fixedDeltaTime;
       
        _rb.AddForce(propulsion);

        // Simulate rudder steering
        Vector3 rudderForce = Quaternion.Euler(0f, currentRudderAngle, 0f) * transform.forward;
        rudderForce *= _throttle * turnForceMultiplier;
       
        _rb.AddForce(rudderForce, ForceMode.Force);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, Vector3.forward * 25f);
    }

}
