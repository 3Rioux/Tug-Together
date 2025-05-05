// TutorialController.cs
using System;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [Tooltip("Drag in the two stage components on child GameObjects here, in order")]
    [SerializeField] private MonoBehaviour[] _stageScripts;

    private ITutorialStage[] _stages;
    private int _currentIndex;

    private void Awake()
    {
        // cast to ITutorialStage
        _stages = Array.ConvertAll(_stageScripts, mb => mb as ITutorialStage);
    }

    private void Start()
    {
        // subscribe to completion events
        for (int i = 0; i < _stages.Length; i++)
            _stages[i].StageCompleted += OnStageCompleted;

        // kick off the first stage
        _currentIndex = 0;
        _stages[_currentIndex].ActivateStage();
    }

    private void OnStageCompleted()
    {
        // hide the one that just finished
        _stages[_currentIndex].DeactivateStage();

        // move to next
        _currentIndex++;
        if (_currentIndex < _stages.Length)
            _stages[_currentIndex].ActivateStage();
        else
            Debug.Log("Tutorial fully complete");
    }

    // convenience methods for stages to disable/re-enable all player controls
    public void DisableAllPlayerControls()
    {
        foreach (var boat in FindObjectsOfType<StrippedTubBoatMovement>())
            boat.SetControlEnabled(false);
    }

    public void EnableAllPlayerControls()
    {
        foreach (var boat in FindObjectsOfType<StrippedTubBoatMovement>())
            boat.SetControlEnabled(true);
    }
}