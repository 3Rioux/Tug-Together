using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Script to detect if the barge has reached the next goal and lets the LevelProgressionManager instance know to AdvanceToNextGoal
/// (Run on the server mainly)
/// </summary>
public class MissionCheckpoint : MonoBehaviour
{
    [SerializeField] private string requiredTag = "Barge";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(requiredTag) && NetworkManager.Singleton.IsServer)
        {
            LevelProgressionManager.Instance.AdvanceToNextGoal();
        }
    }
}
