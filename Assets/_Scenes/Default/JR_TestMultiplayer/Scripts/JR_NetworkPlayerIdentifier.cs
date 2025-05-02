using TMPro;
using Unity.Netcode;
using UnityEngine;

public class JR_NetworkPlayerIdentifier : NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI playerIDText;
    [SerializeField] TextMeshPro playerID3DText;


    //this network variable is updated OnNetworkSpawn with the OwnerClientID from the network behaviour 
    //that throws OnNetworkSpawn
    NetworkVariable<ulong> playerIdNetworkV = new NetworkVariable<ulong>();

    //flag set to make sure this only hits on the first update and then stops 
    bool isPlayerIdSet = false;

    public override void OnNetworkSpawn()
    {
        //This makes sure the server (Host) Also updates)
        if (IsServer)
        {
            playerIdNetworkV.Value = OwnerClientId + 1; // this makes it making so that if player 2 disconnects and rejoins he will be player 3??? needs testing 
            //playerIdNetworkV.Value = (ulong)NetworkManager.Singleton.ConnectedClients.Count + 1; // this will allow player to disconnect and rejoin as player 2 for example(I think)
        }

        base.OnNetworkSpawn();
        
    }

    private void Update()
    {
        if (!isPlayerIdSet)
        {
            SetPlayerIdText();
            //only set once 
            isPlayerIdSet=true;
        }
    }

    void SetPlayerIdText()
    {
        if(playerIDText != null) playerIDText.text = $"Player_{playerIdNetworkV.Value}"; // + 1 because first index is 0
        if(playerID3DText != null) playerID3DText.text = $"Player_{playerIdNetworkV.Value}"; // + 1 because first index is 0
    }





    //[Rpc] is Run Once! for everyone 

    //Let other players Know that you joinned the game 
    [Rpc(SendTo.ClientsAndHost)]
    public void SetPlayerIdentifierTextRpc(string val)
    {
        /* 
         * This notifies all clients and host of the update text field value
         * without this the data will not sync
         */

        //playerIDTextbox.text = $"Player_{NetworkManager.LocalClientId + 1}";
        playerIDText.text = val;

    }
}
