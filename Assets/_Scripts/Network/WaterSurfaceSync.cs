using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class WaterSurfaceNetworkSync : NetworkBehaviour
{
    // Reference to the water component on this GameObject.
    public WaterSurface waterSurface;

    // Network variables for synchronizing water simulation.
    // Use double for DateTime storage (OADate conversion).
    public NetworkVariable<double> networkSimulationStart = new NetworkVariable<double>(0);
    public NetworkVariable<float> networkSimulationTime = new NetworkVariable<float>(0);

    private void Awake()
    {
        if (waterSurface == null)
        {
            waterSurface = GetComponent<WaterSurface>();
            if (waterSurface == null)
                Debug.LogError("WaterSurface component not found.", this);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Set the reference water surface simulation start as current time.
            networkSimulationStart.Value = DateTime.Now.ToOADate();
            waterSurface.simulationStart = DateTime.Now;
            waterSurface.simulationTime = 0;
            networkSimulationTime.Value = 0;
        }
        else
        {
            // Clients subscribe to changes.
            networkSimulationStart.OnValueChanged += OnSimulationStartChanged;
            networkSimulationTime.OnValueChanged += OnSimulationTimeChanged;
        }
    }

    private void Update()
    {
        if (IsServer && waterSurface != null)
        {
            // Update simulation time on the host.
            networkSimulationTime.Value = waterSurface.simulationTime;
        }
    }

    private void OnSimulationStartChanged(double previousValue, double newValue)
    {
        // Convert stored double back to DateTime and assign it.
        waterSurface.simulationStart = DateTime.FromOADate(newValue);
    }

    private void OnSimulationTimeChanged(float previousValue, float newValue)
    {
        waterSurface.simulationTime = newValue;
    }

    private void OnDestroy()
    {
        if (!IsServer)
        {
            networkSimulationStart.OnValueChanged -= OnSimulationStartChanged;
            networkSimulationTime.OnValueChanged -= OnSimulationTimeChanged;
        }
    }
}