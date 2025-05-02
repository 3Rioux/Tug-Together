using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Multiplayer;

namespace Unity.Multiplayer.Widgets.NetcodeForEntitiesSetup
{
    /// <summary>
    /// Example Driver Constructor for your game. Configured to work well with UGS services.
    /// Copied from <see cref="Unity.Services.Multiplayer.EntitiesDriverConstructor"/>.
    /// </summary>
    public class MyDriverConstructor : INetworkStreamDriverConstructor
    {
        public NetworkConfiguration Configuration;
        public const int InvalidDriverId = 0;

        public int ClientIpcDriverId { get; private set; } = InvalidDriverId;
        public int ClientUdpDriverId { get; private set; } = InvalidDriverId;
        public int ClientWebSocketDriverId { get; private set; } = InvalidDriverId;

        public int ServerIpcDriverId { get; private set; } = InvalidDriverId;
        public int ServerUdpDriverId { get; private set; } = InvalidDriverId;
        public int ServerWebSocketDriverId { get; private set; } = InvalidDriverId;

        public void CreateClientDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            var settings = DefaultDriverBuilder.GetNetworkSettings();

            const int driverId = 1;
            if (Configuration.Role == NetworkRole.Host || Configuration.Role == NetworkRole.Server)
            {
                UnityEngine.Debug.Log($"[{this}] Registering Client Ipc Driver ({driverId})");
                DefaultDriverBuilder.RegisterClientIpcDriver(world, ref driverStore, netDebug, settings);
                ClientIpcDriverId = driverId;
            }
            else if (Configuration.Role == NetworkRole.Client)
            {
                if (Configuration.Type == NetworkType.Relay)
                {
                    var relayClientData = Configuration.RelayClientData;
                    settings.WithRelayParameters(ref relayClientData);
                }

                UnityEngine.Debug.Log($"[{this}] Registering Client Udp Driver ({driverId})");
                DefaultDriverBuilder.RegisterClientUdpDriver(world, ref driverStore, netDebug, settings);
                ClientUdpDriverId = driverId;
            }
        }

        public void CreateServerDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            var ipcSettings = DefaultDriverBuilder.GetNetworkSettings();

            var driverId = 1;

            if (Configuration.Role == NetworkRole.Host)
            {
                UnityEngine.Debug.Log($"[{this}] Registering Server Ipc Driver ({driverId})");
                DefaultDriverBuilder.RegisterServerIpcDriver(world, ref driverStore, netDebug, ipcSettings);
                ServerIpcDriverId = driverId;
                driverId++;
            }

            var udpSettings = DefaultDriverBuilder.GetNetworkSettings();

            if (Configuration.Type == NetworkType.Relay)
            {
                var relayServerData = Configuration.RelayServerData;
                udpSettings.WithRelayParameters(ref relayServerData);
            }

            UnityEngine.Debug.Log($"[{this}] Registering Server Udp Driver ({driverId})");
            DefaultDriverBuilder.RegisterServerUdpDriver(world, ref driverStore, netDebug, udpSettings);
            ServerUdpDriverId = driverId;
        }
    }
}