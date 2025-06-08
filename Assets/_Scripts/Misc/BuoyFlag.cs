using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BuoyFlag : MonoBehaviour
{
    [Header("Flag References")]
    [SerializeField] private GameObject[] flagLODs; // Array of flag LODs

    [Header("Wind Settings")]
    [SerializeField] private bool lockOnBarge = false;
    [SerializeField] private bool lockOnEndTrigger = false;
    [SerializeField] private float windStrength = 1.0f;
    [SerializeField] private float damping = 0.2f;

    [Header("Performance Settings")]
    [SerializeField] private float updateInterval = 0.1f; // Update less frequently
    [SerializeField] private float maxUpdateDistance = 200f; // Don't update if too far
    [SerializeField] private bool disableClothWhenFar = true;
    [SerializeField] private float clothDisableDistance = 100f;

    [Header("Mesh LOD Settings")]
    [SerializeField] private float meshRotationMultiplier = 30f; // Controls rotation amount

    // Target references
    private Transform bargeTransform;
    private Transform endGameTrigger;
    private Vector3 currentWindDirection;
    private Cloth clothComponent;
    private Camera mainCamera;
    private float updateTimer;
    private bool isVisible = true;
    private Renderer flagRenderer;
    private static int buoyUpdateGroup = 0;
    private int myUpdateGroup;

    // Store initial rotations for mesh LODs
    private Quaternion[] initialLODRotations;
    private Vector3 flagPoleDirection;
    
    // Reusable vectors to reduce GC
    private Vector3 rightDirectionCache;
    private Vector3 rotationAxisCache;

    private void Start()
    {
        // Assign to update group for staggering updates
        myUpdateGroup = buoyUpdateGroup;
        buoyUpdateGroup = (buoyUpdateGroup + 1) % 3; // Split buoys into 3 groups
        
        mainCamera = Camera.main;
        flagRenderer = GetComponentInChildren<Renderer>();
        
        // Get references from LevelVariableManager
        if (LevelVariableManager.Instance != null)
        {
            bargeTransform = LevelVariableManager.Instance.GlobalBargeRigidBody.transform;
            endGameTrigger = LevelVariableManager.Instance.GlobalEndGameTrigger.transform;

            if (bargeTransform == null || endGameTrigger == null)
            {
                Debug.LogWarning("Required references not found in LevelVariableManager", this);
            }
        }
        else
        {
            Debug.LogWarning("LevelVariableManager instance not found", this);
        }

        flagPoleDirection = transform.up;

        if (flagLODs != null && flagLODs.Length > 0)
        {
            initialLODRotations = new Quaternion[flagLODs.Length];

            for (int i = 0; i < flagLODs.Length; i++)
            {
                if (flagLODs[i] != null)
                {
                    initialLODRotations[i] = flagLODs[i].transform.rotation;

                    if (i == 0)
                    {
                        clothComponent = flagLODs[i].GetComponent<Cloth>();
                    }
                }
            }
        }
    }

    private void Update()
    {
        // Only process every X seconds based on update group
        if (Time.frameCount % 3 != myUpdateGroup) return;
        
        updateTimer += Time.deltaTime;
        if (updateTimer < updateInterval) return;
        updateTimer = 0;

        // Skip updates when far from camera
        if (mainCamera != null)
        {
            float distToCamera = Vector3.Distance(transform.position, mainCamera.transform.position);
            
            // Toggle cloth based on distance
            if (disableClothWhenFar && clothComponent != null)
            {
                bool shouldEnableCloth = distToCamera < clothDisableDistance;
                if (clothComponent.enabled != shouldEnableCloth)
                    clothComponent.enabled = shouldEnableCloth;
            }

            // Skip processing if too far away
            if (distToCamera > maxUpdateDistance) return;
            
            // Skip if not visible
            isVisible = flagRenderer != null && flagRenderer.isVisible;
            if (!isVisible) return;
        }

        if (bargeTransform == null || endGameTrigger == null || flagLODs == null || flagLODs.Length == 0)
            return;

        // Update wind direction only if exactly one target is selected
        if ((lockOnBarge && !lockOnEndTrigger) || (!lockOnBarge && lockOnEndTrigger))
        {
            Transform targetToUse = lockOnBarge ? bargeTransform : endGameTrigger;
            Vector3 targetDirection = (targetToUse.position - transform.position).normalized;
            currentWindDirection = Vector3.Lerp(currentWindDirection, targetDirection, damping * Time.deltaTime * 10);

            // Apply to cloth if enabled
            if (clothComponent != null && clothComponent.enabled)
            {
                clothComponent.externalAcceleration = currentWindDirection * windStrength;
            }

            // Apply rotation to mesh LODs
            ApplyWindRotationToMeshLODs();
        }
    }

    private void ApplyWindRotationToMeshLODs()
    {
        // Skip LOD_0 as it uses cloth simulation
        for (int i = 1; i < flagLODs.Length; i++)
        {
            if (flagLODs[i] == null) continue;

            // Reuse cached vectors
            rightDirectionCache = Vector3.Cross(flagPoleDirection, currentWindDirection).normalized;

            if (rightDirectionCache.magnitude < 0.001f)
            {
                rightDirectionCache = Vector3.Cross(flagPoleDirection, Vector3.forward).normalized;
            }

            rotationAxisCache = Vector3.Cross(rightDirectionCache, flagPoleDirection).normalized;
            float windStrengthFactor = Vector3.Dot(currentWindDirection, rightDirectionCache);

            // Create rotation to apply to the flag mesh
            Quaternion windRotation = Quaternion.AngleAxis(windStrengthFactor * meshRotationMultiplier, rotationAxisCache);
            Quaternion zAxisOffset = Quaternion.Euler(0, 0, 55f);
            Quaternion combinedRotation = initialLODRotations[i] * windRotation * zAxisOffset;

            // Apply rotation with damping
            flagLODs[i].transform.rotation = Quaternion.Slerp(
                flagLODs[i].transform.rotation,
                combinedRotation,
                damping * Time.deltaTime * 5
            );
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Only draw gizmos when selected to reduce editor overhead
        if (!UnityEditor.Selection.Contains(gameObject)) return;
        if (currentWindDirection == Vector3.zero) return;

        Vector3 start = transform.position + Vector3.up * 25f;
        Vector3 end = start + currentWindDirection * 9f;

        // Main arrow line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, end);

        // Arrow head (simplified)
        float headSize = 2f;
        Vector3 right = Vector3.Cross(currentWindDirection, Vector3.up).normalized * headSize;
        if (right.magnitude < 0.001f)
            right = Vector3.Cross(currentWindDirection, Vector3.forward).normalized * headSize;

        Vector3 up = Vector3.Cross(right, currentWindDirection).normalized * headSize;

        Gizmos.DrawLine(end, end - currentWindDirection * headSize + right);
        Gizmos.DrawLine(end, end - currentWindDirection * headSize - right);
    }
#endif
}