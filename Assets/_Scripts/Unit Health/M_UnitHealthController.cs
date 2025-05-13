using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// This is the Network Multiplay Variant of the Unit Heath controller.
/// </summary>
[RequireComponent(typeof(Collider))] // needs a collider of any kind attached 
public class M_UnitHealthController : NetworkBehaviour, IDamageable
{
    [Header("Player Health: ")]
    public int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private TextMeshProUGUI healthText;
    
    public UnitHealth CurrentUnitHeath = new UnitHealth(100, 100); //make it public so that the other scripts can damage this unit *** Can change to private 

    //Network unit health 
    public NetworkVariable<int> NetworkUnitCurrentHealth = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    // public HealthBar _playerHealthBar; //reference to the healthBar or your damaged parts script stuff 

    [Header("Player Timed Heal: ")]
    [SerializeField] private bool canAutoHeal = false; // toggle if this unit can auto heal or not 
    [SerializeField] private float healDelay = 5f; // Time in seconds to wait before starting healing
    [SerializeField] private int healAmountPerSecond = 1; // Amount healed per second
    [SerializeField] private int healTimeDelay = 1; // Time Between heals

    //track when to auto heal
    private float timeSinceLastDamage = 0f; // Tracks time since last damage
    private bool isUnitHealing = false; // Tracks if healing coroutine is running


    private void OnEnable()
    {
        NetworkUnitCurrentHealth.OnValueChanged += OnNetworkHealthChanged;
    }

    private void OnDisable()
    {
        NetworkUnitCurrentHealth.OnValueChanged -= OnNetworkHealthChanged;
    }


    private void Awake()
    {
        currentHealth = maxHealth;
        CurrentUnitHeath = new UnitHealth(currentHealth, maxHealth);
        NetworkUnitCurrentHealth.Value = CurrentUnitHeath.CurrentHealth;
    }


    private void Update()
    {
        if (!IsServer) return;

        // Increment time since last damage
        timeSinceLastDamage += Time.deltaTime;

        // Start healing if conditions are met
        if (!isUnitHealing && timeSinceLastDamage >= healDelay)
        {
            //Debug.Log("Start Healling");
            StartCoroutine(UnitTimeHeal());
        }


        //Player DEAD XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Trigger Unit Death
        //if (CurrentUnitHeath.CurrentHealth <= 0)
        //{
        //    //Player is Dead 
        //    //LevelManager.Instance.PlayerDeath();//call the player death method from the GM when players HP less than or equal 0

        //}else
        //{
        //    healthText.text = "HP => " + CurrentUnitHeath.CurrentHealth.ToString();
        //}
        //Player DEAD XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Trigger Unit Death

        //------------------TESTING--------------------------
        if(!IsOwner) return;

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
        //------------------TESTING--------------------------
    }//end update 

    #region MultiplayerCode
    //=======================================MULTIPLAYER NETWORK LOGIC=======================================

    /// <summary>
    /// Method to update the player health if the health bar was changed by the server 
    /// </summary>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    private void OnNetworkHealthChanged(int oldValue, int newValue)
    {
        if (IsOwner) // Only update UI for this local player
        {
            CurrentUnitHeath.CurrentHealth = NetworkUnitCurrentHealth.Value;
            healthText.text = $"Health: {newValue}";
        }

        if (newValue <= 0 && IsServer)
        {
            Die(); // Only the server can trigger death logic
        }
    }

    public void UnitTakeDamage(int damage)
    {
        if (IsOwner)
        {
            TakeDamageServerRpc(damage);
        }
    }

    public void UnitHeal(int amount)
    {
        if (IsOwner)
        {
            HealServerRpc(amount);
        }
    }

    [ServerRpc]
    private void TakeDamageServerRpc(int damage)
    {
        timeSinceLastDamage = 0f;

        NetworkUnitCurrentHealth.Value = Mathf.Max(NetworkUnitCurrentHealth.Value - damage, 0);
        Debug.Log($"{name} took {damage} damage. Health now: {NetworkUnitCurrentHealth.Value}");
    }

    [ServerRpc]
    private void HealServerRpc(int amount)
    {
        NetworkUnitCurrentHealth.Value = Mathf.Min(NetworkUnitCurrentHealth.Value + amount, maxHealth);
        Debug.Log($"{name} healed by {amount}. Health now: {NetworkUnitCurrentHealth.Value}");
    }


    private IEnumerator UnitTimeHeal()
    {
        isUnitHealing = true;

        while (NetworkUnitCurrentHealth.Value < maxHealth && timeSinceLastDamage >= healDelay)
        {
            NetworkUnitCurrentHealth.Value = Mathf.Min(NetworkUnitCurrentHealth.Value + healAmountPerSecond, maxHealth);
            yield return new WaitForSeconds(healTimeDelay);
        }

        isUnitHealing = false;
    }

    //=======================================MULTIPLAYER NETWORK LOGIC=======================================
    #endregion

    void Die()
    {
        // Your death logic here...
        Debug.Log($"{name} died!");
    }

    #region SinglePlayerCode

    /// <summary>
    /// Auto Heals the Unit gradually if not damaged for a set amount of time.
    /// </summary>
    //private IEnumerator UnitTimeHeal()
    //{
    //    isUnitHealing = true;

    //    //While Healt is not = Max Health && apply heal delay 
    //    while (CurrentUnitHeath.CurrentHealth < CurrentUnitHeath.MaxHealth && timeSinceLastDamage >= healDelay)
    //    {
    //        //healt Unit
    //        UnitHeal(healAmountPerSecond);
    //        yield return new WaitForSeconds(healTimeDelay); // Heal every second
    //    }



    //    //Done healing -> change state 
    //    isUnitHealing = false;
    //}

    ///// <summary>
    ///// Method called when the Unit takes Damage. 
    ///// @Daniel This is where you can add all the body part falling off triggers ----------------------------------------
    ///// </summary>
    ///// <param name="damage"></param>
    //public void UnitTakeDamage(int damage)
    //{
    //    // Reset the damage timer
    //    timeSinceLastDamage = 0f;

    //    //change current player health
    //    CurrentUnitHeath.DamageUnits(damage);

    //    //display current health to the user 
    //    //LevelManager.Instance._playerHealthBar.SetHealth(LevelManager.Instance.PlayerHeath.NetworkUnitCurrentHealth);
    //    Debug.Log($"{name} took {damage} damage, health now {CurrentUnitHeath.CurrentHealth}.");

    //    // Debug.Log(LevelManager.Instance.PlayerHeath.NetworkUnitCurrentHealth.ToString());

    //    if (currentHealth <= 0) Die();
    //}


    ///// <summary>
    ///// Method to handle the player healing. 
    ///// increase unit Health and update Health bar 
    ///// @Daniel This is where you can add all the body part being reattached ----------------------------------------
    ///// </summary>
    ///// <param name="healing"></param>
    //private void UnitHeal(int healing)
    //{
    //    //change current player health
    //   CurrentUnitHeath.HealUnits(healing);

    //    //display current health to the user 
    //    //LevelManager.Instance._playerHealthBar.SetHealth(LevelManager.Instance.PlayerHeath.NetworkUnitCurrentHealth);

    //    Debug.Log("Current Health" + CurrentUnitHeath.CurrentHealth.ToString());
    //}

    /// <summary>
    /// method called when player Uses a medkit in the inventory 
    /// </summary>
    /// <param name="healing"></param>
    /// <returns></returns>
    public bool TryHealthItemPlayerHeal(int healing)
    {
        if (!CurrentUnitHeath.IsUnitHealthFull())
        {
            UnitHeal(healing);
            return true;//item was used 
        }
        else return false; // item not used health was already full 
    }
    #endregion
}
