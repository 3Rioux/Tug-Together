// C#
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using TMPro;
using Unity.Netcode;
using Unity.Cinemachine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider), typeof(NetworkObject))]
public class TutorialStageTwo : NetworkBehaviour, ITutorialStage
{
    public event Action StageCompleted;

    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private RawImage    _rawImage;
    [SerializeField] private TextMeshProUGUI _counterText;
    [SerializeField] private Collider     _triggerCollider;

    [Header("Cutscene")]
    [SerializeField] private CinemachineVirtualCamera _cutsceneCam;
    [SerializeField] private CinemachineVirtualCamera _playerCam;
    [SerializeField] private Transform             _cutsceneTarget;
    [SerializeField] private GameObject            _underwaterObject;
    [SerializeField] private CinemachineImpulseSource _impulse;
    [SerializeField] private float _camMoveTime = 3f;
    [SerializeField] private float _riseTime    = 5f;
    [SerializeField] private float _returnTime  = 2f;

    private HashSet<ulong> _inside = new();
    private int TotalPlayers => NetworkManager.Singleton.ConnectedClientsIds.Count;

    public void ActivateStage()
    {
        gameObject.SetActive(true);
        _rawImage.gameObject.SetActive(false);
        _underwaterObject.SetActive(false);
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
            StartStageServerRpc();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer || !other.CompareTag("Player")) return;
        var id = other.GetComponent<NetworkObject>().OwnerClientId;
        _inside.Remove(id);
        UpdateCounterClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartStageServerRpc() => StartStageClientRpc();

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
        FindObjectOfType<TutorialController>().DisableAllPlayerControls();
        StartCoroutine(CutsceneSequence());
    }

    private IEnumerator CutsceneSequence()
    {
        yield return new WaitForSeconds((float)_videoPlayer.clip.length + 0.5f);
        _videoPlayer.Stop();
        _rawImage.gameObject.SetActive(false);

        var startPos = _cutsceneCam.transform.position;
        var startRot = _cutsceneCam.transform.rotation;
        var endPos   = _cutsceneTarget.position;
        var endRot   = _cutsceneTarget.rotation;
        float t = 0f;
        while (t < _camMoveTime)
        {
            t += Time.deltaTime;
            float f = t / _camMoveTime;
            _cutsceneCam.transform.position = Vector3.Lerp(startPos, endPos, f);
            _cutsceneCam.transform.rotation = Quaternion.Slerp(startRot, endRot, f);
            yield return null;
        }

        _underwaterObject.SetActive(true);
        var objStart = _underwaterObject.transform.position;
        var objEnd   = objStart + Vector3.up * 2f;
        t = 0f;
        while (t < _riseTime)
        {
            t += Time.deltaTime;
            float f = t / _riseTime;
            _underwaterObject.transform.position = Vector3.Lerp(objStart, objEnd, f);
            _impulse.GenerateImpulse();
            yield return null;
        }

        yield return new WaitForSeconds(1f);

        startPos = _cutsceneCam.transform.position;
        startRot = _cutsceneCam.transform.rotation;
        endPos   = _playerCam.transform.position;
        endRot   = _playerCam.transform.rotation;
        t = 0f;
        while (t < _returnTime)
        {
            t += Time.deltaTime;
            float f = t / _returnTime;
            _cutsceneCam.transform.position = Vector3.Lerp(startPos, endPos, f);
            _cutsceneCam.transform.rotation = Quaternion.Slerp(startRot, endRot, f);
            yield return null;
        }

        FindObjectOfType<TutorialController>().EnableAllPlayerControls();
        if (IsServer)
            StageCompleted?.Invoke();
    }
    
    private void UpdateCounterText()
        => _counterText.text = $"{_inside.Count} of {TotalPlayers} players here";
}