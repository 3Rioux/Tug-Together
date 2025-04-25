#if NETCODE_FOR_GAMEOBJECTS_INSTALLED && COMBINED_NETCODE_QUICKSTART_AVAILABLE
using System;
using Unity.Multiplayer.Center.Integrations.DynamicSample;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unity.Multiplayer.Widgets.Editor
{
    [Serializable]
    internal class WidgetsNetcodeForGameobjectsSetup : IDynamicSample
    {

        const string k_ConnectUiTypeError = "Could not find TemporaryUI type. Make sure you didn't alter the namespace and class name. Expected Namespace: Unity.Multiplayer.Center.NetcodeForGameObjectsExample. Expected Class Name: TemporaryUI.";
        const string k_ConnectionUiError = "Could not find TemporaryUI in the scene. Make sure the Netcode Quickstart Sample Scene is open and the ConnectionUI component is present. Expected Scene Name: NGO_.";

        public string SampleName => "Widgets in Netcode For Entities Gameobjects Sample";
        public string PathToImportedSample => "";

        int m_MultiplayerCenterPlayerCount;
        ConnectionType m_ConnectionType;
        static Type TemporaryUIType => Type.GetType("Unity.Multiplayer.Center.NetcodeForGameObjectsExample.TemporaryUI, Unity.Multiplayer.Center.NetcodeForGameObjectsExample, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

        internal WidgetsNetcodeForGameobjectsSetup(int playerCount, ConnectionType connectionType)
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
            if (TemporaryUIType == null)
            {
                Debug.LogError(k_ConnectUiTypeError);
                return false;
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
            return true;
        }

        public bool IsReadyForPostImport => true;

        public bool PostImport()
        {
            if (!TryGetTemporaryUI(out var temporaryUI))
                return false;

            Undo.SetCurrentGroupName($"Add Widget Quickstart to {WidgetsOnboardingSection.NetcodeQuickstartSceneName}");
            var undoID = Undo.GetCurrentGroup();

            Undo.RecordObject(temporaryUI.gameObject, "Deactivate TemporaryUI");
            temporaryUI.gameObject.SetActive(false);
            WidgetCreation.CreateQuickStartSample(m_MultiplayerCenterPlayerCount, m_ConnectionType);

            // cleanup
            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Undo.CollapseUndoOperations(undoID);
            return true;
        }

        static bool TryGetTemporaryUI(out MonoBehaviour temporaryUI)
        {
            temporaryUI = null;

            if (TemporaryUIType == null)
            {
                Debug.LogError(k_ConnectUiTypeError);
                return false;
            }

            temporaryUI = Object.FindFirstObjectByType(TemporaryUIType, FindObjectsInactive.Include) as MonoBehaviour;
            if (temporaryUI == null)
            {
                Debug.LogError(k_ConnectionUiError);
                return false;
            }

            return true;
        }
    }
}
#endif
