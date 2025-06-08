using UnityEngine;

/// <summary>
/// In theory was going to use this to lock a spawn point if a player was already there but changed the spawn code instead 
/// </summary>
public class RespawnPointTrigger : MonoBehaviour
{
    [SerializeField] private Vector3 halfExtents = new Vector3();
    [SerializeField] private LayerMask obstructionLayers; // Layers to check for collisions (e.g., Default, Obstacles, Environment)
    [SerializeField] private Collider[] colliders; // Store the current colliders the point is colliding with 


    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            UnitHealthController unitHPController = other.GetComponent<UnitHealthController>();
            if (unitHPController != null && !unitHPController.IsColliderActive)
            {
                StartCoroutine(unitHPController.EnableCollidersAfterDelay(1f, this.transform.position));
            }
        }
    }

    public bool IsValidSpawnPoint() 
    {
        Collider[] colliders = Physics.OverlapBox(this.transform.position, halfExtents, new Quaternion(0,0,0,0), obstructionLayers, QueryTriggerInteraction.Ignore);
        if (colliders.Length > 0)
        {
            return false;
        }

        return true;
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(this.transform.position, halfExtents);
    }
}
