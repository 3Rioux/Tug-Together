using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))] // needs a collider of any kind attached 
public class UnitHealthController : NetworkBehaviour, IDamageable
{
#if UNITY_EDITOR
    private bool toggleInvincibility = false; // bool to allow me to change the Invincibility on/off during testing without changing the player Prefab (merge conflicts) 
#else
//in build will always be ON as a safety mesure incase we forget to change it back 
    private bool toggleInvincibility = true; // bool to allow me to change the Invincibility on/off during testing without changing the player Prefab (merge conflicts) 
#endif

    public NetworkObject PlayerNetObj;
    [SerializeField] private GameObject boatModel;
    [SerializeField] private GameObject boatEffect;
    [SerializeField] private GameObject boatName;
    [SerializeField] private TugboatMovementWFloat tugMovement;
    [SerializeField] private SpringTugSystem tugSpringTugSystem;


    //[Header("Network Player Info Sync: ")]
    //[SerializeField] private NetworkPlayerInfo netPlayerInfo;

    [Header("Player Health: ")]
    public int MaxHealth = 100;
    public GameObject HealthParent;

    //[SerializeField] private int currentHealth;
    [SerializeField] private TextMeshProUGUI healthText;
    [Tooltip("This variable will store the UI health bar that only the local player will be able to see.")]
    [SerializeField] private Slider healthBar;
    [Tooltip("This variable will store the above head health bar that other players will be able to see each players health.")]
    [SerializeField] private Slider healthBarGlobal;

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

    [Header("Animation")]
    [SerializeField] private float closeAnimationDuration = 1.5f; // Duration of the alpha transition


    [Header("Colliders")]
    [SerializeField] private Collider[] allColliders = new Collider[4]; // max 3 colliders 
    public bool IsColliderActive = true;

    private BoatInputActions controls;
    private bool isDead = false;


    private void Awake()
    {

        //currentHealth = MaxHealth;
        //CurrentUnitHealth.Value = new UnitHealth(MaxHealth, MaxHealth);

        //lets simplify things lol:
        healthBar.maxValue = MaxHealth;
        healthBar.value = MaxHealth;

        healthBarGlobal.maxValue = MaxHealth;
        healthBarGlobal.value = MaxHealth;

        controls = new BoatInputActions();

        // Bind the KillPlayer action
        controls.Boat.KillPlayer.performed += ctx => OnKillPlayer();
        //netPlayerInfo = this.GetComponent<NetworkPlayerInfo>();

        //Make sure the Health bar is only active if they are the owner (overlap problems)
        if (IsOwner)
        {
            this.healthBar.gameObject.SetActive(true);
            //hide overhead bar if owner 
            this.healthBarGlobal.gameObject.SetActive(false);
        }else
        {
            this.healthBar.gameObject.SetActive(false);
            //show overhead bar if NOT owner 
            this.healthBarGlobal.gameObject.SetActive(true);
        }
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
        //All Players need to set this
        PlayerNetObj = GetComponent<NetworkObject>();

        //allColliders = this.GetComponentsInChildren<Collider>(); // <-- All the children colliders as well 
        allColliders = this.GetComponents<Collider>(); // just the colliders on this gameobject 
        Debug.Log("Found " + allColliders.Length + " colliders.");
        allColliders = this.GetComponentsInChildren<Collider>();
        Debug.Log("Found " + allColliders.Length + " colliders.");


        //Get set the local net object to this gameobject:
        if (IsOwner && IsLocalPlayer)
        {
            this.healthBar.gameObject.SetActive(true);
            OnHealthChanged += HandleHealthChanged;

            
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
        if (isInvincible || isDead) return; // Ignore damage if invincible or already Dead

        // Reset the damage timer
        timeSinceLastDamage = 0f;

        /// Apply damage
        CurrentUnitHealth = Mathf.Max(CurrentUnitHealth - damage, 0);
        //OnHealthChanged();


        // Start invincibility period
        if(toggleInvincibility) StartCoroutine(InvincibilityCoroutine());

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

    private UnitInfoReporter unitInfoReporter;

    private void HandleHealthChanged(int newHealth)
    {
        healthBar.value = newHealth;



        if (unitInfoReporter == null)
        {
            unitInfoReporter = GetComponent<UnitInfoReporter>();
        }

        unitInfoReporter.CurrentUnitHealth = newHealth;

        //Update the Health Bar on all copies of this player in the other clients games.
        UpdateHealthBarServerRpc(CurrentUnitHealth, PlayerNetObj.OwnerClientId);
    }


    [ServerRpc(RequireOwnership = false)]
    private void UpdateHealthBarServerRpc(int newHealth, ulong playerObjectId,  ServerRpcParams rpcParams = default)
    {
        BroadcastHealthBarClientRpc(newHealth, playerObjectId);
    }

    [ClientRpc]
    private void BroadcastHealthBarClientRpc(int newHealth, ulong playerObjectId)
    {
        if (playerObjectId == this.PlayerNetObj.OwnerClientId)
        {
            healthBarGlobal.value = newHealth;
            OnOverHeadHealthBarEffect(newHealth); // *** Always running might want to add a bool to only run to turn on/off 
        }
    }

    /// <summary>
    /// Add an effect to the health bar thats changed values, such as getting an outline + being enabled 
    /// </summary>
    private void OnOverHeadHealthBarEffect(int newHealth)
    {
        // Make sure the health bar is active for the fade effect
        if (!healthBarGlobal.gameObject.activeSelf)
        {
            healthBarGlobal.gameObject.SetActive(true);
        }

        // If health is full, fade out the health bar (alpha to 0)
        if (newHealth >= MaxHealth)
        {
            StartCoroutine(HideShowGlobalHealthBar(0f, closeAnimationDuration)); 
        }
        else
        {
            // Otherwise, fade in the health bar (alpha to 1)
            StartCoroutine(HideShowGlobalHealthBar(1f, 0f)); // instantly turn it on when damaged 
        }
    }


    private IEnumerator HideShowGlobalHealthBar(float alphaGoal, float animationDuration)
    {
        CanvasGroup canvasGroup = healthBarGlobal.gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = healthBarGlobal.gameObject.AddComponent<CanvasGroup>();
        }

        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / animationDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, alphaGoal, t);
            yield return null;
        }

        // Ensure we reach the exact target alpha
        canvasGroup.alpha = alphaGoal;
    }

    #region DEATH

    public void Die()
    {
        if (IsLocalPlayer)
        {
            if (PlayerRespawn.Instance.LocalPlayerHealthController == null)
            {
                PlayerRespawn.Instance.LocalPlayerHealthController = this;
            }
            this.isDead = true;

            GetComponent<NetworkTransform>().PositionLerpSmoothing = false;

            //disable all colliders on the player
            foreach (Collider col in allColliders)
            {
                col.enabled = false;
            }
            this.IsColliderActive = false;

            //Tell all copies of the object to also hide
            PlayerDiedServerRpc(CurrentUnitHealth, PlayerNetObj.OwnerClientId);

            //hide boat: 
            boatModel.SetActive(false);
            boatEffect.SetActive(false);

            


            //Make sure to Detach the player when dead 
            this.gameObject.GetComponent<SpringTugSystem>().Detach();
            this.gameObject.GetComponent<SpringTugSystem>().isDead = this.isDead;


            //this.gameObject.transform.position = LevelVariableManager.Instance.GlobalRespawnTempMovePoint.position;
            StartCoroutine(MovePlayerAfterDelay(2f));

            LevelVariableManager.Instance.GlobalPlayerRespawnController.TriggerDeath(CurrentUnitHealth);
            // Your death logic here...
            Debug.Log($"{name} died!");
        }else
        {
            Debug.Log($"{name} Cant die here not local player!");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerDiedServerRpc(int newHealth, ulong playerObjectId, ServerRpcParams rpcParams = default)
    {
        BroadcastPlayerDeathClientRpc(newHealth, playerObjectId);
    }

    [ClientRpc]
    private void BroadcastPlayerDeathClientRpc(int newHealth, ulong playerObjectId)
    {
        if (playerObjectId == this.PlayerNetObj.OwnerClientId)
        {
            //disable all colliders on the player
            foreach (Collider col in allColliders)
            {
                col.enabled = false;
            }
            this.IsColliderActive = false;

            //hide boat: 
            this.boatModel.SetActive(false);
            this.boatEffect.SetActive(false);

            //this.gameObject.transform.position = LevelVariableManager.Instance.GlobalRespawnTempMovePoint.position;
            StartCoroutine(MovePlayerAfterDelay(1f));
        }
    }

    //Respawn ----------------------------------------------

    public void RespawnHealthSet(Transform respawnPos)
    {
        //if (!IsOwner) return;
        this.isDead = false;
        

        this.gameObject.transform.position = respawnPos.position;

        CurrentUnitHealth = MaxHealth;

        //Enable all colliders on the player
        // Start coroutine to delay collider enabling
        StartCoroutine(EnableCollidersAfterDelay(2f, respawnPos.position));
        //foreach (Collider col in allColliders)
        //{
        //    col.enabled = true;
        //}

        

        PlayerRespawnServerRpc(PlayerNetObj.OwnerClientId, respawnPos.position);

        GetComponent<NetworkTransform>().PositionLerpSmoothing = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerRespawnServerRpc(ulong playerObjectId, Vector3 respawnPos, ServerRpcParams rpcParams = default)
    {
        BroadcastPlayerRespawnClientRpc(playerObjectId, respawnPos);
    }

    [ClientRpc]
    private void BroadcastPlayerRespawnClientRpc(ulong playerObjectId, Vector3 respawnPos)
    {
        if (playerObjectId == this.PlayerNetObj.OwnerClientId)
        {
            StartCoroutine(EnableCollidersAfterDelay(2f, respawnPos));
        }
    }

    public IEnumerator EnableCollidersAfterDelay(float delay, Vector3 respawnPos)
    {
        yield return new WaitForSeconds(delay/2);
        //Show boat: 
        boatModel.SetActive(true);
        boatEffect.SetActive(true);
        yield return new WaitForSeconds(delay/2);

        //while (this.transform.position.x <= respawnPos.x + 2f || this.transform.position.x >= respawnPos.x - 2f)
        //{
        //    yield return new WaitForSeconds(0.2f);
        //}

        // Enable all colliders on the player
        foreach (Collider col in allColliders)
        {
            col.enabled = true;
        }
        this.IsColliderActive = true;

       

        tugSpringTugSystem.isDead = this.isDead;
    }


    private IEnumerator MovePlayerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        this.gameObject.transform.position = LevelVariableManager.Instance.GlobalRespawnTempMovePoint.position;
    }


    #endregion

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
