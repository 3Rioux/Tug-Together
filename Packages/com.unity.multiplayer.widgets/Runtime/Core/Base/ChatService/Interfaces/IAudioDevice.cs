using System.Threading.Tasks;

namespace Unity.Multiplayer.Widgets
{
    internal interface IAudioDevice
    {
        /// <summary>
        /// The name of the device
        /// </summary>
        string DeviceName { get ; }
        
        /// <summary>
        /// The ID of the device
        /// </summary>
        string DeviceID { get; }

        ///<summary>
        /// Set this Input Device to be the active Device
        ///</summary>
        /// <returns> A task for the operation </returns>
        Task SetActiveDeviceAsync();
    }
}
