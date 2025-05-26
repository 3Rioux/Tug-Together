using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
public class CustomFitToWaterSurface : MonoBehaviour
{
    [SerializeField] private float3 floatingOffset;

    [Header("Smooth out movement")]
    //Smooth out the Surface feel 
    [SerializeField] bool enableDamping = false;

    [Tooltip("How long (in seconds) it takes to catch up to the true water height.")]
    [SerializeField] float dampingTime = 0.2f;

    private Vector3 _dampVelocity = Vector3.zero;


    public WaterSurface targetSurface = null;
    public bool includeDeformation = true;
    public bool excludeSimulation = false;

    [Header("Orientation")]
    public bool alignToWaterNormal = true;
    [Range(0f, 10f)] public float orientationSmoothSpeed = 5f;

    private Vector3 previousPosition;
    [SerializeField] private float pitchAngleMultiplier = 2f;
    [SerializeField] private float rollAngleMultiplier = 3f;

    

    // Internal search params
    WaterSearchParameters searchParameters = new WaterSearchParameters();
    WaterSearchResult searchResult = new WaterSearchResult();

    private void Start()
    {
        //get the water surface if Null
        if (targetSurface == null && LevelVariableManager.Instance != null)
        {
            //set water surface to the Gobal Water surface 
            targetSurface = LevelVariableManager.Instance.GlobalWaterSurface;
        }

        previousPosition = transform.position;
    }


    // Update is called once per frame
    void Update()
    {
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
            if (!enableDamping)
            {
                gameObject.transform.position = searchResult.projectedPositionWS + floatingOffset;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,                   // current position
                    (searchResult.projectedPositionWS + floatingOffset),     // target on-water position + floatingOffset
                    ref _dampVelocity,                    // velocity state
                    dampingTime);                         // smoothing time
            }
           

            //Align the boat to the water 
            if (alignToWaterNormal)
            {
                AlignToWaterNormal(searchResult.normalWS);
            }
        }
    }



    /// <summary>
    /// method to align the gameobject to the water surface with damping 
    /// </summary>
    /// <param name="waterNormal"></param>
    //private void AlignToWaterNormal(float3 waterNormal)
    //{
    //    // Maintain yaw, update pitch and roll to match water surface
    //    Vector3 boatForward = transform.forward;
    //    Vector3 targetRight = Vector3.Cross(Vector3.up, boatForward);
    //    Vector3 flattenedForward = Vector3.Cross(targetRight, waterNormal).normalized;

    //    // Compose new rotation
    //    Quaternion targetRotation = Quaternion.LookRotation(flattenedForward, waterNormal);
    //    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, orientationSmoothSpeed * Time.deltaTime);
    //}

    private void AlignToWaterNormal(float3 waterNormal)
    {
        // Calculate velocity with safeguards against timing issues
        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f); // Prevent division by zero
        Vector3 velocity = (transform.position - previousPosition) / deltaTime;
        previousPosition = transform.position;

        // Flatten for horizontal velocity
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);
    
        // Clamp values to prevent extreme angles
        float forwardSpeed = Mathf.Clamp(localVelocity.z, -10f, 10f);
        float sideSpeed = Mathf.Clamp(localVelocity.x, -10f, 10f);

        // Simulate pitch (nose up/down from forward acceleration)
        float pitchAngle = -forwardSpeed * pitchAngleMultiplier;

        // Simulate roll (lean into turns / side drift)
        float rollAngle = sideSpeed * rollAngleMultiplier;
    
        // Clamp final angles to reasonable values
        pitchAngle = Mathf.Clamp(pitchAngle, -45f, 45f);
        rollAngle = Mathf.Clamp(rollAngle, -45f, 45f);

        Quaternion targetTilt = Quaternion.Euler(pitchAngle, 0f, rollAngle);

        // Rest of the method remains the same...
        Vector3 forwardProjected = Vector3.ProjectOnPlane(transform.forward, waterNormal);

        if (forwardProjected.sqrMagnitude < 0.001f)
        {
            forwardProjected = Vector3.ProjectOnPlane(Vector3.forward, waterNormal);
            if (forwardProjected.sqrMagnitude < 0.001f)
            {
                forwardProjected = Vector3.Cross(waterNormal, Vector3.right);
                if (forwardProjected.sqrMagnitude < 0.001f)
                    forwardProjected = Vector3.Cross(waterNormal, Vector3.up);
            }
        }

        forwardProjected.Normalize();
        Quaternion waterAligned = Quaternion.LookRotation(forwardProjected, waterNormal);
        Quaternion targetRotation = waterAligned * targetTilt;

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, orientationSmoothSpeed * Time.deltaTime);
    }


}
