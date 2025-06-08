using TMPro;
using UnityEngine;

/// <summary>
/// Displays teh Game Over UI + time taken to complete the level.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance;

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI timePlayedText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public void ShowGameOver(string timePlayed)
    {
        gameOverPanel.SetActive(true);
        timePlayedText.text = $"Time Played: {timePlayed}";
    }

    public void HideGameOver()
    {
        gameOverPanel.SetActive(false);
    }
}
