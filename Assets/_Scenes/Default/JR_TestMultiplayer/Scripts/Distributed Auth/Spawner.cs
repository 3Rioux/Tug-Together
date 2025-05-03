#if NGO_2_OR_NEWER && SERVICES_INSTALLED
using Unity.Netcode;
using UnityEngine;
#if NEW_INPUT_SYSTEM_INSTALLED
using UnityEngine.InputSystem;
#endif

namespace Unity.Multiplayer.Center.NetcodeForGameObjectsExample.DistributedAuthority
{
    /// <summary>
    /// Spawns <see cref="PrefabToSpawn"/> GameObject when the Space Bar is pressed.
    /// If you want to modify this Script please copy it into your own project and add it to your Player Prefab.
    /// </summary>
    public class Spawner : NetworkBehaviour
    {
        /// <summary>
        /// Prefab that will get spawned.
        /// </summary>
        public GameObject PrefabToSpawn;

        void Update()
        {
            if (!IsSpawned || !HasAuthority)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM && NEW_INPUT_SYSTEM_INSTALLED
            if (Keyboard.current.spaceKey.wasReleasedThisFrame)
#else
            // Old input backends are enabled.
            if (Input.GetKeyUp(KeyCode.Space))
#endif
            {
                SpawnSphere();
            }
        }

        void SpawnSphere()
        {
            var instance = Instantiate(PrefabToSpawn);
            instance.transform.position = transform.position;
            var instanceNetworkObject = instance.GetComponent<NetworkObject>();
            instanceNetworkObject.Spawn();
        }
    }
}
#endif
