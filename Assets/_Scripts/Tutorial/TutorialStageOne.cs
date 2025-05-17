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
    [SerializeField] private SpawnManager _spawnManager; // Reference to SpawnManager

    private HashSet<ulong> _inside = new();
    private bool _playersEnabled = false;
    
    private int TotalPlayers => NetworkManager.Singleton.ConnectedClientsIds.Count;
    private bool AllPlayersInside => _inside.Count == TotalPlayers && TotalPlayers > 0;

    public void ActivateStage()
    {
        gameObject.SetActive(true);
        _playersEnabled = false;
        
        if (IsServer)
        {
            // Disable all player controls initially
            DisablePlayerControlsClientRpc();
        }
        
        UpdateCounterText();
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

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !other.CompareTag("Player")) return;
        
        var networkObject = other.GetComponent<NetworkObject>();
        if (networkObject == null) return;
        
        var id = networkObject.OwnerClientId;
        _inside.Add(id);
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

    private void UpdateCounterText()
    {
        _counterText.text = $"{_inside.Count} of {TotalPlayers} players here";
    }
}