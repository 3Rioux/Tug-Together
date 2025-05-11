// using Unity.Services.Multiplayer;
// using Unity.Multiplayer.Widgets;
// using UnityEngine;
//
// public class ReadySystem : MonoBehaviour
// {
//     // Reference to the active session
//     private ISession currentSession;
//     
//     // Start listening for events
//     private void OnEnable()
//     {
//         // Subscribe to the session joined event
//         var eventDispatcher = WidgetEventDispatcher.Instance;
//         eventDispatcher.OnSessionJoinedEvent.AddListener(OnSessionJoined);
//         eventDispatcher.OnSessionLeftEvent.AddListener(OnSessionLeft);
//     }
//     
//     private void OnDisable()
//     {
//         // Unsubscribe from events
//         var eventDispatcher = WidgetEventDispatcher.Instance;
//         eventDispatcher.OnSessionJoinedEvent.RemoveListener(OnSessionJoined);
//         eventDispatcher.OnSessionLeftEvent.RemoveListener(OnSessionLeft);
//     }
//     
//     private void OnSessionJoined(ISession session, WidgetConfiguration config)
//     {
//         currentSession = session;
//         // Subscribe to session events
//         currentSession.PlayerJoined += OnPlayerJoined;
//         // Other event handling...
//     }
//     
//     private void OnSessionLeft()
//     {
//         if (currentSession != null)
//         {
//             currentSession.PlayerJoined -= OnPlayerJoined;
//             // Unsubscribe from other events...
//             currentSession = null;
//         }
//     }
//     
//     private void OnPlayerJoined(string playerId)
//     {
//         Debug.Log($"Player joined: {playerId}");
//         // Handle player joined
//     }
// }