using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using TMPro;
using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;


/// <summary>
/// Tugboat controller using Rigidbody, HDRP Custom Water, and reverse-compatible thrust.
/// Suitable for towing heavy vessels.
/// https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/water-deform-a-water-surface.html#bow-wave
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class TugboatMovementWFloat : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI debugSpeedText;

    [Header("Thrust & Speed")]
    public float maxSpeed = 8f;
    //public float boostMaxSpeed = 100f;
    public float throttleForce = 2000f;
    public float throttleForceMultiplier = 10f;
    [HideInInspector] public float defaultThrottleForceMultiplier = 10f;
    public float reverseThrottleFactor = 0.5f;    // Less power in reverse
    public float accelerationSmoothing = 2f;

    [Header("Movement Effects")]
    [SerializeField] WaterDecal[] bowWaveDecals; 
    [SerializeField] Material bowWaveMaterial; 
    [SerializeField] WaterFoamGenerator foamGenerator; 
    [SerializeField] VisualEffect rearSplashVFX;
    [SerializeField] private Material speedLinesMaterial;
    
    [Header("Ship Sails")]
    [SerializeField] private GameObject[] sailObjects; // Changed from Material[] sailMaterials
    private Material[] _sailMaterials; // Will store extracted materials
    private float _currentSailStrength = 0.2f;
    

    private Vector3 _dampVelocity = Vector3.zero;

    [Header("Turning")]
    public float turnTorque = 1500f;
    public float turnResponsiveness = 3f;

    [Header("Camera Settings")]
    [SerializeField] private Transform cameraRig; // Drag your CameraRig here in Inspector
    [SerializeField] private float lookSensitivity = 1.5f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Physics")]
    public float dragWhenNotMoving = 2f;
    public float angularDrag = 0.5f;

    //flaoting/movement Damping
    [Tooltip("How long (in seconds) it takes to catch up to the true water height.")]
    [SerializeField] float dampingTime = 0.2f;

    [Space(10)]
    [Header("Float")]
    [SerializeField] private float3 floatingOffset;

    public WaterSurface targetSurface = null;
    public bool includeDeformation = true;
    public bool excludeSimulation = false;

    [Header("Orientation")]
    public bool alignToWaterNormal = true;
    [Range(0f, 10f)] public float OrientationSmoothSpeed = 5f;
    [Range(0f, 10f)] public float OrientationDefaultSmoothSpeed = 5f;

    private Vector3 previousPosition;
    [SerializeField] private float pitchAngleMultiplier = 2f;
    [SerializeField] private float pitchAngleDefaultMultiplier = 2f;
    [SerializeField] private float rollAngleMultiplier = 3f;
    [SerializeField] private float rollAngleDefaultMultiplier = 3f;
    // Internal search params
    WaterSearchParameters searchParameters = new WaterSearchParameters();
    WaterSearchResult searchResult = new WaterSearchResult();
    
    
    [Header("Pole Rotation")]
    [SerializeField] private Transform[] poleMasts; // Assign main pole first, then secondary
    [SerializeField] private float mainPoleMaxRotation = 15f;
    [SerializeField] private float secondaryPoleMaxRotation = 8f;
    [SerializeField] private float poleRotationDuration = 0.5f; // DOTween duration
    
    // Add to TugboatMovementWFloat.cs
    [Header("Camera Shake")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private CinemachineInputAxisController inputProvider;
    [SerializeField] private float collisionShakeThreshold = 2f;
    [SerializeField] private float collisionShakeMultiplier = 0.5f;
    [SerializeField] private float hookShakeIntensity = 0.3f;

    // NetworkVariable to sync pole rotation across clients
    private readonly NetworkVariable<float> _syncedPoleRotation = new NetworkVariable<float>(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Track last local rotation value for smoothing
    private float _lastPoleRotationValue = 0f;
    private bool _isRotating = false;
    
    
    private bool isPlayingIdleSound = false;
    private bool isPlayingMoveSound = false;
    private bool isPlayingTurningSound = false;
    private float minSpeedForMoving = 0.5f;
    private float turnSoundStopDelay = 1.0f;
    private float turnSoundTimer = 0f;
    private EventInstance turningInstance;
    private EventInstance moveInstance; // To control speed parameter

    private float hornCooldown = 0f;
    private const float HORN_COOLDOWN_DURATION = 1.5f; // Prevent spam


    private Rigidbody rb;
    private Vector2 moveVector;
    private Vector2 lookVector;
    private float yaw;
    private float pitch;
    private float targetThrottle;
    private float currentThrottle;
    private SpringTugSystem _sprintTugSystem;

    private BoatInputActions controls;
    


    private readonly NetworkVariable<float> _syncedSpeed = new NetworkVariable<float>(0f, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Awake()
    {
        //Save the start OrientationSmoothSpeed
        OrientationDefaultSmoothSpeed = OrientationSmoothSpeed;
        pitchAngleDefaultMultiplier = pitchAngleMultiplier;
        rollAngleDefaultMultiplier = rollAngleMultiplier;

        _sprintTugSystem = GetComponent<SpringTugSystem>();

        rb = GetComponent<Rigidbody>();
        rb.angularDamping = angularDrag;

        controls = new BoatInputActions();
        //Movement input
        controls.Boat.Move.performed += ctx => moveVector = ctx.ReadValue<Vector2>();
        controls.Boat.Move.canceled += _ => moveVector = Vector2.zero; // remove this to enable Toggle speed 
        
        //Look input
        controls.Boat.Look.performed += ctx => lookVector = ctx.ReadValue<Vector2>();
        controls.Boat.Look.canceled += _ => lookVector = Vector2.zero; // remove this to enable camera drift 
        
        // Add horn input handling
        controls.Boat.Horn.performed += ctx => PlayHorn();
        
        // Pole rotation input handling
        controls.Boat.Move.performed += ctx => {
            Vector2 input = ctx.ReadValue<Vector2>();

            // If horizontal input is significant, rotate poles
            if (Mathf.Abs(input.x) > 0.1f) {
                _isRotating = true;
                float targetRotation = input.x * mainPoleMaxRotation;

                if (IsOwner) {
                    _syncedPoleRotation.Value = targetRotation;
                    RotatePoles(targetRotation);
                }
            }
            // If horizontal input is near zero BUT we were rotating before,
            // reset rotation (this handles the case of releasing D while still holding W)
            else if (_isRotating) {
                _isRotating = false;
        
                if (IsOwner) {
                    _syncedPoleRotation.Value = 0f;
                    RotatePoles(0f);
                }
            }
        };
        
        controls.Boat.Move.canceled += _ => {
            if (_isRotating) {
                _isRotating = false;

                if (IsOwner) {
                    _syncedPoleRotation.Value = 0f;
                    RotatePoles(0f);
                }
            }
        };
    }

    private void Start()
    {
        // Existing code
        defaultThrottleForceMultiplier = throttleForceMultiplier;

        // If targetSurface is not set, find the Ocean game object and get its WaterSurface component
        if (targetSurface == null)
        {
            targetSurface = LevelVariableManager.Instance.GlobalWaterSurface;
        }
    
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
        // Get or add impulse source
        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>() ?? gameObject.AddComponent<CinemachineImpulseSource>();
    }

    public override void OnNetworkSpawn()
    {
        //base.OnNetworkSpawn();

        
        if (targetSurface == null)
        {
            InitializeWaterTarget();
        }
        
        // Initialize sound when spawned
        InitializeSound();
    }

    void OnEnable()
    {
        if (targetSurface == null)
        {
            targetSurface = LevelVariableManager.Instance.GlobalWaterSurface; // get the water surface 
        }
        controls.Enable();
    }


    void OnDisable()
    {
        controls.Disable();
    }
    
    public void SetControlEnabled(bool enabled)
    {
        if (enabled)
        {
            controls.Enable();

            //Also disable /enable UnitInfoReporter Inputs
            this.gameObject.GetComponent<UnitInfoReporter>().SetControlEnabled(enabled);

            // Enable Cinemachine input
            if (inputProvider != null)
                inputProvider.enabled = true;
        }
        else
        {
            controls.Disable();
            //Also disable /enable UnitInfoReporter Inputs
            this.gameObject.GetComponent<UnitInfoReporter>().SetControlEnabled(enabled);

            moveVector = Vector2.zero;
            lookVector = Vector2.zero;
        
            // Disable Cinemachine input
            if (inputProvider != null)
                inputProvider.enabled = false;
        }
    }
    

    private void Update()
    {
        // Local player logic
        if (IsOwner)
        {
            //Change Max Speed + Tilt when close to barge:

            if (_sprintTugSystem.DistanceToTowedObject <= 50f)
            {
                OrientationSmoothSpeed = 10f;
                pitchAngleMultiplier = 0f; //stop pitch or even give it a -0.01
                rollAngleMultiplier = 0f; // test this out
            }
            else
            {
                if (OrientationSmoothSpeed != OrientationDefaultSmoothSpeed)
                {
                    OrientationSmoothSpeed = OrientationDefaultSmoothSpeed;
                    pitchAngleMultiplier = pitchAngleDefaultMultiplier;
                    rollAngleMultiplier = rollAngleDefaultMultiplier;
                }
            }



            float speed = rb.linearVelocity.magnitude - 10f;
            if (speed <= 0.4f) speed = 0f;
        
            // Update network variable with current speed
            _syncedSpeed.Value = speed;
        
            // Display speed in UI
            if (debugSpeedText != null)
                debugSpeedText.text = $"Speed: {speed:0}m/s";
            
            // Apply visual effects for local player using local calculations
            ApplyMovementEffects(speed);
        }
        // Remote player logic
        else
        {
            // Apply visual effects for remote player using network-synced speed
            ApplyMovementEffects(_syncedSpeed.Value);
        }
        
        UpdateSounds();
        UpdateHorn();
       
    }
   
    
    private void ApplyMovementEffects(float currentSpeed)
    {
        // Normalize speed to a value between 0 and 1
        float normalizedSpeed = Mathf.InverseLerp(0, maxSpeed - 10f, currentSpeed);

        // == Wave Bow Effects (now handles array) ==
        if (bowWaveDecals != null && bowWaveDecals.Length > 0)
        {
            foreach (WaterDecal decal in bowWaveDecals)
            {
                if (decal == null) continue;
            
                // Smoothly transition amplitude between 0 and 3 based on normalized speed
                decal.amplitude = Mathf.Lerp(0, 3, normalizedSpeed);
            
                // Smoothly transition bow wave decal region size
                Vector2 minSize = new(30.5f, 43f);
                Vector2 maxSize = new(45f, 55f);
                decal.regionSize = Vector2.Lerp(minSize, maxSize, normalizedSpeed);
            }
        }
        // == Water Splash Effect ==
        if (currentSpeed > 1.0f && targetSurface != null)
        {
            // Get current position of the splash effect
            Vector3 splashPosition = rearSplashVFX.transform.position;
    
            // Create search parameters just for the splash
            WaterSearchParameters splashParams = new WaterSearchParameters
            {
                startPositionWS = splashPosition + Vector3.up * 2f,
                targetPositionWS = splashPosition,
                includeDeformation = includeDeformation,
                excludeSimulation = excludeSimulation,
                error = 0.01f,
                maxIterations = 4
            };

            // Find water height at splash position
            if (targetSurface.ProjectPointOnWaterSurface(splashParams, out WaterSearchResult splashResult))
            {
                // Only update the Y position to match water height
                splashPosition.y = splashResult.projectedPositionWS.y;
                rearSplashVFX.transform.position = splashPosition;
            }
    
            // Set VFX size based on speed
            float splashSize = Mathf.Lerp(0.3f, 2.5f, normalizedSpeed);
            rearSplashVFX.SetFloat("Size", splashSize);
            rearSplashVFX.gameObject.SetActive(true);
        }
        else
        {
            rearSplashVFX.gameObject.SetActive(false);
        }

        // == Sails Effect ==
        
        // Calculate sail strength but round to 2 decimal places
        float rawStrength = Mathf.Lerp(0.2f, 0.8f, normalizedSpeed);
        float roundedStrength = Mathf.Round(rawStrength * 100) / 100f; // Round to 2 decimal places (0.XX)
    
        // Only update if the rounded value actually changed
        if (Mathf.Abs(_currentSailStrength - roundedStrength) > 0.001f)
        {
            _currentSailStrength = roundedStrength;
        
            if (_sailMaterials != null)
            {
                foreach (Material sail in _sailMaterials)
                {
                    if (sail != null)
                    {
                        sail.SetFloat("_Strength", _currentSailStrength);
                    }
                }
            }
        }
        
        // == Speed Lines Effect ==
        
        // Handle speed lines material
        if (speedLinesMaterial != null)
        {
            // Calculate density directly from sail strength rather than separately from speed
            float linesDensity = Mathf.InverseLerp(0.2f, 0.8f, _currentSailStrength) * 0.36f;
            speedLinesMaterial.SetFloat("_Lines_Density", linesDensity);
        }
    }

    void FixedUpdate()
    {

        if (!IsOwner)
        {
            return;
        }

        HandleThrottle();
        HandleTurning();
        //HandleDrag();

         // Handle pole rotation for remote players
        if (!IsOwner) {
            // Only update when the network value changes
            if (!Mathf.Approximately(_syncedPoleRotation.Value, _lastPoleRotationValue)) {
                _lastPoleRotationValue = _syncedPoleRotation.Value;
                RotatePoles(_syncedPoleRotation.Value);
            }
        }


        if (targetSurface == null)
            return;


        // Build the search parameters
        searchParameters.startPositionWS = searchResult.candidateLocationWS;
        searchParameters.targetPositionWS = gameObject.transform.position;
        searchParameters.error = 0.01f;
        searchParameters.maxIterations = 8;
        searchParameters.includeDeformation = includeDeformation;
        searchParameters.excludeSimulation = excludeSimulation;

        // Do the search
        if (targetSurface.ProjectPointOnWaterSurface(searchParameters, out searchResult))
        {
            //Attempt Smoother movement
            //Vector3 currentPositon = transform.position;
            //gameObject.transform.position = new Vector3(currentPositon.x, Mathf.Lerp(transform.position.y, searchResult.projectedPositionWS.y, surfaceStickLerpAmount), currentPositon.z); //
            // OLD WAY --->>>
            //gameObject.transform.position = searchResult.projectedPositionWS;

            //damping fit to surface to filter small waves 
            transform.position = Vector3.SmoothDamp(
                      transform.position,                   // current position
                      searchResult.projectedPositionWS,     // target on-water position
                      ref _dampVelocity,                    // velocity state
                      dampingTime);                         // smoothing time
        }

        //Align the boat to the water 
        if (alignToWaterNormal)
        {
            AlignToWaterNormal(searchResult.normalWS);
        }
    }

    private void RotatePoles(float targetRotation)
    {
        if (poleMasts == null || poleMasts.Length == 0)
            return;

        // Main pole (first in array)
        if (poleMasts[0] != null)
        {
            DOTween.Kill(poleMasts[0]); // Stop any existing tweens
            poleMasts[0].DOLocalRotate(new Vector3(0, targetRotation, 0), poleRotationDuration)
                .SetEase(Ease.OutQuad);
        }

        // Secondary pole (second in array) with reduced rotation
        if (poleMasts.Length > 1 && poleMasts[1] != null)
        {
            float secondaryRotation = targetRotation * (secondaryPoleMaxRotation / mainPoleMaxRotation);
            DOTween.Kill(poleMasts[1]); // Stop any existing tweens
            poleMasts[1].DOLocalRotate(new Vector3(0, secondaryRotation, 0), poleRotationDuration)
                .SetEase(Ease.OutQuad);
        }
    }

    /// <summary>
    /// Get the water surface when spawned 
    /// </summary>
    private void InitializeWaterTarget()
    {
        if (targetSurface == null)
        {
            //Get teh Scenes Water Surface 
            targetSurface = LevelVariableManager.Instance.GlobalWaterSurface; 

            // Log an error if still not found.
            if (targetSurface == null)
            {

                Debug.LogError("WaterSurface component not found. Ensure an object named Ocean or a WaterSurface instance exists in the scene.", this);
            }
        }
    }


    private void HandleThrottle()
    {
        float vertical = moveVector.y;
        targetThrottle = vertical;

        // Smoothing for gradual thrust changes (prevents jerky trailer movement)
        currentThrottle = Mathf.MoveTowards(currentThrottle, targetThrottle, accelerationSmoothing * Time.fixedDeltaTime);

        // Throttle adjusted for direction
        float adjustedForce = throttleForce * currentThrottle * throttleForceMultiplier;
        if (currentThrottle < 0)
            adjustedForce *= reverseThrottleFactor;

        Vector3 force = transform.forward * adjustedForce * Time.fixedDeltaTime;
        rb.AddForce(force, ForceMode.Force);

        // Limit max resistence (for tugboat realism)
        Vector3 flatVel = rb.linearVelocity;
        flatVel.y = 0f;
        if (flatVel.magnitude > maxSpeed)
        {
            Vector3 limited = flatVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(limited.x, rb.linearVelocity.y, limited.z);
        }
    }

    private void HandleTurning()
    {
        float turnInput = moveVector.x;

        // Only allow turn input when there is forward/reverse motion
        if (Mathf.Abs(currentThrottle) > 0.05f)
        {
            float torqueAmount = turnInput * turnTorque * Time.fixedDeltaTime;
            rb.AddTorque(Vector3.up * torqueAmount, ForceMode.Force);
        }
    }

    private void HandleDrag()
    {
        // Apply higher drag if throttle is idle to resist drifting when towing
        if (Mathf.Approximately(currentThrottle, 0f))
            rb.linearDamping = dragWhenNotMoving;
        else
            rb.linearDamping = 0f;
    }


    /// <summary>
    /// method to align the gameobject to the water surface with damping 
    /// </summary>
    /// <param name="waterNormal"></param>
    private void AlignToWaterNormal(float3 waterNormal)
    {
        //Vector3 velocity = (transform.position - previousPosition) / Time.deltaTime;
        //previousPosition = transform.position;


        // Flatten for horizontal velocity
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        float forwardSpeed = localVelocity.z;
        float sideSpeed = localVelocity.x;

        // Simulate pitch (nose up/down from forward acceleration)
        float pitchAngle = -forwardSpeed * pitchAngleMultiplier;

        // Simulate roll (lean into turns / side drift)
        float rollAngle = sideSpeed * rollAngleMultiplier;

        Quaternion targetTilt = Quaternion.Euler(pitchAngle, 0f, rollAngle);

        // Base upright rotation from water normal
        Vector3 forwardProjected = Vector3.ProjectOnPlane(transform.forward, waterNormal).normalized;
        Quaternion waterAligned = Quaternion.LookRotation(forwardProjected, waterNormal);

        // Combine boat tilt + wave normal
        Quaternion targetRotation = waterAligned * targetTilt;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, OrientationSmoothSpeed * Time.deltaTime);

    }

    #region OnCollisionAndScreenShake
    
    private float _lastImpactTime = 0f;
    private const float ImpactCooldown = 0.5f; // seconds
    private const float MinImpactVelocity = 1.5f; // minimum velocity to count as a real hit

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return;

        float impactForce = collision.relativeVelocity.magnitude;

        // Only shake if impact is significant, not just touching, and cooldown elapsed
        if (impactForce > collisionShakeThreshold && impactForce > MinImpactVelocity && Time.time - _lastImpactTime > ImpactCooldown)
        {
            _lastImpactTime = Time.time;
            float shakeIntensity = Mathf.Min(impactForce * collisionShakeMultiplier, 1.3f);
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.PlayerImpact, transform.position);
            GenerateScreenShake(shakeIntensity);
        }
    }
    
    // Method to trigger screen shake
    public void GenerateScreenShake(float intensity)
    {
        if (impulseSource != null && IsOwner)
        {
            impulseSource.GenerateImpulse(intensity);
        }
    }
    
    #endregion



    #region Sounds
        
    private void InitializeSound()
    {
        // Start with idle sound
        if (!isPlayingIdleSound && IsOwner)
        {
            AudioManager.Instance.StartLoop(FMODEvents.Instance.PlayerIdle, gameObject);
            isPlayingIdleSound = true;
            isPlayingMoveSound = false;
        }
    }

    private void UpdateSounds()
    {
        if (!IsOwner) return;

        float speed = rb.linearVelocity.magnitude;
        bool shouldBeMoving = speed > minSpeedForMoving;
        bool isTurning = Mathf.Abs(moveVector.x) > 0.1f && shouldBeMoving;

        // Handle background engine loops with better transitions
        if (shouldBeMoving)
        {
            // Update speed parameter on move sound if already playing
            if (isPlayingMoveSound && moveInstance.isValid())
            {
                float normalizedSpeed = Mathf.Clamp01(speed / maxSpeed);
                moveInstance.setParameterByName("Speed", normalizedSpeed);
            }
            // Start move sound if not already playing
            else if (!isPlayingMoveSound)
            {
                // First stop idle sound with quick fade
                if (isPlayingIdleSound)
                {
                    AudioManager.Instance.FadeOutLoop(0.3f);
                    isPlayingIdleSound = false;
                }

                // Then start move sound
                moveInstance = RuntimeManager.CreateInstance(FMODEvents.Instance.PlayerMove);
                RuntimeManager.AttachInstanceToGameObject(moveInstance, transform);
                moveInstance.start();
                isPlayingMoveSound = true;
            }
        }
        else // Should be idle
        {
            // Handle transition to idle sound
            if (!isPlayingIdleSound)
            {
                // First stop move sound with fade
                if (isPlayingMoveSound && moveInstance.isValid())
                {
                    moveInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    moveInstance.release();
                    isPlayingMoveSound = false;
                }

                // Then start idle sound
                AudioManager.Instance.StartLoop(FMODEvents.Instance.PlayerIdle, gameObject);
                isPlayingIdleSound = true;
            }
        }

        // Handle turning sound logic (no changes needed here)
        if (isTurning && !isPlayingTurningSound)
        {
            turningInstance = RuntimeManager.CreateInstance(FMODEvents.Instance.PlayerTurning);
            RuntimeManager.AttachInstanceToGameObject(turningInstance, transform);
            turningInstance.start();
            isPlayingTurningSound = true;
            turnSoundTimer = 0f;
        }
        else if (!isTurning && isPlayingTurningSound)
        {
            turnSoundTimer += Time.deltaTime;
            if (turnSoundTimer >= turnSoundStopDelay)
            {
                if (turningInstance.isValid())
                {
                    turningInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    turningInstance.release();
                }
                isPlayingTurningSound = false;
            }
        }
        else if (isTurning)
        {
            turnSoundTimer = 0f;
        }
    }

// Update the horn methods in TugboatMovementWFloat.cs
    [ServerRpc]
    private void PlayHornServerRpc()
    {
        // Use the actual client ID from the server's perspective
        PlayHornClientRpc(NetworkObjectId);
    }

    [ClientRpc]
    private void PlayHornClientRpc(ulong boatNetworkId)
    {
        // Find the boat by its network object ID instead of client ID
        NetworkObject boatObject = null;
    
        // If this is our own boat
        if (NetworkObjectId == boatNetworkId)
        {
            boatObject = NetworkObject;
        }
        else
        {
            // Try to find the boat in the scene by network ID
            foreach (NetworkObject networkObj in FindObjectsOfType<NetworkObject>())
            {
                if (networkObj.NetworkObjectId == boatNetworkId)
                {
                    boatObject = networkObj;
                    break;
                }
            }
        }
    
        if (boatObject != null)
        {
            // Create horn instance and attach it to the correct boat
            EventInstance hornInstance = RuntimeManager.CreateInstance(FMODEvents.Instance.PlayerHorn);
            RuntimeManager.AttachInstanceToGameObject(hornInstance, boatObject.transform);
            hornInstance.start();
        
            // Release after the sound would have finished playing
            StartCoroutine(ReleaseAfterPlay(hornInstance, 3.0f));
        }
    }

    private IEnumerator ReleaseAfterPlay(EventInstance instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (instance.isValid())
        {
            instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            instance.release();
        }
    }

    private void PlayHorn()
    {
        // Only owner can trigger horn and only if cooldown elapsed
        if (!IsOwner || hornCooldown > 0) return;
    
        // Set cooldown
        hornCooldown = HORN_COOLDOWN_DURATION;
    
        // Request server to play horn on all clients
        PlayHornServerRpc();
    }

    // Add this to your Update method
    private void UpdateHorn()
    {
        // Update horn cooldown
        if (hornCooldown > 0)
        {
            hornCooldown -= Time.deltaTime;
        }
    }
    
    
    
    #endregion
    
    public override void OnDestroy()
    {
        // Clean up any playing sounds
        if (IsOwner)
        {
            AudioManager.Instance.StopLoop();

            if (isPlayingTurningSound && turningInstance.isValid())
            {
                turningInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                turningInstance.release();
            }
        
            if (isPlayingMoveSound && moveInstance.isValid())
            {
                moveInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                moveInstance.release();
            }
        }
    }

    // private void OnApplicationFocus(bool focus)
    // {
    //     if(SceneManager.GetActiveScene().name != "MainMenu")
    //     Cursor.lockState = CursorLockMode.Locked;
    //     
    // }


   
}
