using GogoGaga.OptimizedRopesAndCables;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;


public class SpringTugSystem : NetworkBehaviour
{

    #region Variables
    [SerializeField] private TextMeshProUGUI distanceText;

   
    [SerializeField] private SpringJoint springJoint;
    [SerializeField] private Rope visualRope;
    [SerializeField] private GameObject boatHook;
    public Rope VisualRope { get => visualRope; set => visualRope = value; }

    [Header("Tow Settings")]
    [SerializeField] private Rigidbody towedObject;
    [SerializeField] private Transform towAnchor;
    [SerializeField] private Transform targetAttachPoint;
    [SerializeField] private Transform ropeAttachPoint;
    [SerializeField] private float springAmount = 35f;
    [SerializeField] private float springMaxDistance = 5f;
    [SerializeField] private float maxTowDistance = 25f;
    [SerializeField] private float distanceToTowedObject;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Aim Settings ")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraFollowPoint;
    [SerializeField] private Transform cameraAimFollowPoint;
    [SerializeField] private LayerMask aimColliderLayerMask = new LayerMask();//the layer the player will hit (i want to ignore the player layer) (Cant hit itself with a ray)
    [SerializeField] private bool isAimMode = false; // bool to stop the auto target closest hook code
    [SerializeField] private List<Transform> visibleHooks = new List<Transform>();
    [SerializeField] private List<Transform> hookPoints;
    [SerializeField] private float hookSwitchCooldown = 1f; // Cooldown in seconds
    [SerializeField] private CinemachineCamera aimCamera; // aim Camera 

    //Stuff To Disable when Aiming:
    //[SerializeField] private GameObject sails;
    //[SerializeField] private GameObject flags;
    //[SerializeField] private GameObject face; // all good i hand the LayerMask as max distance lol

    [Tooltip("How far in degrees can you move the aim camera up")]
    [SerializeField] private float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the aim camera down")]
    [SerializeField] private float BottomClamp = -30.0f;

    [Tooltip("Additional degress to override the aim camera. Useful for fine tuning camera position when locked")]
    [SerializeField] private float CameraAngleOverride = 0.0f;

    public float Sensitivity = 1.0f;//----------------reduce the mouse sensitivity when aiming--------------------------------------------------------------------------------------
    private const float _threshold = 0.01f;
    private float yaw;
    private float pitch;
    private float lastHookSwitchTime = 0f; // Time when the last switch happened
    private Vector2 lookVector;

    private BoatInputActions controls;
    private bool isAttached = false; // allow re-attaching and detattaching

    //Visualise attach points 
    private Transform currentClosestAttachPoint;
    public int selectedHookIndex; // store teh index of the closes point !!! dont like this !!!
    private Transform lastClosestAttachPoint;

    // Different material for highlight
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material defaultMaterial;



    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return controls.Boat.Look.activeControl.Equals("KeyboardMouse");
#else
				return false;
#endif
        }
    }
#endregion


    private void Awake()
    {
        // Make sure the towedObject rigidbody is properly set up
        if (towedObject != null)
        {
            // Freeze rotation on X and Z axes, allowing only Y rotation
            towedObject.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        if(boatHook != null)
        {
            boatHook.SetActive(true); // make sure it active 
        }
    }


    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();

        //visualRope.gameObject.GetComponent<NetworkObject>().Spawn();
        visualRope.gameObject.SetActive(false); //needs to create the network object before being deactivated 

        if (towedObject == null)
        {
            towedObject = JR_NetBoatRequiredComponentsSource.Instance.GlobalBargeRigidBody;
            hookPoints = towedObject.GetComponent<TowableObjectController>().TowPointList;
        }

        // springJoint.autoConfigureConnectedAnchor = false;
        //springJoint.connectedAnchor = ropeAttachPoint.position;
    }

    private void OnEnable()
    {
        //if (!IsOwner) return;

        controls = new BoatInputActions();
        controls.Boat.Enable();

        // Register event handlers
        controls.Boat.Hook.performed += OnHookTriggered;
        controls.Boat.AimHook.performed += OnAimTriggered;
        controls.Boat.AimHook.performed += ctx => isAimMode = true; //set to true when pressed 
        controls.Boat.AimHook.canceled += ctx => isAimMode = false; // set to false when cancelled 

        //cam look 
        controls.Boat.Look.performed += ctx => lookVector = ctx.ReadValue<Vector2>();
        controls.Boat.Look.canceled += _ => lookVector = Vector2.zero; // remove this to enable camera drift 
        //}else
        //{
        //    // Unregister event handlers
        //    //controls.Boat.Hook.performed -= OnHookTriggered;
        //    //controls.Boat.AimHook.performed -= OnHookTriggered;

        //    lookVector = Vector2.zero;
        //}


        //set aim cam to off by default 
        aimCamera.gameObject.SetActive(false);// off by default 
        isAimMode = false;

        if (towedObject == null)
        {
            towedObject = JR_NetBoatRequiredComponentsSource.Instance.GlobalBargeRigidBody;
            hookPoints = towedObject.GetComponent<TowableObjectController>().TowPointList;
        }
    }

    private void OnDisable()
    {
        //if (!IsOwner) return;
        // Unregister event handlers
        controls.Boat.Hook.performed -= OnHookTriggered;
        controls.Boat.AimHook.performed -= OnHookTriggered;

        // Disable the action map
        controls.Boat.Disable();
    }

    private void Update()
    {
        // *** Always update this even if its just a copy on another client ***
        //Only update the visual rope length if we are attached
        //if (isAttached)
        //{
        //    //set ropeLength to max lenght to simulate tention in the rope.
        //    // * if not here it will make the rope length == the the distance between the boat & cargo when attached *

        //    //Check if we are passed the max tow distance 
        //    if (distanceToTowedObject >= springMaxDistance)
        //    {
        //        visualRope.ropeLength = springMaxDistance;
        //    }
        //    else
        //    {
        //       // visualRope.ropeLength = distanceToTowedObject;
        //    }
        //}
        // *** Always update this even if its just a copy on another client ***

        if (towedObject == null || !IsOwner)
        {
           
            //Debug.LogError("towedObject is not assigned. Please assign it in the inspector.");
            return;
        }
        
        
        //Visual Rope Length
        distanceToTowedObject = Vector3.Distance(transform.position, towedObject.position);

        //Display the distance between the Player and the Barge (For tweeking sprint settings)
        if (distanceText != null) distanceText.text = distanceToTowedObject.ToString() + " m";

        // ====== 
        // ===Hooking Mechanic Start=== 
        // ====== 

        // ---Aim Mode ---
        //if (isAimMode && visibleHooks.Count > 0) //<-- Only allow aim if hookpoints are in view 
        Vector3 cameraForward = Camera.main.transform.forward;
        //cameraForward.y = 0f;
        //cameraForward.Normalize();

        //Quaternion newRot = Quaternion.LookRotation(cameraForward);

        //cameraAimFollowPoint.rotation = Quaternion.LookRotation(cameraForward); // instant snap
        cameraAimFollowPoint.forward = Camera.main.transform.forward;

        cameraAimFollowPoint.rotation = Quaternion.LookRotation(cameraForward); // instant snap

        if (isAimMode)
        {//can always aim 

           
            // turn on aim cam when aim button pressed
            aimCamera.gameObject.SetActive(true);

            // Camera snap to look direction        
            //Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
            //Vector3 worldAimTarget = new Vector3(Screen.width / 2f, 0, Screen.height / 2f);
            //Vector3 worldAimTarget = aimCamera.gameObject.transform.forward;
            //worldAimTarget.y = targetAttachPoint.position.y; // 
            //Vector3 aimDirection = (worldAimTarget - transform.position).normalized;

           
            ////rotate the player
            //targetAttachPoint.forward = Camera.main.transform.position;//  aimDirection - Camera.main.transform.position;// Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 20.0f);
            

            if (distanceToTowedObject <= maxTowDistance)
            {
                //Logic to determine the closest hook to the camera center 
                FindVisibleHooks();

                //lock to camera to the hooks and when the player moves they mouse it will lock onto the next hook in that direction 
                SelectClosestVisibleHook();


                if (!isAttached && visibleHooks.Count > 0)
                {
                    lineRenderer.enabled = true;
                    //draw a line 
                    lineRenderer.SetPosition(0, visualRope.StartPoint.position);
                    lineRenderer.SetPosition(1, currentClosestAttachPoint.position);
                }
                else
                {
                    //reset attached
                    lineRenderer.enabled = false;
                }

                //Switch aim hook after cooldown
                if (lookVector.magnitude > 2f && Time.time >= lastHookSwitchTime + hookSwitchCooldown)
                {
                    // Example: if mouse moved right, select next hook
                    if (lookVector.x > 2f)
                    {
                        SelectNextHook(1);
                        lastHookSwitchTime = Time.time; // Reset cooldown
                    }
                    else if (lookVector.x < -2f)
                    {
                        SelectNextHook(-1);
                        lastHookSwitchTime = Time.time; // Reset cooldown
                    }
                }

               

            }//end if close enough to hook 
            

        }//end if aimMode 
        else
        {
            aimCamera.gameObject.SetActive(false);// turn off aim when not aiming  
        }

        //Auto Mode (Only active while close)
        if (distanceToTowedObject <= maxTowDistance) 
        {

            
            if (towedObject != null && !isAimMode)
            { //if we are close enough to hook show clossest hook point && that we are NOT in Aim mode 
                UpdateClosestAttachPoint();

               
                aimCamera.gameObject.SetActive(false);// turn off aim when not aiming  

                //only draw the line if not already attached
                if (!isAttached)
                {
                    lineRenderer.enabled = true;
                    //draw a line 
                    lineRenderer.SetPosition(0, visualRope.StartPoint.position);
                    lineRenderer.SetPosition(1, currentClosestAttachPoint.position);
                }
                else
                {
                    //reset attached
                    lineRenderer.enabled = false;
                }

            }

        }
        else
        {
            //reset when out of range 
            lineRenderer.enabled = false;
            //add check for the start of the game whenre we dont have a hook point 
            if (currentClosestAttachPoint != null)
            {
                SetAttachPointHighlight(currentClosestAttachPoint, false);
            }
            //aimCamera.gameObject.SetActive(false);// turn off aim when out of range  aiming  
        }


        // ====== 
        // ===Hooking Mechanic End=== 
        // ====== 


    }//end update 

    private void LateUpdate()
    {
        //if (!IsOwner)
        //{
        //    return;
        //}
        //if (towedObject == null)
        //{
        //    towedObject = JR_NetBoatRequiredComponentsSource.Instance.GlobalBargeRigidBody;
        //    hookPoints = towedObject.GetComponent<TowableObjectController>().TowPointList;
        //}

        CameraRotation();
    }
    
    private void FixedUpdate()
    {
        // Only apply if we're attached and towing
        if (IsHost && isAttached && towedObject != null)
        {
            // Get the current rotation
            Quaternion currentRotation = towedObject.transform.rotation;
        
            // Create a corrected rotation that only preserves Y rotation
            Vector3 eulerAngles = currentRotation.eulerAngles;
            Quaternion targetRotation = Quaternion.Euler(0, eulerAngles.y, 0);
        
            // Apply the corrected rotation
            towedObject.MoveRotation(targetRotation);

            // Alternative approach - reset angular velocity on unwanted axes
            Vector3 angularVelocity = towedObject.angularVelocity;
            towedObject.angularVelocity = new Vector3(0, angularVelocity.y, 0);
        }
    }

   


    #region AimCameraMotion

    private void CameraRotation()
    {
        // if there is an input
        if (lookVector.sqrMagnitude >= _threshold)
        {
            //Don't multiply mouse input by Time.deltaTime;
            //float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
            float deltaTimeMultiplier = Time.deltaTime;

            yaw += lookVector.x * deltaTimeMultiplier * Sensitivity;
            pitch += -lookVector.y * deltaTimeMultiplier * Sensitivity;
        }

        // clamp our rotations so our values are limited 360 degrees
        yaw = ClampAngle(yaw, float.MinValue, float.MaxValue);
        pitch = ClampAngle(pitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        cameraAimFollowPoint.transform.rotation = Quaternion.Euler(pitch + CameraAngleOverride,
            yaw, 0.0f);

    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    #endregion


    #region AimMethods

    /// <summary>
    /// Event/Action triggered method to switch to the aim camera.
    /// Now -> just toggles the aim camera + runs an initial visible hook check 
    /// Used to -> aim the camera towards the closest hookable object and allow the user to aim at the Hook they want to connect to 
    /// </summary>
    /// <param name="context"></param>
    private void OnAimTriggered(InputAction.CallbackContext context)
    {
        //check if we are close enough to hook to the barge 
        if (distanceToTowedObject > maxTowDistance)
        {
            //aimCamera.enabled = false;
            isAimMode = false; //jsut to be safe 
            return;
        }
     

        isAimMode = true;

        //toggle the Aim Camera:
        aimCamera.gameObject.SetActive(true);// turn off aim when not aiming  

        //Logic to determine the closest hook to the camera center 
        FindVisibleHooks();

        //lock to camera to the hooks and when the player moves they mouse it will lock onto the next hook in that direction 
        SelectClosestVisibleHook();
    }



    /// <summary>
    /// Finds and saves the hooks the player can "see" thus hook onto. Saves into List visibleHooks that is used by the Aim. (For now)
    /// </summary>
    private void FindVisibleHooks()
    {
        visibleHooks.Clear();

        // Use the provided camera, or fall back to Camera.main if none set.
        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        Vector3 camPosition = cam.transform.position;

        foreach (Transform hook in towedObject.GetComponent<TowableObjectController>().TowPointList)
        {
            // Convert the hookpoint's position to viewport space.
            Vector3 viewportPos = cam.WorldToViewportPoint(hook.position);

            // Check if the hookpoint is in front of the camera and inside the viewport.
            bool inViewport = viewportPos.z > 0 &&
                              viewportPos.x > 0 && viewportPos.x < 1 &&
                              viewportPos.y > 0 && viewportPos.y < 1;

            if (!inViewport)
                continue;

            // Perform a raycast from the camera to the hookpoint.
            //Vector3 direction = hook.position - inViewRayPointOrigin.position;
            Vector3 direction = hook.position - camPosition;
            RaycastHit hit;

            // Draw the ray in red with a duration of 0.5 seconds.
            //Debug.DrawRay(camPosition, direction, Color.red, 1f);

            // if(Physics.Linecast(camPosition, direction, out hit))
            if (Physics.Raycast(camPosition, direction, out hit, maxTowDistance + 5f, aimColliderLayerMask))
            {
                Debug.DrawRay(camPosition, direction, Color.magenta, 1f);
                if (hit.collider != null)
                {
                    if(hit.collider.gameObject.CompareTag("AttachPoint"))
                    {
                        // Draw the ray in red with a duration of 0.5 seconds.
                        Debug.DrawRay(camPosition, direction, Color.green, 1f);
                        visibleHooks.Add(hook);
                    }else
                    {
                        Debug.Log($"Failled collision = {hit.collider.tag}");
                        Debug.DrawRay(camPosition, direction, Color.red, 1f);
                    }
                }else
                {
                    Debug.Log($"Failled collision = {hit.collider.tag}");
                }
            }
        }//end foreach

    }

    /// <summary>
    /// Changes the currently selected hook point based on if its in view of the player & 
    /// the proximity to the center of the screen.
    /// </summary>
    private void SelectClosestVisibleHook()
    {
        if (visibleHooks.Count == 0)
        {
            //isAimMode = false; i can now allways aim 
            return;
        }

        // Use the provided camera, or fall back to Camera.main if none set.
        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        Vector3 camPosition = cam.transform.position;
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);

        float closestDistance = Mathf.Infinity;
        Transform closestHook = null;

        foreach (Transform hook in visibleHooks)
        {
            Vector3 viewportPos = cam.WorldToViewportPoint(hook.position);// get teh position of the hook 
            Vector2 viewport2D = new Vector2(viewportPos.x, viewportPos.y);

            float distance = Vector2.Distance(screenCenter, viewport2D);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestHook = hook;
            }
        }

        currentClosestAttachPoint = closestHook;
        HighlightSelectedHook();
    }


    /// <summary>
    /// Changes the current aimed at hook point to the next one in the visible hook points list when the player moves the mouse. (Depreciated instead use SelectClosestVisibleHook())
    /// (This method was used when the aim was hard locking to the hook points) (Depreciated but keeping incase we give the setting to turn this back on)
    /// </summary>
    /// <param name="direction"></param>
    private void SelectNextHook(int direction)
    {
        if (visibleHooks.Count == 0)
            return;

        selectedHookIndex += direction;

        if (selectedHookIndex >= visibleHooks.Count)
            selectedHookIndex = 0;
        if (selectedHookIndex < 0)
            selectedHookIndex = visibleHooks.Count - 1;

        currentClosestAttachPoint = visibleHooks[selectedHookIndex];
        //aimCamera.LookAt = visibleHooks[selectedHookIndex].gameObject.transform;

        HighlightSelectedHook();
    }

    #endregion

   
    /// <summary>
    /// sets the color of all hook point by looping through them. (
    /// If the hook == the Current selected hookpoint on the towable object (barge) 
    /// change green else change to orange.
    /// </summary>
    private void HighlightSelectedHook()
    {
        //foreach (Transform hook in visibleHooks) // <-- had an error where if the selected hook point became out of view before changing the point in would remain green
        foreach (Transform hook in hookPoints)
        {
            SetAttachPointHighlight(hook, hook == currentClosestAttachPoint);
        }
    }

    #region RPCMethods

    //=================================================================================================================================== Make Network SpringJoint Work

    // Called on the client to request a joint
    void RequestSpringJoint(GameObject player, GameObject targetObject)
    {
        var playerNetObj = player.GetComponent<NetworkObject>();
        var targetNetObj = targetObject.GetComponent<NetworkObject>();
        AttachSpringJointServerRpc(playerNetObj, targetNetObj);
    }

    [ServerRpc]
    void AttachSpringJointServerRpc(NetworkObjectReference playerRef, NetworkObjectReference targetRef)
    {
        // Resolve references on the server
        if (playerRef.TryGet(out NetworkObject playerNet) && targetRef.TryGet(out NetworkObject targetNet))
        {
            print("Attaching joint");
            var playerBody = playerNet.GetComponent<Rigidbody>();
            //var spring = playerNet.GetComponent<SpringJoint>() ?? playerNet.gameObject.AddComponent<SpringJoint>();
            var spring = playerNet.gameObject.AddComponent<SpringJoint>();
            spring.connectedBody = targetNet.GetComponent<Rigidbody>();
            // Configure spring settings as needed...
            //set Spring properties 
            spring.connectedBody = towedObject;
            spring.spring = springAmount;
            spring.damper = 1;
            spring.maxDistance = springMaxDistance;
        }
    }

    //Detach the joint!
    void RequestDetach(GameObject player)
    {
        var playerNetObj = player.GetComponent<NetworkObject>();

        DetachSpringJointServerRpc(playerNetObj);
    }

    [ServerRpc]
    void DetachSpringJointServerRpc(NetworkObjectReference playerRef)
    {
        // Resolve references on the server
        if (playerRef.TryGet(out NetworkObject playerNet))
        {
            print("Detach joint on server");
            var playerBody = playerNet.GetComponent<Rigidbody>();
            //var spring = playerNet.GetComponent<SpringJoint>() ?? playerNet.gameObject.AddComponent<SpringJoint>();
            var spring = playerNet.GetComponent<SpringJoint>();

            //Destroy the spring on the server copy 
            Destroy(spring);
        }
    }

    //=================================================================================================================================== Make Network SpringJoint Work

    //===================================================START SHOW ROPE ON ALL CLIENT INSTANCES================================================================================

    [ServerRpc]
    public void AttachRopeServerRpc(NetworkObjectReference ropeOwnerRef, int attachPositionIndex)
    {
        ShowRopeClientRpc(ropeOwnerRef, attachPositionIndex);
    }

    //Make the Rope Spawn on all clients instances of this players game object: 
    [ClientRpc]
    void ShowRopeClientRpc(ulong clientId, int attachPositionIndex)
    {
        var playerNet = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        SpringTugSystem pTugSystem = playerNet.gameObject.GetComponent<SpringTugSystem>();

        var towPoints = towedObject.gameObject.GetComponent<TowableObjectController>().TowPointList;

       
        //get the tow point at the given index 
        pTugSystem.VisualRope.EndPoint = towPoints[attachPositionIndex];
        pTugSystem.VisualRope.gameObject.SetActive(true);
    }

    [ClientRpc]
    void ShowRopeClientRpc(NetworkObjectReference ropeOwnerRef, int attachPositionIndex)
    {
        Debug.Log($"==={ropeOwnerRef}=== Attach Rope Client");

        if (ropeOwnerRef.TryGet(out NetworkObject playerNet))
        {
            SpringTugSystem pTugSystem = playerNet.GetComponent<SpringTugSystem>();

            var towPoints = towedObject.GetComponent<TowableObjectController>().TowPointList;

            // Set endpoint and activate the rope
            pTugSystem.VisualRope.EndPoint = towPoints[attachPositionIndex];
            pTugSystem.VisualRope.gameObject.SetActive(true);
        }
    }

    //===================================================END SHOW ROPE ON ALL CLIENT INSTANCES================================================================================

    #endregion

    #region Hook-Attach/Detach

    /// <summary>
    /// Method triggered my Hook Action ("R") that will attach and deattach the player from the barge 
    /// </summary>
    /// <param name="context"></param>
    private void OnHookTriggered(InputAction.CallbackContext context)
    {
        Debug.Log($"===Hook Triggered  isAttached == {isAttached}===");
        if (isAttached)
        {
            Detach();
        }
        else
        {
            Attach();
        }
    }



    /// <summary>
    /// Will add a Spring joint to the boat and connect it to the target object(Barge) 
    /// as well as connect the rope end point to the closest attachpoint on the target object
    /// and display the rope 
    /// </summary>
    public void Attach()
    {
        //Check everything needed is properly set
        if (towedObject == null || currentClosestAttachPoint == null)
            return;

        //if there is still a spring joint attached for some reason then remove it 
        if (springJoint != null)
            Destroy(springJoint);

        //Check if the boat is close enough to attach to the towedObject:
        if (distanceToTowedObject <= maxTowDistance)
        {

            //Find closest attach point 
            //targetAttachPoint = FindClosestAttachPoint();
            targetAttachPoint = currentClosestAttachPoint;

            if (targetAttachPoint == null)
            {
                Debug.LogWarning("No attach point found on the towed object!");
                return;
            }

            ////Move the rope endPoint to the attachpoint 
            //visualRope.EndPoint = targetAttachPoint;

            //// display the rope:
            //visualRope.gameObject.SetActive(true);

            // StartCoroutine("AttachRopeEffect");
            AttachRopeWEffect();

             var towPoints = towedObject.gameObject.GetComponent<TowableObjectController>().TowPointList;

            int attachPointIndex = towPoints.IndexOf(targetAttachPoint);


            //AttachRopeServerRpc(OwnerClientId, index);
            AttachRopeServerRpc(NetworkObject, attachPointIndex);


            //only the host can derectly attach the spring joint 
            if (!IsServer)
            {
                //request to attach a joint on the server side copy of the client game object 
                RequestSpringJoint(this.gameObject, towedObject.gameObject);
            }

            //ALWAYS create a spring locally as well:

            //Create and attach the Spring Joint 
            springJoint = gameObject.AddComponent<SpringJoint>(); // attach a spring joint to this gameobject plus set it as springJoint

            //set Spring properties 
            springJoint.connectedBody = towedObject;
            springJoint.spring = springAmount;
            springJoint.damper = 1;
            springJoint.maxDistance = springMaxDistance;

            

            //Set attached state 
            isAttached = true;

        }else
        {
            // attach failled VFX + SFX due to boat being to far from target 
            Debug.LogWarning("Attach failed: too far from target.");
        }
    }

    /// <summary>
    /// Will Detach the boat from the barge by removing the Spring joint and hidding the rope.
    /// </summary>
    public void Detach()
    {
        if (springJoint != null)
        {
            //Disconnect local Joint
            Destroy(springJoint);

            //Hide Rope Mesh:
            //visualRope.gameObject.GetComponent<RopeMesh>().enabled = false;
            //or just the gameobject itself 
            //visualRope.ropeLength = 5f; // this should create a cool effect 
            //visualRope.gameObject.SetActive(false);


            DetatchRopeWEffect();



            //Make sure the Spring joint is destroyed on the Server side copy of the player as well 
            if (!IsServer)
            {
                //request to attach a joint on the server side copy of the client game object 
                RequestDetach(this.gameObject);
            }

        }

        isAttached = false; // no longer attached 
    }


    private void AttachRopeWEffect()
    {
        visualRope.gameObject.SetActive(true);

        visualRope.EndPoint = boatHook.transform; // this should create a cool effect when next attaching 

        //Move the rope endPoint to the attachpoint 
        visualRope.EndPoint = targetAttachPoint;

        visualRope.ropeLength = springMaxDistance; // this should create a cool effect when next attaching 
       

       //wait for a second before disableing the hook?

        boatHook.SetActive(false); // make sure it off when hooked in 
    }


    private void DetatchRopeWEffect()
    {
        visualRope.ropeLength = springMaxDistance; // this should create a cool effect when next attaching 
        visualRope.EndPoint = boatHook.transform; // this should create a cool effect when next attaching 
        visualRope.gameObject.SetActive(false);

        boatHook.SetActive(true); // make sure it active 
    }

    #endregion

    #region AutoHookConnection

    /// <summary>
    /// Finds the closest attach point (Transform) on the towed object.
    /// Assumes that attach points are children with a specific tag or are grouped.
    /// </summary>
    private Transform FindClosestAttachPoint()
    {
        Transform closestPoint = null;
        float closestDistance = Mathf.Infinity;

        var towPoints = towedObject.gameObject.GetComponent<TowableObjectController>().TowPointList;

        //foreach (Transform towPoint in towedObject.gameObject.GetComponent<TowableObjectController>().TowPointList)
        foreach (Transform towPoint in towPoints)
        {
            if (towPoint.CompareTag("AttachPoint"))
            {
                //Find Distance 
                float distance = Vector3.Distance(transform.position, towPoint.position);
                //set closest if closer 
                if (distance < closestDistance)
                {  
                    closestDistance = distance;
                    closestPoint = towPoint;
                    selectedHookIndex = towedObject.gameObject.GetComponent<TowableObjectController>().TowPointList.IndexOf(closestPoint);
                }
            }
        }

        return closestPoint;
    }

    /// <summary>
    /// will set the current closes tow point and reset old closest point 
    /// </summary>
    private void UpdateClosestAttachPoint()
    {
        if (towedObject != null)
        {
            Transform closest = FindClosestAttachPoint();

            if (closest != currentClosestAttachPoint)
            {
                // Unhighlight last one
                if (currentClosestAttachPoint != null)
                {
                    SetAttachPointHighlight(currentClosestAttachPoint, false);
                }

                // Highlight new one
                if (closest != null)
                {
                    SetAttachPointHighlight(closest, true);
                }

                currentClosestAttachPoint = closest;
            }
        }
    }

    /// <summary>
    /// Changes the material of the attachpoint base on bool 
    /// </summary>
    /// <param name="attachPoint"></param>
    /// <param name="highlight"></param>
    private void SetAttachPointHighlight(Transform attachPoint, bool highlight)
    {
        // Assume the attach points have a MeshRenderer
        MeshRenderer renderer = attachPoint.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = highlight ? highlightMaterial : defaultMaterial;
        }
    }

    #endregion

    #region Debuging

    private void OnDrawGizmos()
    {
        if (springJoint != null)
        {
            Gizmos.color = Color.black;
            //Gizmos.DrawSphere(transform.position, maxTowDistance);

            //foreach (Transform hook in towedObject.GetComponent<TowableObjectController>().TowPointList)
            //{              
            //    Gizmos.DrawLine(transform.position, hook.position);
            //}

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, currentClosestAttachPoint.position);


            Gizmos.DrawSphere(currentClosestAttachPoint.position, 0.1f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(springJoint.connectedAnchor, 5f);


        }
    }

    #endregion

}
