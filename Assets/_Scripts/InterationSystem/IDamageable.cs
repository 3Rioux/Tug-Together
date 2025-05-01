using UnityEngine;

public interface IDamageable
{
    /// <summary>
    /// Apply damage to this object.
    /// </summary>
    /// <param name="amount">How much health to remove.</param>
    void UnitTakeDamage(int amount);
}
