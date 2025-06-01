using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LevelProgressionManager : NetworkBehaviour
{
    public static LevelProgressionManager Instance;

    [Tooltip("Stores the Levels Goals Messages")]
    [SerializeField] private List<string> missionGoals = new();

    [SerializeField] private NetworkVariable<int> currentGoalIndex = new(0);

    public delegate void MissionGoalUpdated(string newGoal);
    public static event MissionGoalUpdated OnMissionGoalUpdated;


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        currentGoalIndex.OnValueChanged += OnGoalIndexChanged;
        if (IsClient)
        {
            // Trigger UI update on client when joined
            OnGoalIndexChanged(0, currentGoalIndex.Value);
        }
    }

    private void OnGoalIndexChanged(int previous, int current)
    {
        if (current >= 0 && current < missionGoals.Count)
        {
            OnMissionGoalUpdated?.Invoke(missionGoals[current]);
        }
    }

    public void AdvanceToNextGoal()
    {
        Debug.Log("AdvanceToNextGoal Triggered");
        if (!IsServer) return;

        if (currentGoalIndex.Value < missionGoals.Count - 1)
        {
            currentGoalIndex.Value++;
        }
        else
        {
            Debug.Log("Final goal reached.");
        }
    }











}
