using Unity.Netcode;
using UnityEngine;
using TMPro;

public class MatchTimer : NetworkBehaviour 
{
    [SerializeField] private bool isCountdown = false; // is it countdown or timer Game Mode

    [SerializeField] private float matchDuration = 300f; // 5 mins
    [SerializeField] private TextMeshProUGUI timerText;

    private float timeRemaining;
    private bool matchRunning = false;

    private float totalTimePlayed = 0f; // Total time before match ends

    public float TotalTimePlayed => totalTimePlayed; // Public getter

    private void Start()
    {
        if (IsServer)
        {
            StartMatch();
        }
    }


    private void Update()
    {
        if (!matchRunning) return;

        if (isCountdown)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUIClientRpc((int)timeRemaining);

            if (timeRemaining <= 0f)
            {
                matchRunning = false;
                TriggerGameOverClientRpc((int)totalTimePlayed); // Notify all clients
            }

        }
        else
        {
            totalTimePlayed += Time.deltaTime;
            UpdateTimerUIClientRpc((int)totalTimePlayed);
        }

       

       
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartMatchServerRpc()
    {
        StartMatch();
    }


    [ServerRpc(RequireOwnership = false)]
    public void EndMatchServerRpc()
    {
        EndMatch();
        TriggerGameOverClientRpc((int)totalTimePlayed);
    }

    public void EndMatch()
    {
        matchRunning = false;
        timerText.gameObject.SetActive(false);
    }


    private void StartMatch()
    {
        timeRemaining = matchDuration;
        totalTimePlayed = 0f;
        matchRunning = true;
    }

    [ClientRpc]
    private void UpdateTimerUIClientRpc(int timeLeft)
    {
        if (timerText != null)
        {
            timerText.text = FormatTime(timeLeft);
        }
    }

    [ClientRpc]
    private void TriggerGameOverClientRpc(int totalSecondsPlayed)
    {
        string timePlayedFormatted = FormatTime(totalSecondsPlayed);
        Debug.Log($"Match over! Time played: {timePlayedFormatted}");

        // Optionally show it in a UI
        GameOverUI.Instance.ShowGameOver(timePlayedFormatted); // You need to implement this
    }

    private string FormatTime(int totalSeconds)
    {
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:D2}:{seconds:D2}";
    }




    #region V1TimerWNetVar
    /*
    [Header("Timer Settings")]
    [SerializeField] private float matchDuration = 300f; // Total time in seconds
    private float currentTime;


    [Header("UI Reference")]
    public TextMeshProUGUI timerText; // Assign in inspector or dynamically

    private NetworkVariable<float> syncedTime = new NetworkVariable<float>(
       0f,
       NetworkVariableReadPermission.Everyone,
       NetworkVariableWritePermission.Server
   );


    private bool timerRunning = false;


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentTime = matchDuration;
            syncedTime.Value = currentTime;
            timerRunning = true;
        }
    }

    void Update()
    {
        if (IsServer && timerRunning)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0f)
            {
                currentTime = 0f;
                timerRunning = false;
                // Trigger game over here if needed
            }

            syncedTime.Value = currentTime;
        }

        // All clients update their UI using the synced time
        UpdateTimerUI(syncedTime.Value);
    }

    private void UpdateTimerUI(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
    */
    #endregion
}
