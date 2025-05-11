
# Create a multiplayer widget

To add one or more widgets to your scene, in the main menu go to **GameObject** > **Multiplayer** > **Widgets**.
To learn about how widgets work, refer to [Introduction to multiplayer widgets](get-started-learn-widgets.md).

|    **Menu**      |**Widget**| **Description** |
|--------------------|-|------|
| **Create** |||
|                    | **Create Session**             | A button that creates a new session with the name you enter in the **Session Name** field. |
| **Join and Leave** ||| 
|                    | **Join Session by Code**       | A field in which you can enter a session code to join a specific session. |
|                    | **Quick Join Session**         | A field in which you can search for a session and join it, or create a new session and join if there is not already a session with that name.|
|                    | **Session list**               | A list of all available sessions. Select a session and select **Join** to join any session on this list.|
|                    | **Leave Session**              | A button that you can use to leave the current session.|
| **Info**           |||
|                    | **Session Player List**        | A list of all players that have joined the session. The host can kick players from the session when enabled. An indicator shows who is speaking when you enable Voice Chat.|
|                    | **Show Session Code**          | Displays the join code of the session the Player is currently in.|
| **Communication**  |||
|                    | **Select Input Audio Device**  | Select an input device to use for voice chat.|
|                    | **Select Output Audio Device** | Select an output device that outputs voice chat.|
|                    | **Text Chat**                  | A text chat window to send and receive messages from players in a session.|


## Prerequisites

To use any widgets in your scene that use [Multiplayer Services package](https://docs.unity.com/ugs/en-us/manual/mps-sdk/manual) functionality, perform the following actions:

1. [Create a project](https://docs.unity.com/ugs/en-us/manual/overview/manual/getting-started#CreateProject) in the Unity Cloud Dashboard.
2. [Link your project](https://docs.unity.com/ugs/en-us/manual/overview/manual/getting-started#LinkProject) to a Unity Editor project.

To learn more, refer to [Get started with Unity Gaming Services (UGS)](https://docs.unity.com/ugs/en-us/manual/overview/manual/getting-started).

## Add a multiplayer widget to your scene

To create a multiplayer widget, perform the following actions: 

1. Right-click anywhere in the **Hierarchy** window to open the Context menu.
2. Select **Multiplayer** > **Widgets**.
3. Select a widget to create. 
4. Unity automatically creates the widget GameObject and its dependencies in the **Hierarchy** window and places it in your scene. 
5. Enter **Play** mode to test the widget.

**Note**: To test multiplayer widgets that use sessions, [install the Multiplayer Play Mode package](https://docs-multiplayer.unity3d.com/mppm/current/install/) and [enable virtual players](https://docs-multiplayer.unity3d.com/mppm/current/virtual-players/virtual-players-enable/).

## Additional resources
* [Introduction to multiplayer widgets](get-started-learn-widgets.md)
* [Configure a session](get-started-widget-configuration.md)
* [Widget configuration reference](ref-widget-configuration.md)
* [Game Server Hosting (Multiplay)](https://docs.unity.com/ugs/en-us/manual/game-server-hosting/manual/welcome)
* [Welcome to Voice and Text Chat (Vivox)](https://docs.unity.com/vivox/Article.html)