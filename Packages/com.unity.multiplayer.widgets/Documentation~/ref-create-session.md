# Create Session reference

| **Property**                                  | **Description**                                                                                                                                                  |
|-----------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Widget configuration**                      | The script that sets the parameters of this session.                                                                                                             |
| **Joining Session ()**                        | Select Add **+** to specify the [UnityEvent](https://docs.unity3d.com/Manual/UnityEvents.html) that occurs when a user attempts to join a specific session.      |
| **Joined Session (ISession)**                 | Select Add **+** to specify the [UnityEvent](https://docs.unity3d.com/Manual/UnityEvents.html) that occurs when a user is in a session.                          |
| **Failed To Join Session (SessionException)** | Select Add **+** to specify the [UnityEvent](https://docs.unity3d.com/Manual/UnityEvents.html) that occurs when a user can't join a session because of an error. |

## Additional resources
* [Configure a session](get-started-widget-configuration.md)
* [Multiplayer Sessions](https://docs.unity.com/ugs/en-us/manual/mps-sdk/manual)