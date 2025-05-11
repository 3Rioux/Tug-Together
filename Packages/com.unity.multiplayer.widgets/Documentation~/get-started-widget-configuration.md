# Configure a Widget 

Widgets that can join a session include a component that uses the default Widget Configuration asset. The component is specific to each widget:

| **Widget**           | **Component**      |
|----------------------|--------------------|
| Create Session       | CreateSession      |
| Quick Create Session | QuickCreateSession |
| Session List         | SessionList        |
| Join Session By Code | JoinSessionByCode  |
| Quick Join Session   | QuickJoinSession   |

 Use this component to set parameters for the session. To learn what each property in the Widget Configuration asset controls, refer to [Widget Configuration reference](ref-widget-configuration.md).

Each **Create Session** component automatically assigns the `DefaultWidgetConfiguration` asset to its **Widget Configuration** field. You can use this asset to set the maximum number of players for this session and control the connection type that a session uses. Choose from the following connection types: 
* **None**: Only creates a Session without further networking capabilities.
* **Direct**: Uses the IP address of a specific device to connect to it.
* **Relay**: Connects to devices outside the host network.
* **Distributed Authority**: Uses the distributed authority network topology. Requires `com.unity.netcode.gameobjects` (Netcode for GameObjects) **2.0.0 or higher**.

## Configure a custom session

To set up a custom Widget Configuration asset, do the following: 
1. Go to **Assets** > **Create** > **Multiplayer** > **Widgets** > **WidgetConfiguration**.
2. In the **Create Session** component assign the new Widget Configuration asset to the **Widget Configuration** field.
3. Change the values of the [Widget Configuration reference](ref-widget-configuration.md) properties.

## Additional resources
* [Get started with UGS](https://docs.unity.com/ugs/en-us/manual/overview/manual/getting-started)
* [Multiplayer Sessions](https://docs.unity.com/ugs/en-us/manual/mps-sdk/manual)
* [Multiplayer Play Mode](https://docs-multiplayer.unity3d.com/mppm/current/about/)
* [Distributed Authority](https://docs-multiplayer.unity3d.com/netcode/current/terms-concepts/distributed-authority/)