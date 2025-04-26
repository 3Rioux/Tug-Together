using GogoGaga.OptimizedRopesAndCables;
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

    private BoatInputActions controls;
    private bool isAttached = false; // allow re-attaching and detattaching

    private void Start()
    {
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
        float distance = Vector3.Distance(transform.position, ropeAttachPoint.position);

        //Only update the rope length if we are attached 
        if (isAttached)
        {
            if (distance >= springJoint.maxDistance)
            {
                //set to max lenght to simulate tention 
                visualRope.ropeLength = springJoint.maxDistance;
            }
            else
            {
                visualRope.ropeLength = distance;
            }
        }

        //Display the distance between the Player and the Barge (For tweeking sprint settings)
        distanceText.text = distance.ToString()+ " m";
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

        //Find closest attach point 

        //Move the rope endPoint to the attachpoint 

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



        private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, ropeAttachPoint.position);

        Gizmos.DrawSphere(ropeAttachPoint.position, 0.1f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(springJoint.connectedAnchor, 5f);
    }

}
