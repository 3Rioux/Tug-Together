using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class JR_NetWaterSync : NetworkBehaviour
{
    public static JR_NetWaterSync instance;

    public WaterSurface GlobalWaterSurface;

   // NetworkVariable<DateTime> referenceSurface = new NetworkVariable<DateTime>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }else
        {
            Destroy(this.gameObject);
        }
    }

    //private void Start()
    //{
    //    //If we are the Host set the time to now:
    //    if (IsHost) referenceSurface.Value = DateTime.Now;

    //    //Then every client will access the same time variable and set it to the waterSurface.simulationStart
    //    if (!IsOwner) return;
    //    // waterSurface.simulationStart = DateTime.Now;
    //    GlobalWaterSurface.simulationStart = referenceSurface.Value; //this should sync all water surfaces 
    //
}
