using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

/// <summary>
/// Script to spawn the players After the level is loaded 
/// </summary>
public class SpawnPlayerManager : MonoBehaviour
{
    // Define your custom spawn positions
    public List<Transform> spawnPoints;
    public WaterSurface levelWaterSurface;

    private void Awake()
    {
        // Persist between scene loads
        DontDestroyOnLoad(gameObject);

       
    }

    private void Start()
    {
        // Subscribe to scene events
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnEnable()
    {
        // Subscribe to scene events
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Track if we've already spawned players
    private bool playersSpawned = false;

    private void OnClientConnected(ulong clientId)
    {
        // Optional: Log or prepare something for each client
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Wait for network scene load and spawn players
        if (NetworkManager.Singleton.IsServer && scene.name != "MainMenu" && !playersSpawned)
        {
            SpawnAllPlayers();
            playersSpawned = true;
        }
    }


    private void SpawnAllPlayers()
    {
        int spawnIndex = 0;

        foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = clientPair.Key;
            GameObject playerObject = clientPair.Value.PlayerObject.gameObject;

            if (spawnIndex >= spawnPoints.Count)
            {
                Debug.LogWarning("Not enough spawn points!");
                break;
            }

            playerObject.transform.position = spawnPoints[spawnIndex].position;
             Debug.Log("Found M_DisableComponentsOnNonOwner: " + playerObject.TryGetComponent<M_DisableComponentsOnNonOwner>(out M_DisableComponentsOnNonOwner setComponentes).ToString());

            setComponentes.EnableCameras();

            // If targetSurface is not set, find the Ocean game object and get its WaterSurface component

            GameObject ocean = GameObject.Find("Ocean");

            Debug.Log("Found TugboatMovementWFloat: " + playerObject.TryGetComponent<TugboatMovementWFloat>(out TugboatMovementWFloat boatMovement).ToString());

            if (boatMovement != null)
            {
                boatMovement.targetSurface = levelWaterSurface;
                if (boatMovement.targetSurface == null)
                    Debug.LogError("WaterSurface component not found on object Ocean", this);
            }
            else
            {
                Debug.LogError("Boat Movement Script object not found", this);
            }

            spawnIndex++;
        }
    }



}
