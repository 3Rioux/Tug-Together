using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Unity.Multiplayer.Widgets
{
    internal class SessionList : EnterSessionBase
    {
        [SerializeField]
        GameObject m_SessionItemPrefab;

        [SerializeField]
        GameObject m_ContentParent;
        
        IList<GameObject> m_ListItems = new List<GameObject>();
        IList<ISessionInfo> m_Sessions;
        ISessionInfo m_SelectedSessionInfo;

        public override void OnServicesInitialized()
        {
            RefreshSessionList();
        }

        internal async void RefreshSessionList()
        {
            await UpdateSessions();
            
            foreach (var listItem in m_ListItems)
            {
                Destroy(listItem);
            }
            
            if (m_Sessions == null)
                return;
            
            foreach (var sessionInfo in m_Sessions)
            {
                var itemPrefab = Instantiate(m_SessionItemPrefab, m_ContentParent.transform);
                if (itemPrefab.TryGetComponent<SessionItem>(out var sessionItem))
                {
                    sessionItem.SetSession(sessionInfo);
                    sessionItem.OnSessionSelected.AddListener(SelectSession);
                }
                m_ListItems.Add(itemPrefab);
            }
        }

        void SelectSession(ISessionInfo sessionInfo)
        {
            m_SelectedSessionInfo = sessionInfo;
            if (Session == null)
                m_EnterSessionButton.interactable = true;
        }

        async Task UpdateSessions()
        {
            m_Sessions = await SessionManager.Instance.QuerySessions();
        }

        protected override EnterSessionData GetSessionData()
        {
            return new EnterSessionData
            {
                SessionAction = SessionAction.JoinById,
                Id = m_SelectedSessionInfo.Id,
                WidgetConfiguration = WidgetConfiguration,
            };
        }
    }
}
