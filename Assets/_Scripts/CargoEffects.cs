using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Handles visual effects for cargo objects that create water disturbances.
/// Syncs effects across the network.
/// </summary>
public class CargoEffects : NetworkBehaviour
{
    [Header("Movement Detection")]
    [SerializeField] private float maxSpeed = 8f;
    [Header("Water Effects")]
    [SerializeField] private WaterDecal bowWaveDecal;

    [Header("Sail/Canvas Effects")]
    [SerializeField] private GameObject[] sailObjects;
    private Material[] _sailMaterials;
    private float _currentSailStrength = 0.2f;

    // For speed calculation
    private Vector3 _lastPosition;
    private Rigidbody _referenceRigidbody;
    
    // Network variable to sync effects across clients
    private readonly NetworkVariable<float> _syncedSpeed = new NetworkVariable<float>(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Start()
    {
        
        _referenceRigidbody = gameObject.GetComponent<Rigidbody>();

        _lastPosition = transform.position;

        // Extract materials from sail GameObjects
        if (sailObjects != null && sailObjects.Length > 0)
        {
            List<Material> materials = new List<Material>();
            foreach (GameObject sail in sailObjects)
            {
                if (sail != null)
                {
                    Renderer renderer = sail.GetComponent<Renderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        materials.Add(renderer.material);
                    }
                }
            }
            _sailMaterials = materials.ToArray();
        }
    }

    private void Update()
    {
        // Only the server or owner calculates speed and updates the network variable
        if (IsServer || IsOwner)
        {
            float speed = CalculateSpeed();
            _syncedSpeed.Value = speed;
            
            // Server applies effects locally (optimization)
            if (IsServer)
            {
                ApplyMovementEffects(speed);
            }
        }
        // Clients receive synced speed and apply effects
        else
        {
            ApplyMovementEffects(_syncedSpeed.Value);
        }
    }

    private float CalculateSpeed()
    {
        // Use the Rigidbody for more accurate speed calculation
        if (_referenceRigidbody != null)
        {
            return _referenceRigidbody.linearVelocity.magnitude;
        }
    
        // Fallback to position-based calculation if needed
        Vector3 displacement = transform.position - _lastPosition;
        float speed = displacement.magnitude / Time.deltaTime;
        _lastPosition = transform.position;
        return speed;
    }

    private void ApplyMovementEffects(float currentSpeed)
    {
        // Normalize speed to a value between 0 and 1
        float normalizedSpeed = Mathf.InverseLerp(0, maxSpeed, currentSpeed);
        //Debug.Log($"Speed: {currentSpeed}, Normalized: {normalizedSpeed}");


        // Apply bow wave effects
        if (bowWaveDecal != null)
        {
            // Smoothly transition amplitude based on normalized speed
            bowWaveDecal.amplitude = Mathf.Lerp(0, 4, normalizedSpeed);

            // Smoothly transition bow wave decal region size
            Vector2 minSize = new(70f, 110f);
            Vector2 maxSize = new(75f, 120f);
            bowWaveDecal.regionSize = Vector2.Lerp(minSize, maxSize, normalizedSpeed);
        }

        // Apply sail/canvas effects with precision of 2 decimal places
        if (_sailMaterials != null && _sailMaterials.Length > 0)
        {
            float rawStrength = Mathf.Lerp(0.2f, 0.8f, normalizedSpeed);
            float roundedStrength = Mathf.Round(rawStrength * 100) / 100f; // Round to 2 decimal places (0.XX)

            // Only update if the rounded value actually changed
            if (Mathf.Abs(_currentSailStrength - roundedStrength) > 0.001f)
            {
                _currentSailStrength = roundedStrength;

                foreach (Material sail in _sailMaterials)
                {
                    if (sail != null)
                    {
                        sail.SetFloat("_Strength", _currentSailStrength);
                    }
                }
            }
        }
    }
}