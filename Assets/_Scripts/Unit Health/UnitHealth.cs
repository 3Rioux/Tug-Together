using UnityEngine;

/// <summary>
/// Class to store the heatlth of the unit its attached to (player or enemy...)
/// </summary>
public class UnitHealth
{

    //Fields:
    private int _currentHealth;
    private int _currentMaxHealth;

    // Properties: 
    public int CurrentHealth
    {
        get
        {
            return _currentHealth;
        }
        set
        {
            _currentHealth = value;
        }
    }//end Health 

    public int MaxHealth
    {
        get
        {
            return _currentMaxHealth;
        }
        set
        {
            _currentMaxHealth = value;
        }
    }//end Health 

    //CONSTRUCTOR
    public UnitHealth(int health, int maxHealth)
    {
        _currentHealth = health;
        _currentMaxHealth = maxHealth;

    }//end UnitHealth Constructor 

    //=========================== Health Methods ==================================
    /// <summary>
    /// Method called when Unit Takes damage 
    /// </summary>
    /// <param name="dmgAmount"></param>
    public void DamageUnits(int dmgAmount)
    {
        if (_currentHealth > 0)
        {
            _currentHealth -= dmgAmount;
        }
    }//end DamageUnits

    /// <summary>
    /// Method called when Unit picksup health item  
    /// </summary>
    /// <param name="healAmount"></param>
    public void HealUnits(int healAmount)
    {
        if (_currentHealth < _currentMaxHealth)
        {
            _currentHealth += healAmount;
        }

        //Make sure player can't heal more HP than Max HP
        if (_currentHealth > _currentMaxHealth)
        {
            _currentHealth = _currentMaxHealth;
        }

    }//end HealUnits


    /// <summary>
    /// Returns true if the Units health is NOT full 
    /// </summary>
    /// <returns></returns>
    public bool IsUnitHealthFull()
    {
        if (CurrentHealth < MaxHealth)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

}
