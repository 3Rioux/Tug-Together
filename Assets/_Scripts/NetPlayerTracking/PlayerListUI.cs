using System.Collections.Generic;
using TMPro;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

/// <summary>
/// Basic player UI that shows all players� names, health, and scores in a scrollable UI.
/// </summary>
public class PlayerListUI : MonoBehaviour
{
    public static PlayerListUI Instance;

    [SerializeField] private GameObject playerInfoPrefab;
    [SerializeField] private GameObject displayList;
    [SerializeField] private Transform contentParent;

    public Dictionary<ulong, LeaderboardEntry> listItems = new();

    private Dictionary<ulong, string> playerNames = new();
    private Dictionary<ulong, int> playerScores = new();
    private Dictionary<ulong, int> playerHealths = new();

    //Visual Variables:
    private RectTransform displayListRectTransform;
    private float defaultWidth = 250f;
    private float defaultHeight = 250f;
    private float playerInfoPrefabHeight;

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

    private void Start()
    {
       
        displayListRectTransform = displayList.GetComponent<RectTransform>();

        //Get teh player info height
        playerInfoPrefabHeight = playerInfoPrefab.GetComponent<RectTransform>().sizeDelta.y; // Get its Height 

        displayList.SetActive(false);
    }

    /// <summary>
    /// changes the 
    /// </summary>
    public void ToggleUIDisplay()
    {
        print("Toggle UI");
        displayListRectTransform.sizeDelta = new Vector2(defaultWidth, (playerInfoPrefabHeight * listItems.Count) + 2f); // displayListRectTransform = # players being displayed + 2 
        displayList.SetActive(!displayList.activeSelf);
    }

    /// <summary>
    /// Override with bool to set the UI on / off with a bool 
    /// </summary>
    /// <param name="setOnOff"></param>
    public void ToggleUIDisplay(bool setOnOff)
    {
        print("Toggle UI");
        displayListRectTransform.sizeDelta = new Vector2(defaultWidth, (playerInfoPrefabHeight * listItems.Count) + 2f); // displayListRectTransform = # players being displayed + 2 
        displayList.SetActive(setOnOff);
    }


    #region AddRemovePlayers

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
        //string playerName = 
        entry.Init($"Player #{playerClientId}", playerMaxHealth, 0);
        //Add to tracking Dictionary
        listItems.Add(playerClientId, entry);

        // Optionally print or display
        Debug.Log($"{playerClientId} Added to List View.");

        return listItems.ContainsKey(playerClientId);
    }

    public bool RemovePlayerFromList(ulong playerClientId, bool disconnect)
    {
        if (!listItems.ContainsKey(playerClientId) || !disconnect)
        {
            //return false if player NOT already in the list
            return false;
        }

       

        //Remove from UI
        Destroy(listItems[playerClientId].gameObject);

        // Optionally print or display
        Debug.Log($"{playerClientId} Remove from List View.");


        bool removalSuccess = listItems.Remove(playerClientId);//Remove from tracking Dictionary
        
        ToggleUIDisplay(true);//think of this like a refresh for the UI 

        //return if removal success
        return removalSuccess;

    }


    #endregion

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
