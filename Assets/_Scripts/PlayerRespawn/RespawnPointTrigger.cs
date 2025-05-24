using UnityEngine;

public class RespawnPointTrigger : MonoBehaviour
{


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
}
