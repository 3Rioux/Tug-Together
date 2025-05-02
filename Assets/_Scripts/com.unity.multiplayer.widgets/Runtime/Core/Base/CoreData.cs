using System;
using Unity.Services.Multiplayer;

namespace Unity.Multiplayer.Widgets
{
    /// <summary>
    /// The different types of SessionActions.
    /// </summary>
    internal enum SessionAction
    {
        Invalid,
        Create,
        StartMatchmaking,
        QuickJoin,
        JoinByCode,
        JoinById
    }
    
    /// <summary>
    /// Data to enter a session.
    /// </summary>
    internal struct EnterSessionData
    {
        public SessionAction SessionAction;
        public string SessionName;
        public string JoinCode;
        public string Id;
        public WidgetConfiguration WidgetConfiguration;
        public AdditionalOptions AdditionalOptions;
    }

    /// <summary>
    /// Additional data to enter specific session types.
    /// </summary>
    internal struct AdditionalOptions
    {
        public MatchmakerOptions MatchmakerOptions;
        public bool AutoCreateSession;
    }
}


