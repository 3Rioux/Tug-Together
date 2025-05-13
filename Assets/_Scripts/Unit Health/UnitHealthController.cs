using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))] // needs a collider of any kind attached 
public class UnitHealthController : MonoBehaviour, IDamageable
{
    [Header("Player Health: ")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private TextMeshProUGUI healthText;

    public UnitHealth CurrentUnitHeath = new UnitHealth(100, 100); //make it public so that the other scripts can damage this unit

    // public HealthBar _playerHealthBar; //reference to the healthBar or your damaged parts script stuff 

    [Header("Player Timed Heal: ")]
    [SerializeField] private bool canAutoHeal = false; // toggle if this unit can auto heal or not 
    [SerializeField] private float healDelay = 5f; // Time in seconds to wait before starting healing
    [SerializeField] private int healAmountPerSecond = 1; // Amount healed per second
    [SerializeField] private int healTimeDelay = 1; // Time Between heals

    //track when to auto heal
    private float timeSinceLastDamage = 0f; // Tracks time since last damage
    private bool isUnitHealing = false; // Tracks if healing coroutine is running

    private void Awake()
    {
        currentHealth = maxHealth;
        CurrentUnitHeath = new UnitHealth(currentHealth, maxHealth); 
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


        //Player DEAD XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Trigger Unit Death
        if (CurrentUnitHeath.CurrentHealth <= 0)
        {
            //Player is Dead 
            //LevelManager.Instance.PlayerDeath();//call the player death method from the GM when players HP less than or equal 0

        }else
        {
            healthText.text = "HP => " + CurrentUnitHeath.CurrentHealth.ToString();
        }
        //Player DEAD XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX Trigger Unit Death

        //------------------TESTING--------------------------
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


    /// <summary>
    /// Auto Heals the Unit gradually if not damaged for a set amount of time.
    /// </summary>
    private IEnumerator UnitTimeHeal()
    {
        isUnitHealing = true;

        //While Healt is not = Max Health && apply heal delay 
        while (CurrentUnitHeath.CurrentHealth < CurrentUnitHeath.MaxHealth && timeSinceLastDamage >= healDelay)
        {
            //healt Unit
            UnitHeal(healAmountPerSecond);
            yield return new WaitForSeconds(healTimeDelay); // Heal every second
        }



        //Done healing -> change state 
        isUnitHealing = false;
    }

    /// <summary>
    /// Method called when the Unit takes Damage. 
    /// @Daniel This is where you can add all the body part falling off triggers ----------------------------------------
    /// </summary>
    /// <param name="damage"></param>
    public void UnitTakeDamage(int damage)
    {
        // Reset the damage timer
        timeSinceLastDamage = 0f;

        //change current player health
        CurrentUnitHeath.DamageUnits(damage);

        //display current health to the user 
        //LevelManager.Instance._playerHealthBar.SetHealth(LevelManager.Instance.PlayerHeath.NetworkUnitCurrentHealth);
        Debug.Log($"{name} took {damage} damage, health now {CurrentUnitHeath.CurrentHealth}.");

        // Debug.Log(LevelManager.Instance.PlayerHeath.NetworkUnitCurrentHealth.ToString());

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        // Your death logic here...
        Debug.Log($"{name} died!");
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
       CurrentUnitHeath.HealUnits(healing);

        //display current health to the user 
        //LevelManager.Instance._playerHealthBar.SetHealth(LevelManager.Instance.PlayerHeath.NetworkUnitCurrentHealth);

        Debug.Log("Current Health" + CurrentUnitHeath.CurrentHealth.ToString());
    }

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

}
