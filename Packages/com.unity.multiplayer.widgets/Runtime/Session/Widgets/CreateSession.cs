// csharp
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Multiplayer.Widgets
{
    internal class CreateSession : EnterSessionBase
    {
        // Call this method from your UI button OnClick event
        public void OnCreateSessionButtonClicked()
        {
            EnterSession();
        }

        protected override EnterSessionData GetSessionData()
        {
            return new EnterSessionData
            {
                SessionAction = SessionAction.Create,
                SessionName = "Session_" + Random.Range(1000, 9999),
                WidgetConfiguration = WidgetConfiguration,
            };
        }
    }
}

// using TMPro;
// using UnityEngine;
//
// namespace Unity.Multiplayer.Widgets
// {
//     internal class CreateSession : EnterSessionBase
//     {
//         TMP_InputField m_InputField;
//         
//         protected override void OnEnable()
//         {
//             m_InputField = GetComponentInChildren<TMP_InputField>();
//             base.OnEnable();
//         }
//
//         public override void OnServicesInitialized()
//         {
//             m_InputField.onEndEdit.AddListener(value =>
//             {
//                 if (Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrEmpty(value))
//                 {
//                     EnterSession();
//                 }
//             });
//             m_InputField.onValueChanged.AddListener(value =>
//             {
//                 m_EnterSessionButton.interactable = !string.IsNullOrEmpty(value) && Session == null;
//             });
//         }
//
//         protected override EnterSessionData GetSessionData()
//         {
//             return new EnterSessionData
//             {
//                 SessionAction = SessionAction.Create,
//                 SessionName = m_InputField.text,
//                 WidgetConfiguration = WidgetConfiguration,
//             };
//         }
//     }
// }
