using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;
using Unity.Cinemachine;

public class PlayerRespawn : NetworkBehaviour
{
    public static PlayerRespawn Instance;

    #region Variables

    //[Header("Health")]
    //public int maxHealth = 100;
    //// Server-authoritative health. Default perms: EveryoneRead, ServerWrite :contentReference[oaicite:2]{index=2}.
    //public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100,
    //    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    [Header("Respawn Settings")]
    public float respawnDelay = 5f;
    private Vector3 respawnPosition;  // last checkpoint location
    [SerializeField] private Transform deathTempPosition; // this is the position ALL players go to when they die 

    [Header("References")]
    public GameObject playerModel;                                      // the root/model GameObject to hide
    // public MonoBehaviour playerController;                           // the input/movement script to disable
    //public Camera playerCamera;                                       // the player’s main camera
    [SerializeField] private CinemachineCamera spectatorCamera;         // a spectator/free camera
    [SerializeField] private GameObject respawnUICanvas;                // UI prefab with a countdown TextMeshProUGUI
    [SerializeField] private TextMeshProUGUI countdownText;

    public UnitHealthController LocalPlayerHealthController;
   
    private bool isDead = false;

    private GameObject _localPlayerGameObject;
    private TugboatMovementWFloat _tugboatMovement;

    #endregion


    private void Awake()
    {
        if (!IsLocalPlayer) return;

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
        respawnPosition = transform.position;  // default spawn

        spectatorCamera.enabled = false;
        respawnUICanvas.SetActive(false);

        // Ensure spectator camera is off initially (on client)
        if (!LocalPlayerHealthController.PlayerNetObj.IsOwner) 
        {
           
            //LocalPlayerHealthController = this.gameObject.GetComponent<UnitHealthController>();
            deathTempPosition = LevelVariableManager.Instance.GlobalRespawnTempMovePoint;

            _localPlayerGameObject = LocalPlayerHealthController.gameObject;
            _tugboatMovement = _localPlayerGameObject.GetComponent<TugboatMovementWFloat>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    private void OnEnable()
    {
       
    }

    public void TriggerDeath(int currentHealth)
    {
        if (!LocalPlayerHealthController.PlayerNetObj.IsOwner) return;
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
        if (!LocalPlayerHealthController.PlayerNetObj.IsOwner) return;

        // Hide player visuals and disable input locally
        playerModel.SetActive(false);
        _tugboatMovement.SetControlEnabled(false);
        //playerController.enabled = false;

        // Switch to spectator camera
        //playerCamera.enabled = false;
        spectatorCamera.enabled = true;

        // Instantiate and show respawn UI (with a TextMeshPro countdown)
        //respawnUI = Instantiate(respawnUICanvas);
        respawnUICanvas.SetActive(true);

        // Start the countdown on the UI
        StartCoroutine(RespawnCountdown(respawnDelay));
    }


    // hide respawn UI and restore player view
    private void HideRespawnUI()
    {
        if (!LocalPlayerHealthController.PlayerNetObj.IsOwner) return;

        // hide the respawn UI
        if (respawnUICanvas != null)
        {
            respawnUICanvas.SetActive(false);
        }

        // Switch back to player camera
        spectatorCamera.enabled = false;
        //playerCamera.enabled = true;

        // Re-enable player visuals and input locally
        playerModel.SetActive(true);
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
    }


     // Server-side death handling: disable input and show UI on client
    private void HandleDeath()
    {
        // Invoke ClientRpc to show UI/spectator for owner (using TargetClientIds):contentReference[oaicite:3]{index=3}
        ShowRespawnUI();

        // Schedule actual respawn after delay
        Invoke(nameof(Respawn), respawnDelay);
    }

   
    // Server-side respawn: move player and restore health
    private void Respawn()
    {
      if (!LocalPlayerHealthController.PlayerNetObj.IsOwner) return;

        // Teleport to last checkpoint and reset health
        transform.position = respawnPosition;

        //allow user to control the boat again 
        _tugboatMovement.SetControlEnabled(true);

        // LocalPlayerHealthController.HealServerRpc(LocalPlayerHealthController.MaxHealth);
        LocalPlayerHealthController.CurrentUnitHeath = LocalPlayerHealthController.MaxHealth;

        //he is alive again!!!
        isDead = false;

        // Notify client to hide UI and re-enable player view
        HideRespawnUI();
    }

    // Public ServerRpc for taking damage (clients request server to apply damage)
    //[ServerRpc(RequireOwnership = false)]
    //public void TakeDamageServerRpc(int damage)
    //{
    //    if (!IsServer) return;
    //    currentHealth.Value = Mathf.Max(currentHealth.Value - damage, 0);
    //}

    // Update the checkpoint/respawn position (called from client trigger)

    public void UpdateSpawnPoint(Vector3 newPosition)
    {
        respawnPosition = newPosition;
    }

}
