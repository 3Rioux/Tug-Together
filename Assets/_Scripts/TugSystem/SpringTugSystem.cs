using GogoGaga.OptimizedRopesAndCables;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;


public class SpringTugSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI distanceText;


    [SerializeField] private SpringJoint springJoint;
    [SerializeField] private Rope visualRope;

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
    [SerializeField] private bool isAimMode = false; // bool to stop the auto target closest hook code
    [SerializeField] private List<Transform> visibleHooks = new List<Transform>();
    [SerializeField] private float hookSwitchCooldown = 1f; // Cooldown in seconds
    [SerializeField] private CinemachineCamera aimCamera; // aim Camera 
   // [SerializeField] private CinemachineCamera followCamera; // normal Camera (Dont need to disable I just set teh AIm cam priority to be 1 higher than follow cam)
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


    

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (aimCamera != null)
        {
            aimCamera.gameObject.SetActive(false);// off by default 
        }

        // springJoint.autoConfigureConnectedAnchor = false;
        //springJoint.connectedAnchor = ropeAttachPoint.position;
    }

    private void OnEnable()
    {
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
    }

    private void OnDisable()
    {
        // Unregister event handlers
        controls.Boat.Hook.performed -= OnHookTriggered;
        controls.Boat.AimHook.performed -= OnHookTriggered;

        // Disable the action map
        controls.Boat.Disable();
    }

    private void Update()
    {
        if (towedObject == null)
        {
            Debug.LogError("towedObject is not assigned. Please assign it in the inspector.");
            return;
        }
        
        
        //Visual Rope Length
        distanceToTowedObject = Vector3.Distance(transform.position, towedObject.position);

        //Display the distance between the Player and the Barge (For tweeking sprint settings)
        distanceText.text = distanceToTowedObject.ToString() + " m";

        // ===Hooking Mechanic=== 
        if (distanceToTowedObject <= maxTowDistance) {

            //Aim Mode 
            if (isAimMode && visibleHooks.Count > 0)
            {
                //Logic to determine the closest hook to the camera center 
                FindVisibleHooks();

                //lock to camera to the hooks and when the player moves they mouse it will lock onto the next hook in that direction 
                SelectClosestVisibleHook();

                aimCamera.gameObject.SetActive(true);// turn on aim cam if aiming 

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
            }
            else if (towedObject != null)
            { //if we are close enough to hook show clossest hook point && that we are NOT in Aim mode 
                UpdateClosestAttachPoint();

                lineRenderer.enabled = true;
                aimCamera.gameObject.SetActive(false);// turn off aim when not aiming  

            }

            //draw a line 
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, currentClosestAttachPoint.position);

        }
        else
        {
            //reset when out of range 
            lineRenderer.enabled = false;
            SetAttachPointHighlight(currentClosestAttachPoint, false);
            aimCamera.gameObject.SetActive(false);// turn off aim when out of range  aiming  
        }


        

        //Only update the rope length if we are attached 
        if (isAttached)
        {
            if (distanceToTowedObject >= springJoint.maxDistance)
            {
                //set to max lenght to simulate tention 
                visualRope.ropeLength = springJoint.maxDistance;
            }
            else
            {
                visualRope.ropeLength = distanceToTowedObject;
            }
        }

        


    }

    /// <summary>
    /// Method triggered my Hook Action ("R") that will attach and deattach the player from the barge 
    /// </summary>
    /// <param name="context"></param>
    private void OnHookTriggered(InputAction.CallbackContext context)
    {
        if (isAttached)
        {
            Detach();
        }else
        {
            Attach();
        }
    }


    /// <summary>
    /// Method to aim the camera towards the hookable object and allow the user to aim at the Hook they want to connect to 
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

    private void FindVisibleHooks()
    {
        visibleHooks.Clear();

        Camera mainCam = Camera.main;

        foreach (Transform hook in towedObject.GetComponent<TowableObjectController>().TowPointList)
        {
            if (hook.CompareTag("AttachPoint"))
            {
                Vector3 viewportPos = mainCam.WorldToViewportPoint(hook.position);

                bool isVisible = viewportPos.z > 0 &&
                                 viewportPos.x > 0 && viewportPos.x < 1 &&
                                 viewportPos.y > 0 && viewportPos.y < 1;

                if (isVisible)
                {
                    visibleHooks.Add(hook);
                }
            }
        }
    }

    

    private void SelectClosestVisibleHook()
    {
        if (visibleHooks.Count == 0)
        {
            isAimMode = false;
            return;
        }

        Camera mainCam = Camera.main;
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);

        float closestDistance = Mathf.Infinity;
        Transform closestHook = null;

        foreach (Transform hook in visibleHooks)
        {
            Vector3 viewportPos = mainCam.WorldToViewportPoint(hook.position);// get teh position of the hook 
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

        // Now lock camera to point at selected hook
        //aimCamera.LookAt = currentClosestAttachPoint;
    }

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
        aimCamera.LookAt = visibleHooks[selectedHookIndex].gameObject.transform;

        HighlightSelectedHook();
    }

    private void HighlightSelectedHook()
    {
        foreach (Transform hook in visibleHooks)
        {
            SetAttachPointHighlight(hook, hook == currentClosestAttachPoint);
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
        if (towedObject == null || targetAttachPoint == null)
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

            //Move the rope endPoint to the attachpoint 
            visualRope.EndPoint = targetAttachPoint;

            // display the rope:
            visualRope.gameObject.SetActive(true);

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
            //Disconnect Joint
            Destroy(springJoint);

            //Hide Rope Mesh:
            //visualRope.gameObject.GetComponent<RopeMesh>().enabled = false;
            //or just the gameobject itself 
            visualRope.gameObject.SetActive(false);

           
        }

        isAttached = false; // no longer attached 
    }


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


    private void OnDrawGizmos()
    {
        if (springJoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, currentClosestAttachPoint.position);

            Gizmos.DrawSphere(currentClosestAttachPoint.position, 0.1f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(springJoint.connectedAnchor, 5f);
        }
    }

}
