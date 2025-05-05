using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;

[RequireComponent(typeof(Collider), typeof(NetworkObject))]
public class TutorialStageOne : NetworkBehaviour, ITutorialStage
{
    public event Action StageCompleted;

    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private RawImage    _rawImage;
    [SerializeField] private TextMeshProUGUI _counterText;
    [SerializeField] private Collider     _triggerCollider;

    private HashSet<ulong> _inside = new();
    private int TotalPlayers => NetworkManager.Singleton.ConnectedClientsIds.Count;

    public void ActivateStage()
    {
        gameObject.SetActive(true);
        _rawImage.gameObject.SetActive(false);
        UpdateCounterText();
    }

    public void DeactivateStage()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !other.CompareTag("Player")) return;
        var id = other.GetComponent<NetworkObject>().OwnerClientId;
        _inside.Add(id);
        UpdateCounterClientRpc();
        if (_inside.Count == TotalPlayers)
            StartStageClientRpc();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer || !other.CompareTag("Player")) return;
        var id = other.GetComponent<NetworkObject>().OwnerClientId;
        _inside.Remove(id);
        UpdateCounterClientRpc();
        if (_inside.Count == 0)
            EndStageOnServer();
    }

    [ClientRpc]
    private void UpdateCounterClientRpc()
    {
        _counterText.text = $"{_inside.Count} of {TotalPlayers} players here";
    }

    [ClientRpc]
    private void StartStageClientRpc()
    {
        _rawImage.gameObject.SetActive(true);
        _videoPlayer.Play();
        TutorialController tc = FindObjectOfType<TutorialController>();
        tc.DisableAllPlayerControls();
    }

    [ClientRpc]
    private void StopStageClientRpc()
    {
        _videoPlayer.Stop();
        _rawImage.gameObject.SetActive(false);
        FindObjectOfType<TutorialController>().EnableAllPlayerControls();
    }

    private void EndStageOnServer()
    {
        if (!IsServer)
            return;
        StopStageClientRpc();
        StageCompleted?.Invoke();
    }

    private void UpdateCounterText()
        => _counterText.text = $"{_inside.Count} of {TotalPlayers} players here";
}