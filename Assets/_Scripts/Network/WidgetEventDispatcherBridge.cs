using System;
using System.Collections;
using System.Reflection;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.Events;

public class WidgetEventDispatcherBridge : MonoBehaviour
{
    // Session Events
    [Header("Session Events")]
    public UnityEvent<ISession> OnSessionChanged = new UnityEvent<ISession>();
    public UnityEvent<SessionState> OnSessionStateChanged = new UnityEvent<SessionState>();
    public UnityEvent<string> OnPlayerJoined = new UnityEvent<string>();
    public UnityEvent<string> OnPlayerLeft = new UnityEvent<string>();
    public UnityEvent OnRemovedFromSession = new UnityEvent();
    public UnityEvent OnSessionDeleted = new UnityEvent();
    
    // Service Events
    [Header("Service Events")]
    public UnityEvent OnServicesInitialized = new UnityEvent();
    
    // The name of the event dispatcher class in the Multiplayer Widgets package
    private const string DispatcherClassName = "Unity.Multiplayer.Widgets.WidgetEventDispatcher";
    
    private object widgetEventDispatcher;
    private Type widgetEventDispatcherType;
    private bool initialized = false;
    
    private void Awake()
    {
        StartCoroutine(InitializeWhenReady());
    }
    
    private IEnumerator InitializeWhenReady()
    {
        // Wait to ensure all packages are loaded
        yield return new WaitForSeconds(0.5f);
        FindWidgetEventDispatcher();
        
        if (widgetEventDispatcher != null)
        {
            ConnectToEvents();
            initialized = true;
            Debug.Log("WidgetEventDispatcherBridge initialized successfully");
        }
        else
        {
            Debug.LogWarning("Failed to find WidgetEventDispatcher, will retry");
            yield return new WaitForSeconds(1f);
            StartCoroutine(InitializeWhenReady());
        }
    }
    
    private void FindWidgetEventDispatcher()
    {
        // Find the WidgetEventDispatcher type using reflection
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                widgetEventDispatcherType = assembly.GetType(DispatcherClassName);
                if (widgetEventDispatcherType != null)
                {
                    // Get the Instance property
                    PropertyInfo instanceProperty = widgetEventDispatcherType.GetProperty(
                        "Instance", BindingFlags.Public | BindingFlags.Static);
                    
                    if (instanceProperty != null)
                    {
                        widgetEventDispatcher = instanceProperty.GetValue(null);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in FindWidgetEventDispatcher: {ex.Message}");
            }
        }
    }
    
    private void ConnectToEvents()
    {
        try
        {
            // Connect to OnSessionChanged event
            ConnectToEvent("OnSessionChangedEvent", "OnSessionChangedCallback");
            
            // Connect to OnSessionStateChanged event
            ConnectToEvent("OnSessionStateChangedEvent", "OnSessionStateChangedCallback");
            
            // Connect to OnPlayerJoinedSession event
            ConnectToEvent("OnPlayerJoinedSessionEvent", "OnPlayerJoinedCallback");
            
            // Connect to OnPlayerLeftSession event
            ConnectToEvent("OnPlayerLeftSessionEvent", "OnPlayerLeftCallback");
            
            // Connect to OnRemovedFromSession event
            ConnectToEvent("OnRemovedFromSessionEvent", "OnRemovedFromSessionCallback");
            
            // Connect to OnSessionDeleted event
            ConnectToEvent("OnSessionDeletedEvent", "OnSessionDeletedCallback");
            
            // Connect to OnServicesInitialized event
            ConnectToEvent("OnServicesInitializedEvent", "OnServicesInitializedCallback");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error connecting to events: {ex.Message}");
        }
    }
    
    private void ConnectToEvent(string eventFieldName, string callbackMethodName)
    {
        var eventField = widgetEventDispatcherType.GetField(
            eventFieldName, BindingFlags.Instance | BindingFlags.Public);
            
        if (eventField == null)
        {
            Debug.LogWarning($"Event field {eventFieldName} not found");
            return;
        }
        
        var eventObj = eventField.GetValue(widgetEventDispatcher);
        var methodInfo = GetType().GetMethod(
            callbackMethodName, BindingFlags.NonPublic | BindingFlags.Instance);
            
        if (methodInfo == null)
        {
            Debug.LogWarning($"Callback method {callbackMethodName} not found");
            return;
        }
        
        var eventType = eventField.FieldType;
        
        if (eventType.IsGenericType)
        {
            var argumentTypes = eventType.GetGenericArguments();
            Type delegateType;
            
            if (argumentTypes.Length == 1)
            {
                delegateType = typeof(UnityAction<>).MakeGenericType(argumentTypes[0]);
            }
            else
            {
                delegateType = typeof(UnityAction);
            }
            
            var delegateInstance = Delegate.CreateDelegate(delegateType, this, methodInfo);
            var addListenerMethod = eventType.GetMethod("AddListener");
            addListenerMethod.Invoke(eventObj, new[] { delegateInstance });
        }
        else
        {
            var addListenerMethod = eventType.GetMethod("AddListener");
            var delegateInstance = Delegate.CreateDelegate(typeof(UnityAction), this, methodInfo);
            addListenerMethod.Invoke(eventObj, new[] { delegateInstance });
        }
    }
    
    // Callback methods
    private void OnSessionChangedCallback(ISession session)
    {
        OnSessionChanged.Invoke(session);
    }
    
    private void OnSessionStateChangedCallback(SessionState state)
    {
        OnSessionStateChanged.Invoke(state);
    }
    
    private void OnPlayerJoinedCallback(string playerId)
    {
        OnPlayerJoined.Invoke(playerId);
    }
    
    private void OnPlayerLeftCallback(string playerId)
    {
        OnPlayerLeft.Invoke(playerId);
    }
    
    private void OnRemovedFromSessionCallback()
    {
        OnRemovedFromSession.Invoke();
    }
    
    private void OnSessionDeletedCallback()
    {
        OnSessionDeleted.Invoke();
    }
    
    private void OnServicesInitializedCallback()
    {
        OnServicesInitialized.Invoke();
    }
    
    private void OnDestroy()
    {
        // Clean up can be added here if needed
        // For events this isn't strictly necessary as the dispatcher will be destroyed
    }
}