using System;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [Tooltip("Drag stage GameObjects here in order")]
    [SerializeField] private GameObject[] _stageObjects;

    private ITutorialStage[] _stages;
    private int _currentIndex;

    private void Awake()
    {
        // If no stage objects are assigned, try to find them in children
        if (_stageObjects == null || _stageObjects.Length == 0)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            // Find GameObjects with stage components
            foreach (Transform child in children)
            {
                if (child.GetComponent<TutorialStageOne>() != null || 
                    child.GetComponent<TutorialStageTwo>() != null)
                {
                    Array.Resize(ref _stageObjects, (_stageObjects?.Length ?? 0) + 1);
                    _stageObjects[_stageObjects.Length - 1] = child.gameObject;
                    Debug.Log($"Found stage: {child.name}");
                }
            }
        }

        // Create stages array and get components directly
        _stages = new ITutorialStage[_stageObjects.Length];
        for (int i = 0; i < _stageObjects.Length; i++)
        {
            if (_stageObjects[i] != null)
            {
                // Try to get either stage type
                _stages[i] = _stageObjects[i].GetComponent<ITutorialStage>();
                
                if (_stages[i] != null)
                    _stageObjects[i].SetActive(false);
                else
                    Debug.LogError($"GameObject '{_stageObjects[i].name}' has no ITutorialStage component");
            }
        }
    }

    private void Start()
    {
        // Add null check for AudioManager
        if (AudioManager.Instance != null && FMODEvents.Instance != null)
        {
            AudioManager.Instance.PlayAmbience(FMODEvents.Instance.Tutorial);
        }

        // Subscribe to completion events
        for (int i = 0; i < _stages.Length; i++)
        {
            if (_stages[i] != null)
            {
                _stages[i].StageCompleted += OnStageCompleted;
            }
        }

        // Start first stage if available
        _currentIndex = 0;
        if (_stages.Length > 0 && _stages[0] != null)
        {
            ActivateCurrentStage();
        }
        else
        {
            Debug.LogError("No valid stages found in TutorialController!");
        }
    }

    private void ActivateCurrentStage()
    {
        if (_currentIndex < _stages.Length && _stages[_currentIndex] != null)
        {
            Debug.Log($"Activating stage {_currentIndex}");
            _stages[_currentIndex].ActivateStage();
        }
    }

    private void OnStageCompleted()
    {
        Debug.Log($"Stage {_currentIndex} completed");
        
        // Hide the one that just finished
        _stages[_currentIndex].DeactivateStage();
        
        // Move to next
        _currentIndex++;
        if (_currentIndex < _stages.Length)
            ActivateCurrentStage();
        else
            Debug.Log("Tutorial fully complete");
    }

    public void DisableAllPlayerControls()
    {
        var playerObjects = FindObjectsOfType<TugboatMovementWFloat>();
        foreach (var player in playerObjects)
        {
            player.enabled = false;
        }
    }

    public void EnableAllPlayerControls()
    {
        var playerObjects = FindObjectsOfType<TugboatMovementWFloat>();
        foreach (var player in playerObjects)
        {
            player.enabled = true;
        }
    }
}