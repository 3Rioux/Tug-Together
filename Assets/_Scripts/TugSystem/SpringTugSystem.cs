using GogoGaga.OptimizedRopesAndCables;
using System.Collections.Generic;
using TMPro;
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

    private BoatInputActions controls;
    private bool isAttached = false; // allow re-attaching and detattaching

    //Visualise attach points 
    private Transform currentClosestAttachPoint;
    private int currentClosestPointIndex; // store teh index of the closes point !!! dont like this !!!
    private Transform lastClosestAttachPoint;

    // Different material for highlight
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material defaultMaterial;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
       
        // springJoint.autoConfigureConnectedAnchor = false;
        //springJoint.connectedAnchor = ropeAttachPoint.position;
    }

    private void OnEnable()
    {
        controls = new BoatInputActions();
        controls.Boat.Enable();

        // Register event handlers
        controls.Boat.Hook.performed += OnHookTriggered;
    }

    private void OnDisable()
    {
        // Unregister event handlers
        controls.Boat.Hook.performed -= OnHookTriggered;

        // Disable the action map
        controls.Boat.Disable();
    }

    private void Update()
    {
        
        //Visual Rope Length
        distanceToTowedObject = Vector3.Distance(transform.position, towedObject.position);

        //Display the distance between the Player and the Barge (For tweeking sprint settings)
        distanceText.text = distanceToTowedObject.ToString() + " m";


        //if we are close enough to hook show clossest hook point 
        if (towedObject != null && distanceToTowedObject <= maxTowDistance)
        {
            UpdateClosestAttachPoint();

            lineRenderer.enabled = true;

            //draw a line 
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, currentClosestAttachPoint.position);
        }else
        {
            //reset when out of range 
            lineRenderer.enabled = false;
            SetAttachPointHighlight(currentClosestAttachPoint, false);
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
                    currentClosestPointIndex = towedObject.gameObject.GetComponent<TowableObjectController>().TowPointList.IndexOf(closestPoint);
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
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, ropeAttachPoint.position);

        Gizmos.DrawSphere(ropeAttachPoint.position, 0.1f);
        Gizmos.color = Color.cyan;
        if (springJoint != null) Gizmos.DrawSphere(springJoint.connectedAnchor, 5f);
    }

}
