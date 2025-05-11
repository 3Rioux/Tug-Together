namespace Unity.Multiplayer.Widgets
{
    /// <summary>
    /// Base interface for all widgets
    /// </summary>
    internal interface IWidget
    {
        /// <summary>
        /// Services are initialized.
        /// </summary>
        bool IsInitialized { get; set; }
        
        /// <summary>
        /// Called when services are initialized.
        /// </summary>
        void OnServicesInitialized();
    }
}
