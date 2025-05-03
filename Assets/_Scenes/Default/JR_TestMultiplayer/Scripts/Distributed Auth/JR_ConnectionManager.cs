//#if NGO_2_OR_NEWER && SERVICES_INSTALLED
using System;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;

    /// <summary>
    /// Handles the connection to the Distributed Authority Backend Service.
    /// If you want to modify this Script please copy it into your own project and add it to your Player Prefab.
    /// </summary>
    public class JR_ConnectionManager : MonoBehaviour
    {
        [SerializeField]
        int m_MaxPlayers = 10;
        NetworkManager m_NetworkManager;
        ISession m_Session;

        /// <summary>
        /// Status of the Connection.
        /// </summary>
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

        /// <summary>
        /// The different states the connection can be in.
        /// </summary>
        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected,
        }

        async void Awake()
        {
            // Find the NetworkManager in the Scene
            m_NetworkManager = FindFirstObjectByType<NetworkManager>();
            m_NetworkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
            m_NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
            await UnityServices.InitializeAsync();
        }

        /// <summary>
        /// Leaves the current session and sets the <see cref="State"/> to Disconnected.
        /// </summary>
        public async void Disconnect()
        {
            if (m_Session != null)
            {
                await m_Session.LeaveAsync();
            }

            State = ConnectionState.Disconnected;
        }

        /// <summary>
        /// Creates or joins an existing session.
        /// </summary>
        /// <param name="sessionName">The name of the session to join or create</param>
        /// <param name="profileName">The name of the player</param>
        public async Task CreateOrJoinSessionAsync(string sessionName, string profileName)
        {
            if (string.IsNullOrEmpty(profileName) || string.IsNullOrEmpty(sessionName))
            {
                Debug.LogError("Please provide a player and session name, to login.");
                return;
            }

            State = ConnectionState.Connecting;
            try
            {
                // Only sign in if not already signed in.
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    AuthenticationService.Instance.SwitchProfile(profileName);
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                // Set the session options.
                var options = new SessionOptions()
                {
                    Name = sessionName,
                    MaxPlayers = m_MaxPlayers
                }.WithDistributedAuthorityNetwork();

                // Join a session if it already exists, or create a new one.
                m_Session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);
                State = ConnectionState.Connected;
            }
            catch (Exception e)
            {
                State = ConnectionState.Disconnected;
                Debug.LogException(e);
            }
        }

        // Just for logging.
        void OnClientConnectedCallback(ulong clientId)
        {
            if (m_NetworkManager.LocalClientId == clientId)
            {
                Debug.Log($"Client-{clientId} is connected and can spawn {nameof(NetworkObject)}s.");
            }
        }

        // Just for logging.
        void OnSessionOwnerPromoted(ulong sessionOwnerPromoted)
        {
            if (m_NetworkManager.LocalClient.IsSessionOwner)
            {
                Debug.Log($"Client-{m_NetworkManager.LocalClientId} is the session owner!");
            }
        }
    }

