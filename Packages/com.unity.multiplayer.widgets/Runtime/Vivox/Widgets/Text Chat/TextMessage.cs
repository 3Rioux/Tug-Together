using TMPro;
using UnityEngine;

namespace Unity.Multiplayer.Widgets
{
    internal class TextMessage : MonoBehaviour
    {
        [SerializeField]
        TMP_Text m_Text;
        
        public void Init(IChatMessage message, string displayName)
        {
            m_Text.text = $"[{displayName}]: {message.Text}";
            m_Text.alignment = message.FromSelf ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
        }
    }    
}