using System;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public class UnitInfoReporter : NetworkBehaviour
{
    [SerializeField] private PlayerNameDisplay playerNameDisplay;

    //These have no use in the end just debugging but i want to leave them due to seeing how its used + can call death logic this way from here as well if i want -> 
    public event Action<string> OnNameChanged;
    public event Action<int> OnHealthChanged;
    public event Action<int> OnScoreChanged;
    //public event Action<bool> OnDisconnectChanged;


    private string _currentUnitName;
    private int _currentUnitHealth;
    private int _currentUnitScore;
    private bool _currentUnitDisconnect;

    //so ugly but im not able to trigger the get name for the host if hes alone without it + more efficient then GetName every time its pressed:
    private bool gotName = false;

    private BoatInputActions controls;


    #region PublicVariableAccessors
    public string CurrentUnitName
    {
        get => _currentUnitName;
        set
        {
            if (_currentUnitName != value)
            {
                _currentUnitName = value;
                OnNameChanged?.Invoke(_currentUnitName);

                print($"name Changed!!! ++ Name: {_currentUnitName}");


                // Notify the server of score change
                ReportNameServerRpc(_currentUnitName);
            }
        }
    }

    public int CurrentUnitHealth
    {
        get => _currentUnitHealth;
        set
        {
            if (_currentUnitHealth != value)
            {
                _currentUnitHealth = value;
                OnHealthChanged?.Invoke(_currentUnitHealth);

                // Notify the server of score change
                ReportHealthServerRpc(_currentUnitHealth);
            }
        }
    }

    public int CurrentUnitScore
    {
        get => _currentUnitScore;
        set
        {
            if (_currentUnitScore != value)
            {
                _currentUnitScore = value;
                OnScoreChanged?.Invoke(_currentUnitScore);

                // Notify the server of score change
                ReportScoreServerRpc(_currentUnitScore);
            }
        }
    }


    //public bool CurrentUnitDisconnect
    //{
    //    //get => _currentUnitDisconnect;
    //    set
    //    {
    //        if (_currentUnitDisconnect != value)
    //        {
    //            _currentUnitDisconnect = value;
    //            OnScoreChanged?.Invoke(_currentUnitDisconnect);

    //            // Notify the server of score change
    //            ReportDisconnectServerRpc(_currentUnitDisconnect);
    //        }
    //    }
    //}


    #endregion

    private void Awake()
    {
        controls = new BoatInputActions();
        controls.Enable();

        controls.Boat.Attack.performed += ctx => TogglePlayerList();
        //controls.Boat.Attack.canceled += _ => TogglePlayerList();
    }

    public void SetControlEnabled(bool enabled)
    {
        if (enabled)
        {
            controls.Enable();

           
        }
        else
        {
            controls.Disable();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkDespawn();
            CurrentUnitName = playerNameDisplay.GetPlayerName();

    }

    /// <summary>
    /// Only done for the ---Local Player--- don't need RPC to toggle all players stuff
    /// public so that the controls trigger can be moved to the 
    /// </summary>
    public void TogglePlayerList()
    {
        if (IsOwner)
        {
            Debug.Log("Is Owner !!!");
            //set player name if not done already 
            //if (!gotName)
            //{
            //    CurrentUnitName = playerNameDisplay.GetPlayerName();

            //    gotName = true;
            //}
            CurrentUnitName = playerNameDisplay.GetPlayerName();

            //Toggle the UI On/Off
            PlayerListUI.Instance?.ToggleUIDisplay();
        }
        else
        {
            Debug.Log("Is Not Owner !!!");
        }
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += ReportDisconnectServerRpc;
    }

    void OnEnable() 
    {
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Disable();
    }

    private void OnDestroy()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= ReportDisconnectServerRpc;
    }


    private void OnClientDisconnect(ulong disconnectClientID)
    {
        ReportDisconnectServerRpc(disconnectClientID);
    }

    


    #region ServerRPCS

    /// <summary>
    /// Request the server to update the Name of the current player OwnerClientId
    /// </summary>
    /// <param name="newName"></param>
    /// <param name="rpcParams"></param>
    [ServerRpc(RequireOwnership = false)]
    void ReportNameServerRpc(string newName, ServerRpcParams rpcParams = default)
    {
        BroadcastNameClientRpc(OwnerClientId, newName);
    }

    /// <summary>
    /// Request the server to update the Health of the current player OwnerClientId
    /// </summary>
    /// <param name="newHealth"></param>
    /// <param name="rpcParams"></param>
    [ServerRpc(RequireOwnership = false)]
    void ReportHealthServerRpc(int newHealth, ServerRpcParams rpcParams = default)
    {
        BroadcastHealthClientRpc(OwnerClientId, newHealth);
    }

    /// <summary>
    /// Request the server to update the Score of the current player OwnerClientId
    /// </summary>
    /// <param name="newScore"></param>
    /// <param name="rpcParams"></param>
    [ServerRpc(RequireOwnership = false)]
    void ReportScoreServerRpc(int newScore, ServerRpcParams rpcParams = default)
    {
        //string playerName = $"Player {OwnerClientId}";

        ////BroadcastScoreClientRpc(playerName, newScore);
        BroadcastScoreClientRpc(OwnerClientId, newScore);
    }

    [ServerRpc(RequireOwnership = false)]
    void ReportDisconnectServerRpc(ulong disconnectClientID)
    {
        BroadcastDisconnectClientRpc(disconnectClientID, true);
    }

    #endregion

    #region ClientBroadcastRPCS
    //Client Broadcasts -------------------------------------------------------------

    /// <summary>
    /// Updates the PlayerListUI Name for the given playerClientId on ALL CLIENTS
    /// </summary>
    /// <param name="playerClientId"></param>
    /// <param name="playerName"></param>
    [ClientRpc]
    void BroadcastNameClientRpc(ulong playerClientId, string playerName)
    {
        Debug.Log($"Player {OwnerClientId}'s Name is Now {playerName}");

        // Store or update Name - OwnerClientId == playerClientId 
        PlayerListUI.Instance?.UpdatePlayerNames(playerClientId, playerName, IsLocalPlayer);
        
    }

    /// <summary>
    /// Updates the PlayerListUI Health for the given playerClientId on ALL CLIENTS
    /// </summary>
    /// <param name="playerClientId"></param>
    /// <param name="newHealth"></param>
    [ClientRpc]
    void BroadcastHealthClientRpc(ulong playerClientId, int newHealth)
    {
        Debug.Log($"{playerClientId} new Health is now: {newHealth}");

        // Store or update score
        PlayerListUI.Instance?.UpdatePlayerHealth(playerClientId, newHealth);
    }

    /// <summary>
    /// Updates the PlayerListUI Score for the given playerClientId on ALL CLIENTS
    /// </summary>
    /// <param name="playerClientId"></param>
    /// <param name="score"></param>
    [ClientRpc]
    void BroadcastScoreClientRpc(ulong playerClientId, int score)
    {
        Debug.Log($"{playerClientId} score is now: {score}");

        // Store or update score
        PlayerListUI.Instance?.UpdatePlayerScore(playerClientId, score);

    }

    [ClientRpc]
    void BroadcastDisconnectClientRpc(ulong playerClientId, bool disconnect)
    {
        Debug.Log($"{playerClientId} is now Disconnected: {disconnect}");

        // Store or update score
        PlayerListUI.Instance?.RemovePlayerFromList(playerClientId, disconnect);

    }

    #endregion


}
