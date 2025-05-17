using UnityEngine;

[RequireComponent (typeof(Collider))]
public class HideHealthBar : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.gameObject.GetComponent<UnitHealthController>().HealthParent.SetActive(false);
        }
    }
}
