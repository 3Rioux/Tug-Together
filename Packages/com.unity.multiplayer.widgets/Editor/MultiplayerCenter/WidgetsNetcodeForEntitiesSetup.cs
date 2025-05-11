#if NETCODE_FOR_ENTITIES_INSTALLED && COMBINED_NETCODE_QUICKSTART_AVAILABLE
using System;
using System.IO;
using System.Reflection;
using Unity.Multiplayer.Center;
using Unity.Multiplayer.Center.Integrations.DynamicSample;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unity.Multiplayer.Widgets.Editor
{
    [Serializable]
    internal class WidgetsNetcodeForEntitiesSetup : IDynamicSample
    {
        internal const string TargetSetupFolder = "Assets/Netcode For Entities Widgets Sample/";
        
        const string k_WidgetsNetworkHandler = "MyWidgetsNetworkHandler";
        const string k_ScriptableObjectPath = TargetSetupFolder + "Custom Network Handler";
        static readonly string k_ScriptableObjectAssetPath = k_ScriptableObjectPath + $"/{k_WidgetsNetworkHandler}.asset";

        const string k_PackagePath = "Packages/com.unity.multiplayer.widgets/";
        const string k_PathInPackage = k_PackagePath + "Samples~/Netcode For Entities/";
        const string k_TargetNamespace = "Unity.Multiplayer.Widgets.NetcodeForEntitiesSetup.";

        const string k_ConnectUiTypeError = "Could not find ConnectionUI type. Make sure you didn't alter the namespace and class name. Expected Namespace: Unity.Multiplayer.Center.NetcodeForEntitiesExample. Expected Class Name: ConnectionUI.";
        const string k_OnBeforeConnectError = "Could not find OnBeforeConnect method in ConnectionUI. Make sure you didn't alter the method name. Expected Method Name: OnBeforeConnect.";
        const string k_ConnectionUiError = "Could not find ConnectionUI in the scene. Make sure the Netcode Quickstart Sample Scene is open and the ConnectionUI component is present. Expected Scene Name: ConnectionUI.";
        
        public string SampleName => "Widgets in Netcode For Entities Sample";
        public string PathToImportedSample => TargetSetupFolder;
        
        int m_MultiplayerCenterPlayerCount;
        ConnectionType m_ConnectionType;
        
        internal WidgetsNetcodeForEntitiesSetup(int playerCount, ConnectionType connectionType)
        {
            m_MultiplayerCenterPlayerCount = playerCount;
            // MP Center has no value for 100 players. Therefor when we get 100 from it, we know we clamped it before and issue this warning.
            if (m_MultiplayerCenterPlayerCount >= 100)
                Debug.LogWarning("Could not set more than 100 players for the DefaultWidgetConfiguration. Multiplayer Services do not support Sessions with more than 100 players. Setting player count to 100.");
            
            m_ConnectionType = connectionType;
        }
        
        public bool PreImportCheck()
        {
            // All necessary types of the Netcode for Entities quickstart setup need to be available to import the sample
            if (!GetNecessaryTypeInformation(out var _, out var _))
                return false;
            
            if (Directory.Exists(TargetSetupFolder))
            {
                var infoText = $"The required Script(s) you are about to import are already in your project. The content of the folder ({TargetSetupFolder}) will be deleted. Do you want to overwrite them?";
                if (!EditorUtility.DisplayDialog("Overwrite?", infoText, "Yes", "No"))
                    return false;
                AssetDatabase.DeleteAsset(TargetSetupFolder);
                CompilationPipeline.RequestScriptCompilation();
            }

            var existingSetup = GameObject.Find(WidgetCreation.quickstartName);
            if (existingSetup)
            {
                var infoText = "The Widgets Quick Start Sample is already in your scene. Do you want to override it?";
                if (!EditorUtility.DisplayDialog("Overwrite?", infoText, "Yes", "No"))
                    return false;
                Object.DestroyImmediate(existingSetup);
            }

            return true;
        }

        public bool CopyScripts()
        {
            return IOUtils.DirectoryCopy(k_PathInPackage, TargetSetupFolder);
        }

        public bool IsReadyForPostImport => FindType(k_WidgetsNetworkHandler, false) != null;
        public bool PostImport()
        {
            if (!GetNecessaryTypeInformation(out var onBeforeConnectMethod, out var connectionUI)) 
                return false;

            Undo.SetCurrentGroupName($"Add Widget Quickstart to {WidgetsOnboardingSection.NetcodeQuickstartSceneName}");
            var undoID = Undo.GetCurrentGroup();
            
            Undo.RecordObject(connectionUI.gameObject, "Deactivate ConnectionUI");
            connectionUI.gameObject.SetActive(false);
            var quickstartSample = WidgetCreation.CreateQuickStartSample(m_MultiplayerCenterPlayerCount, m_ConnectionType);
            
            // create scriptable object for MyWidgetsNetworkHandler
            var myWidgetsNetworkHandler = ScriptableObject.CreateInstance(FindType(k_WidgetsNetworkHandler).Name);
            var loadSceneModeMember = myWidgetsNetworkHandler.GetType().GetField("LoadSceneMode");
            loadSceneModeMember.SetValue(myWidgetsNetworkHandler, LoadSceneMode.Additive);

            // link MyWidgetsNetworkHandler to default widget configurations
            Directory.CreateDirectory(k_ScriptableObjectPath);
            AssetDatabase.CreateAsset(myWidgetsNetworkHandler, k_ScriptableObjectAssetPath);
            Undo.RegisterCreatedObjectUndo(myWidgetsNetworkHandler, $"Create {k_WidgetsNetworkHandler}");

            var defaultWidgetConfigurationPath = WidgetCreation.FindOrCreateDefaultWidgetConfiguration(m_MultiplayerCenterPlayerCount, m_ConnectionType);
            
            var widgetConfiguration = AssetDatabase.LoadAssetAtPath<WidgetConfiguration>(defaultWidgetConfigurationPath);
            Undo.RecordObject(widgetConfiguration, "Set {k_MyWidgetsNetworkHandler}");
            widgetConfiguration.NetworkHandler = myWidgetsNetworkHandler as CustomWidgetsNetworkHandler;
            EditorUtility.SetDirty(widgetConfiguration);
            
            // wire events to ConnectionUI
            var onBeforeConnectDelegate = Delegate.CreateDelegate(typeof(UnityAction), connectionUI, onBeforeConnectMethod) as UnityAction;
            
            var createSession = quickstartSample.GetComponentInChildren<CreateSession>();
            var joinSessionByCode = quickstartSample.GetComponentInChildren<JoinSessionByCode>();
            
            Undo.RecordObject(createSession, "Add OnConnected to CreateSession");
            UnityEventTools.AddVoidPersistentListener(createSession.JoiningSession, onBeforeConnectDelegate);
            
            Undo.RecordObject(joinSessionByCode, "Add OnConnected to JoinSessionByCode");
            UnityEventTools.AddVoidPersistentListener(joinSessionByCode.JoiningSession, onBeforeConnectDelegate);
            
            // cleanup
            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            
            Undo.CollapseUndoOperations(undoID);
            AssetDatabase.SaveAssets();

            return true;
        }

        static bool GetNecessaryTypeInformation(out MethodInfo onBeforeConnectMethod, out MonoBehaviour connectionUI)
        {
            onBeforeConnectMethod = null;
            connectionUI = null;
            
            var connectionUIType = Type.GetType("Unity.Multiplayer.Center.NetcodeForEntitiesSetup.ConnectionUI, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            if (connectionUIType == null)
            {
                Debug.LogError(k_ConnectUiTypeError);
                return false;
            }

            onBeforeConnectMethod = connectionUIType.GetMethod("OnBeforeConnect");
            if (onBeforeConnectMethod == null)
            {
                Debug.LogError(k_OnBeforeConnectError);
                return false;
            }
            
            connectionUI = Object.FindFirstObjectByType(connectionUIType, FindObjectsInactive.Include) as MonoBehaviour;
            if (connectionUI == null)
            {
                Debug.LogError(k_ConnectionUiError);
                return false;
            }

            return true;
        }

        static Type FindType(string typeName, bool logError = true)
        {
            var typeString = $"{k_TargetNamespace}{typeName}, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            var type = Type.GetType(typeString);

            if (type != null)
                return type;

            if (logError)
                Debug.LogError($"Could not find {typeName}. Please check the script location.");

            return null;
        }
    }
}
#endif
