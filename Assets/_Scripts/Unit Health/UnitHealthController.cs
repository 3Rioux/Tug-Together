using System.Collections;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEditor.ShaderGraph;
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


    [Header("Player Health: ")]
    public int MaxHealth = 100;
    public GameObject HealthParent;
    [SerializeField] private int CurrentUnitHeath = 100;
    //[SerializeField] private int currentHealth;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Slider healthBar;

    //public UnitHealth CurrentUnitHeath = new UnitHealth(100, 100); //make it public so that the other scripts can damage this unit
    //public NetworkVariable<int> CurrentUnitHeath = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
        //CurrentUnitHeath.Value = new UnitHealth(MaxHealth, MaxHealth);

        //lets simplify things lol:
        healthBar.maxValue = MaxHealth;
        OnHealthChanged();


        controls = new BoatInputActions();

        // Bind the KillPlayer action
        //controls.Boat.KillPlayer.performed += ctx => OnKillPlayer();


    }

    private void OnEnable()
    {
        // Disable the action map
        controls.Boat.Enable();
    }

    private void OnDisable()
    {
        controls.Boat.Disable();
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
            Debug.LogError($"Not owner{IsOwner} or player{IsLocalPlayer} LocalPlayerHealthController", this);
        }
        // Initialize health and checkpoint on server
        CurrentUnitHeath = MaxHealth;
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
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            UnitTakeDamage(20);
            //Debug.Log(gm_reference.PlayerHeath.Health.ToString());
        }

        //test healing damage current key == e
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
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
        while (CurrentUnitHeath < MaxHealth && timeSinceLastDamage >= healDelay)
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
        CurrentUnitHeath = Mathf.Max(CurrentUnitHeath - damage, 0);
        OnHealthChanged();
        

        // Start invincibility period
        StartCoroutine(InvincibilityCoroutine());

        //display current health to the user 
        //LevelManager.Instance._playerHealthBar.SetHealth(LevelManager.Instance.PlayerHeath.NetworkUnitCurrentHealth);
        Debug.Log($"{name} took {damage} damage, health now {CurrentUnitHeath}.");

        // Debug.Log(LevelManager.Instance.PlayerHeath.NetworkUnitCurrentHealth.ToString());

        if (CurrentUnitHeath <= 0)
        {
            Die();
            CurrentUnitHeath = MaxHealth; //reset Health 
            OnHealthChanged();
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
    //    CurrentUnitHeath.Value = Mathf.Max(CurrentUnitHeath.Value - damage, 0);
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
        CurrentUnitHeath = Mathf.Min(CurrentUnitHeath + healing, MaxHealth);
        OnHealthChanged();

        //display current health to the user 
        //LevelManager.Instance._playerHealthBar.SetHealth(LevelManager.Instance.PlayerHeath.NetworkUnitCurrentHealth);

        Debug.Log("Current Health" + CurrentUnitHeath.ToString());
    }

    //[ServerRpc(RequireOwnership = false)]
    //public void HealServerRpc(int healing)
    //{
    //    if (!IsServer) return;
    //    CurrentUnitHeath.Value = Mathf.Min(CurrentUnitHeath.Value + healing, MaxHealth);
    //}

    public void RespawnHealthSet(Transform respawnPos)
    {
        //if (!IsOwner) return;
        this.isDead = false;

        this.gameObject.transform.position = respawnPos.position;

        CurrentUnitHeath = MaxHealth;
        //Show boat: 
        boatModel.SetActive(true);
        boatEffect.SetActive(true);

        tugSpringTugSystem.isDead = this.isDead;

        OnHealthChanged();
    }


    private void OnHealthChanged()
    {
        healthBar.value = CurrentUnitHeath;
    }

  

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


            LevelVariableManager.Instance.GlobalPlayerRespawnController.TriggerDeath(CurrentUnitHeath);
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
    //    if (!CurrentUnitHeath.IsUnitHealthFull())
    //    {
    //        UnitHeal(healing);
    //        return true;//item was used 
    //    }
    //    else return false; // item not used health was already full 
    //}

}
