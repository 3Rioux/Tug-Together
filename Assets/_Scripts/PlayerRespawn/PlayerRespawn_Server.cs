using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;

public class PlayerRespawn_Server : NetworkBehaviour
{
    public static PlayerRespawn_Server Instance;

    #region Variables

    //[Header("Health")]
    //public int maxHealth = 100;
    //// Server-authoritative health. Default perms: EveryoneRead, ServerWrite :contentReference[oaicite:2]{index=2}.
    //public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100,
    //    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    [Header("Respawn Settings")]
    public float respawnDelay = 5f;
    private Vector3 respawnPosition;  // last checkpoint location
    [SerializeField] private Vector3 deathTempPosition; // this is the position ALL players go to when they die 

    [Header("References")]
    public GameObject playerModel;        // the root/model GameObject to hide
    public MonoBehaviour playerController; // the input/movement script to disable
    public Camera playerCamera;           // the player’s main camera
    public Camera spectatorCamera;        // a spectator/free camera
    public GameObject respawnUIPrefab;    // UI prefab with a countdown TextMeshProUGUI



    private UnitHealthController _localPlayerHealthController;
    private GameObject respawnUIInstance;
    private bool isDead = false;

    #endregion


    private void Awake()
    {
        if (!IsLocalPlayer && !IsOwner) return;

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
        if (IsServer)
        {
            // Initialize health and checkpoint on server
            //currentHealth.Value = maxHealth;
            respawnPosition = transform.position;  // default spawn
            //currentHealth.OnValueChanged += OnHealthChanged;

        }
        // Ensure spectator camera is off initially (on client)
        if (IsLocalPlayer)
        {
            spectatorCamera.enabled = false;
            _localPlayerHealthController = this.gameObject.GetComponent<UnitHealthController>();
        }
    }

    // Server-only handler: when health changes, check for death
    private void OnHealthChanged(int oldValue, int newValue)
    {
        if (!IsServer) return;
        if (!isDead && newValue <= 0)
        {
            isDead = true;
            HandleDeath();
        }
    }

    public void TriggerDeath(int currentHealth)
    {
        if (!IsServer) return;
        if (!isDead && currentHealth <= 0)
        {
            isDead = true;
            HandleDeath();
        }
    }


    // Client-side method (RPC) to display respawn UI, switch cameras, etc.
    [ClientRpc]
    private void ShowRespawnUIClientRpc(ClientRpcParams rpcParams = default)
    {
        // Only the owning client executes this block
        if (!IsOwner) return;

        // Hide player visuals and disable input locally
        playerModel.SetActive(false);
        playerController.enabled = false;

        // Switch to spectator camera
        playerCamera.enabled = false;
        spectatorCamera.enabled = true;

        // Instantiate and show respawn UI (with a TextMeshPro countdown)
        respawnUIInstance = Instantiate(respawnUIPrefab);
        respawnUIInstance.SetActive(true);

        // Start the countdown on the UI
        StartCoroutine(RespawnCountdown(respawnDelay));
    }


    // Client-side method (RPC) to hide respawn UI and restore player view
    [ClientRpc]
    private void HideRespawnUIClientRpc(ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;

        // Destroy the respawn UI
        if (respawnUIInstance != null)
        {
            Destroy(respawnUIInstance);
        }

        // Switch back to player camera
        spectatorCamera.enabled = false;
        playerCamera.enabled = true;

        // Re-enable player visuals and input locally
        playerModel.SetActive(true);
        playerController.enabled = true;
    }


    // Coroutine for the UI countdown (runs on client)
    private IEnumerator RespawnCountdown(float seconds)
    {
        TextMeshProUGUI countdownText = respawnUIInstance.GetComponentInChildren<TextMeshProUGUI>();
        float remaining = seconds;
        while (remaining > 0)
        {
            countdownText.text = "Respawn in " + Mathf.CeilToInt(remaining).ToString();
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }
        countdownText.text = "Respawning...";
    }


     // Server-side death handling: disable input and show UI on client
    private void HandleDeath()
    {
        // Disable input on server side as well (optional, for host)
        playerController.enabled = false;

        // Invoke ClientRpc to show UI/spectator for owner (using TargetClientIds):contentReference[oaicite:3]{index=3}
        ShowRespawnUIClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
        });

        // Schedule actual respawn after delay
        Invoke(nameof(Respawn), respawnDelay);
    }

   
    // Server-side respawn: move player and restore health
    private void Respawn()
    {
        if (!IsServer) return;

        // Teleport to last checkpoint and reset health
        transform.position = respawnPosition;
       
       // LocalPlayerHealthController.HealServerRpc(LocalPlayerHealthController.MaxHealth);
        _localPlayerHealthController.CurrentUnitHeath = _localPlayerHealthController.MaxHealth;

        //he is alive again!!!
        isDead = false;

        // Notify client to hide UI and re-enable player view
        HideRespawnUIClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
        });
    }

    // Public ServerRpc for taking damage (clients request server to apply damage)
    //[ServerRpc(RequireOwnership = false)]
    //public void TakeDamageServerRpc(int damage)
    //{
    //    if (!IsServer) return;
    //    currentHealth.Value = Mathf.Max(currentHealth.Value - damage, 0);
    //}

    // ServerRpc to update the checkpoint/respawn position (called from client trigger)
    [ServerRpc(RequireOwnership = false)]
    public void UpdateSpawnPointServerRpc(Vector3 newPosition)
    {
        if (!IsServer) return;
        respawnPosition = newPosition;
    }

}
