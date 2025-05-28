using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;
    
    [Tooltip("If true, billboard will rotate on all axes to fully face camera. If false, only rotates around Y axis.")]
    [SerializeField] private bool fullBillboard = true;

    // Find the main camera in Start
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("Billboard script couldn't find main camera. Make sure a camera is tagged as 'MainCamera'");
        }
    }

    // Update billboard orientation in LateUpdate after all camera movements
    void LateUpdate()
    {
        if (mainCamera == null) return;

        if (fullBillboard)
        {
            // Full billboarding - canvas completely faces the camera
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up);
        }
        else
        {
            // Y-axis only billboarding - maintains upright orientation
            Vector3 direction = mainCamera.transform.position - transform.position;
            direction.y = 0; // Zero out the y component to only rotate around y-axis

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(-direction);
            }
        }
    }
}