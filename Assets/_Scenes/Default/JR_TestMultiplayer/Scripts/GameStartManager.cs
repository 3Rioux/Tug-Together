using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameStartManager : NetworkBehaviour
{
    [SerializeField] GameObject playerEntryPrefab;
    [SerializeField] Transform playerEntryParent;
    [SerializeField] TMP_InputField lobbyCodeText;


    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            Debug.Log($"Client {clientId} connected. PlayerObject = {NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject}");
        };
    }

    private void OnEnable()
    {
        NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
    }



    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
        {
            if (IsServer) // Server = Host
            {
                Debug.Log(NetworkManager.Singleton.ConnectedClientsList.Count.ToString());

                // Spawn all players manually
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    var player = Instantiate(NetworkManager.Singleton.NetworkConfig.PlayerPrefab, GetSpawnPoint(), Quaternion.identity);
                    player.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.ClientId);

                    if(!IsLocalPlayer) continue;
                    //Display active users info Top Right:
                    PlayerEntry playerUIEntry = Instantiate(playerEntryPrefab, playerEntryParent).GetComponent< PlayerEntry>();

                    UnitHealthController playerHealthController = player.GetComponent<UnitHealthController>();

                    playerUIEntry.SetPlayerInfo($"Player_{client.ClientId}", playerHealthController.CurrentUnitHeath.CurrentHealth, IsLocalPlayer);


                    //set the Lobby Code Text to the Current Lobby Code:
                    lobbyCodeText.text = LobbyManager.Instance.GetLobbyCode();

                }
            }
        }
    }

    private Vector3 GetSpawnPoint()
    {
        // Use a spawn manager or just random position for now
        return new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
    }











}
