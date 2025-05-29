using TMPro;
using UnityEngine;

/// <summary>
/// Points the compass needle toward the end goal of the scene.
/// </summary>
public class Compass : MonoBehaviour
{
    [SerializeField] private Transform player;      // The player or camera
    [SerializeField] private Transform arrow;       // The rotating arrow for end goal
    [SerializeField] private Transform needle;      // The rotating arrow for barge
    [SerializeField] private float rotationSpeed = 5f;    // Smoothing factor

    //UI
    [SerializeField] private TextMeshProUGUI distanceText; // Text to show distance

    //Private
    private Transform _target;                               // The object to point to
    private Vector3 _targetDirection;                        // Save the direction of the _target
    private Vector3 _bargeDirection;                         // Save the direction of the barge
    private Transform _bargeTransform;                       // Reference to the barge transform

    private void Start()
    {
        if (LevelVariableManager.Instance != null)
        {
            _target = LevelVariableManager.Instance.GlobalEndGameTrigger.transform;
            _bargeTransform = LevelVariableManager.Instance.GlobalBargeRigidBody.transform;
        }
        else
        {
            Debug.LogWarning("LevelVariableManager instance not found. Please assign the _target manually.");
        }
    }

    void Update()
    {
        if (player == null || _target == null || arrow == null) return;

        // Calculate direction to _target
        _targetDirection = _target.position - player.position;
        _targetDirection.y = 0; // Ignore vertical difference

        // Calculate angle between player forward and _target direction
        float angle = Vector3.SignedAngle(player.forward, _targetDirection, Vector3.up);

        // Apply rotation to the arrow's Z-axis to point toward the _target
        Quaternion _targetRotation = Quaternion.Euler(0, 0, angle);
        arrow.localRotation = Quaternion.Slerp(arrow.localRotation, _targetRotation, rotationSpeed * Time.deltaTime);

        // Handle the barge needle
        if (_bargeTransform != null && needle != null)
        {
            // Calculate direction to barge
            _bargeDirection = _bargeTransform.position - player.position;
            _bargeDirection.y = 0; // Ignore vertical difference

            // Calculate angle between player forward and barge direction
            float bargeAngle = Vector3.SignedAngle(player.forward, _bargeDirection, Vector3.up);

            // Apply rotation to the needle's Z-axis to point toward the barge
            Quaternion bargeRotation = Quaternion.Euler(0, 0, bargeAngle);
            needle.localRotation = Quaternion.Slerp(needle.localRotation, bargeRotation, rotationSpeed * Time.deltaTime);
        }

        // Update distance text
        float distance = Vector3.Distance(player.position, _target.position);
        if (distance > 1000f)
        {
            // If distance is greater than 1000, show in kilometers
            distanceText.text = $"{distance / 1000:F2} km";
        }
        else if (distance > 100f)
        {
            // show in meters
            distanceText.text = $"{distance:F0} m";
        }
    }
}