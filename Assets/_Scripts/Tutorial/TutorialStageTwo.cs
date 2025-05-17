using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Cinemachine;

[RequireComponent(typeof(Collider), typeof(NetworkObject))]
public class TutorialStageTwo : NetworkBehaviour, ITutorialStage
{
    public event Action StageCompleted;

    [SerializeField] private TextMeshProUGUI _counterText;
    [SerializeField] private Collider _triggerCollider;
    
    [Header("Cargo Animation")]
    [SerializeField] private GameObject _cargo;
    [SerializeField] private CinemachineCamera _cutsceneCamera;
    [SerializeField] private float _riseTime = 5f;
    [SerializeField] private CinemachineImpulseSource _cameraShake;
    
    private HashSet<ulong> _inside = new();
    private bool _playersEnabled = false;
    private bool _animationStarted = false;
    private TutorialController _tutorialController;
    
    private int TotalPlayers => NetworkManager.Singleton != null ? 
        NetworkManager.Singleton.ConnectedClientsIds.Count : 0;
    private bool AllPlayersInside => _inside.Count == TotalPlayers && TotalPlayers > 0;

    private void Awake()
    {
        _tutorialController = GetComponentInParent<TutorialController>();
    }
    
    public void ActivateStage()
    {
        gameObject.SetActive(true);
        _playersEnabled = false;
        _animationStarted = false;
        _inside.Clear(); // Clear the list when activating
    
        if (_cutsceneCamera != null)
            _cutsceneCamera.gameObject.SetActive(false);
        
        // Force an immediate update of the counter text
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
        if (IsServer && !_animationStarted && AllPlayersInside && NetworkManager.Singleton != null)
        {
            _animationStarted = true;
            StartCargoAnimationClientRpc();
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
    private void StartCargoAnimationClientRpc()
    {
        // Disable player controls
        FindObjectOfType<TutorialController>()?.DisableAllPlayerControls();
        
        // Activate cutscene camera
        if (_cutsceneCamera != null)
            _cutsceneCamera.gameObject.SetActive(true);
        
        // Disable water surface fitting on cargo
        CustomFitToWaterSurface cargoWaterFit = _cargo.GetComponent<CustomFitToWaterSurface>();
        if (cargoWaterFit != null)
            cargoWaterFit.enabled = false;
        
        // Set cargo initial position
        Vector3 cargoPos = _cargo.transform.position;
        cargoPos.y = -63.21f;
        _cargo.transform.position = cargoPos;
        
        // Start cargo animation coroutine
        StartCoroutine(AnimateCargo());
    }
    
    private System.Collections.IEnumerator AnimateCargo()
    {
        Vector3 startPos = _cargo.transform.position;
        Vector3 endPos = new Vector3(startPos.x, 4f, startPos.z);
        float elapsed = 0f;
        
        while (elapsed < _riseTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _riseTime;
            
            // Move cargo upward
            _cargo.transform.position = Vector3.Lerp(startPos, endPos, t);
            
            // Apply camera shake
            if (_cameraShake != null)
                _cameraShake.GenerateImpulse();
                
            yield return null;
        }
        
        // Re-enable water surface fitting
        CustomFitToWaterSurface cargoWaterFit = _cargo.GetComponent<CustomFitToWaterSurface>();
        if (cargoWaterFit != null)
            cargoWaterFit.enabled = true;
            
        // Disable cutscene camera
        if (_cutsceneCamera != null)
            _cutsceneCamera.gameObject.SetActive(false);
            
        // Re-enable player controls
        FindObjectOfType<TutorialController>()?.EnableAllPlayerControls();
        
        // Set flag to allow stage completion when players exit
        if (IsServer)
            _playersEnabled = true;
    }
}