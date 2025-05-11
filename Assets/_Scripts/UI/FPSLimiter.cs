using UnityEngine;

namespace _Scripts.Managers.UI
{
    /// <summary>
    /// script to limit the max fps of the game
    /// </summary>
    public class FPSLimiter : MonoBehaviour
    {
        // The target frames per second (FPS) for the application
        [SerializeField] private int _targetFPS = 240;

        // Awake is called when the script instance is being loaded
        private void Awake()
        {
            // Disable vertical sync to allow manual control of the frame rate
            QualitySettings.vSyncCount = 0;

            // Set the application's target frame rate to the specified value
            Application.targetFrameRate = _targetFPS;
        }
    }
}