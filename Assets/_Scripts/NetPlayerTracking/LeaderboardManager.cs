using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Authentication;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private RectTransform leaderboardContainer;

    private Dictionary<ulong, LeaderboardEntry> playerEntries = new();

    private void OnEnable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;
    }

    private void OnDisable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateLeaderboard()
    {
        // Clear existing entries
        foreach (var entry in playerEntries.Values)
        {
            Destroy(entry.gameObject);
        }
        playerEntries.Clear();

        // Loop through all connected players
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null) continue;

            var playerInfo = playerObject.GetComponent<M_PlayerInfo>();
            var health = playerObject.GetComponent<M_UnitHealthController>();
            if (playerInfo == null || health == null) continue;

            GameObject entryGO = Instantiate(leaderboardEntryPrefab, leaderboardContainer);
            LeaderboardEntry entry = entryGO.GetComponent<LeaderboardEntry>();
            playerEntries[client.ClientId] = entry;

            entry.Setup(playerInfo);

            // Register live updates
            //playerInfo.GetPlayerName().OnValueChanged += (oldVal, newVal) =>
            //    entry.UpdateName(newVal.ToString());

            health.NetworkUnitCurrentHealth.OnValueChanged += (oldVal, newVal) =>
                entry.UpdateHealth(newVal);
        }
    }

    //=================================

    public void RegisterPlayer(M_PlayerInfo playerIdentity)
    {
        ulong clientId = playerIdentity.OwnerClientId;

        if (playerEntries.ContainsKey(clientId)) return;

        GameObject entryGO = Instantiate(leaderboardEntryPrefab, leaderboardContainer);
        LeaderboardEntry entry = entryGO.GetComponent<LeaderboardEntry>();
        M_PlayerInfo playerInfo = playerIdentity.GetComponent<M_PlayerInfo>();
        M_UnitHealthController health = playerIdentity.GetComponent<M_UnitHealthController>();

        if (playerInfo == null || health == null)
        {
            Debug.LogError("M_PlayerInfo or UnitHealthController missing.");
            return;
        }

        entry.Setup(playerInfo);
        playerEntries[clientId] = entry;

        // Register for updates
        //playerInfo.playerName.OnValueChanged += (oldVal, newVal) => entry.UpdateName(newVal.ToString());
        health.NetworkUnitCurrentHealth.OnValueChanged += (oldVal, newVal) => entry.UpdateHealth(newVal);
    }


    private void OnPlayerConnected(ulong clientId)
    {
        StartCoroutine(WaitAndAddPlayer(clientId));
    }

    private IEnumerator WaitAndAddPlayer(ulong clientId)
    {
        // Wait until the player and components are fully spawned
        yield return new WaitForSeconds(1f);

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            M_PlayerInfo playerInfo = client.PlayerObject.GetComponent<M_PlayerInfo>();

            if (playerInfo != null)
            {
                AddLeaderboardEntry(clientId, playerInfo);
            }
        }
    }

    public void AddLeaderboardEntry(ulong clientId, M_PlayerInfo playerInfo)
    {
        //Clear all Previous entries:
        ClearAllPreviousEntries();

        GameObject entryGO = Instantiate(leaderboardEntryPrefab, leaderboardContainer.transform);
        LeaderboardEntry entry = entryGO.GetComponent<LeaderboardEntry>();

        //leaderboardContainer.sizeDelta = new Vector2(leaderboardContainer.sizeDelta.x, 36 * playerEntries.Count);
        //this way we can change the perfab heigth without a problem:
        leaderboardContainer.sizeDelta = new Vector2(leaderboardContainer.sizeDelta.x, entry.GetComponent<RectTransform>().sizeDelta.y * playerEntries.Count);

        playerEntries[clientId] = entry;

        entry.Setup(playerInfo);

        //playerInfo.playerName.OnValueChanged += (oldVal, newVal) =>
        //{
        //    entry.UpdateName(newVal.ToString());
        //};

        playerInfo.GetComponent<M_UnitHealthController>().NetworkUnitCurrentHealth.OnValueChanged += (oldVal, newVal) =>
        {
            entry.UpdateHealth(newVal);
        };
    }



    private void OnPlayerDisconnected(ulong clientId)
    {
        if (playerEntries.TryGetValue(clientId, out var entry))
        {
            Destroy(entry.gameObject);
            playerEntries.Remove(clientId);
        }
    }

    private void ClearAllPreviousEntries()
    {
        foreach(var entry in playerEntries.Values)
        {
            Destroy(entry.gameObject);
        }
    }
}
