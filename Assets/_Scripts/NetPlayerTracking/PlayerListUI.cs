using System.Collections.Generic;
using TMPro;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

/// <summary>
/// Basic player UI that shows all players’ names, health, and scores in a scrollable UI.
/// </summary>
public class PlayerListUI : MonoBehaviour
{
    public static PlayerListUI Instance;

    [SerializeField] private GameObject playerInfoPrefab;
    [SerializeField] private Transform contentParent;

    private Dictionary<ulong, LeaderboardEntry> listItems = new();

    private Dictionary<ulong, string> playerNames = new();
    private Dictionary<ulong, int> playerScores = new();
    private Dictionary<ulong, int> playerHealths = new();

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

    // Update is called once per frame
    //void Update()
    //{
    //    var infos = NetworkPlayerInfoManager.Instance?.GetAllPlayerInfos();
    //    if (infos == null)
    //    {
    //        Debug.Log("Player List Empty", this);
    //        return;
    //    }

    //    foreach (var pair in infos)
    //    {
    //        ulong id = pair.Key;
    //        NetworkPlayerInfo info = pair.Value;

    //        if (!listItems.ContainsKey(id))
    //        {
    //            var go = Instantiate(playerInfoPrefab, contentParent);
    //            listItems[id] = go;
    //        }

    //        var item = listItems[id];
    //        item.GetComponentInChildren<TextMeshProUGUI>().text =
    //            $"Name: {info.PlayerName.Value}\n" +
    //            $"|Health: {info.Health.Value}|Score: {info.Score.Value}";
    //    }//end foreach 
    // }


    /// <summary>
    /// Called by the spawn Manager to update the players in the game 
    /// </summary>
    /// <param name="playerClientId"></param>
    public bool AddPlayerToList(ulong playerClientId, int playerMaxHealth)
    {
        if (listItems.ContainsKey(playerClientId))
        {
            //return true if already in the list
            return true;
        }

        //Create new entry Game Object
        GameObject player = Instantiate(playerInfoPrefab, contentParent);
        player.SetActive(true);

        //get needed entry script
        LeaderboardEntry entry = player.GetComponent<LeaderboardEntry>();
        //Set initial values:
        entry.Init($"Player #{playerClientId}", playerMaxHealth, 0);
        //Add to tracking Dictionnary
        listItems.Add(playerClientId, entry);

        // Optionally print or display
        Debug.Log($"{playerClientId} Added to List View.");

        return listItems.ContainsKey(playerClientId);
    }


    #region Name

    /// <summary>
    /// Updates the given players score 
    /// </summary>
    public void UpdatePlayerNames(ulong playerClientId, string name, bool isLocalPlayer)
    {
        playerNames[playerClientId] = name;

        listItems[playerClientId].UpdateName(name, isLocalPlayer);

        // Optionally print or display
        Debug.Log($"{playerClientId} Name is now: {name}");
    }

    public string GetName(ulong clientId)
    {
        return playerNames.TryGetValue(clientId, out string pName) ? pName : "FailedFetchNameInGetName";
    }

    public Dictionary<ulong, string> GetAllNames()
    {
        return new Dictionary<ulong, string>(playerNames);
    }

    #endregion


    #region Health

    /// <summary>
    /// Updates the given players health 
    /// </summary>
    public void UpdatePlayerHealth(ulong playerClientId, int newHealth)
    {
        playerHealths[playerClientId] = newHealth;

        listItems[playerClientId].UpdateHealth(newHealth);

        // Optionally print or display
        Debug.Log($"{playerClientId} Health is now: {newHealth}");
    }

    /// <summary>
    /// Get the Given player id current health 
    /// </summary>
    /// <param name="clientId"></param>
    /// <returns></returns>
    public int GetHealth(ulong clientId)
    {
        return playerHealths.TryGetValue(clientId, out var health) ? health : 999;
    }

    public Dictionary<ulong, int> GetAllPlayerHealth()
    {
        return new Dictionary<ulong, int>(playerHealths);
    }
    #endregion



    #region Score

    /// <summary>
    /// Updates the given players score 
    /// </summary>
    public void UpdatePlayerScore(ulong playerClientId, int score)
    {
        playerScores[playerClientId] = score;

        listItems[playerClientId].UpdateScore(score);

        // Optionally print or display
        Debug.Log($"{playerClientId} score is now: {score}");
    }

    public int GetScore(ulong clientId)
    {
        return playerScores.TryGetValue(clientId, out var score) ? score : 999;
    }

    public Dictionary<ulong, int> GetAllScores()
    {
        return new Dictionary<ulong, int>(playerScores);
    }
    #endregion
}
