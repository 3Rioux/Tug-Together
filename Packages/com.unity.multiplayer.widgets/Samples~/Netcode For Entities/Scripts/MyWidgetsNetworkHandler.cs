using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Multiplayer.Widgets;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Unity.Multiplayer.Widgets.NetcodeForEntitiesSetup
{
    /// <summary>
    ///     Sessions use the <see cref="INetworkHandler"/> interface to allow user-code (like this) to initialize/bootstrap Netcode for Entities.
    ///     However, as Widgets are providing this workflow via their UI button helpers (example: <see cref="QuickJoinSession"/>),
    ///     user-code must instead pass Widgets this <see cref="INetworkHandler"/> configuration. They do so via this <see cref="CustomWidgetsNetworkHandler"/>.
    /// </summary>
    /// <remarks>
    ///     This script shows just one example of this integration, which you are free to customize.
    ///     It makes the following assumptions:
    ///     <list type="bullet">
    ///     <item>There is a 'UI' scene containing Widgets UI, and said UI is configured to use the <see cref="WidgetConfiguration"/> which itself uses this <see cref="CustomWidgetsNetworkHandler"/> (via <see cref="WidgetConfiguration.NetworkHandler"/>).</item>
    ///     <item>There is a different 'Gameplay' scene containing all gameplay entities (via a sub-scene), where any Netcode for Entities related authoring <see cref="GameObject"/>'s exist.</item>
    ///     <item>Said 'Gameplay' scene will be exclusively loaded (via <see cref="LoadSceneMode.Single"/>) in this handler.</item>
    ///     <item>Similarly, the Widgets UI scene will be exclusively loaded (via <see cref="LoadSceneMode.Single"/>) when the session ends.</item>
    ///     <item>You have disabled automatic Entities bootstrapping (see <see cref="Unity.Entities.ICustomBootstrap"/>) (which Netcode for Entities overrides via <see cref="ClientServerBootstrap"/>). See https://docs.unity3d.com/Packages/com.unity.netcode@1.3/api/Unity.NetCode.OverrideAutomaticNetcodeBootstrap.html for details on how to disable this.</item>
    ///     </list>
    /// </remarks>
    [CreateAssetMenu(fileName = "MyWidgetsNetworkHandler", menuName = "MyWidgetsNetworkHandler", order = 1)]
    public class MyWidgetsNetworkHandler : CustomWidgetsNetworkHandler
    {
        public string GameplayScene = "Gameplay";
        public double ConnectionTimeoutSeconds = 7;
        public LoadSceneMode LoadSceneMode = LoadSceneMode.Single;
        
        string m_MenuScenePath;
        World m_ClientWorld;
        World m_ServerWorld;
        NetworkEndpoint m_TargetEndpoint;
        EntityQuery m_ClientDriverQuery;
        EntityQuery m_ClientConnectionQuery;
        EntityQuery m_ServerDriverQuery;
        ref NetworkStreamDriver ClientDriverRW => ref m_ClientDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;
        ref NetworkStreamDriver ServerDriverRW => ref m_ServerDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;
        readonly MyDriverConstructor m_DriverConstructor = new MyDriverConstructor();
        CancellationTokenSource m_CancellationToken;
        ConnectionState.State m_NetcodeDriverStatus;

        public override async Task StartAsync(NetworkConfiguration configuration)
        {
            // Validate the 'Netcode for Entities' necessary conditions to begin:
            {
                Debug.Assert(m_DriverConstructor.Configuration == null);
                Debug.Assert(m_CancellationToken == null);
                // Check to see if OTHER client and server worlds exist:
                var clientWorlds = ClientServerBootstrap.ClientWorlds.Count;
                var serverWorlds = ClientServerBootstrap.ServerWorlds.Count;
                if (serverWorlds + clientWorlds != 0)
                    throw new InvalidOperationException($"[{name}] StartAsync(Session[Role:`{configuration.Role}`, Type:`{configuration.Type}`, Listen:'{configuration.DirectNetworkListenAddress}', Connect:'{configuration.DirectNetworkPublishAddress}`]), but we've detected {clientWorlds} existing ClientWorlds and/or {serverWorlds} existing ServerWorlds, which this handler is unable to infer what to do with. These existing worlds are therefore incompatible. They are likely created by Netcode for Entities ClientServerBootstrap implementation (which is enabled by default). Therefore, we strongly recommend disabling the Netcode for Entities automatic bootstrapping when using Widgets UI Session management flows like this one. See https://docs.unity3d.com/Packages/com.unity.netcode@1.3/api/Unity.NetCode.OverrideAutomaticNetcodeBootstrap.html doc page for details.");
            }

            try
            {
                m_DriverConstructor.Configuration = configuration;
                m_CancellationToken = new CancellationTokenSource();
                // We don't need to set the NetworkStreamReceiveSystem.DriverConstructor as we're creating our own drivers.
                SetupAndValidateNetcodeWorld(out m_ClientWorld, WorldFlags.GameClient, ClientServerBootstrap.CreateClientWorld);
                SetupAndValidateNetcodeWorld(out m_ServerWorld, WorldFlags.GameServer, ClientServerBootstrap.CreateServerWorld);

                var widgetsScene = SceneManager.GetActiveScene();
                if (!widgetsScene.IsValid())
                    throw new InvalidOperationException($"[{name}] Failed to fetch the active scene, so won't be able to return to it: `{widgetsScene.path}`!");
                m_MenuScenePath = widgetsScene.path;

                var loadedScene = SceneManager.LoadScene(GameplayScene, new LoadSceneParameters
                {
                    loadSceneMode = LoadSceneMode,
                    localPhysicsMode = LocalPhysicsMode.None,
                });
                await Awaitable.NextFrameAsync();
                if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                    throw new InvalidOperationException($"[{name}] Failed to load Gameplay scene '{GameplayScene}'!");

                // Drivers:
                if (m_DriverConstructor.Configuration.Role != NetworkRole.Client)
                {
                    SetupServerDriver();
                    Listen();
                    RunContinuousMaintenanceTask(MaintainServerConnection);
                }
                if (m_DriverConstructor.Configuration.Role != NetworkRole.Server)
                {
                    SetupClientDriver();
                    RunContinuousMaintenanceTask(MaintainClientConnection);
                    await ValidateConnectionAsync();
                }
            }
            catch
            {
                // TODO - Remove this try & CleanupAll call once INetworkHandler supports calling StopAsync automatically in the error case.
                CleanupAll();
                throw;
            }
        }

        void RunContinuousMaintenanceTask(Action action)
        {
            _ = Task.Run(Loop, m_CancellationToken.Token);

            async void Loop()
            {
                try
                {
                    await Awaitable.MainThreadAsync();
                    while (true)
                    {
                        m_CancellationToken.Token.ThrowIfCancellationRequested();
                        action();
                        m_CancellationToken.Token.ThrowIfCancellationRequested();
                        await Awaitable.NextFrameAsync(m_CancellationToken.Token);
                        await Awaitable.MainThreadAsync();
                    }
                }
                catch (TaskCanceledException)
                {
                    Debug.Log($"[MyWidgetsNetworkHandler] Maintenance task '{action.Method.Name}' cancelled!");
                }    
                catch (OperationCanceledException)
                {
                    Debug.Log($"[MyWidgetsNetworkHandler] Maintenance task '{action.Method.Name}' cancelled!");
                }
                catch (Exception e)
                {
                    // TODO - Find a way to report this to the session!
                    Debug.LogException(e);
                    Debug.LogError($"[MyWidgetsNetworkHandler] Maintenance task '{action.Method.Name}' failed by throwing an unexpected `{e.GetType()}`! '{e.Message}'");
                }
            }
        }

        /// <summary>
        /// Ensures the ServerWorld is alive.
        /// Throws if the world is null!
        /// </summary>
        void MaintainServerConnection()
        {
            if (m_ServerWorld == null || !m_ServerWorld.IsCreated)
                throw new InvalidOperationException($"[{name}] Server world disposed during the Session!");
        }

        /// <summary>
        /// Continuously keep attempting to reconnect N4E to the desired endpoint, if it disconnects.
        /// Throws if the world is null!
        /// </summary>
        void MaintainClientConnection()
        {
            if (!TryGetClientConnectionEntity(out var connectionEntity))
            {
                // We don't need to check for Disconnected state here,
                // because Netcode for Entities will delete the connection entity after one frame anyway.
                Debug.Log($"[{name}] Detected ClientWorld '{m_ClientWorld.Name}' has no connection, so connecting now with role `{m_DriverConstructor.Configuration.Role}`...");
                switch (m_DriverConstructor.Configuration.Role)
                {
                    case NetworkRole.Client:
                        Connect();
                        break;
                    case NetworkRole.Host:
                        SelfConnect();
                        break;
                    default: throw new ArgumentOutOfRangeException($"[{name}] Invalid role `{m_DriverConstructor.Configuration.Role}`");
                }
            }
            m_NetcodeDriverStatus = TryGetClientConnectionEntity(out connectionEntity) 
                ? m_ClientWorld.EntityManager.GetComponentData<NetworkStreamConnection>(connectionEntity).CurrentState
                : ConnectionState.State.Unknown;
        }

        bool TryGetClientConnectionEntity(out Entity connectionEntity)
        {
            if (m_ClientWorld == null || !m_ClientWorld.IsCreated)
                throw new InvalidOperationException($"[{name}] Client world disposed during the Session!");
            var numConnections = m_ClientConnectionQuery.CalculateEntityCount();
            if(numConnections > 1)
                throw new InvalidOperationException($"[{name}] Client world has {numConnections} connection entities!?");
            if (numConnections == 1)
            {
                connectionEntity = m_ClientConnectionQuery.GetSingletonEntity();
                return true;
            }
            connectionEntity = default;
            return false;
        }

        /// <inheritdoc cref="EntitiesNetcodeNetworkHandler"/>
        void SetupAndValidateNetcodeWorld(out World newWorld, WorldFlags worldType, Func<string, World> worldCreationFunc)
        {
            // Create the required world (assuming we need it):
            newWorld = default;
            var worldTypeMustExistForConfiguredRole = m_DriverConstructor.Configuration.Role switch
            {
                NetworkRole.Client => worldType == WorldFlags.GameClient,
                NetworkRole.Server => worldType == WorldFlags.GameServer,
                NetworkRole.Host => worldType == WorldFlags.GameClient || worldType == WorldFlags.GameServer,
                _ => throw new ArgumentOutOfRangeException($"Cannot resolve `worldTypeMustExistForConfiguredRole` as unknown role `{m_DriverConstructor.Configuration.Role}`!"),
            };
            if(!worldTypeMustExistForConfiguredRole)
                return;
            
            var worldName = worldType switch
            {
                WorldFlags.GameClient => $"MPSClientWorld",
                WorldFlags.GameServer => $"MPSServerWorld",
                _ => throw new ArgumentOutOfRangeException($"Invalid worldType `{worldType}`!"),
            };

            Debug.Log($"[{name}] Creating Netcode `{worldType}` World automatically (named: '{worldName}'). Disposal of this world will be handled automatically here, during the exiting of a session.");
            try // World-creation can catastrophically fail, so this try/catch is an attempt to clean-up after ourselves.
            {
                newWorld = worldCreationFunc(worldName);
            }
            catch (Exception createException)
            {
                Debug.LogError($"[{name}] `{createException.GetType()}` thrown while creating '{worldName}': '{createException.Message}'!");
                Debug.LogException(createException);
                throw; // Rethrow as we did fail, we'll clean-up the world when we get back.
            }

            // Validate that our created world matches our configuration:
            var chosenWorldMatchesRole = m_DriverConstructor.Configuration.Role switch
            {
                NetworkRole.Client => newWorld.IsClient(),
                NetworkRole.Server => newWorld.IsServer(),
                NetworkRole.Host => newWorld.IsClient() || newWorld.IsServer(),
                _ => throw new InvalidOperationException($"[{name}] Cannot resolve `chosenWorldMatchesRole` as unknown role `{m_DriverConstructor.Configuration.Role}`!"),
            };
            if (!chosenWorldMatchesRole)
                throw new InvalidOperationException($"Session configuration role is `{m_DriverConstructor.Configuration.Role}`, but Netcode `{worldType}` world '{newWorld.Name}' (`{newWorld.Flags}`) does not have the expected flag `WorldFlags.{worldType}` required for this role!");
        }

        void SetupClientDriver()
        {
            m_ClientDriverQuery = m_ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            m_ClientConnectionQuery = m_ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamConnection>());
            using (var debugQuery = m_ClientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetDebug>()))
            {
                var netDebug = debugQuery.GetSingleton<NetDebug>();
                var driverStore = new NetworkDriverStore();
                m_DriverConstructor.CreateClientDriver(m_ClientWorld, ref driverStore, netDebug);
                ClientDriverRW.ResetDriverStore(m_ClientWorld.Unmanaged, ref driverStore);
            }
        }

        void SetupServerDriver()
        {
            m_ServerDriverQuery = m_ServerWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            using (var debugQuery = m_ServerWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetDebug>()))
            {
                var netDebug = debugQuery.GetSingleton<NetDebug>();
                var driverStore = new NetworkDriverStore();
                m_DriverConstructor.CreateServerDriver(m_ServerWorld, ref driverStore, netDebug);
                ServerDriverRW.ResetDriverStore(m_ServerWorld.Unmanaged, ref driverStore);
            }
        }

        void Listen()
        {
            NetworkEndpoint listenEndpoint = m_DriverConstructor.Configuration.Type switch
            {
                NetworkType.Direct => m_DriverConstructor.Configuration.DirectNetworkListenAddress,
                NetworkType.Relay => NetworkEndpoint.AnyIpv4,
                _ => throw new InvalidOperationException($"[{name}] Netcode for Entities does not support `{m_DriverConstructor.Configuration.Type}`!"),
            };
            if (!listenEndpoint.IsValid)
                throw new InvalidOperationException($"[{name}] Invalid NetworkEndpoint specified to listen on: '{listenEndpoint}'!");

            if (!ServerDriverRW.Listen(listenEndpoint))
                throw new InvalidOperationException($"[{name}] We assume the first driver created is IPC Network Interface! Check your `INetworkStreamDriverConstructor` concrete implementation `{m_DriverConstructor.GetType()}`!");

            var serverUdpPort = ServerDriverRW.GetLocalEndPoint(m_DriverConstructor.ServerUdpDriverId).Port;
            Debug.Log($"[{name}] ServerDriver[Udp]:`{serverUdpPort}` set on ServerWorld '{m_ServerWorld.Name}'.");

            if (m_DriverConstructor.Configuration.Type == NetworkType.Direct)
            {
                m_DriverConstructor.Configuration.UpdatePublishPort(serverUdpPort);
            }
        }

        void SelfConnect()
        { 
            var ipcPort = ServerDriverRW.GetLocalEndPoint(m_DriverConstructor.ServerIpcDriverId).Port;
            m_TargetEndpoint = NetworkEndpoint.LoopbackIpv4.WithPort(ipcPort);
            Debug.Log($"[{name}] ServerDriver[Ipc]:{ServerDriverRW.GetLocalEndPoint(m_DriverConstructor.ServerIpcDriverId).Port} pulled from ServerWorld '{m_ServerWorld.Name}', ClientDriver SelfConnect:'{m_TargetEndpoint}' set on ClientWorld '{m_ClientWorld.Name}'.");
            ClientDriverRW.Connect(m_ClientWorld.EntityManager, m_TargetEndpoint);
        }

        void Connect()
        {
            m_TargetEndpoint = m_DriverConstructor.Configuration.Type switch
            {
                NetworkType.Direct => m_DriverConstructor.Configuration.DirectNetworkPublishAddress,
                NetworkType.Relay => m_DriverConstructor.Configuration.RelayClientData.Endpoint,
                _ => throw new InvalidOperationException($"[{name}] Netcode for Entities does not support `{m_DriverConstructor.Configuration.Type}`!"),
            };
            if (!m_TargetEndpoint.IsValid)
                throw new InvalidOperationException($"[{name}] Invalid NetworkEndpoint specified to connect to: '{m_TargetEndpoint}'!");

            Debug.Log($"[{name}] ClientDriver Connect: '{m_TargetEndpoint}' on ClientWorld '{m_ClientWorld.Name}'!");
            ClientDriverRW.Connect(m_ClientWorld.EntityManager, m_TargetEndpoint);
        }

        async Task ValidateConnectionAsync()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (m_NetcodeDriverStatus != ConnectionState.State.Connected)
            {
                await Awaitable.NextFrameAsync(m_CancellationToken.Token);
                if (stopwatch.Elapsed.TotalSeconds >= ConnectionTimeoutSeconds)
                {
                    await ClientRequestDisconnectAsync(NetworkStreamDisconnectReason.Timeout);
                    throw new Exception($"[{name}] Connection timeout of '{ConnectionTimeoutSeconds}' hit!");
                }
            }
        }

        public override Task StopAsync()
        {
            Debug.Assert(m_DriverConstructor.Configuration != null, $"[{name}] StopAsync called but already out of the session!");
            CleanupAll();
            return Task.CompletedTask;
        }

        void CleanupAll()
        {
            Debug.Log($"[{name}] Cleaning up everything...");
            m_CancellationToken?.Cancel(throwOnFirstException:false);
            m_CancellationToken = default;
            AttemptCleanupWorld(m_ClientWorld);
            AttemptCleanupWorld(m_ServerWorld);
            // Note: The EntityQuery type does not need to be manually disposed here,
            // as they're all automatically disposed when the world is.
            m_DriverConstructor.Configuration = default;
            m_NetcodeDriverStatus = ConnectionState.State.Unknown;
            m_TargetEndpoint = default;

            // Return to the main menu:
            if (!string.IsNullOrWhiteSpace(m_MenuScenePath))
            {
                Debug.Log($"[{name}] Returning to Widgets Menu scene '{m_MenuScenePath}'...");
                SceneManager.LoadScene(m_MenuScenePath, LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError($"[{name}] Unable to return to the Widgets Menu scene as no valid path!");
            }
            m_MenuScenePath = default;

            void AttemptCleanupWorld(World world)
            {
                try 
                {
                    if (world != null && world.IsCreated)
                    {
                        Debug.Log($"[{name}] Disposing previously created World '{world.Name}'!");
                        world.Dispose();
                    }
                    else Debug.LogWarning($"[{name}] Attempting to dispose previously created World, but it was already disposed!");
                }
                catch (Exception disposeException)
                {
                    Debug.LogException(disposeException);
                }
                world = default;
            }
        }

        async Task ClientRequestDisconnectAsync(NetworkStreamDisconnectReason disconnectReason)
        {
            if (m_ClientWorld != null && m_ClientWorld.IsCreated)
            {
                if (TryGetClientConnectionEntity(out var connectionEntity))
                {
                    m_ClientWorld.EntityManager.AddComponentData(connectionEntity, new NetworkStreamRequestDisconnect {Reason = disconnectReason});
                    await Awaitable.NextFrameAsync(m_CancellationToken.Token); // Wait for this NetworkStreamRequestDisconnect to go through!
                }
            }
        }
    }
}