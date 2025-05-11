using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
public class FitToWaterSurface : MonoBehaviour
{
    //Smooth out the Surface feel 
    [SerializeField] bool enableDamping = false;

    [Tooltip("How long (in seconds) it takes to catch up to the true water height.")]
    [SerializeField] float dampingTime = 0.2f;

    private Vector3 _dampVelocity = Vector3.zero;


    public WaterSurface targetSurface = null;
    public bool includeDeformation = true;
    public bool excludeSimulation = false;

    // Internal search params
    WaterSearchParameters searchParameters = new WaterSearchParameters();
    WaterSearchResult searchResult = new WaterSearchResult();

    // Update is called once per frame
    void Update()
    {
        if (targetSurface != null)
        {
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
                    gameObject.transform.position = searchResult.projectedPositionWS;
                }
                else
                {
                    transform.position = Vector3.SmoothDamp(
                        transform.position,                   // current position
                        searchResult.projectedPositionWS,     // target on-water position
                        ref _dampVelocity,                    // velocity state
                        dampingTime);                         // smoothing time
                }
            }
        }
    }
}
