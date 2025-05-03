using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;


/// <summary>
/// Script to spawn the players After the level is loaded 
/// </summary>
public class SpawnPlayerManager : NetworkBehaviour
{
    public GameObject playerPrefab; // Your player prefab

    private void Start()
    {

            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoadComplete;
       
    }

    private void OnEnable()
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoadComplete;
    }

    private void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        Debug.Log(" OnSceneLoadComplete");

        if (sceneName == "JR_MainMenu") return;


            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;

        //// Only spawn if not already spawned
        //if (!NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject)
        //{
        //    GameObject playerInstance = Instantiate(playerPrefab, GetSpawnPosition(), Quaternion.identity);
        //    playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        //}
        if (client.PlayerObject == null)
        {
            Vector3 spawnPos = GetSpawnPosition();
            GameObject playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }


    }

    private Vector3 GetSpawnPosition()
    {
        // Customize your spawn position logic here
        return new Vector3(Random.Range(-3, 3), 1, Random.Range(-3, 3));
    }

    private void OnDestroy()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
        }
    }


    #region OLDTry
    //[SerializeField] private GameObject playerPrefab; // Your player prefab

    //// Define your custom spawn positions
    //public List<Transform> spawnPoints;
    //public WaterSurface levelWaterSurface;

    //private void Awake()
    //{
    //    // Persist between scene loads
    //    DontDestroyOnLoad(gameObject);


    //}

    //private void Start()
    //{
    //    // Subscribe to scene events
    //    NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    //    SceneManager.sceneLoaded += OnSceneLoaded;
    //    // Subscribe to Netcode scene events
    //    NetworkManager.Singleton.SceneManager.OnSceneEvent += OnNetworkSceneEvent;
    //}

    //private void OnEnable()
    //{
    //    // Subscribe to scene events
    //    NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    //    SceneManager.sceneLoaded  += OnSceneLoaded;

    //}

    //private void OnDisable()
    //{
    //    NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    //    SceneManager.sceneLoaded -= OnSceneLoaded;
    //    if (NetworkManager.Singleton != null)
    //        NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnNetworkSceneEvent;
    //}

    //// Track if we've already spawned players
    //private bool playersSpawned = false;

    //private void OnClientConnected(ulong clientId)
    //{
    //    if (!NetworkManager.Singleton.IsServer)
    //        return;

    //    // Delay player spawn until after scene is loaded if needed
    //    StartCoroutine(SpawnPlayerAfterSceneLoad(clientId));
    //}

    //private IEnumerator<WaitForSeconds> SpawnPlayerAfterSceneLoad(ulong clientId)
    //{
    //    // Small delay to ensure the scene is ready
    //    yield return new WaitForSeconds(0.2f);

    //    // Choose a spawn point (e.g., based on order or randomly)
    //    Vector3 spawnPos = spawnPoints.Count > 0 ? spawnPoints[(int)(clientId % (ulong)spawnPoints.Count)].position : Vector3.zero;

    //    GameObject playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
    //    var networkObject = playerInstance.GetComponent<NetworkObject>();

    //    // Spawn and assign this player object to the connected client
    //    networkObject.SpawnAsPlayerObject(clientId);
    //}

    //private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    //{
    //    // Wait for network scene load and spawn players
    //    if (NetworkManager.Singleton.IsServer && scene.name != "MainMenu" && !playersSpawned)
    //    {
    //        SpawnAllPlayers();
    //        playersSpawned = true;
    //    }
    //}

    //private void OnNetworkSceneEvent(SceneEvent sceneEvent)
    //{
    //    // Wait for server-side scene load complete
    //    if (sceneEvent.SceneEventType == SceneEventType.LoadComplete &&
    //        NetworkManager.Singleton.IsServer &&
    //        sceneEvent.SceneName == "_Scenes/Tutorial" &&
    //        !playersSpawned)
    //    {
    //        Debug.Log("Scene loaded via Netcode: Spawning players...");
    //        SpawnAllPlayers();
    //        playersSpawned = true;
    //    }
    //}

    //private void SpawnAllPlayers()
    //{
    //    int spawnIndex = 0;

    //    foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
    //    {
    //        ulong clientId = clientPair.Key;
    //        GameObject playerObject = clientPair.Value.PlayerObject.gameObject;

    //        if (spawnIndex >= spawnPoints.Count)
    //        {
    //            Debug.LogWarning("Not enough spawn points!");
    //            break;
    //        }

    //        playerObject.transform.position = spawnPoints[spawnIndex].position;
    //         Debug.Log("Found M_DisableComponentsOnNonOwner: " + playerObject.TryGetComponent<M_DisableComponentsOnNonOwner>(out M_DisableComponentsOnNonOwner setComponentes).ToString());

    //        setComponentes.EnableCameras();

    //        // If targetSurface is not set, find the Ocean game object and get its WaterSurface component

    //        GameObject ocean = GameObject.Find("Ocean");

    //        Debug.Log("Found TugboatMovementWFloat: " + playerObject.TryGetComponent<TugboatMovementWFloat>(out TugboatMovementWFloat boatMovement).ToString());

    //        if (boatMovement != null)
    //        {
    //            boatMovement.targetSurface = levelWaterSurface;
    //            if (boatMovement.targetSurface == null)
    //                Debug.LogError("WaterSurface component not found on object Ocean", this);
    //        }
    //        else
    //        {
    //            Debug.LogError("Boat Movement Script object not found", this);
    //        }

    //        spawnIndex++;
    //    }
    //}
    #endregion


}
