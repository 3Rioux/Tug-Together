using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if NEW_INPUT_SYSTEM_INSTALLED
using UnityEngine.InputSystem.UI;
#endif
#if NETCODE_FOR_GAMEOBJECTS_INSTALLED
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
#endif

namespace Unity.Multiplayer.Widgets.Editor
{
    internal class WidgetCreation
    {
        const string k_PrefabPath = "Packages/com.unity.multiplayer.widgets/Assets/";

        internal const string k_DefaultWidgetConfigurationName = "DefaultWidgetConfiguration";
        const string k_DefaultWidgetConfigurationNameWithExtension = k_DefaultWidgetConfigurationName + ".asset";
        
        const string k_WidgetMenuPath = "GameObject/Multiplayer Widgets/";
        const string k_CreateMenu = "Create/";
        const string k_JoinLeaveMenu = "Join and Leave/";
        const string k_InfoMenu = "Info/";
        const string k_CommunicationMenu = "Communication/";

        internal const string quickstartName = "Widget Quick Start Sample";

        enum MenuPriorities
        {
            Create = 10,
            JoinLeave = 15,
            Info = 20,
            Communication = 25
        }

        [MenuItem(k_WidgetMenuPath + k_JoinLeaveMenu + "Quick Join Session", false, (int)MenuPriorities.JoinLeave)]
        public static void CreateQuickJoinSessionWidget()
        {
            CreateWidget(AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(k_PrefabPath, "Prefabs/Join/Quick Join Session.prefab")), 
                "Quick Join Session");
        }
        
        // The Matchmaker Widget is still under development which is why we're hiding it for now.
        // Please check back in a future release for an updated Matchmaker Widget.
        // [MenuItem(k_CreatePath + k_JoinLeaveMenu + "Join Session By Matchmaker", false, (int)MenuPriorities.JoinLeave))]
        public static void CreateJoinSessionWithMatchmakerWidget()
        {
            CreateWidget(AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(k_PrefabPath,"Prefabs/Join/Join Session By Matchmaker.prefab")), 
                "Join Session By Matchmaker");
        }

        [MenuItem(k_WidgetMenuPath + k_InfoMenu + "Show Session Code", false, (int)MenuPriorities.Info)]
        public static void CreateShowSessionCodeWidget()
        {
            CreateWidget(AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(k_PrefabPath,"Prefabs/Information/Show Session Code.prefab")), 
                "Show Session Code");
        }
        
        [MenuItem(k_WidgetMenuPath + k_JoinLeaveMenu + "Join Session By Code", false, (int)MenuPriorities.JoinLeave)]
        public static void CreateJoinSessionByCodeWidget()
        {
            CreateWidget(AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(k_PrefabPath,"Prefabs/Join/Join Session By Code.prefab")),
                "Join Session By Code");
        }
        
        [MenuItem(k_WidgetMenuPath + k_CreateMenu + "Create Session", false, (int)MenuPriorities.Create)]
        public static void CreateCreateSessionWidget()
        {
            CreateWidget(AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(k_PrefabPath, "Prefabs/Create/Create Session.prefab")),
                "Create Session");
        }
        
        [MenuItem(k_WidgetMenuPath + k_JoinLeaveMenu + "Session List", false, (int)MenuPriorities.JoinLeave)]
        public static void CreateSessionListWidget()
        {
            CreateWidget(AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(k_PrefabPath, "Prefabs/Join/Session List.prefab")),
                "Session List");
        }
        
#if VIVOX_AVAILABLE
        [MenuItem(k_WidgetMenuPath + k_CommunicationMenu + "Select Input Audio Device", false, (int)MenuPriorities.Communication)]
        public static void CreateSelectInputAudioDeviceWidget()
        {
            CreateWidget(AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(k_PrefabPath,"Prefabs/Communication/Audio Input Device.prefab")),
                "Select Input Audio Device");
        }
        
        [MenuItem(k_WidgetMenuPath + k_CommunicationMenu + "Select Output Audio Device", false, (int)MenuPriorities.Communication)]
        public static void CreateSelectOutputAudioDeviceWidget()
        {
            CreateWidget(AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(k_PrefabPath,"Prefabs/Communication/Audio Output Device.prefab")),
                "Select Output Audio Device");
        }
        
        [MenuItem(k_WidgetMenuPath + k_CommunicationMenu + "Text Chat", false, (int)MenuPriorities.Communication)]
        public static void CreateTextChatWidget()
        {
            CreateWidget(AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(k_PrefabPath, "Prefabs/Communication/Text Chat.prefab")), 
                "Text Chat");
        }
#endif

        [MenuItem(k_WidgetMenuPath + k_InfoMenu + "Session Player List", false, (int)MenuPriorities.Info)]
        public static void CreateSessionPlayerListWidget()
        {
            CreateWidget(AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(k_PrefabPath, "Prefabs/Information/Session Player List.prefab")),
                "Session Player List");
        }
        
        [MenuItem(k_WidgetMenuPath + k_JoinLeaveMenu + "Leave Session", false, (int)MenuPriorities.JoinLeave + 2)]
        public static void CreateLeaveSessionWidget()
        {
            CreateWidget(AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(k_PrefabPath, "Prefabs/Leave Session.prefab")),
                "Leave Session");
        }

        public static GameObject CreateQuickStartSample(int playerCount, ConnectionType connectionType)
        {
            return CreateWidget(AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(k_PrefabPath, $"QuickStartSample/{quickstartName}.prefab")),
                "Quick Start Sample", playerCount, connectionType);
        }

        static GameObject CreateWidget(GameObject source, string name, int playerCount = 4, ConnectionType connectionType = ConnectionType.Relay)
        {
            Undo.SetCurrentGroupName($"Create {name}");
            var undoID = Undo.GetCurrentGroup();

            var canvas = CreateOrFindCanvas(out var selectedObjectIsInCanvasHierarchy);
            
            CreateNetworkManagerIfNotPresentInScene();

            // Instantiate our widget
            var createdWidget = Object.Instantiate(source, selectedObjectIsInCanvasHierarchy ? Selection.activeTransform : canvas.transform);
            createdWidget.name = createdWidget.name.Replace("(Clone)", "");
            Undo.RegisterCreatedObjectUndo(createdWidget, "Create Widget");
            
            LoadDefaultWidgetConfigurationIntoCreatedWidget(createdWidget, playerCount, connectionType);

            Undo.CollapseUndoOperations(undoID);
            EditorGUIUtility.PingObject(createdWidget);
            return createdWidget;
        }

        static void LoadDefaultWidgetConfigurationIntoCreatedWidget(GameObject createdWidget, int playerCount = 4, ConnectionType connectionType = ConnectionType.Relay)
        {
            // load configuration from project
            var assetPath = FindOrCreateDefaultWidgetConfiguration(playerCount, connectionType);

            var widgetConfiguration = AssetDatabase.LoadAssetAtPath<WidgetConfiguration>(assetPath);
            var sessionBase = createdWidget.GetComponent<EnterSessionBase>();
            if (sessionBase != null)
            {
                sessionBase.WidgetConfiguration = widgetConfiguration;
            }
            else
            {
                // We might have a widget with a hierarchy and one or more nested SessionBase Components.
                var sessionBases = createdWidget.GetComponentsInChildren<EnterSessionBase>();
                foreach (var baseComponent in sessionBases)
                {
                    baseComponent.WidgetConfiguration = widgetConfiguration;
                }
            }
        }

        internal static string FindOrCreateDefaultWidgetConfiguration(int playerCount, ConnectionType connectionType)
        {
            var assetGUIDs = AssetDatabase.FindAssets($"t:{nameof(WidgetConfiguration)}", null);
            foreach (var assetGUID in assetGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                if (Path.GetFileNameWithoutExtension(assetPath) == k_DefaultWidgetConfigurationName)
                {
                    return assetPath;
                }
            }
            
            // we didn't find the default configuration, so we create it
            var widgetConfiguration = ScriptableObject.CreateInstance<WidgetConfiguration>();
            widgetConfiguration.Name = "Session";
            widgetConfiguration.MaxPlayers = playerCount;
            widgetConfiguration.ConnectionType = connectionType;
            var path = $"Assets/{k_DefaultWidgetConfigurationNameWithExtension}";
            AssetDatabase.CreateAsset(widgetConfiguration, path);
            return path;
        }

        static void CreateNetworkManagerIfNotPresentInScene()
        {
#if NETCODE_FOR_GAMEOBJECTS_INSTALLED
            if(Object.FindFirstObjectByType<NetworkManager>() == null)
            {
                var networkManagerGameObject = new GameObject("NetworkManager", typeof(NetworkManager));
                var networkManager = networkManagerGameObject.GetComponent<NetworkManager>();
                var transport = networkManagerGameObject.AddComponent<UnityTransport>();
                networkManager.NetworkConfig.NetworkTransport = transport;
                Undo.RegisterCreatedObjectUndo(networkManagerGameObject, "Create NetworkManager");
            }
#endif
        }

        static Canvas CreateOrFindCanvas(out bool selectedObjectIsInCanvasHierarchy)
        {
            Canvas canvas = null;
            selectedObjectIsInCanvasHierarchy = false;
            
            // check if we can find a canvas in the parent hierarchy
            if (Selection.activeGameObject)
            {
                canvas = Selection.activeGameObject.GetComponentInParent<Canvas>();
                if (canvas)
                    selectedObjectIsInCanvasHierarchy = true;
            }
            
            // if we didn't find a canvas in the selection because there was no object selected
            // or there is no canvas in the parent hierarchy we try to find one in the scene
            if (canvas == null)
            {
                canvas = Object.FindFirstObjectByType<Canvas>();

                // if we still didn't find a canvas we create one
                if (canvas == null)
                {
                    var canvasGameObject = new GameObject("Canvas");
                    canvas = canvasGameObject.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasGameObject.AddComponent<CanvasScaler>();
                    canvasGameObject.AddComponent<GraphicRaycaster>();
                    canvasGameObject.layer = LayerMask.NameToLayer("UI");

                    Undo.RegisterCreatedObjectUndo(canvasGameObject, "Create Canvas");
                }

                CreateEventSystemIfNotPresentInScene();
            }

            return canvas;
        }

        static void CreateEventSystemIfNotPresentInScene()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var inputType = typeof(StandaloneInputModule);
#if ENABLE_INPUT_SYSTEM && NEW_INPUT_SYSTEM_INSTALLED
                inputType = typeof(InputSystemUIInputModule);
#endif
                var eventSystemGameObject = new GameObject("EventSystem", typeof(EventSystem), inputType);
                Undo.RegisterCreatedObjectUndo(eventSystemGameObject, "Create EventSystem");
            }
        }
    }
    
    
}
