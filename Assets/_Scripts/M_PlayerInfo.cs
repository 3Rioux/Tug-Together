using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;

public class M_PlayerInfo : NetworkBehaviour
{
    private bool hasRegistered = false;

    //private NetworkVariable<FixedString32Bytes> playerName = new(
    //   new FixedString32Bytes("Player"),
    //   NetworkVariableReadPermission.Everyone,
    //   NetworkVariableWritePermission.Owner
    //);

    //reference to the player NETWORK Health 
    private M_UnitHealthController healthController;
    private string playerName;

    private void Awake()
    {
        healthController = GetComponent<M_UnitHealthController>();
    }

    private void Start()
    {
        SetPlayerName();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer || hasRegistered) return;

        hasRegistered = true;

        //SetPlayerName();

        LeaderboardManager.Instance?.RegisterPlayer(this);
    }

    public string GetPlayerName()
    {
        //SetPlayerName();
        return playerName;
    }
    private async void SetPlayerName()
    {
        if (true)
        {
            playerName = await WidgetDependenciesWrapper.GetPlayerNameAsync();
        }
    }

    public int GetCurrentHealth()
    {
        return healthController != null ? healthController.NetworkUnitCurrentHealth.Value : 0;
    }
}
