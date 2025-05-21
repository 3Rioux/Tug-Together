using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))] // needs a collider of any kind attached 
public class UnitHealthController : NetworkBehaviour, IDamageable
{
    public NetworkObject PlayerNetObj;
    [SerializeField] private GameObject boatModel;
    [SerializeField] private GameObject boatEffect;
    [SerializeField] private GameObject boatName;
    [SerializeField] private TugboatMovementWFloat tugMovement;
    [SerializeField] private SpringTugSystem tugSpringTugSystem;


    [Header("Network Player Info Sync: ")]
    [SerializeField] private NetworkPlayerInfo netPlayerInfo;

    [Header("Player Health: ")]
    public int MaxHealth = 100;
    public GameObject HealthParent;
    
    //[SerializeField] private int currentHealth;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Slider healthBar;

    public event Action<int> OnHealthChanged;

    [SerializeField] private int currentUnitHealth = 100;
    // Property to encapsulate the field
    public int CurrentUnitHealth
    {
        get => currentUnitHealth;
        set
        {
            if (currentUnitHealth != value)
            {
                currentUnitHealth = value;
                OnHealthChanged?.Invoke(currentUnitHealth);
            }
        }
    }

    //public int CurrentUnitScore
    //{
    //    get => _currentUnitScore;
    //    set
    //    {
    //        if (_currentUnitScore != value)
    //        {
    //            _currentUnitScore = value;
    //            OnScoreChanged?.Invoke(_currentUnitScore);
    //        }
    //    }
    //}

    //public UnitHealth CurrentUnitHealth = new UnitHealth(100, 100); //make it public so that the other scripts can damage this unit
    //public NetworkVariable<int> CurrentUnitHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // public HealthBar _playerHealthBar; //reference to the healthBar or your damaged parts script stuff 

    [Header("Player Timed Heal: ")]
    [SerializeField] private bool canAutoHeal = false; // toggle if this unit can auto heal or not 
    [SerializeField] private float healDelay = 5f; // Time in seconds to wait before starting healing
    [SerializeField] private int healAmountPerSecond = 1; // Amount healed per second
    [SerializeField] private int healTimeDelay = 1; // Time Between heals

    //track when to auto heal
    private float timeSinceLastDamage = 0f; // Tracks time since last damage
    private bool isUnitHealing = false; // Tracks if healing coroutine is running

    [Header("Invincible State")]
    [SerializeField] private float invincibilityDuration = 2f; // seconds
    private bool isInvincible = false;


    private BoatInputActions controls;
    private bool isDead = false;


    private void Awake()
    {
       
        //currentHealth = MaxHealth;
        //CurrentUnitHealth.Value = new UnitHealth(MaxHealth, MaxHealth);

        //lets simplify things lol:
        healthBar.maxValue = MaxHealth;

        controls = new BoatInputActions();

        // Bind the KillPlayer action
        //controls.Boat.KillPlayer.performed += ctx => OnKillPlayer();
        netPlayerInfo = this.GetComponent<NetworkPlayerInfo>();

    }

    private void OnEnable()
    {
        if (controls != null)
        {//it was turning off this script sometimes (Dont know why so im just adding this check)
            // Disable the action map
            controls.Boat.Enable();
        }
    }

    private void OnDisable()
    {
        if (controls != null)
        {
            controls.Boat.Disable();
        }
    }

    //public override void OnNetworkSpawn()
    //{
    //    base.OnNetworkSpawn();

    //    //Get set the local net object to this gameobject:
    //    if (IsOwner)
    //    {
    //        PlayerNetObj = GetComponent<NetworkObject>();
    //        PlayerRespawn.Instance.LocalPlayerHealthController = this;
    //    }
    //}

    void Start()
    {


        //Get set the local net object to this gameobject:
        if (IsOwner && IsLocalPlayer)
        {
            OnHealthChanged += HandleHealthChanged;

            PlayerNetObj = GetComponent<NetworkObject>();
            tugMovement = GetComponent<TugboatMovementWFloat>();
            tugSpringTugSystem = GetComponent<SpringTugSystem>();

            if (PlayerRespawn.Instance != null)
            {
                PlayerRespawn.Instance.LocalPlayerHealthController = this;
                Debug.Log("Set LocalPlayerHealthController", this);
            }
            else
            {
                Debug.LogError("PlayerRespawn.Instance is null", this);
            }
        }
        else
        {
            Debug.LogError($"Not owner{IsOwner} or player{IsLocalPlayer} LocalPlayerHealthController {name}", this);
        }
        
        // Initialize health and checkpoint on server
        CurrentUnitHealth = MaxHealth;



        //    if (netPlayerInfo.IsNetworkPlayerInfoInitialised) OnHealthChanged();
    }

    private void Update()
    {
        // Increment time since last damage
        timeSinceLastDamage += Time.deltaTime;

        // Start healing if conditions are met
        if (!isUnitHealing && timeSinceLastDamage >= healDelay)
        {
            //Debug.Log("Start Healling");
            StartCoroutine(UnitTimeHeal());
        }

        //------------------TESTING--------------------------

#if UNITY_EDITOR
        //test taking damage current key == q
        if (Keyboard.current.digit1Key.wasPressedThisFrame && IsOwner)
        {
            UnitTakeDamage(20);
            //Debug.Log(gm_reference.PlayerHeath.Health.ToString());
        }

        //test healing damage current key == e
        if (Keyboard.current.digit2Key.wasPressedThisFrame && IsOwner)
        {
            UnitHeal(10);
            //Debug.Log(gm_reference.PlayerHeath.Health.ToString());
        }
#endif
        //------------------TESTING--------------------------
    }//end update

    /// <summary>
    /// Auto Heals the Unit gradually if not damaged for a set amount of time.
    /// </summary>
    private IEnumerator UnitTimeHeal()
    {
        isUnitHealing = true;

        //While Healt is not = Max Health && apply heal delay
        while (CurrentUnitHealth < MaxHealth && timeSinceLastDamage >= healDelay)
        {
            //healt Unit
            UnitHeal(healAmountPerSecond);
            yield return new WaitForSeconds(healTimeDelay); // Heal every second
        }

        //Done healing -> change state 
        isUnitHealing = false;
    }

    public void OnKillPlayer()
    {
        //Kill the player by making damage = MaxHealth * 2 (Just to be safe)
        UnitTakeDamage(MaxHealth * 2);
    }

    /// <summary>
    /// Method called when the Unit takes Damage. 
    /// @Daniel This is where you can add all the body part falling off triggers ----------------------------------------
    /// </summary>
    /// <param name="damage"></param>
    public void UnitTakeDamage(int damage)
    {
        if (isInvincible) return; // Ignore damage if invincible

        // Reset the damage timer
        timeSinceLastDamage = 0f;

        /// Apply damage
        CurrentUnitHealth = Mathf.Max(CurrentUnitHealth - damage, 0);
        //OnHealthChanged();
        

        // Start invincibility period
        StartCoroutine(InvincibilityCoroutine());

        //display current health to the user 
        //LevelManager.Instance._playerHealthBar.SetHealth(LevelManager.Instance.PlayerHeath.NetworkUnitCurrentHealth);
        Debug.Log($"{name} took {damage} damage, health now {CurrentUnitHealth}.");

        // Debug.Log(LevelManager.Instance.PlayerHeath.NetworkUnitCurrentHealth.ToString());

        if (CurrentUnitHealth <= 0)
        {
            Die();
            CurrentUnitHealth = MaxHealth; //reset Health 
            //OnHealthChanged();
        }
    }


    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    // Public ServerRpc for taking damage (clients request server to apply damage)
    //[ServerRpc(RequireOwnership = false)]
    //public void TakeDamageServerRpc(int damage)
    //{
    //    if (!IsServer) return;
    //    CurrentUnitHealth.Value = Mathf.Max(CurrentUnitHealth.Value - damage, 0);
    //}


    /// <summary>
    /// Method to handle the player healing. 
    /// increase unit Health and update Health bar 
    /// @Daniel This is where you can add all the body part being reattached ----------------------------------------
    /// </summary>
    /// <param name="healing"></param>
    private void UnitHeal(int healing)
    {
        //change current player health

        //HealServerRpc(healing);
        CurrentUnitHealth = Mathf.Min(CurrentUnitHealth + healing, MaxHealth);
        //OnHealthChanged();

        //display current health to the user 
        //LevelManager.Instance._playerHealthBar.SetHealth(LevelManager.Instance.PlayerHeath.NetworkUnitCurrentHealth);

        Debug.Log("Current Health" + CurrentUnitHealth.ToString());
    }

    //[ServerRpc(RequireOwnership = false)]
    //public void HealServerRpc(int healing)
    //{
    //    if (!IsServer) return;
    //    CurrentUnitHealth.Value = Mathf.Min(CurrentUnitHealth.Value + healing, MaxHealth);
    //}

    public void RespawnHealthSet(Transform respawnPos)
    {
        //if (!IsOwner) return;
        this.isDead = false;

        this.gameObject.transform.position = respawnPos.position;

        CurrentUnitHealth = MaxHealth;
        //Show boat: 
        boatModel.SetActive(true);
        boatEffect.SetActive(true);

        tugSpringTugSystem.isDead = this.isDead;

        //OnHealthChanged();
    }

    private UnitInfoReporter unitInfoReporter;

    private void HandleHealthChanged(int newHealth)
    {
        healthBar.value = newHealth;

        if (unitInfoReporter == null)
        {
            unitInfoReporter = GetComponent<UnitInfoReporter>();
        }

        unitInfoReporter.CurrentUnitHealth = newHealth;
            
        SendHealthToServerRpc(CurrentUnitHealth);

        //Also update the Network Health tracker for the player.
        //SyncHealthServerRpc();
    }


    [ServerRpc]
    private void SendHealthToServerRpc(int newHealth, ServerRpcParams rpcParams = default)
    {
        BroadcastHealthClientRpc(newHealth);
    }

    [ClientRpc]
    private void BroadcastHealthClientRpc(int newHealth)
    {
        netPlayerInfo.UpdateHealth(CurrentUnitHealth);
    }

    //[ServerRpc(RequireOwnership = false)]
    //private void SyncHealthServerRpc()
    //{
    //    netPlayerInfo.UpdateHealth(CurrentUnitHealth);
    //}

    public void Die()
    {
       
        if (IsLocalPlayer)
        {
            if (PlayerRespawn.Instance.LocalPlayerHealthController == null)
            {
                PlayerRespawn.Instance.LocalPlayerHealthController = this;
            }
            this.isDead = true;

            //hide boat: 
            boatModel.SetActive(false);
            boatEffect.SetActive(false);

            this.gameObject.transform.position = LevelVariableManager.Instance.GlobalRespawnTempMovePoint.position;

            //Make sure to Detach the player when dead 
            this.gameObject.GetComponent<SpringTugSystem>().Detach();
            this.gameObject.GetComponent<SpringTugSystem>().isDead = this.isDead;


            LevelVariableManager.Instance.GlobalPlayerRespawnController.TriggerDeath(CurrentUnitHealth);
            // Your death logic here...
            Debug.Log($"{name} died!");
        }else
        {
            Debug.Log($"{name} Cant die here not local player!");
        }
    }

    //===============Not yet part of the game===============

    ///// <summary>
    ///// method called when player Uses a medkit in the inventory 
    ///// </summary>
    ///// <param name="healing"></param>
    ///// <returns></returns>
    //public bool TryHealthItemPlayerHeal(int healing)
    //{
    //    if (!CurrentUnitHealth.IsUnitHealthFull())
    //    {
    //        UnitHeal(healing);
    //        return true;//item was used 
    //    }
    //    else return false; // item not used health was already full 
    //}

}
