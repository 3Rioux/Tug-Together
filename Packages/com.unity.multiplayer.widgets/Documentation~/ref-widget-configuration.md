# Widget configuration reference


| **Property**                   |                        | **Description**                                                                                                                                          |
|--------------------------------|------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Name**                       |                        | Enter a name for this Widget configuration.                                                                                                              |
| **Connection Type**            |                        | Select the type of connection that this session uses: **None**, **Direct**, **Relay** or **Distributed Authority**                                       |
| **Direct Connection Settings** |                        |                                                                                                                                                          |
|                                | **Connection Mode**    | Select a mode for the `Direct` connection: **Listen** or **Publish**                                                                                     |
|                                | **Listen Ip Address**  | Enter the listen IP address that this session listens to for incoming connections.                                                                       |
|                                | **Publish Ip Address** | Enter the publish IP address that this session uses as a public IP for clients to connect to. This property is only available when you set `Connection Mode` to `Publish`. |
|                                | **Port**               | Select the port that this session uses to make a `Direct` connection.                                                                                    |
| **Network Handler**            |                        | Adds a custom network handler to the session creation. Unity uses the Multiplayer Services Network Handler when it creates a session by default. For more information, refer to [INetworkHandler](https://docs.unity3d.com/Packages/com.unity.services.multiplayer@1.0/api/Unity.Services.Multiplayer.INetworkHandler.html) in the Multiplayer Services API for reference. To get started with Netcode for Entities and the multiplayer widgets, use the [Multiplayer Center's Quickstart tab](widgets-and-the-multiplayer-center.md). |
| **Session Settings**           |                        |                                                                                                                                                          |
|                                | **Max Players**        | Select the maximum number of players that can join this session.                                                                                         |
| **Vivox Settings**             |                        | Vivox Settings are only available when the `Vivox` package exists in your project.                                                                                |
|                                | **Enable Voice Chat**  | When enabled, the use automatically joins a Voice Chat when they join a Session.                                                                  |

## Additional resources

* [Configure a session](get-started-widget-configuration.md)
* [Create Session reference](ref-create-session.md)
* [Get started with multiplayer widgets](get-started.md)
* [Get started with Relay](https://docs.unity.com/ugs/en-us/manual/relay/manual/get-started)
* [Vivox overview](https://docs.unity.com/ugs/en-us/manual/vivox-unity/manual/Unity/Unity)
