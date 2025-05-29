using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Handles all the cargo barge states and information storage 
/// </summary>
public class TowableObjectController : MonoBehaviour
{
    [Tooltip("Stores all the attache point transforms")]
    [SerializeField] private List<Transform> towAttachentPointList = new List<Transform>();

    [Header("Stabilization Settings")]
    [SerializeField] private float stabilizationStrength = 0.5f; // How strongly it returns to level position
    [SerializeField] private float stabilizationThreshold = 0.8f; // Speed below which stabilization happens
    [SerializeField] private float maxTiltAngle = 8f; // Maximum allowed tilt during movement

    private Rigidbody _rigidbody;

    public List<Transform> TowPointList { get => towAttachentPointList; set => towAttachentPointList = value; }

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        
        // Increase angular drag for more realistic large object rotation
        if (_rigidbody != null)
        {
            _rigidbody.angularDamping = 2.5f; // Higher value means more resistance to rotation
            _rigidbody.mass = Mathf.Max(10f, _rigidbody.mass); // Ensure minimum mass
        }
    }

    private void FixedUpdate()
    {
       // if (!IsServer) return;

        if (_rigidbody == null) return;

        // Get current speed
        float speed = _rigidbody.linearVelocity.magnitude;
        
        // Get current rotation angles
        Vector3 eulerAngles = transform.rotation.eulerAngles;
        
        // Normalize angles to -180 to 180 range
        float xAngle = eulerAngles.x > 180f ? eulerAngles.x - 360f : eulerAngles.x;
        float zAngle = eulerAngles.z > 180f ? eulerAngles.z - 360f : eulerAngles.z;

        // Create target rotation based on speed
        Quaternion targetRotation;
        
        if (speed > stabilizationThreshold)
        {
            // When moving fast, allow limited tilt
            targetRotation = Quaternion.Euler(
                Mathf.Clamp(xAngle, -maxTiltAngle, maxTiltAngle),
                eulerAngles.y,
                Mathf.Clamp(zAngle, -maxTiltAngle, maxTiltAngle)
            );
            
            // Apply rotation with moderate strength
            _rigidbody.MoveRotation(Quaternion.Slerp(
                _rigidbody.rotation, 
                targetRotation, 
                0.2f * Time.deltaTime
            ));
        }
        else
        {
            // When still or moving slowly, gradually level out
            targetRotation = Quaternion.Euler(0f, eulerAngles.y, 0f);
            
            // Apply rotation with configurable strength
            _rigidbody.MoveRotation(Quaternion.Slerp(
                _rigidbody.rotation, 
                targetRotation, 
                stabilizationStrength * Time.deltaTime
            ));
        }
    }
    [SerializeField] private float debugRangeRadius = 25f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position + new Vector3(0, 0, 12f), debugRangeRadius);
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.CompareTag("Player"))
    //    {
    //        TugboatMovementWFloat tugMovement = other.GetComponent<TugboatMovementWFloat>();
    //        if (tugMovement != null)
    //        {
    //            tugMovement.OrientationSmoothSpeed = 10f;// Stop player boat front from tilting up
    //        }
    //    }
    //}

    //private void OnTriggerExit(Collider other)
    //{
    //    if (other.CompareTag("Player"))
    //    {
    //        TugboatMovementWFloat tugMovement = other.GetComponent<TugboatMovementWFloat>();
    //        if (tugMovement != null)
    //        {
    //            tugMovement.OrientationSmoothSpeed = tugMovement.OrientationDefaultSmoothSpeed;// Return the player boats tilt to what is was 
    //        }
    //    }
    //}


}