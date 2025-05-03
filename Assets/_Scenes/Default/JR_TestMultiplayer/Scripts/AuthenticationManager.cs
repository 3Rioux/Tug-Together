using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class AuthenticationManager : MonoBehaviour
{
    public static AuthenticationManager Instance;

    public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
    [SerializeField] private bool isSignedIn;

    private async void Awake()
    {
       
        // Singleton pattern (optional)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await InitializeServices();
    }

    private async Task InitializeServices()
    {
        try
        {
            // Initialize Unity Services
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Signed in as: {AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Authentication error: {ex.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Service initialization failed: {ex.Message}");
        }
    }

    private void Update()
    {
        isSignedIn = IsSignedIn;
    }
}
