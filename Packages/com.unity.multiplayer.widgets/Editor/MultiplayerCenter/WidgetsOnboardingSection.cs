#if MULTIPLAYER_CENTER_AVAILABLE
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Multiplayer.Center.Common;
using Unity.Multiplayer.Center.Onboarding;

#if (NETCODE_FOR_ENTITIES_INSTALLED || NETCODE_FOR_GAMEOBJECTS_INSTALLED) && COMBINED_NETCODE_QUICKSTART_AVAILABLE
using Unity.Multiplayer.Center.Integrations.DynamicSample;
using Object = UnityEngine.Object;
#endif

#if MULTIPLAYER_CENTER_ANALYTICS_AVAILABLE
using Unity.Multiplayer.Center.Common.Analytics;
#endif

using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Widgets.Editor
{
    /// <summary>
    /// If the multiplayer center is installed, this section will be added to the "Getting Started" tab.
    /// </summary>
    [OnboardingSection(OnboardingSectionCategory.ConnectingPlayers, "Multiplayer Widgets", TargetPackageId = "com.unity.multiplayer.widgets",
        Priority = 10)]
    class WidgetsOnboardingSection : ISectionDependingOnUserChoices
#if MULTIPLAYER_CENTER_ANALYTICS_AVAILABLE
        , ISectionWithAnalytics
#endif
    {
        const string k_DocumentationLink = "https://docs.unity3d.com/Packages/com.unity.multiplayer.widgets@latest";
        public VisualElement Root { get; private set; }

#if MULTIPLAYER_CENTER_ANALYTICS_AVAILABLE
        public IOnboardingSectionAnalyticsProvider AnalyticsProvider { get; set; }
#endif

        Button m_Button;
        Button m_NetcodeButton;
        Label m_NetcodeDescription;

        int m_MultiplayerCenterPlayerCount = 4;
        ConnectionType m_ConnectionType = ConnectionType.Relay;

        const string k_Title = "Multiplayer Widgets";

        const string k_ButtonLabel = "Create and open scene with Widget sample";

        const string k_ShortDescription = "Multiplayer Widgets allows you to quickly test and create multiplayer sessions from the Multiplayer Services package without the need to set up those services in code.\n\n" +
            "All Widgets can be accessed by right clicking the Hierarchy.";

        /// <summary>
        /// The name of the Quickstart Scene that needs to be open for the NetcodeSample Button to be active.
        /// </summary>
#if NETCODE_FOR_GAMEOBJECTS_INSTALLED && COMBINED_NETCODE_QUICKSTART_AVAILABLE
        internal const string NetcodeQuickstartSceneName = "NGO_Setup";
#else
        internal const string NetcodeQuickstartSceneName = "ConnectionUI";
#endif

        static readonly string k_NetcodeSampleDescriptionNetcodeSceneOpen = $"Add the Widget Quickstart to the {NetcodeQuickstartSceneName} Scene from the Netcode Quickstart. This deactivates the existing UI and replaces it with a Widget equivalent.";
        static readonly string k_NetcodeSampleDescriptionNetcodeSceneNotOpen = $"Open the {NetcodeQuickstartSceneName} Scene from the Netcode Quickstart to add a Widget Quickstart to the existing scene. This deactivates the existing UI and replaces it with a Widget equivalent.";
        static readonly string k_AddWidgetsToNetcodeSampleButtonText = $"Add Widget Quickstart to {NetcodeQuickstartSceneName} Scene";

        static readonly string k_WidgetsQuickstartScenePath = $"{k_WidgetsQuickstartSceneDirectory}/WidgetsQuickstart.unity";
        const string k_WidgetsQuickstartSceneDirectory = "Assets/Widgets/Scenes"; 
        
        Action OnButtonClicked => CreateQuickStartSample;
        
        void CreateQuickStartSample()
        {
#if MULTIPLAYER_CENTER_ANALYTICS_AVAILABLE
            AnalyticsProvider.SendInteractionEvent(InteractionDataType.CallToAction, k_ButtonLabel);
#endif
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            
            if(AssetDatabase.AssetPathExists(k_WidgetsQuickstartScenePath))
            {
                if(!EditorUtility.DisplayDialog("Overwrite?", "The Widgets Quickstart Scene already exists. Do you want to overwrite it?", "Yes", "No"))
                    return;
                AssetDatabase.DeleteAsset(k_WidgetsQuickstartScenePath);
            }
            
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            WidgetCreation.CreateQuickStartSample(m_MultiplayerCenterPlayerCount, m_ConnectionType);
            
            Directory.CreateDirectory(k_WidgetsQuickstartSceneDirectory);
            AssetDatabase.Refresh();
            EditorSceneManager.SaveScene(scene, k_WidgetsQuickstartScenePath);
        }

#if (NETCODE_FOR_ENTITIES_INSTALLED || NETCODE_FOR_GAMEOBJECTS_INSTALLED) && COMBINED_NETCODE_QUICKSTART_AVAILABLE
        [SerializeReference]
        DynamicSampleImporter m_Importer;

        void ImportAndSetupWidgetsInNetcodeQuickstart()
        {
            if(m_Importer == null)
                m_Importer = ScriptableObject.CreateInstance<DynamicSampleImporter>();
            m_Importer.OnImported += OnImportFinished;

#if NETCODE_FOR_GAMEOBJECTS_INSTALLED
            m_Importer.Import(new WidgetsNetcodeForGameobjectsSetup(m_MultiplayerCenterPlayerCount, m_ConnectionType));
#else
            m_Importer.Import(new WidgetsNetcodeForEntitiesSetup(m_MultiplayerCenterPlayerCount, m_ConnectionType));
#endif
        }

        void OnImportFinished(bool success)
        {
            m_Importer.OnImported -= OnImportFinished;
            Object.DestroyImmediate(m_Importer);
        }

        void AddNetcodeQuickstartUI()
        {
            var isNetCodeQuickstartScene = SceneManager.GetActiveScene().name == NetcodeQuickstartSceneName;
            m_NetcodeDescription = new Label(isNetCodeQuickstartScene ? k_NetcodeSampleDescriptionNetcodeSceneOpen : k_NetcodeSampleDescriptionNetcodeSceneNotOpen);
            m_NetcodeDescription.AddToClassList(StyleConstants.OnBoardingShortDescription);
            Root.Add(m_NetcodeDescription);

            var netcodeButtonContainer = new VisualElement();
            Root.Add(netcodeButtonContainer);

            m_NetcodeButton = new Button(ImportAndSetupWidgetsInNetcodeQuickstart) { text = k_AddWidgetsToNetcodeSampleButtonText };
            m_NetcodeButton.AddToClassList(StyleConstants.OnBoardingSectionMainButton);
            m_NetcodeButton.SetEnabled(isNetCodeQuickstartScene);
            netcodeButtonContainer.Add(m_NetcodeButton);
        }

#endif

        static IEnumerable<(string, string)> Links => new[]
        {
            ("Documentation", k_DocumentationLink)
        };

        public void Load()
        {
            Root ??= new VisualElement();

            Root.style.display = DisplayStyle.Flex;
            Root.AddToClassList("onboarding-section");

            var titleContainer = new VisualElement();
            titleContainer.AddToClassList("horizontal-container");
            var title = new Label(k_Title);
            title.AddToClassList("onboarding-section-title");
            titleContainer.Add(title);
            Root.Add(titleContainer);

            if(!string.IsNullOrEmpty(k_ShortDescription))
            {
                var text = new Label(k_ShortDescription);
                text.AddToClassList(StyleConstants.OnBoardingShortDescription);
                Root.Add(text);
            }

            var buttonContainer = new VisualElement();
            Root.Add(buttonContainer);
            if(!string.IsNullOrEmpty(k_ButtonLabel) && OnButtonClicked != null)
            {
                m_Button = new Button(OnButtonClicked) { text = k_ButtonLabel };
                m_Button.AddToClassList(StyleConstants.OnBoardingSectionMainButton);
                buttonContainer.Add(m_Button);
            }

#if(NETCODE_FOR_GAMEOBJECTS_INSTALLED || NETCODE_FOR_ENTITIES_INSTALLED) && COMBINED_NETCODE_QUICKSTART_AVAILABLE
            AddNetcodeQuickstartUI();
            // due to this function having the possibility to being called several times we always unregister before we register
            UnregisterSceneEvents();
            RegisterSceneEvents();
#endif
            AddResourcesLinks();
        }

        void AddResourcesLinks()
        {
            var links = new List<(string, string)>(Links);
            if(links.Count == 0)
                return;

            Root.Add(new Label("Resources"));

            var linksDisplay = new VisualElement();
            linksDisplay.AddToClassList("horizontal-container");
            linksDisplay.AddToClassList("flex-wrap");
            for (var index = 0; index < links.Count; index++)
            {
                var (label, link) = links[index];
                var docButton = new Button() { text = label };
                docButton.AddToClassList("doc-button");
                docButton.clicked += () => OpenResourceLink(link, label);
                linksDisplay.Add(docButton);

                if(index < links.Count - 1)
                    linksDisplay.Add(new Label("|") { name = "doc-button-separator" });
            }

            Root.Add(linksDisplay);
        }

        void OpenResourceLink(string link, string label)
        {
#if MULTIPLAYER_CENTER_ANALYTICS_AVAILABLE
            AnalyticsProvider.SendInteractionEvent(InteractionDataType.Link, label);
#endif
            Application.OpenURL(link);
        }

        public void Unload()
        {
            if(m_Button != null)
                m_Button.clicked -= OnButtonClicked;

#if (NETCODE_FOR_GAMEOBJECTS_INSTALLED || NETCODE_FOR_ENTITIES_INSTALLED) && COMBINED_NETCODE_QUICKSTART_AVAILABLE
            if(m_NetcodeButton != null)
            {
                m_NetcodeButton.clicked -= ImportAndSetupWidgetsInNetcodeQuickstart;
            }
#endif
            UnregisterSceneEvents();
        }

        void UnregisterSceneEvents()
        {
            EditorSceneManager.sceneOpened -= EditorSceneManagerOnSceneOpened;
            SceneManager.sceneLoaded -= SceneManagerOnSceneLoaded;
            EditorSceneManager.newSceneCreated -= EditorSceneManagerNewSceneCreated;
            EditorSceneManager.sceneSaved -= EditorSceneManagerOnSceneSaved;
        }

        void RegisterSceneEvents()
        {
            EditorSceneManager.sceneOpened += EditorSceneManagerOnSceneOpened;
            SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
            EditorSceneManager.newSceneCreated += EditorSceneManagerNewSceneCreated;
            EditorSceneManager.sceneSaved += EditorSceneManagerOnSceneSaved;
        }

        void EditorSceneManagerOnSceneSaved(Scene scene)
        {
            if(SceneManager.GetActiveScene() == scene)
                UpdateNetcodeButton(scene);
        }

        void EditorSceneManagerNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            if(mode == NewSceneMode.Additive)
                return;

            UpdateNetcodeButton(scene);
        }

        void SceneManagerOnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if(mode == LoadSceneMode.Additive)
                return;

            UpdateNetcodeButton(scene);
        }

        void EditorSceneManagerOnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if(mode == OpenSceneMode.Additive)
                return;

            UpdateNetcodeButton(scene);
        }

        void UpdateNetcodeButton(Scene scene)
        {
            if(m_NetcodeButton == null || m_NetcodeDescription == null)
                return;
            m_NetcodeButton.SetEnabled(scene.name == NetcodeQuickstartSceneName);
            m_NetcodeDescription.text = scene.name == NetcodeQuickstartSceneName ? k_NetcodeSampleDescriptionNetcodeSceneOpen : k_NetcodeSampleDescriptionNetcodeSceneNotOpen;
        }

        public void HandleAnswerData(AnswerData answerData)
        {
            foreach (var answeredQuestion in answerData.Answers)
            {
                if(answeredQuestion.QuestionId == "PlayerCount")
                {
                    var playerCount = Mathf.Clamp(int.Parse(answeredQuestion.Answers[0].TrimEnd('+')), 0, 100);
                    m_MultiplayerCenterPlayerCount = playerCount;
                }
            }
        }

        public void HandleUserSelectionData(SelectedSolutionsData selectedSolutionsData)
        {
            if(selectedSolutionsData == null)
                return;
            
            switch (selectedSolutionsData.SelectedNetcodeSolution)
            {
                case SelectedSolutionsData.NetcodeSolution.NGO:
#if NGO_2_AVAILABLE
                    if(selectedSolutionsData.SelectedHostingModel == SelectedSolutionsData.HostingModel.DistributedAuthority)
                    {
                        m_ConnectionType = ConnectionType.DistributedAuthority;
                        break;
                    }
#endif

                    // If dedicated server select direct connection
                    if(SetDirectConnectionWhenDedicatedServer(selectedSolutionsData))
                        break;

                    // in all other cases Relay is the default
                    m_ConnectionType = ConnectionType.Relay;
                    break;
                case SelectedSolutionsData.NetcodeSolution.N4E:
                    // If dedicated server select direct connection
                    if(SetDirectConnectionWhenDedicatedServer(selectedSolutionsData))
                        break;

                    m_ConnectionType = ConnectionType.Relay;
                    break;
                case SelectedSolutionsData.NetcodeSolution.CustomNetcode:
                case SelectedSolutionsData.NetcodeSolution.NoNetcode:
                    m_ConnectionType = ConnectionType.None;
                    break;
                case SelectedSolutionsData.NetcodeSolution.None:
                default:
                    m_ConnectionType = ConnectionType.Relay;
                    break;
            }
        }

        bool SetDirectConnectionWhenDedicatedServer(SelectedSolutionsData selectedSolutionsData)
        {
            if(selectedSolutionsData.SelectedHostingModel == SelectedSolutionsData.HostingModel.DedicatedServer)
            {
                m_ConnectionType = ConnectionType.Direct;
                return true;
            }

            return false;
        }
    }
}
#endif
