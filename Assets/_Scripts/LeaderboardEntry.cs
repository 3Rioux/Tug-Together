using UnityEngine;
using TMPro;

/// <summary>
/// UI entry script to update UI elements for the Leaderboard 
/// </summary>
public class LeaderboardEntry : MonoBehaviour 
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI healthText;

    public void Setup(M_PlayerInfo playerInfo)
    {
        nameText.text = playerInfo.GetPlayerName();
        healthText.text = playerInfo.GetCurrentHealth().ToString();
    }


    public void UpdateName(string newName)
    {
        nameText.text = newName;
    }

    public void UpdateHealth(int newHealth)
    {
        healthText.text = newHealth.ToString();
    }


}