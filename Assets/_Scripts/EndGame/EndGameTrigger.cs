using Unity.Netcode;
using UnityEngine;

public class EndGameTrigger : NetworkBehaviour
{
    [Tooltip("Assign the Game Over Canvas that should be shown on all clients.")]
    [SerializeField] private GameObject gameOverCanvasPrefab; // optional if each client has it already

    private void Start()
    {
        gameOverCanvasPrefab.SetActive(false); // off by default
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // Only the server should handle the trigger

        if (other.CompareTag("Barge"))
        {
            Debug.Log("Barge has entered the end zone!");
            TriggerGameOverClientRpc();
        }
    }

    [ClientRpc]
    void TriggerGameOverClientRpc()
    {
        Debug.Log("Triggering Game Over UI on client");

        // Try to find a canvas tagged or named appropriately, or make this reference explicit
        //GameObject canvas = GameObject.Find("GameOverCanvas"); // or use a static reference/UI manager
        GameObject canvas = gameOverCanvasPrefab;
        if (canvas != null)
        {
            canvas.SetActive(true);
        }
        else
        {
            Debug.LogWarning("GameOverCanvas not found in the scene.");
        }
    }
}
