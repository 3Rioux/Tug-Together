using UnityEngine;

public class MoveObject : MonoBehaviour
{
    [Header("Wind Parameters")]
    public Vector3 windDirection = Vector3.right;
    public float windSpeed = 1.0f;
    public float windStrength = 1.0f;
    public float windFrequency = 1.0f;
    public float damping = 0.1f;
    
    [Header("Rotation Parameters")]
    public float rotationStrength = 5.0f;
    public Vector3 rotationAxis = Vector3.forward; // Which axis to rotate around
    public bool usePositionEffect = false; // Set false for pure rotation

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 currentOffset;
    private Vector3 normalizedWindDirection;
    private float randomOffset;
    private float timePassed = 0f;

    private void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        randomOffset = Random.Range(0.0f, 2.0f * Mathf.PI);
        normalizedWindDirection = windDirection.normalized;
    }

    private void Update()
    {
        // Accumulate time based on wind speed
        timePassed += Time.deltaTime * windSpeed;

        // Calculate wind effect with sine wave
        float windEffect = Mathf.Sin(timePassed * windFrequency + randomOffset) * windStrength;
        
        // Handle position movement if enabled
        if (usePositionEffect)
        {
            // Calculate target offset
            Vector3 targetOffset = normalizedWindDirection * windEffect;
            
            // Apply damping
            currentOffset = Vector3.Lerp(currentOffset, targetOffset, damping);
            
            // Set position
            transform.position = initialPosition + currentOffset;
        }
        
        // Apply rotation based on wind effect
        // Using a cosine offset makes the rotation slightly lag behind position changes
        float rotationEffect = Mathf.Sin(timePassed * windFrequency + randomOffset - 0.2f) * rotationStrength;
        
        // Create rotation
        Quaternion windRotation = Quaternion.AngleAxis(rotationEffect, rotationAxis);
        
        // Apply rotation with damping
        transform.rotation = Quaternion.Slerp(transform.rotation, 
                                             initialRotation * windRotation, 
                                             damping);
                                             
        // Update normalized direction if needed
        if (windDirection.normalized != normalizedWindDirection)
        {
            normalizedWindDirection = windDirection.normalized;
        }
    }
    
    // Add this to your existing MoveObject.cs class
    private void OnDrawGizmos()
    {
        // Make sure we use the current wind direction
        Vector3 direction = (Application.isPlaying && normalizedWindDirection != Vector3.zero) 
            ? normalizedWindDirection 
            : windDirection.normalized;
    
        // Get start position
        Vector3 start = transform.position;
    
        // Make a reasonably sized arrow
        float arrowLength = 5f;
        Vector3 end = start + direction * arrowLength;
    
        // Main arrow line
        Gizmos.color = Color.cyan; // Distinct color for wind
        Gizmos.DrawLine(start, end);
    
        // Arrow head
        float headSize = 1f;
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * headSize;
        // Handle edge case if direction is parallel to up
        if (right.magnitude < 0.001f)
            right = Vector3.Cross(direction, Vector3.forward).normalized * headSize;
    
        Vector3 up = Vector3.Cross(right, direction).normalized * headSize;
    
        // Draw arrow head
        Gizmos.DrawLine(end, end - direction * headSize + right);
        Gizmos.DrawLine(end, end - direction * headSize - right);
        Gizmos.DrawLine(end, end - direction * headSize + up);
        Gizmos.DrawLine(end, end - direction * headSize - up);
    }
}