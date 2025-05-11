using Unity.Netcode;
using UnityEngine;

/// <summary>
/// This script handles the whirlpool triggering / collision detection with the player.
/// Will slow down the player once player collides with it.
/// Optional Additions:
///     - Add Collision effects with the Barge (Ex: Damage or Slower speed)
/// </summary>
public class WhirlpoolTrigger : NetworkBehaviour
{
    [Tooltip("This variable will determine the amount to divide the collided objects speed by.")]
    [SerializeField] private float speedMultiplier = 0.5f;


    private void OnTriggerEnter(Collider other)
    {
        //if (!IsServer) return; // Only the server handles game logic in NGO
        Debug.Log($"Whirlpool Trigger Enter with -> Tag == {other.tag}.");

        if (other.CompareTag("Player"))
        {

            if (other.gameObject.TryGetComponent<TugboatMovementWFloat>(out var boat))
            {
                boat.throttleForceMultiplier = boat.defaultThrottleForceMultiplier * speedMultiplier;

                //Increase Drag when in the whirlpool
                other.gameObject.GetComponent<Rigidbody>().linearDamping = 2f;

                Debug.Log($"Founnd TugboatMovementWFloat Script on object!!! force multiplyer == {boat.defaultThrottleForceMultiplier * speedMultiplier}");

            }
            else
            {
                Debug.Log($"Failled to find TugboatMovementWFloat Script on object ");
            }
        }

        if (other.CompareTag("Barge"))
        {

        }
    }

    private void OnTriggerExit(Collider other)
    {
        //if (!IsServer) return;

        if (other.gameObject.TryGetComponent<TugboatMovementWFloat>(out var boat))
        {
            boat.throttleForceMultiplier = boat.defaultThrottleForceMultiplier; // Reset speed using boats saved default multiplyer
            
            //Reset Drag when leaving the whirlpool
            other.gameObject.GetComponent<Rigidbody>().linearDamping = 1f;
        }
    }
}
