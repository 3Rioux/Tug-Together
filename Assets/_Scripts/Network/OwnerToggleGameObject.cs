using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OwnerToggleGameObject : NetworkBehaviour
{
    [Header("Target Reference")]
    [Tooltip("Reference to the GameObject to be toggled.")]
    public GameObject targetObject;

    [Header("Behavior")]
    [Tooltip("When true, it will disable the object on the owner and enable it on the client.")]
    public bool reverseOwnerActivation = false;

    private void Start()
    {
        if (targetObject == null)
        {
            Debug.LogError("Target object not assigned", this);
            return;
        }

        // Toggle active state based on ownership, reversing if needed.
        targetObject.SetActive(reverseOwnerActivation ? !IsOwner : IsOwner);
    }
}