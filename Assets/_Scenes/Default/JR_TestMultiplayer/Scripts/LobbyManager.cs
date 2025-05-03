using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    public Lobby CurrentLobby { get; private set; }
    private int heartbeatInterval = 15;

    public string GetLobbyCode()
    {
        return CurrentLobby?.LobbyCode;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region Create Lobby

    public async Task<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate)
    {
        try
        {
            var options = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = new Player(id: AuthenticationService.Instance.PlayerId),
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, "") }
                }
            };

            CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            Debug.Log($"Lobby created! ID: {CurrentLobby.Id}, JoinCode: {CurrentLobby.LobbyCode}");

            //StartCoroutine(HeartbeatLobby());
            

            return CurrentLobby;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Lobby creation failed: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Join Lobby by Code

    public async Task<Lobby> JoinLobbyByCodeAsync(string lobbyCode)
    {
        try
        {
            var options = new JoinLobbyByCodeOptions
            {
                Player = new Player(id: AuthenticationService.Instance.PlayerId)
            };

            CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
            Debug.Log($"Joined lobby with code: {lobbyCode}");

            return CurrentLobby;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Failed to join lobby: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Quick Join Lobby

    public async Task<Lobby> QuickJoinLobbyAsync()
    {
        try
        {
            var options = new QuickJoinLobbyOptions
            {
                Player = new Player(id: AuthenticationService.Instance.PlayerId)
            };

            CurrentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            Debug.Log($"Quick-joined lobby: {CurrentLobby.Id}");

            return CurrentLobby;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogWarning("No available lobbies to join.");
            return null;
        }
    }

    #endregion

    #region Heartbeat (Host Only)

    //private IEnumerator HeartbeatLobby()
    //{
    //    while (CurrentLobby != null)
    //    {


    //        //await Task.Delay(heartbeatInterval * 1000); // Convert seconds to milliseconds
    //        yield return new WaitForSeconds(heartbeatInterval);

    //        try
    //        {
    //            yield return LobbyService.Instance.SendHeartbeatPingAsync(CurrentLobby.Id);
    //        }
    //        catch (LobbyServiceException ex)
    //        {
    //            Debug.LogWarning($"Heartbeat failed: {ex.Message}");
    //        }
    //    }
    //}

    #endregion

    #region Leave Lobby

    public async void LeaveLobby()
    {
        if (CurrentLobby == null) return;

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(CurrentLobby.Id, AuthenticationService.Instance.PlayerId);
            CurrentLobby = null;
            Debug.Log("Left lobby.");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Error leaving lobby: {ex.Message}");
        }
    }

    #endregion
}