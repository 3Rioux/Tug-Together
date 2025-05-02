
# Introduction to multiplayer widgets

Use multiplayer widgets to test specific stages of the [Multiplayer Sessions](https://docs.unity.com/ugs/en-us/manual/mps-sdk/manual) workflow. Each multiplayer widget implements features of the Multiplayer Sessions package that uses the session SDK to group users together.

When you [create a multiplayer widget](get-started-create-widget.md), Unity creates [GameObjects](https://docs.unity3d.com/Manual/GameObjects.html) that contain the components that widget requires. Multiplayer widgets that use the session SDK contain the **Create Session** component. To use this component, [configure it for the session you want to use](get-started-widget-configuration.md).

|    **Menu**      |**Widget**| **Description** |
|--------------------|-|------|
| **Create** |||
|                    | **Create Session**             | A button that creates a new session with the name you enter in the **Session Name** field. |
| **Join and Leave** ||| 
|                    | **Join Session by Code**       | A field in which you can enter a session code to join a specific session. |
|                    | **Quick Join Session**         | A field in which you can search for a session and join it, or create a new session and join if there isn't already a session with that name.|
|                    | **Session list**               | A list of all available sessions. Select a session and select **Join** to join any session on this list.|
|                    | **Leave Session**              | A button that you can use to leave the current session.|
| **Info**           |||
|                    | **Session Player List**        | A list of all players that have joined the session. The host can kick players from the session when enabled. An indicator shows who is speaking when you enable Voice Chat.|
|                    | **Show Session Code**          | Displays the join code of the session the Player is currently in.|
| **Communication**  |||
|                    | **Select Input Audio Device**  | Select an input device to use for voice chat.|
|                    | **Select Output Audio Device** | Select an output device that outputs voice chat.|
|                    | **Text Chat**                  | A text chat window to send and receive messages from players in a session.|

**Note**: To test session behavior, [install the Multiplayer Play Mode package](https://docs-multiplayer.unity3d.com/mppm/current/install/) and [enable virtual players](https://docs-multiplayer.unity3d.com/mppm/current/virtual-players/virtual-players-enable/).

## Additional resources
* [Create a multiplayer widget](get-started-create-widget.md)
* [Get started with UGS](https://docs.unity.com/ugs/en-us/manual/overview/manual/getting-started)
* [Install Multiplayer Play Mode](https://docs-multiplayer.unity3d.com/mppm/current/install/)
