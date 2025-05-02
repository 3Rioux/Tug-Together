using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unity.Multiplayer.Widgets
{
    internal class ShowJoinCode : WidgetBehaviour, ISessionLifecycleEvents, ISessionProvider
    {
        const string k_NoCode = "<wave amp=4 cw=.3 tw=.3><grow>...";

        public ISession Session { get; set; }
        [SerializeField] TMP_Text m_Text;
        [SerializeField] Button m_CopyCodeButton;

        void Start()
        {
            if (m_Text == null)
                m_Text = GetComponentInChildren<TMP_Text>();

            m_CopyCodeButton.onClick.AddListener(CopySessionCodeToClipboard);
            m_CopyCodeButton.gameObject.SetActive(false);
        }

        public void OnSessionJoined()
        {
            m_Text.text = Session?.Code != null ? "<size=30><+fade>" + Session.Code : k_NoCode;
            m_CopyCodeButton.gameObject.SetActive(true);

            CanvasGroup canvasGroup = m_CopyCodeButton.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = m_CopyCodeButton.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            StartCoroutine(WaitAndFade(canvasGroup, .5f, 1f, 1f)); // Wait 1 second then fade in over 1 second.
        }

        public void OnSessionLeft()
        {
            m_Text.text = k_NoCode;
            m_CopyCodeButton.gameObject.SetActive(false);
        }

        void CopySessionCodeToClipboard()
        {
            EventSystem.current.SetSelectedGameObject(null);

            var code = m_Text.text;
            if (Session?.Code == null || string.IsNullOrEmpty(code))
            {
                return;
            }

            string plainText = Regex.Replace(m_Text.text, "<.*?>", string.Empty);
            GUIUtility.systemCopyBuffer = plainText;
        }

        private IEnumerator WaitAndFade(CanvasGroup target, float delay, float endAlpha, float duration)
        {
            yield return new WaitForSeconds(delay);
            float startAlpha = target.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                yield return null;
            }
            target.alpha = endAlpha;
            m_CopyCodeButton.interactable = true;
        }
    }
}