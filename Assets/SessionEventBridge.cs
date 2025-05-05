using UnityEngine;
using UnityEngine.Events;
using System;
using System.Linq;
using System.Reflection;

public class SessionEventBridge : MonoBehaviour
{
    public UnityEvent OnJoiningSession = new UnityEvent();
    public UnityEvent<Unity.Services.Multiplayer.ISession> OnJoinedSession = new UnityEvent<Unity.Services.Multiplayer.ISession>();
    public UnityEvent<Unity.Services.Multiplayer.SessionException> OnFailedToJoinSession = new UnityEvent<Unity.Services.Multiplayer.SessionException>();

    [SerializeField] private GameObject sessionWidgetObject;

    void Start()
    {
        // Find CreateSession component if not assigned
        if (sessionWidgetObject == null)
        {
            // First check parent
            var sessionComponent = GetComponentInParent<Transform>().GetComponentInChildren(
                Type.GetType("Unity.Multiplayer.Widgets.CreateSession, Unity.Multiplayer.Widgets"));
                
            if (sessionComponent != null)
            {
                sessionWidgetObject = sessionComponent.gameObject;
            }
            else
            {
                // Try to find it in the scene
                var foundComponents = FindObjectsOfType<Component>().Where(
                    c => c != null && c.GetType().FullName == "Unity.Multiplayer.Widgets.CreateSession");
                    
                if (foundComponents.Any())
                {
                    sessionWidgetObject = foundComponents.First().gameObject;
                }
            }
            
            if (sessionWidgetObject == null)
            {
                Debug.LogError("Could not find a CreateSession component", this);
                return;
            }
        }

        // Now find the EnterSessionBase component
        Component enterSessionComponent = null;
        foreach (var component in sessionWidgetObject.GetComponents<Component>())
        {
            if (component == null) continue;
            
            Type type = component.GetType();
            if (type == null) continue;
            
            // Look for base class of type EnterSessionBase
            Type baseType = type;
            while (baseType != null)
            {
                if (baseType.FullName == "Unity.Multiplayer.Widgets.EnterSessionBase")
                {
                    enterSessionComponent = component;
                    break;
                }
                baseType = baseType.BaseType;
            }
            
            if (enterSessionComponent != null) break;
        }
        
        if (enterSessionComponent == null)
        {
            Debug.LogError("Could not find an EnterSessionBase component", this);
            return;
        }
        
        // Connect to the events
        ConnectToEvents(enterSessionComponent);
    }
    
    private void ConnectToEvents(Component sessionComponent)
    {
        try
        {
            Type type = sessionComponent.GetType();
            
            // Connect to JoiningSession event
            var joiningField = type.GetField("JoiningSession", 
                BindingFlags.Public | BindingFlags.Instance);
            if (joiningField != null)
            {
                var joiningEvent = joiningField.GetValue(sessionComponent) as UnityEvent;
                if (joiningEvent != null)
                {
                    joiningEvent.AddListener(() => {
                        Debug.Log("Bridge: Join session starting");
                        OnJoiningSession.Invoke();
                    });
                }
            }
            
            // Connect to JoinedSession event
            var joinedField = type.GetField("JoinedSession",
                BindingFlags.Public | BindingFlags.Instance);
            if (joinedField != null)
            {
                var eventType = joinedField.FieldType;
                if (eventType.IsGenericType)
                {
                    var methodInfo = GetType().GetMethod("OnJoinedSessionInternal", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var delegateType = typeof(UnityAction<>).MakeGenericType(
                        eventType.GetGenericArguments()[0]);
                    var delegateInstance = Delegate.CreateDelegate(delegateType, this, methodInfo);
                    
                    var addListenerMethod = eventType.GetMethod("AddListener");
                    addListenerMethod.Invoke(joinedField.GetValue(sessionComponent), new[] { delegateInstance });
                }
            }
            
            // Connect to FailedToJoinSession event
            var failedField = type.GetField("FailedToJoinSession",
                BindingFlags.Public | BindingFlags.Instance);
            if (failedField != null)
            {
                var eventType = failedField.FieldType;
                if (eventType.IsGenericType)
                {
                    var methodInfo = GetType().GetMethod("OnFailedSessionInternal",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var delegateType = typeof(UnityAction<>).MakeGenericType(
                        eventType.GetGenericArguments()[0]);
                    var delegateInstance = Delegate.CreateDelegate(delegateType, this, methodInfo);
                    
                    var addListenerMethod = eventType.GetMethod("AddListener");
                    addListenerMethod.Invoke(failedField.GetValue(sessionComponent), new[] { delegateInstance });
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error connecting to events: {ex.Message}", this);
        }
    }
    
    // Methods that will be called via reflection
    private void OnJoinedSessionInternal(Unity.Services.Multiplayer.ISession session)
    {
        Debug.Log("Bridge: Session joined successfully");
        OnJoinedSession.Invoke(session);
    }
    
    private void OnFailedSessionInternal(Unity.Services.Multiplayer.SessionException exception)
    {
        Debug.Log($"Bridge: Failed to join session: {exception.Message}");
        OnFailedToJoinSession.Invoke(exception);
    }
}