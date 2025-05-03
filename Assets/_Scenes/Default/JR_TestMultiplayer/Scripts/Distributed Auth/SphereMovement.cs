#if NGO_2_OR_NEWER && SERVICES_INSTALLED
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Center.NetcodeForGameObjectsExample.DistributedAuthority
{
    /// <summary>
    /// Simple network behaviour that moves an object up and down in a sine wave pattern.
    /// </summary>
    public class SphereMovement : NetworkBehaviour
    {
        /// <summary>
        /// The frequency of the oscillation.
        /// </summary>
        public float Frequency = 1f;

        /// <summary>
        /// The amplitude of the oscillation.
        /// </summary>
        public float Amplitude = 1.5f;

        /// <summary>
        /// The height the object will oscillate around.
        /// </summary>
        public float BaseHeight = 1.5f;

        // The network tick when this object was spawned
        NetworkVariable<int> m_SpawnedTick = new();

        /// <summary>
        /// Called when the object is spawned on the network.
        /// Sets the name of the object.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            name = $"Sphere-{NetworkObjectId}";
        }

        protected override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();

            // Only set init values if we have authority
            if (!IsSpawned || !HasAuthority)
                return;

            // Save off the network tick this object was spawned for motion reference purposes
            m_SpawnedTick.Value = NetworkManager.ServerTime.Tick;
            Debug.Log($"{name} Spawned! Tick: {m_SpawnedTick.Value}");
        }

        void Update()
        {
            // Only update the position if the object is spawned and we have authority
            if (!IsSpawned || !HasAuthority)
            {
                return;
            }

            UpdatePosition();
        }

        void UpdatePosition()
        {
            var currentPosition = transform.position;

            // Get the start time based on the delta from the current tick and starting tick
            var startTime = NetworkManager.ServerTime.TimeTicksAgo(NetworkManager.ServerTime.Tick - m_SpawnedTick.Value);

            // Get the delta time that has passed since spawning
            var timePassedSinceSpawn = NetworkManager.ServerTime.TimeAsFloat - startTime.TimeAsFloat;

            // Calculate the Sine Movement
            var radianAngle = timePassedSinceSpawn % (2 * Mathf.PI);
            var sineOfAngle = Mathf.Sin(radianAngle * Frequency);
            currentPosition.y = BaseHeight + (sineOfAngle * Amplitude);

            // Lerp to the new position for smooth authority side motion.
            transform.position = Vector3.Lerp(transform.position, currentPosition, Time.deltaTime);
        }
    }
}
#endif
