using TMPro;
using UnityEngine;
using Unity.Netcode;

public class PlayerNameDisplay : NetworkBehaviour
{
    public float minScale = 0.5f;
    public float maxScale = 2f;

    // Distance thresholds for scaling.
    public float minDistance = 5f;
    public float maxDistance = 20f;

    // Vertical offset to position the label above the player.
    public float verticalOffset = 2f;

    private Transform ownerTransform;
    private Transform localPlayerTransform;
    private Transform cam;

    private void Start()
    {
        // Assume the label is a child of the player.
        ownerTransform = transform.parent;
        if (ownerTransform == null)
        {
            Debug.LogError("Owner transform not found. Ensure label is a child of the player.");
            return;
        }

        // Get the main camera for billboarding.
        if (Camera.main != null)
        {
            cam = Camera.main.transform;
        }
        else
        {
            Debug.LogError("Main camera not found.");
        }

        // Obtain the local player's transform.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
        {
            localPlayerTransform = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().transform;
        }
        else
        {
            Debug.LogError("Local player object not found.");
        }
    }

    private void LateUpdate()
    {
        if (ownerTransform == null || localPlayerTransform == null || cam == null)
            return;

        // Position the label above the owner.
        Vector3 targetPosition = ownerTransform.position + Vector3.up * verticalOffset;
        transform.position = targetPosition;

        // Billboard the label using the main camera's forward direction.
        transform.LookAt(targetPosition + cam.forward);

        // Adjust scale based on the distance between owner and local player.
        float distance = Vector3.Distance(ownerTransform.position, localPlayerTransform.position);
        float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
        float scale = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = Vector3.one * scale;
    }
}