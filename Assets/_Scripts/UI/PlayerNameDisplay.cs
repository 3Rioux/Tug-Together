// C#
using System;
using TMPro;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;

public class PlayerNameDisplay : NetworkBehaviour
{
    public TextMeshProUGUI playerName;
    
    public float minScale = 0.5f;
    public float maxScale = 2f;
    public float minDistance = 5f;
    public float maxDistance = 20f;
    public float verticalOffset = 2f;

    private Transform ownerTransform;
    private Transform localPlayerTransform;

    [SerializeField]
    private string playerCustomName = "Unknown";

    public NetworkVariable<FixedString32Bytes> networkPlayerName = new NetworkVariable<FixedString32Bytes>(
        "Unknown", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public event Action<string> OnNameChanged;

// C#
    private async void Start()
    {
        ownerTransform = transform.parent;
        if (ownerTransform == null)
        {
            Debug.LogError("Owner transform not found. Ensure the label is a child of the player.");
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogError("Main camera not found. Ensure a camera has the MainCamera tag.");
        }
        else
        {
            Debug.Log("Main camera found: " + Camera.main.gameObject.name);
        }

        // Wait a moment for the local player to spawn
        await System.Threading.Tasks.Task.Delay(1000);
        if (NetworkManager.Singleton != null)
        {
            var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localPlayer != null)
            {
                localPlayerTransform = localPlayer.transform;
                Debug.Log("Local player object found: " + localPlayer.name);
            }
            else
            {
                Debug.LogError("Local player object not found. Ensure the local player has spawned.");
            }
        }
        else
        {
            Debug.LogError("NetworkManager Singleton is null.");
        }
    }
    
    private void Update()
    {
        if (ownerTransform == null || localPlayerTransform == null)
            return;

        Vector3 targetPosition = ownerTransform.position + Vector3.up * verticalOffset;
        transform.position = targetPosition;
    
        if (Camera.main != null)
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward);

        float distance = Vector3.Distance(ownerTransform.position, localPlayerTransform.position);
        float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
        float scale = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = Vector3.one * scale;
    }

    public override async void OnNetworkSpawn()
    {
        if (playerName == null)
        {
            Debug.LogError("PlayerName TextMeshProUGUI component is not assigned.", this);
            return;
        }

        if (IsOwner)
        {
            string sessionName = await WidgetDependenciesWrapper.GetPlayerNameAsync();
            if (!string.IsNullOrEmpty(sessionName))
            {
                networkPlayerName.Value = new FixedString32Bytes(sessionName);
            }
            else if (!string.IsNullOrEmpty(playerCustomName))
            {
                networkPlayerName.Value = new FixedString32Bytes(playerCustomName);
            }
        }

        playerName.text = networkPlayerName.Value.ToString();
        networkPlayerName.OnValueChanged += NetworkPlayerName_OnValueChanged;
        OnNameChanged?.Invoke(networkPlayerName.Value.ToString());
    }

    private void NetworkPlayerName_OnValueChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        if (playerName != null)
        {
            playerName.text = newValue.Value;
        }
        OnNameChanged?.Invoke(newValue.Value);
    }

    public string GetPlayerName()
    {
        return networkPlayerName.Value.ToString();
    }
}