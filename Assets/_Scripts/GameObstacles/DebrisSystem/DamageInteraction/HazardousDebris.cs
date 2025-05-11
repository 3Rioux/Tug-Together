using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HazardousDebris : MonoBehaviour
{
    [Tooltip("How much damage this debris inflicts on collision.")]
    public int damageAmount = 10;

    [Tooltip("If true, debris is treated as a Trigger; otherwise, uses physics collisions.")]
    public bool useTrigger = true;

    [Tooltip("Should debris be returned to pool upon hitting a unit")]
    public bool destroyOnImpact = true;

    // Cache the Collider and ensure it’s set correctly
    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = useTrigger;
    }


    void OnValidate()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = useTrigger;
    }

    //Damage Unit if Trigger or Collision Enter 

    // Called when another collider enters this one (if isTrigger = true)
    void OnTriggerEnter(Collider other)
    {
        TryDamage(other.gameObject);
    }

    // Called on physics collision (if isTrigger = false)
    void OnCollisionEnter(Collision collision)
    {
        TryDamage(collision.gameObject);
    }

    
    /// <summary>
    /// Deals damage to the player 
    /// </summary>
    /// <param name="other"></param>
    private void TryDamage(GameObject other)
    {
        // Look for the IDamageable interface
        IDamageable dmg = other.GetComponent<IDamageable>();
        if (dmg != null)
        {
            dmg.UnitTakeDamage(damageAmount);

            if (destroyOnImpact)
            {
                // If using pooling, call your pool’s return method here instead
                Destroy(gameObject);
                // Example, assuming a static PoolManager:
                //PoolManager.Instance.ReturnToPool(gameObject);
            }
        }
    }

}
