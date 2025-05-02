# Control how Unity initializes a Multiplayer service

When you enter Play Mode in a project that uses Multiplayer Widgets, Unity does the following by default:
* Initializes UnityServices.
* Initializes Vivox if it already exists in your project. 
* Uses the anonymous sign-in to authenticate a user.

If your project uses another authentication method, or you want to initialize these features at another time, override the default initialization behavior. To do this:
* In the main menu, go to **Edit** > **Project Settings**. 
* Select **Widgets**.
* Enable **Use Custom Service Initialization**.

When you enable this property, you need to manually initialize the following services for Widgets to work: 

* [Unity Gaming Services (UGS)](https://docs.unity.com/ugs/manual/overview/manual/getting-started#InitializingUGS)
* [Authentication](https://docs.unity.com/ugs/en-us/manual/authentication/manual/use-anon-sign-in)
* [Vivox Service](https://docs.unity.com/ugs/en-us/manual/authentication/manual/use-anon-sign-in), when the Vivox Package exists in your Project.

When you enable **Use Custom Service Initialization** Unity creates a ScriptableObject in the Assets folder under **Assets** > **Multiplayer Widgets** > **Resources** > **MultiplayerWidgetsSettings**.
To tell Multiplayer Widgets that you have initialized one or more of these services, call `WidgetServiceInitialization.ServicesInitialized` in the **MultiplayerWidgetsSettings** script.

## Additional resources

* [Get started with UGS](https://docs.unity.com/ugs/en-us/manual/overview/manual/getting-started)
* [Configure a session](get-started-widget-configuration.md)
* [Widget configuration reference](ref-widget-configuration.md)
* [Game Server Hosting (Multiplay)](https://docs.unity.com/ugs/en-us/manual/game-server-hosting/manual/welcome)
* [Welcome to Voice and Text Chat (Vivox)](https://docs.unity.com/vivox/Article.html)