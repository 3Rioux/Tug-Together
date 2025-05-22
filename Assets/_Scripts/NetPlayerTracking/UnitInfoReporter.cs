using System;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public class UnitInfoReporter : NetworkBehaviour
{
    public event Action<string> OnNameChanged;
    public event Action<int> OnHealthChanged;
    public event Action<int> OnScoreChanged;


    private string _currentUnitName;
    private int _currentUnitHealth;
    private int _currentUnitScore;

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
#endregion


   

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

        // Store or update Name
        PlayerListUI.Instance?.UpdatePlayerNames(playerClientId, playerName, OwnerClientId == playerClientId);
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

    #endregion


}
