using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Attach this script to each player’s GameObject (with NetworkObject).
/// Will Update and Keep Track of the player info its attached too.
/// </summary>
public class NetworkPlayerInfo : NetworkBehaviour
{
    [SerializeField] private static readonly int MaxNameLength = 20;

    [Header("Live Player Info")]
    public NetworkVariable<FixedString32Bytes> PlayerName = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> Health = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> Score = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public bool IsNetworkPlayerInfoInitialised = false;

    [SerializeField] private UnitHealthController playerHealth; //store the current health 

    private void Awake()
    {
        playerHealth = this.GetComponent<UnitHealthController>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
           // PlayerPrefs.GetString("PlayerName", "Player")
            SetInitialInfo($"{name}", playerHealth.CurrentUnitHealth); // Default health = 100
            if(IsHost)
            {
                Debug.Log($"Host Player Info set {name}");
            }
        }
    }

    private void SetInitialInfo(string name, int startHealth)
    {
        PlayerName.Value = new FixedString32Bytes(name.Substring(0, Mathf.Min(name.Length, MaxNameLength)));
        Health.Value = startHealth;
        Score.Value = 0;

        //change state to set:
        IsNetworkPlayerInfoInitialised = true;
    }

    public void UpdateHealth(int newHealth)
    {
       if (IsOwner) 
            Health.Value = newHealth;
    }

    public void AddScore(int points)
    {
        if (IsOwner)
            Score.Value += points;
    }






}
