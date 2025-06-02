using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LevelProgressionManager : NetworkBehaviour
{
    public static LevelProgressionManager Instance;

    [Tooltip("Stores the Levels Goals Messages")]
    [SerializeField] private List<string> missionGoals = new();
    /*   Mission Description for Clipboard:
     *   Mission #1-1: Find a way out from the basin deep enough for the barge to pass through.
     *   Mission #1-1: Use the buoys to navigate to the next area.
     *   Mission #2: Get past the Debris field without taking to much damage!
     *   Mission #3: Navigate through the whirlpool zone as fast as possible!
     *   Mission #4: Make your way to the entrance of the sand dune corridor by going left.
     *   
     *   Mission #5: Work together to get the barge through the narrow sand dune corridor without getting stuck!
     *   or 
     *   Mission #5: Work together to get through the narrow dune corridor without getting stuck!  <--Fits better
     *   
     *   Mission #6: Bring the barge to the dock! Good work out there!
     */

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
