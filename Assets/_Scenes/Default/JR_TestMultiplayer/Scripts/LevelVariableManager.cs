using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// This script is no longer used for anything other than the boat to get the water Source from a scene.
/// </summary>
public class LevelVariableManager : NetworkBehaviour
{
    public static LevelVariableManager Instance;

    public WaterSurface GlobalWaterSurface;
    public Rigidbody GlobalBargeRigidBody;
    public GameObject GlobalEndGameTrigger;
    public PlayerRespawn GlobalPlayerRespawnController;
    public Transform GlobalRespawnTempMovePoint;

    //NetworkVariable<DateTime> referenceSurface = new NetworkVariable<DateTime>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }else
        {
            Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        //If we are the Host set the time to now:
        //if (IsHost) referenceSurface.Value = DateTime.Now;

        //Then every client will access the same time variable and set it to the waterSurface.simulationStart
        if (!IsOwner) return;
        // waterSurface.simulationStart = DateTime.Now;
        //GlobalWaterSurface.simulationStart = referenceSurface.Value; //this should sync all water surfaces 
    }
}
