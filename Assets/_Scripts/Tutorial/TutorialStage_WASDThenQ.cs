using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using Unity.Netcode;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class TutorialStage_WASDThenQ : NetworkBehaviour, ITutorialStage
{
    public event Action StageCompleted;

    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private RawImage _rawImage;
    [SerializeField] private TextMeshProUGUI _counterText;
    [SerializeField] private Collider _triggerCollider;
    [SerializeField] private TutorialController _tutorialController;

    // which keys to watch in sequence
    private KeyCode[][] _phases = new[]
    {
        new[] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D },
        new[] { KeyCode.Q }
    };

    private int _currentPhase;
    private readonly HashSet<ulong> _inTrigger = new();
    private readonly Dictionary<ulong, int> _playerProgress = new();

    public void ActivateStage()
    {
        _currentPhase = 0;
        _inTrigger.Clear();
        _playerProgress.Clear();

        _rawImage.gameObject.SetActive(false);
        _counterText.gameObject.SetActive(true);
        _counterText.text = "0 of players here";

        // ensure trigger is trigger-only
        _triggerCollider.isTrigger = true;
    }

    public void DeactivateStage()
    {
        _rawImage.gameObject.SetActive(false);
        _counterText.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsClient || !other.CompareTag("Player")) return;

        ulong clientId = NetworkManager.Singleton.LocalClientId;
        _inTrigger.Add(clientId);
        _playerProgress[clientId] = 0;
        UpdateCounter();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsClient || !other.CompareTag("Player")) return;

        ulong clientId = NetworkManager.Singleton.LocalClientId;
        _inTrigger.Remove(clientId);
        _playerProgress.Remove(clientId);
        UpdateCounter();
    }

    private void Update()
    {
        if (_inTrigger.Count == 0) 
            return;

        // once all players have entered, start the tutorial video
        if (!_videoPlayer.isPlaying && _rawImage.gameObject.activeSelf == false)
        {
            // everyone here?
            if (_inTrigger.Count == NetworkManager.Singleton.ConnectedClientsIds.Count)
                BeginPhase();
        }

        // during video playback, watch keys
        if (_rawImage.gameObject.activeSelf)
        {
            foreach (var key in _phases[_currentPhase])
            {
                if (Input.GetKeyDown(key))
                    ReportKeyPressedServerRpc(key);
            }
        }
    }

    private void BeginPhase()
    {
        // disable movement globally
        _tutorialController.DisableAllPlayerControls();

        _rawImage.gameObject.SetActive(true);
        _videoPlayer.Play();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReportKeyPressedServerRpc(KeyCode key, ServerRpcParams rpcParams = default)
    {
        var sender = rpcParams.Receive.SenderClientId;
        // if they are not inside, ignore
        if (!_inTrigger.Contains(sender)) 
            return;

        // did they press a valid key for this phase?
        var phaseKeys = _phases[_currentPhase];
        if (System.Array.IndexOf(phaseKeys, key) < 0)
            return;

        // advance their progress
        _playerProgress[sender]++;

        // if they have pressed all unique keys in this phase
        if (_playerProgress[sender] >= phaseKeys.Length)
        {
            // check if everyone is done
            bool allDone = true;
            foreach (var clientId in _inTrigger)
            {
                if (!_playerProgress.TryGetValue(clientId, out var prog) 
                    || prog < phaseKeys.Length)
                {
                    allDone = false;
                    break;
                }
            }

            if (allDone)
                AdvancePhaseServerRpc();
        }
    }

    [ServerRpc]
    private void AdvancePhaseServerRpc()
    {
        // stop current video
        StopPhaseClientRpc();

        _currentPhase++;
        if (_currentPhase < _phases.Length)
        {
            // start next phase video
            BeginPhaseClientRpc();
        }
        else
        {
            // tutorial stage done
            StageCompleted?.Invoke();
            _tutorialController.EnableAllPlayerControls();
        }
    }

    [ClientRpc]
    private void StopPhaseClientRpc()
    {
        _videoPlayer.Stop();
        _rawImage.gameObject.SetActive(false);
    }

    [ClientRpc]
    private void BeginPhaseClientRpc()
    {
        _rawImage.gameObject.SetActive(true);
        _videoPlayer.Play();
    }

    private void UpdateCounter()
    {
        int here = _inTrigger.Count;
        int total = NetworkManager.Singleton.ConnectedClientsIds.Count;
        _counterText.text = $"{here} of {total} players here";
    }
}
