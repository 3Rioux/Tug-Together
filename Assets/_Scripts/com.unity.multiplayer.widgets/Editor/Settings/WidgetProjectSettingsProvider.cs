using System;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Widgets.Editor
{
    static class WidgetProjectSettingsProvider
    {
        const string k_WidgetSettingsDirectory = "Assets/Multiplayer Widgets/Resources/";
        static string s_WidgetSettingsPath = $"{k_WidgetSettingsDirectory}{nameof(MultiplayerWidgetsSettings)}.asset";
        
        [SettingsProvider]
        public static SettingsProvider CreateCustomSettingsProvider()
        {
            var provider = new SettingsProvider("Project/Multiplayer/Widgets", SettingsScope.Project)
            {
                label = "Widgets",
                activateHandler = (searchContext, rootElement) =>
                {
                    var serializedObject = new SerializedObject(GetOrCreateSettings());
                    
                    var title = new Label
                    {
                        text = "Multiplayer Widgets Settings",
                        style =
                        {
                            fontSize = 19,
                            unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold),
                            paddingBottom = 8
                        }
                    };
                    rootElement.Add(title);

                    var properties = new VisualElement();
                    var propertyField = new PropertyField(serializedObject.FindProperty("m_UseCustomServiceInitialization"))
                    {
                        tooltip = MultiplayerWidgetsSettings.k_CustomserviceInitializationTooltip
                    };
                    properties.Add(propertyField);
                    rootElement.Add(properties);

                    rootElement.style.paddingLeft = 9;
                    rootElement.style.paddingTop = 9;
                    rootElement.Bind(serializedObject);
                }
            };

            return provider;
        }
        
        static MultiplayerWidgetsSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MultiplayerWidgetsSettings>(s_WidgetSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<MultiplayerWidgetsSettings>();
                settings.UseCustomServiceInitialization = false;
                Directory.CreateDirectory(k_WidgetSettingsDirectory);
                AssetDatabase.CreateAsset(settings, s_WidgetSettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }
    }    
}