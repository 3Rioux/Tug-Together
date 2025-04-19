using UnityEngine;

/// <summary>
/// Simple tugboat movement controller using Unity Rigidbody physics
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BoatMovementV1 : MonoBehaviour
{
    [SerializeField] private float throttleForce = 1000f;  // Forward/backward propulsion
    [SerializeField] private float turnTorque = 200f;      // Left/right turning power


    private Rigidbody _rb; // this rigid body component
    
    private float _throttle; // current throttle amount
    private float _turn; // current turn amount 


    void Start()
    {
        _rb = GetComponent<Rigidbody>(); // get it from this gameobject
    }

    private void Update()
    {
        // Get input
        _throttle = Input.GetAxis("Vertical");  // W/S or Up/Down Arrow
        _turn = Input.GetAxis("Horizontal");    // A/D or Left/Right Arrow
    }

    void FixedUpdate()
    {
        // Apply forward force
        Vector3 force = transform.forward * _throttle * throttleForce * Time.fixedDeltaTime;
        _rb.AddForce(force);

        // Apply turning torque
        Vector3 torque = Vector3.up * _turn * turnTorque * Time.fixedDeltaTime;
        _rb.AddTorque(torque);
    }

}
