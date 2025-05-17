using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

[RequireComponent(typeof(Collider), typeof(NetworkObject))]
public class TutorialStageOne : NetworkBehaviour, ITutorialStage
{
    public event Action StageCompleted;

    [SerializeField] private TextMeshProUGUI _counterText;
    [SerializeField] private Collider _triggerCollider;
    
    private HashSet<ulong> _inside = new();
    private bool _playersEnabled = false;
    
    private int TotalPlayers => NetworkManager.Singleton.ConnectedClientsIds.Count;
    private bool AllPlayersInside => _inside.Count == TotalPlayers && TotalPlayers > 0;

    private void Awake()
    {
        // Ensure the collider is set as a trigger
        if (_triggerCollider != null)
        {
            _triggerCollider.isTrigger = true;
        }
        else
        {
            // Find and set up the collider component if it wasn't assigned
            _triggerCollider = GetComponent<Collider>();
            if (_triggerCollider != null)
            {
                _triggerCollider.isTrigger = true;
            }
            else
            {
                Debug.LogError("No collider found on " + gameObject.name);
            }
        }
    }
    
    public void ActivateStage()
    {
        gameObject.SetActive(true);
        _playersEnabled = false;
        _inside.Clear(); // Clear the list when activating
    
        if (IsServer)
        {
            // Disable all player controls initially
            DisablePlayerControlsClientRpc();
        }
    
        UpdateCounterText();
    }

    private void UpdateCounterText()
    {
        if (_counterText != null)
        {
            int totalPlayers = NetworkManager.Singleton != null ? 
                NetworkManager.Singleton.ConnectedClientsIds.Count : 0;
            _counterText.text = $"{_inside.Count} of {totalPlayers} players here";
        }
    }

    public void DeactivateStage()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (IsServer && !_playersEnabled && AllPlayersInside)
        {
            _playersEnabled = true;
            EnablePlayerControlsClientRpc();
        }
    }
    
    private void OnValidate()
    {
        if (_triggerCollider == null)
            _triggerCollider = GetComponent<Collider>();
        
        // Make sure it's a trigger
        if (_triggerCollider != null)
            _triggerCollider.isTrigger = true;
    }

// Add debugging to see if players are entering the trigger
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger entered by: {other.name}, has Player tag: {other.CompareTag("Player")}, IsServer: {IsServer}");
    
        if (!IsServer || !other.CompareTag("Player")) return;
    
        var networkObject = other.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogWarning("Player missing NetworkObject component");
            return;
        }
    
        var id = networkObject.OwnerClientId;
        _inside.Add(id);
        Debug.Log($"Player {id} entered trigger, total inside: {_inside.Count}");
        UpdateCounterClientRpc();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer || !other.CompareTag("Player")) return;
        
        var networkObject = other.GetComponent<NetworkObject>();
        if (networkObject == null) return;
        
        var id = networkObject.OwnerClientId;
        _inside.Remove(id);
        UpdateCounterClientRpc();
        
        // If all players have exited the trigger and players were enabled
        if (_inside.Count == 0 && _playersEnabled)
        {
            StageCompleted?.Invoke();
        }
    }

    [ClientRpc]
    private void UpdateCounterClientRpc()
    {
        _counterText.text = $"{_inside.Count} of {TotalPlayers} players here";
    }

    [ClientRpc]
    private void DisablePlayerControlsClientRpc()
    {
        FindObjectOfType<TutorialController>()?.DisableAllPlayerControls();
    }

    [ClientRpc]
    private void EnablePlayerControlsClientRpc()
    {
        FindObjectOfType<TutorialController>()?.EnableAllPlayerControls();
    }
}