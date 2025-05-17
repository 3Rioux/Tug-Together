using UnityEngine;

public class HookSwing : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform hookTransform;
    [SerializeField] private Transform boatTransform;

    [Header("Swing Settings")]
    [SerializeField] private float maxHookAngle = 80f;
    [SerializeField] private float swingFactor = 25f; // Increased from 8f
    [SerializeField] private float returnSpeed = 1.5f; // Slightly reduced
    [SerializeField] private float swingDamping = 0.85f; // Reduced from 0.95f

    // State tracking
    private float currentVelocity = 0f;
    private float currentAngle = 0f;
    private Vector3 lastEulerAngles;

    private void Start()
    {
        if (boatTransform == null)
            boatTransform = transform.parent;

        lastEulerAngles = boatTransform.eulerAngles;
    }

    private void Update()
    {
        UpdateHookSwing();
    }

    private void UpdateHookSwing()
    {
        // Calculate rotation delta
        Vector3 currentEulerAngles = boatTransform.eulerAngles;
        Vector3 deltaRotation = NormalizeAngles(currentEulerAngles - lastEulerAngles);
        lastEulerAngles = currentEulerAngles;

        // Calculate swing force from rotation delta - amplified by 3x
        float rotationForce = (-deltaRotation.y * 1.8f - deltaRotation.z * 1.2f);
        
        // Also consider absolute boat rotation to enhance effect
        Vector3 normalizedBoatRotation = NormalizeAngles(boatTransform.eulerAngles);
        rotationForce += -normalizedBoatRotation.z * 0.1f;

        // Apply physics-like behavior
        currentVelocity += rotationForce * swingFactor * Time.deltaTime;
        currentVelocity *= swingDamping;

        // Return to center when boat is straight - reduced threshold
        if (Mathf.Abs(rotationForce) < 0.05f)
            currentVelocity += -currentAngle * returnSpeed * Time.deltaTime;

        // Update angle with velocity
        currentAngle += currentVelocity * Time.deltaTime;

        // Clamp to limits
        currentAngle = Mathf.Clamp(currentAngle, -maxHookAngle, maxHookAngle);

        // Apply rotation to hook
        hookTransform.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
    }

    private Vector3 NormalizeAngles(Vector3 angles)
    {
        angles.x = NormalizeAngle(angles.x);
        angles.y = NormalizeAngle(angles.y);
        angles.z = NormalizeAngle(angles.z);
        return angles;
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        else if (angle < -180f) angle += 360f;
        return angle;
    }
}