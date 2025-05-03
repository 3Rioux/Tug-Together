using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerEntry : MonoBehaviour
{
    public TextMeshProUGUI PlayerNameText;
    public TextMeshProUGUI HealthText;

    public void SetPlayerInfo(string name, float health, bool isLocalPlayer)
    {
        PlayerNameText.text = name;
        if (isLocalPlayer)
        {
            PlayerNameText.color = Color.blue; //make different color is local player
        }else
        {
            PlayerNameText.color = Color.white;
        }

        HealthText.text = $"HP: {health:0}";

        if (health > (health/2))
        {
            HealthText.color = Color.green;
        }else
        {
            HealthText.color = Color.red;
        }
    }
}
