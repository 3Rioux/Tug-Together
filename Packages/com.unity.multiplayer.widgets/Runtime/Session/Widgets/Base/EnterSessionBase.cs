// csharp
using System;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Unity.Multiplayer.Widgets
{
    internal class EnterSessionBase : WidgetBehaviour, ISessionLifecycleEvents, ISessionProvider
    {
        [Header("Widget Configuration")]
        public WidgetConfiguration WidgetConfiguration;
        
        [Header("Join Session Events")]
        public UnityEvent JoiningSession = new();
        public UnityEvent<ISession> JoinedSession = new();
        public UnityEvent<SessionException> FailedToJoinSession = new();

        [SerializeField, HideInInspector]
        protected Button m_EnterSessionButton;
        
        protected bool _isJoining;
        
        public ISession Session { get; set; }
        
        protected virtual void Awake()
        {
            m_EnterSessionButton = GetComponentInChildren<Button>();
            if (m_EnterSessionButton != null)
            {
                m_EnterSessionButton.onClick.AddListener(EnterSession);
                //m_EnterSessionButton.interactable = false;
                m_EnterSessionButton.interactable = true;

            }
        }

        public override void OnServicesInitialized()
        {
            if (m_EnterSessionButton != null)
                m_EnterSessionButton.interactable = true;
        }

        protected virtual void OnDestroy()
        {
            if (m_EnterSessionButton != null)
                m_EnterSessionButton.onClick.RemoveListener(EnterSession);
        }

        public void OnSessionJoining()
        {
            JoiningSession?.Invoke();
            if (m_EnterSessionButton != null)
                //m_EnterSessionButton.interactable = false;
                m_EnterSessionButton.interactable = true;
        }

        public void OnSessionFailedToJoin(SessionException sessionException)
        {
            FailedToJoinSession?.Invoke(sessionException);
            if (m_EnterSessionButton != null)
                m_EnterSessionButton.interactable = true;
        }

        public void OnSessionJoined()
        {
            JoinedSession?.Invoke(Session);
            if (m_EnterSessionButton != null)
                //m_EnterSessionButton.interactable = Session == null;
                m_EnterSessionButton.interactable = true;
            
        }

        public void OnSessionLeft()
        {
            if (m_EnterSessionButton != null)
                m_EnterSessionButton.interactable = true;
        }
        
        protected virtual EnterSessionData GetSessionData()
        {
            return new EnterSessionData { SessionAction = SessionAction.Invalid };
        }
        
        // csharp
        protected async void EnterSession()
        {
            // Prevent join if already in progress or joined.
            if (_isJoining || Session != null)
                return;
    
            _isJoining = true;
    
            if (m_EnterSessionButton != null)
                //m_EnterSessionButton.interactable = false;
                m_EnterSessionButton.interactable = true;
                
    
            await SessionManager.Instance.EnterSession(GetSessionData());

            // Reset _isJoining if the session was not established.
            _isJoining = false;
    
            if (m_EnterSessionButton != null)
                //m_EnterSessionButton.interactable = Session == null;
                m_EnterSessionButton.interactable = true;

        }
        
        // protected async void EnterSession()
        // {
        //     await SessionManager.Instance.EnterSession(GetSessionData());
        // }
    }
}