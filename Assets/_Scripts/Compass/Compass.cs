using TMPro;
using UnityEngine;

/// <summary>
/// Points the compass needle toward the end goal of the scene.
/// </summary>
public class Compass : MonoBehaviour
{
    [SerializeField] private Transform player;      // The player or camera
    [SerializeField] private Transform target;      // The object to point to
    [SerializeField] private Transform needle;      // The rotating arrow
    [SerializeField] private float rotationSpeed = 5f;    // Smoothing factor

    //UI
    [SerializeField] private TextMeshProUGUI distanceText; // Text to show distance

    //Private
    private Vector3 _targetDirection;                        // Save the direction of the target

    private void Start()
    {
        if (LevelVariableManager.Instance != null)
        {
            target = LevelVariableManager.Instance.GlobalEndGameTrigger.transform;
        }
        else
        {
            Debug.LogWarning("LevelVariableManager instance not found. Please assign the target manually.");
        }
    }

    void Update()
    {
        if (player == null || target == null || needle == null) return;

        // Calculate direction to target
        _targetDirection = target.position - player.position;
        _targetDirection.y = 0; // Ignore vertical difference

        // Calculate angle between player forward and target direction
        float angle = Vector3.SignedAngle(player.forward, _targetDirection, Vector3.up);

        // Apply rotation to the needle's Y-axis to point toward the target
        // Using Slerp for smooth rotation
        Quaternion targetRotation = Quaternion.Euler(0, 0,angle);
        needle.localRotation = Quaternion.Slerp(needle.localRotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Update distance text
        float distance = Vector3.Distance(player.position, target.position);
        distanceText.text = $"{distance:F0} m";
    }
}