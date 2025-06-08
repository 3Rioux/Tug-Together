using UnityEngine;
using TMPro;
using Unity.Services.Authentication;

/// <summary>
/// UI entry script to update UI elements for the Leaderboard 
/// </summary>
public class LeaderboardEntry : MonoBehaviour 
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI scoreText;

    public void Setup(M_PlayerInfo playerInfo)
    {
        nameText.text = playerInfo.GetPlayerName();
        healthText.text = playerInfo.GetCurrentHealth().ToString();
    }

    public void Init(string name, int health, int score)
    {
        nameText.text = name;
        healthText.text = $"Health:{health}";
        scoreText.text = $"{score}points";
    }


    public void UpdateName(string newName, bool isLocalPlayer)
    {
        nameText.text = $"{newName}";

        //Change the color of the text for the local player 
        if (isLocalPlayer)
        {
            nameText.color = Color.green;
            healthText.color = Color.green;
            scoreText.color = Color.green;
        }
        else
        {
            //just to be safe 
            nameText.color = Color.white;
            healthText.color = Color.white;
            scoreText.color = Color.white;
        }

    }

    public void UpdateHealth(int newHealth)
    {
        healthText.text = $"Health:{newHealth}";
    }

    public void UpdateScore(int newScore)
    {
        scoreText.text = $"{newScore}points";
    }

}