using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class WindLineEmitter : MonoBehaviour
{
    [Header("Particle References")]
    [SerializeField] private ParticleSystem[] windParticlePrefabs;
    
    [Header("Wind Settings")]
    [Range(0.1f, 3f)]
    [SerializeField] private float simulationSpeed = 1f;
    [SerializeField] private Vector2 speedVariance = new Vector2(0.9f, 1f);
    [SerializeField] private bool detachFromMovement = true;

    [Header("Target Settings")]
    [SerializeField] private bool useTargetDirection = false;
    [SerializeField] private bool lockYAxis = false;
    [SerializeField] private float switchDistanceThreshold = 100f;

    [Header("Emission Settings")]
    [Range(0.1f, 100)]
    [SerializeField] private float particlesPerSecond = 10f;
    [Range(0.1f, 10f)]
    [SerializeField] private float particleLifetime = 4f;
    [Range(0.01f, 1f)]
    [SerializeField] private float minParticleSize = 0.05f;
    [Range(0.01f, 1f)]
    [SerializeField] private float maxParticleSize = 0.15f;

    // Internal variables
    private BoxCollider triggerBox;
    private List<ParticleSystem> activeParticleSystems = new List<ParticleSystem>();
    private float spawnTimer = 0f;
    private static Transform particleContainer; // Static container for all detached particles
    
    [Header("Debug Settings DONT TOUCH")]
    [SerializeField] private Transform _target = null;
    [SerializeField] private Transform _bargeTransform = null;
    [SerializeField] private Vector3 windDirection;

    private void Start()
    {
        // Get box collider
        triggerBox = GetComponent<BoxCollider>();
        triggerBox.isTrigger = true;

        // Create a global particle container if needed
        if (detachFromMovement && particleContainer == null)
        {
            GameObject container = new GameObject("Global_Wind_Particles");
            particleContainer = container.transform;
            DontDestroyOnLoad(container);
        }

        // Setup targets if using target direction
        if (useTargetDirection && LevelVariableManager.Instance != null)
        {
            _target = LevelVariableManager.Instance.GlobalEndGameTrigger.transform;
            _bargeTransform = LevelVariableManager.Instance.GlobalBargeRigidBody.transform;
            
            if (_target == null || _bargeTransform == null)
            {
                Debug.LogWarning("Target or Barge reference not found in LevelVariableManager", this);
                useTargetDirection = false;
            }
        }
        
        // Initialize wind direction
        UpdateWindDirection();

        // Setup particle systems
        SetupParticleSystems();
    }

    private void SetupParticleSystems()
    {
        // Clear any existing particle systems
        foreach (var system in activeParticleSystems)
        {
            if (system != null)
                Destroy(system.gameObject);
        }
        activeParticleSystems.Clear();

        Transform parentTransform = detachFromMovement ? particleContainer : transform;

        // If we have prefabs, instantiate all of them
        if (windParticlePrefabs != null && windParticlePrefabs.Length > 0)
        {
            foreach (ParticleSystem prefab in windParticlePrefabs)
            {
                if (prefab != null)
                {
                    ParticleSystem newSystem = Instantiate(prefab, parentTransform);
                    newSystem.transform.position = transform.position;
                    newSystem.transform.rotation = transform.rotation; // Start with same rotation
                    ConfigureParticleSystem(newSystem);
                    activeParticleSystems.Add(newSystem);
                }
            }
        }

        // If no prefabs were available, create a default particle system
        if (activeParticleSystems.Count == 0)
        {
            GameObject particleObj = new GameObject("WindParticles");
            particleObj.transform.SetParent(parentTransform);
            if (detachFromMovement)
            {
                particleObj.transform.position = transform.position;
            }
            else
            {
                particleObj.transform.localPosition = Vector3.zero;
            }

            ParticleSystem newSystem = particleObj.AddComponent<ParticleSystem>();
            var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.1f;
            renderer.lengthScale = 2f;

            ConfigureParticleSystem(newSystem);
            activeParticleSystems.Add(newSystem);
        }
    }

    private void ConfigureParticleSystem(ParticleSystem system)
    {
        // Configure the particle system
        var main = system.main;
        main.startLifetime = particleLifetime;
        main.startSize = new ParticleSystem.MinMaxCurve(minParticleSize, maxParticleSize);
        main.startSpeed = 0; // Set to zero since we'll use velocity in EmitParams
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.CeilToInt(particlesPerSecond * particleLifetime * 2);
        main.simulationSpeed = simulationSpeed;

        // Disable automatic emission
        var emission = system.emission;
        emission.enabled = false;

        // Configure shape module to not emit automatically
        var shape = system.shape;
        shape.enabled = false;

        // Configure renderer for proper stretching
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.1f; // Adjust based on desired stretch amount
            renderer.lengthScale = 2f;
            // Ensure particles align with velocity
            renderer.alignment = ParticleSystemRenderSpace.Velocity;
        }
    }

    private void Update()
    {
        if (activeParticleSystems.Count == 0) return;

        // Update the wind direction
        UpdateWindDirection();

        // Spawn particles based on rate
        spawnTimer += Time.deltaTime;
        float spawnInterval = 1f / particlesPerSecond;
        while (spawnTimer >= spawnInterval)
        {
            SpawnParticle();
            spawnTimer -= spawnInterval;
        }

        // Update particle systems
        foreach (var system in activeParticleSystems)
        {
            if (system == null) continue;
            
            // Update simulation speed
            var main = system.main;
            main.simulationSpeed = simulationSpeed;
            
            // Make the particle system face the wind direction
            system.transform.rotation = Quaternion.LookRotation(windDirection);
        }
    }

    private void UpdateWindDirection()
    {
        // Default to transform's forward direction
        windDirection = transform.forward;

        // If using target-based direction and references exist
        if (useTargetDirection && _target != null && _bargeTransform != null)
        {
            // Calculate distance to barge
            float distanceToBarge = Vector3.Distance(transform.position, _bargeTransform.position);

            // Choose target based on distance threshold
            Transform currentTarget = (distanceToBarge > switchDistanceThreshold) ? _bargeTransform : _target;

            // Calculate direction FROM emitter TO target (not the reverse)
            windDirection = currentTarget.position - transform.position;

            // Lock Y-axis if needed
            if (lockYAxis)
            {
                windDirection.y = 0;
            }

            // Normalize the direction
            if (windDirection.magnitude > 0.01f)
            {
                windDirection.Normalize();
            }
            
            windDirection = Quaternion.Euler(0, -90, 0) * windDirection;
        }
    }

    private void SpawnParticle()
    {
        // Calculate a random position within the box collider
        Vector3 randomPos = GetRandomPositionInBox();

        // Calculate the particle direction based on wind direction
        Vector3 particleDirection = windDirection;

        // Random speed variance
        float speedMultiplier = Random.Range(speedVariance.x, speedVariance.y);

        // Create particle emission parameters
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
        {
            position = randomPos,
            velocity = particleDirection * speedMultiplier,
            startSize = Random.Range(minParticleSize, maxParticleSize)
        };

        // Emit the particle
        if (activeParticleSystems.Count > 0)
        {
            int randomIndex = Random.Range(0, activeParticleSystems.Count);
            activeParticleSystems[randomIndex].Emit(emitParams, 1);
        }
    }

    private Vector3 GetRandomPositionInBox()
    {
        // Get the size and center of the box collider in local space
        Vector3 boxSize = triggerBox.size;
        Vector3 boxCenter = triggerBox.center;

        // Generate a random position within the box
        Vector3 randomPos = new Vector3(
            Random.Range(-boxSize.x, boxSize.x) * 0.5f,
            Random.Range(-boxSize.y, boxSize.y) * 0.5f,
            Random.Range(-boxSize.z, boxSize.z) * 0.5f
        );

        // Convert to world space
        return transform.TransformPoint(boxCenter + randomPos);
    }
    
    private void OnDrawGizmos()
    {
        if (triggerBox == null) triggerBox = GetComponent<BoxCollider>();
        if (triggerBox == null) return;

        // Draw the box
        Gizmos.color = new Color(0, 0.8f, 1f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(triggerBox.center, triggerBox.size);
        
        // Draw arrow for wind direction
        Vector3 center = transform.TransformPoint(triggerBox.center);
        
        // Determine which direction to draw
        Vector3 directionToShow;
        if (Application.isPlaying && useTargetDirection && windDirection.magnitude > 0.01f)
        {
            directionToShow = windDirection;
        }
        else
        {
            directionToShow = transform.forward;
        }
        
        Vector3 arrowEnd = center + directionToShow * 25f;

        // Main arrow line
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(center, arrowEnd);

        // Arrow head
        float arrowHeadSize = 6f;
        Vector3 right = transform.right * arrowHeadSize;
        Vector3 up = transform.up * arrowHeadSize;
        Vector3 back = -directionToShow.normalized * arrowHeadSize;

        Gizmos.DrawLine(arrowEnd, arrowEnd + back + up);
        Gizmos.DrawLine(arrowEnd, arrowEnd + back - up);
        Gizmos.DrawLine(arrowEnd, arrowEnd + back + right);
        Gizmos.DrawLine(arrowEnd, arrowEnd + back - right);
    }
    
    private void OnDestroy()
    {
        // Clean up particles when destroyed
        if (detachFromMovement)
        {
            foreach (var system in activeParticleSystems)
            {
                if (system != null)
                {
                    var main = system.main;
                    main.stopAction = ParticleSystemStopAction.Destroy;
                    system.Stop(true);
                }
            }
        }
    }
}