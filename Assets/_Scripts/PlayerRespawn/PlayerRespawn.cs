using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor.PackageManager;
using System.Linq;
using UnityEngine.Rendering.HighDefinition;

public class PlayerRespawn : MonoBehaviour
{
    public static PlayerRespawn Instance;

    #region Variables

    //[Header("Health")]
    //public int maxHealth = 100;
    //// Server-authoritative health. Default perms: EveryoneRead, ServerWrite :contentReference[oaicite:2]{index=2}.
    //public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100,
    //    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Spawn Validation Settings")]
    [SerializeField] private WaterSurface waterSurface; // Assign your HDRP Water Surface
    [SerializeField] private Vector3 playerBoatDimensions = new Vector3(2f, 1.5f, 4f); // Approx. L x H x W of player boat
    [SerializeField] private LayerMask obstructionLayers; // Layers to check for collisions (e.g., Default, Obstacles, Environment)
    [SerializeField] private float waterHeightTolerance = 0.5f; // How far above/below water surface is acceptable
    [SerializeField] private float minDepthForSpawn = 0.2f; // Min water depth required at spawn point
    [SerializeField] private float maxSpawnHeightAboveWater = 1.0f; // Max height spawn point can be above water


    [Header("Respawn Settings")]
    public float respawnDelay = 5f;
    [SerializeField] private Transform respawnPosition;  // last checkpoint location
    [SerializeField] private List<Transform> listRespawnPosition;  // last checkpoint location
    [SerializeField] private List<Transform> listBackupRespawnPosition;  // last checkpoint location
    [SerializeField] private Transform deathTempPosition; // this is the position ALL players go to when they die 

    [Header("References")]
    public GameObject playerModel;                                      // the root/model GameObject to hide
    // public MonoBehaviour playerController;                           // the input/movement script to disable
    //public Camera playerCamera;                                       // the player’s main camera
    [SerializeField] private CinemachineCamera spectatorCamera;         // a spectator/free camera
    [SerializeField] private GameObject respawnUICanvas;                // UI prefab with a countdown TextMeshProUGUI
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Transform cameraDefaultLocation;

    public UnitHealthController LocalPlayerHealthController;
   
    private bool isDead = false;

    private GameObject _localPlayerGameObject;
    private TugboatMovementWFloat _tugboatMovement;

    #endregion


    private void Awake()
    {
       // if (!IsOwner) return;

        if (Instance == null )
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    void Start()
    {
        //LocalPlayerHealthController = this.gameObject.GetComponent<UnitHealthController>();
        deathTempPosition = LevelVariableManager.Instance.GlobalRespawnTempMovePoint;
       
        ////Fix the problem of every time a client quits and returns Index is x++ so it can exceed the total points max of 4 (0->3):
        //int localID = (int)NetworkManager.Singleton.LocalClientId;
        //if (localID >= 4)
        //{
        //    localID = 3;
        //}

        //// default spawn point is the local ID 
        //respawnPosition = listRespawnPosition[localID];  
        ////Bad because every player can get index 3 if they all quit enough lol

        //Instead just get the local player Index in the connected clients list
        this.respawnPosition = this.GetLocalPlayerIndex(LocalPlayerHealthController.PlayerNetObj.OwnerClientId);

        spectatorCamera.enabled = false;
        respawnUICanvas.SetActive(false);

        //// Ensure spectator camera is off initially (on client)
        //if (!LocalPlayerHealthController.PlayerNetObj.IsOwner)
        //{

        //    //LocalPlayerHealthController = this.gameObject.GetComponent<UnitHealthController>();
        //    deathTempPosition = LevelVariableManager.Instance.GlobalRespawnTempMovePoint;

        //    _localPlayerGameObject = LocalPlayerHealthController.gameObject;
        //    _tugboatMovement = _localPlayerGameObject.GetComponent<TugboatMovementWFloat>();
        //}

    }

    public void TriggerDeath(int currentHealth)
    {
        if (waterSurface == null && LevelVariableManager.Instance != null)
        {
            waterSurface = LevelVariableManager.Instance.GlobalWaterSurface;
        }

        if (_localPlayerGameObject == null)
        {
            _localPlayerGameObject = LocalPlayerHealthController.gameObject;
            _tugboatMovement = _localPlayerGameObject.GetComponent<TugboatMovementWFloat>();
        }

        //if (!LocalPlayerHealthController.PlayerNetObj.IsOwner) return;
        if (!isDead && currentHealth <= 0)
        {
            isDead = true;
            HandleDeath();
        }
    }


    // Display respawn UI, switch cameras, etc.

    private void ShowRespawnUI()
    {
        // Only the owning client executes this block
        //if (!LocalPlayerHealthController.PlayerNetObj.IsOwner) return;

        // Hide player visuals and disable input locally
        //playerModel.SetActive(false);
        _tugboatMovement.SetControlEnabled(false);
        //playerController.enabled = false;

        // Switch to spectator camera
        //playerCamera.enabled = false;
        //spectatorCamera.transform = cameraDefaultLocation;
        spectatorCamera.enabled = true;

        // Instantiate and show respawn UI (with a TextMeshPro countdown)
        //respawnUI = Instantiate(respawnUICanvas);
        respawnUICanvas.SetActive(true);

        _localPlayerGameObject.transform.position = deathTempPosition.position;

        // Start the countdown on the UI
        StartCoroutine(RespawnCountdown(respawnDelay));
    }


    // hide respawn UI and restore player view
    private void HideRespawnUI()
    {
        //if (!LocalPlayerHealthController.PlayerNetObj.IsOwner) return;

        // hide the respawn UI
        if (respawnUICanvas != null)
        {
            respawnUICanvas.SetActive(false);
        }

        // Switch back to player camera
        spectatorCamera.enabled = false;
        //playerCamera.enabled = true;

        // Re-enable player visuals and input locally
        //playerModel.SetActive(true);
    }


    // Coroutine for the UI countdown (runs on client)
    private IEnumerator RespawnCountdown(float seconds)
    {

       if(countdownText == null) countdownText = respawnUICanvas.GetComponentInChildren<TextMeshProUGUI>();
        float remaining = seconds;
        while (remaining > 0)
        {
            //countdownText.text = "Respawn in " + Mathf.CeilToInt(remaining).ToString();
            countdownText.text = Mathf.CeilToInt(remaining).ToString();
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }
        countdownText.text = "Respawning...";
        yield return new WaitForSeconds(1f);
    }


     // Server-side death handling: disable input and show UI on client
    private void HandleDeath()
    {
        // Invoke ClientRpc to show UI/spectator for owner (using TargetClientIds):contentReference[oaicite:3]{index=3}
        ShowRespawnUI();

        //Update the Respawn point
        //RespawnPointManager.Instance.RespawnPointRequest();

        // Schedule actual respawn after delay
        Invoke(nameof(Respawn), respawnDelay);
    }

   
    // Server-side respawn: move player and restore health
    private void Respawn()
    {
        if (!LocalPlayerHealthController.PlayerNetObj.IsOwner) return;

        // Teleport to last checkpoint and reset health
        this.respawnPosition = this.GetLocalPlayerIndex(LocalPlayerHealthController.PlayerNetObj.OwnerClientId);
        //respawnPosition = listRespawnPosition[Random.Range(0, listRespawnPosition.Count)];
        //respawnPosition = RespawnPointManager.Instance.GetAvailableSpawnPointServerRpc(LocalPlayerHealthController.PlayerNetObj.OwnerClientId);
        //respawnPosition = listRespawnPosition[0];
        //_localPlayerGameObject.transform.position = respawnPosition.position;

        //allow user to control the boat again 
        _tugboatMovement.SetControlEnabled(true);

        // LocalPlayerHealthController.HealServerRpc(LocalPlayerHealthController.MaxHealth);
        //LocalPlayerHealthController.CurrentUnitHeath = LocalPlayerHealthController.MaxHealth;
        LocalPlayerHealthController.RespawnHealthSet(respawnPosition);

        //he is alive again!!!
        isDead = false;

        // Notify client to hide UI and re-enable player view
        HideRespawnUI();
    }


    // Update the checkpoint/respawn position (called from client trigger)
    public void UpdateSpawnPoint(Vector3 newPosition)
    {
        respawnPosition.position = newPosition;
    }

    private Transform GetLocalPlayerIndex(ulong localClientId)
    {
        if (listRespawnPosition == null || listRespawnPosition.Count == 0)
        {
            Debug.LogError("listRespawnPosition is not set or empty!");
            return null;
        }

        List<ulong> localConnectedPlayers = NetworkManager.Singleton.ConnectedClientsIds.ToList<ulong>();
        for (int clientIndex = 0; clientIndex < localConnectedPlayers.Count; clientIndex++)
        {
            if (localClientId == localConnectedPlayers[clientIndex])
            {
                //stop if local player found set position to the index in the list
                RespawnPointTrigger respawnPointValid;
                if (listRespawnPosition[clientIndex].gameObject.TryGetComponent<RespawnPointTrigger>(out respawnPointValid))
                {
                    if (respawnPointValid.IsValidSpawnPoint())
                    {
                        //return the respawn point in the normal list 
                        return listRespawnPosition[clientIndex];
                    }else
                    {
                        //return a point in the backup list
                        return listBackupRespawnPosition[clientIndex];
                    }
                }
                else
                {
                    Debug.LogError("Failed to get RespawnPointTrigger.", this);
                }

                //return listRespawnPosition[clientIndex];
            }
        }//end for loop 

        //have a default fallback return if something goes wrong
        return listRespawnPosition[0];
    }

    #region SpawnPointValidationChecker(WorksButWontSetInTimeIThink)

    private Transform GetLocalPlayerIndexWithCheck(ulong localClientId)
    {
        if (listRespawnPosition == null || listRespawnPosition.Count == 0)
        {
            Debug.LogError("listRespawnPosition is not set or empty!");
            return null;
        }

        List<ulong> localConnectedPlayers = NetworkManager.Singleton.ConnectedClientsIds.ToList<ulong>();

        //incase respawn point is invalid: 
        int preferredIndex = -1;

        for (int clientIndex = 0; clientIndex < localConnectedPlayers.Count; clientIndex++)
        {
            if (localClientId == localConnectedPlayers[clientIndex])
            {
                preferredIndex = clientIndex;
                ////stop if local player found set position to the index in the list
                //return listRespawnPosition[clientIndex];
            }
        }


        // Ensure preferredIndex is within bounds of listRespawnPosition
        if (preferredIndex >= listRespawnPosition.Count)
        {
            preferredIndex = listRespawnPosition.Count - 1; // Cap at max index
            Debug.LogWarning($"Client index {preferredIndex} out of bounds for respawn list. Capping.");
        }
        if (preferredIndex < 0) preferredIndex = 0; // Should not happen if client is connected


        // 1. Try the preferred spawn point
        if (preferredIndex < listRespawnPosition.Count && preferredIndex >= 0)
        {
            Transform preferredSpawnPoint = listRespawnPosition[preferredIndex];
            if (IsSpawnPointValid(preferredSpawnPoint))
            {
                Debug.Log($"Player {localClientId} assigned preferred valid spawn point: {preferredSpawnPoint.name}");
                return preferredSpawnPoint;
            }
            else
            {
                Debug.LogWarning($"Preferred spawn point {preferredSpawnPoint.name} for player {localClientId} is invalid. Searching for others.");
            }
        }


        // 2. If preferred is invalid or not found, iterate through all spawn points
        //    to find the first available valid one.
        //    You could shuffle this list or iterate in a specific order if desired.
        for (int i = 0; i < listRespawnPosition.Count; i++)
        {
            // Skip the one we already checked if it was the preferred one
            if (i == preferredIndex && preferredIndex < listRespawnPosition.Count && preferredIndex >= 0) continue;

            Transform potentialSpawnPoint = listRespawnPosition[i];
            if (IsSpawnPointValid(potentialSpawnPoint))
            {
                Debug.Log($"Player {localClientId} assigned fallback valid spawn point: {potentialSpawnPoint.name}");
                return potentialSpawnPoint;
            }
        }


        // 3. If no valid spawn points are found after checking all.
        Debug.LogError($"CRITICAL: No valid spawn points found for player {localClientId} after checking all options!");

        return null;
    }

    /// <summary>
    /// Checks if a given spawn point transform is valid for spawning a player boat.
    /// THIS MUST BE CALLED ON THE SERVER.
    /// </summary>
    /// <param name="spawnPoint">The Transform of the potential spawn point.</param>
    /// <returns>True if the spawn point is valid, false otherwise.</returns>
    public bool IsSpawnPointValid(Transform spawnPoint)
    {
        if (spawnPoint == null) return false;

        // 1. Obstruction Check
        // We use OverlapBox. Half extents are half of the dimensions.
        // The center of the box check should be slightly above the spawnPoint.position if the pivot is at the base.
        // Or, adjust spawnPoint's y position to be the center of the boat.
        // For simplicity, let's assume spawnPoint.position is the desired center of the spawned boat.
        Vector3 checkPosition = spawnPoint.position;
        Quaternion checkRotation = spawnPoint.rotation; // Consider boat's orientation
        Vector3 halfExtents = playerBoatDimensions / 2f;

        Collider[] colliders = Physics.OverlapBox(checkPosition, halfExtents, checkRotation, obstructionLayers, QueryTriggerInteraction.Ignore);

        // It's important to ensure the OverlapBox doesn't hit the main boat itself if spawn points are children.
        // One way is to put the main boat on a layer not included in obstructionLayers.
        // Or, iterate through colliders and ignore specific known colliders (e.g., the main boat's collider).
        foreach (Collider col in colliders)
        {
            // Example: If your main boat has a specific tag or component you can identify
            // if (col.CompareTag("MainShipCollider")) continue;
            // if (col.GetComponentInParent<MainShipIdentifier>() != null) continue; // Assuming you have such a component

            // If any other collider is found, it's obstructed.
            // You might need more sophisticated filtering here depending on your setup.
            // For now, any hit on an obstruction layer is considered invalid.
            Debug.LogWarning($"Spawn point {spawnPoint.name} at {spawnPoint.position} is obstructed by {col.name}.", spawnPoint);
            return false;
        }

        // 2. Water Check
        if (waterSurface != null)
        {
            WaterSearchParameters searchParams = new WaterSearchParameters();
            searchParams.targetPositionWS = spawnPoint.position;
            //searchParams.startPositionWS = spawnPoint.position; // Can refine start position if needed
            searchParams.error = 0.01f; // Smaller error for more precision
            searchParams.maxIterations = 8;

            if (waterSurface.ProjectPointOnWaterSurface(searchParams, out WaterSearchResult result))
            {
                float waterHeight = result.projectedPositionWS.y;
                float spawnPointY = spawnPoint.position.y;

                // Check if spawn point is within acceptable vertical range of water surface
                if (spawnPointY < waterHeight - waterHeightTolerance - minDepthForSpawn) // Too deep or below water bed
                {
                    Debug.LogWarning($"Spawn point {spawnPoint.name} is too far below water surface ({spawnPointY} vs water {waterHeight}).", spawnPoint);
                    return false;
                }
                if (spawnPointY > waterHeight + maxSpawnHeightAboveWater) // Too high above water
                {
                    Debug.LogWarning($"Spawn point {spawnPoint.name} is too far above water surface ({spawnPointY} vs water {waterHeight}).", spawnPoint);
                    return false;
                }
                // Optional: Check water depth if necessary (e.g., raycast down from spawn point)
                // This is somewhat covered by the waterHeight check if the boat dimensions are considered.
            }
            else
            {
                // Water surface height couldn't be found at this position (e.g., spawn point is over land far from water)
                Debug.LogWarning($"Spawn point {spawnPoint.name} is not over water (or query failed).", spawnPoint);
                return false;
            }
        }
        else
        {
            Debug.LogWarning("PlayerRespawn: WaterSurface reference is null, cannot perform water validation for spawn points.", this);
            // Decide if you want to allow spawning or not if water surface is missing.
            // For a boat game, probably not: return false;
            return false; // Or true if you want to bypass water check when surface is missing
        }

        // If all checks pass
        return true;
    }
    #endregion


    #region SpawnPlayerNETWORKExample
    //// Example of how you might call this (SERVER-SIDE)
    //public void RespawnPlayer(ulong clientId)
    //{
    //    if (!IsServer) return;

    //    Transform spawnPoint = GetValidSpawnPointForPlayer(clientId);

    //    if (spawnPoint != null)
    //    {
    //        // Get the player's NetworkObject
    //        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client)) <-------------------------------------?!?!?!?! Its so easy !?!?!?!
    //        {
    //            if (client.PlayerObject != null)
    //            {
    //                // This is a very basic respawn. You might have more complex logic
    //                // like disabling/enabling components, resetting health, etc.
    //                // This also assumes the player object itself is not destroyed but rather "moved".
    //                // If you destroy and recreate, the logic is different.
    //                client.PlayerObject.transform.position = spawnPoint.position;
    //                client.PlayerObject.transform.rotation = spawnPoint.rotation;
    //                Debug.Log($"Player {clientId} respawned at {spawnPoint.name}");

    //                // If you need to tell the client it has respawned (e.g., to re-enable controls),
    //                // you'd use a ClientRpc.
    //                // PlayerRespawnedClientRpc(clientId, spawnPoint.position, spawnPoint.rotation);
    //            }
    //            else
    //            {
    //                Debug.LogError($"Player object for client {clientId} not found!");
    //            }
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogError($"Failed to find a valid spawn point for player {clientId}. Player not respawned.");
    //        // Implement retry logic or inform the player
    //    }
    //}

    // [ClientRpc]
    // private void PlayerRespawnedClientRpc(ulong clientId, Vector3 position, Quaternion rotation, ClientRpcParams rpcParams = default)
    // {
    //     // This would only target the specific client if you filter rpcParams
    //     if (NetworkManager.Singleton.LocalClientId == clientId)
    //     {
    //         // Client-side logic after being respawned by the server
    //         // e.g., transform.position = position; transform.rotation = rotation;
    //         // playerController.EnableControls();
    //     }
    // }

    #endregion

}
