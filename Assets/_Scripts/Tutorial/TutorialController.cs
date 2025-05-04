using System;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] _stageBehaviours;  // drag-in your stage scripts here

    private ITutorialStage[] _stages;
    private int _currentStageIndex;

    private void Awake()
    {
        // cast to interface
        _stages = Array.ConvertAll(_stageBehaviours,
            b => b as ITutorialStage);
    }

    private void Start()
    {
        _currentStageIndex = 0;
        for (int i = 0; i < _stages.Length; i++)
        {
            _stages[i].StageCompleted += OnStageCompleted;
            _stages[i].DeactivateStage();
        }

        if (_stages.Length > 0)
            _stages[0].ActivateStage();
    }

    private void OnStageCompleted()
    {
        // unsubscribe current
        _stages[_currentStageIndex].StageCompleted -= OnStageCompleted;
        _stages[_currentStageIndex].DeactivateStage();

        _currentStageIndex++;

        if (_currentStageIndex < _stages.Length)
            _stages[_currentStageIndex].ActivateStage();
        else
            Debug.Log("Tutorial finished");
    }

    /// <summary>
    /// Utility the stages can call to disable all boat controls
    /// </summary>
    public void DisableAllPlayerControls()
    {
        foreach (var boat in FindObjectsOfType<StrippedTubBoatMovement>())
            boat.SetControlEnabled(false);
    }

    /// <summary>
    /// Utility the stages can call to re-enable all boat controls
    /// </summary>
    public void EnableAllPlayerControls()
    {
        foreach (var boat in FindObjectsOfType<StrippedTubBoatMovement>())
            boat.SetControlEnabled(true);
    }
}