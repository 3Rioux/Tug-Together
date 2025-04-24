using UnityEngine;

[RequireComponent(typeof(FloatingObject))]
public class BoatController : MonoBehaviour
{
    [Header("Boat Balancing")]
    [SerializeField] private Vector3 centerOfMass;          //Store the center of mass for the boat 

    [Space (15)]
    [Header("Boat Movement")]
    [SerializeField] private float maxSpeed = 10.0f;            //the boats current Speed 
    [SerializeField] private float speed = 10.0f;               //the boats current Speed 
    [SerializeField] private float steerSpeed = 10.0f;          //the boats
    [SerializeField] private float movementThreshold = 5f;      // max boat lean forward /backwards 
    [SerializeField] private float steeringThreshold = 2.5f;    // max resistence 


    private Rigidbody _rigidbody;                               // this rigid boady
    private Transform m_centerMass;                             // current calculated center of mass 

    //movement
    private Vector2 inputVector = Vector2.zero;
    private float verticalInput;
    private float movementFactor;

    private float horizontalInput;
    private float steerFactor;


    //Input:
    private BoatInputActions controls;


    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        controls = new BoatInputActions();
        controls.Boat.Move.performed += ctx => inputVector = ctx.ReadValue<Vector2>();
        controls.Boat.Move.canceled += _ => inputVector = Vector2.zero;
    }


    private void Update()
    {
        Balance();
        Movement();
        Steer();
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();


    /// <summary>
    /// Method to balance the boat based on center of mass and boat position movement
    /// </summary>
    private void Balance()
    {
        ////check if m_centerMass exist
        //if (!m_centerMass)
        //{
        //    m_centerMass = new GameObject("CenterOfMass").transform;

        //    //set the parent of this New GObj to this scripts GObj
        //    m_centerMass.SetParent(transform);
        //}

        //// calculate the position of the boats center of mass 
        //m_centerMass.position = centerOfMass;// + transform.position;

        ////Set the boats rigid body center of mass 
        //_rigidbody.centerOfMass = m_centerMass.position;

    }//end Balance


    /// <summary>
    /// 
    /// </summary>
    private void Movement()
    {
        verticalInput = inputVector.y;
        //Debug.Log("Vertical Movement: " + verticalInput);
        movementFactor = Mathf.Lerp(movementFactor, verticalInput, Time.deltaTime / movementThreshold);

        //Move towards the movementFactor * resistence 
        transform.Translate(0, 0, movementFactor * speed);
        //_rigidbody.MovePosition(new Vector3(0, 0, movementFactor * resistence));

        // Clamp max resistence
        if (_rigidbody.linearVelocity.magnitude > maxSpeed)
            _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * maxSpeed;

    }//end Movement 



    private void Steer()
    {
        horizontalInput = inputVector.x;
        //steerFactor = Mathf.Lerp(steerFactor, horizontalInput, Time.deltaTime / movementThreshold); // cant move & steer with this 
        steerFactor = Mathf.Lerp(steerFactor, horizontalInput * verticalInput, Time.deltaTime / steeringThreshold);

        transform.Rotate(0, steerFactor * steerSpeed, 0);
    }
}
