using System;
using System.Collections.Generic;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample.DistributedAuthority;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Random = UnityEngine.Random;

/// <summary>
/// DebrisSpawner continuously spawns floating and stationary debris in an HDRP water scene.
/// </summary>
public class DebrisSpawner : MonoBehaviour
{
    // Prefab lists (assign multiple debris models via the Inspector)
    //public List<GameObject> floatingDebrisPrefabs;
    //public List<GameObject> stationaryDebrisPrefabs;
    
    [Header("Floating Debris Pools")]
    public List<DebrisPool> floatingDebrisPools; // each unique game object has its own pool 

    [Header("Stationary Debris Pools")]
    public List<DebrisPool> stationaryDebrisPools;


    [Header("Floating Debris Pools")]
    // Predefined spawn points for stationary debris (assign Transforms in the scene)
    public List<Transform> stationarySpawnPoints;


    // Reference to the HDRP WaterSurface (assign via Inspector if using water height queries)
    [SerializeField] private WaterSurface waterSurface;


    // Spawn settings
    [Tooltip("Maximum number of debris instances allowed in the scene at once.")]
    [SerializeField] private int maxDebris = 50;
    [Tooltip("Time interval (sec) between spawning new floating debris.")]
    [SerializeField] private float spawnInterval = 2f;
    [Tooltip("Random drift speed range for floating debris.")]
    [SerializeField] private float driftSpeedMin = 0.5f;
    [SerializeField] private float driftSpeedMax = 2f;
    [Tooltip("Horizontal spawn radius around this object for floating debris.")]
    [SerializeField] private float spawnRadius = 20f;


    [Header("Destruction Bounds (relative to spawner)")]
    [Tooltip("Maximum distance from spawner before debris is destroyed.\nVector4: +Z (Forward), -Z (Back), -X (Left), +X (Right)")]
    [SerializeField] private Vector4 destroyBounds = new(30f, 30f, 30f, 30f); // forward, backward, left, right

    // Internal state
    private float spawnTimer = 0f;
    private int currentDebrisCount = 0;

    // Data structure to track floating debris movement
    private class FloatingDebrisData
    {
        public bool isNotBuoyency; // Whether or not the object is using buoyency to float (if yes DONT add Drift)
        public GameObject obj;     // Instance of the debris GameObject
        public Vector3 direction;  // Horizontal drift direction
        public float speed;        // Drift speed
    }
    private List<FloatingDebrisData> floatingDebrisList = new List<FloatingDebrisData>();
    //private static readonly List<FloatingDebrisData> floatingDebrisList = new List<FloatingDebrisData>();

    //=======================



    void Start()
    {
        //get the water surface if Null
        if (waterSurface == null)
        {
            //set water surface to the Gobal Water surface 
            waterSurface = LevelVariableManager.Instance.GlobalWaterSurface;
        }

        //Initialize All Pools:
        foreach (var pool in floatingDebrisPools)
            pool.Initialize(this.transform, waterSurface);

        foreach (var pool in stationaryDebrisPools)
            pool.Initialize(this.transform, waterSurface);


        // Spawn stationary debris at the specified world positions
        foreach (Transform point in stationarySpawnPoints)
        {
            //Cant spawn more stationary obj then there are spawn points 
            if (currentDebrisCount >= stationarySpawnPoints.Count - 1) break;
           
            int index = Random.Range(0, stationaryDebrisPools.Count);
            GameObject obj = stationaryDebrisPools[index].Get();
            obj.transform.position = point.position;
            obj.transform.rotation = Quaternion.identity;

            currentDebrisCount++;
        }



    }//end start

    void Update()
    {
        // Spawn floating debris at intervals, up to maxDebris
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval && currentDebrisCount < maxDebris)
        {
            spawnTimer = 0f;
            SpawnFloatingDebris();
        }

        // Update movement of all floating debris
        //foreach (var data in floatingDebrisList)
        for (int i = floatingDebrisList.Count - 1; i >= 0; i--)
        {
            var data = floatingDebrisList[i];

            // Simple horizontal drift
            if(data.isNotBuoyency) data.obj.transform.position += data.direction * data.speed * Time.deltaTime;

            // Optional: keep object at the water surface height
            if (waterSurface != null)
            {
                //using boyency instead 
                //WaterSearchParameters searchParams = new WaterSearchParameters();
                //searchParams.targetPositionWS = data.obj.transform.position;
                //searchParams.startPositionWS = data.obj.transform.position;
                //searchParams.error = 0.05f;
                //searchParams.maxIterations = 8;

                
                //if (waterSurface.FindWaterSurfaceHeight(searchParams, out WaterSearchResult result))
                //{
                //    Vector3 pos = data.obj.transform.position;
                //    pos.y = result.height;
                //    data.obj.transform.position = pos;
                //}
            }

            // Destruction after objects reach max bounds check
            Vector3 localOffset = transform.InverseTransformPoint(data.obj.transform.position);

            if (localOffset.z > destroyBounds.x ||      // Forward
                localOffset.z < -destroyBounds.y ||     // Backward
                localOffset.x < -destroyBounds.z ||     // Left
                localOffset.x > destroyBounds.w)        // Right
            {
                //Destroy(data.obj);
                ReturnToPool(data.obj);
                floatingDebrisList.RemoveAt(i);
                currentDebrisCount--;
            }

        }//end foreach 

    }//end update 

    /// <summary>
    /// Instantiate a floating debris prefab at a random horizontal position.
    /// </summary>
    private void SpawnFloatingDebris()
    {
        if (floatingDebrisPools.Count == 0) return;

        // Compute a random spawn position in XZ around this spawner
        Vector2 randCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(randCircle.x, 0f, randCircle.y);


        //Get Object from Pool: 
        int index = Random.Range(0, floatingDebrisPools.Count);
        GameObject obj = floatingDebrisPools[index].Get();

        //stop the original spawn spinning effect
        //Rigidbody rb = obj.GetComponent<Rigidbody>();
        //if (rb != null)
        //{
        //    rb.freezeRotation = true;
        //}
        int randomRotation = Random.Range(0, 126);

        obj.transform.position = spawnPos;
        
        if (obj.layer == 12)
        {
            Debug.Log(randomRotation.ToString()+ " " + obj.name);
        }
        obj.transform.rotation = new Quaternion(obj.transform.position.x, obj.transform.rotation.y + randomRotation, obj.transform.position.z +(randomRotation + randomRotation), 1); //unsure about the w lol 

        // Assign random horizontal drift direction and speed
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 driftDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
        float driftSpeed = Random.Range(driftSpeedMin, driftSpeedMax);

        //allow object to rotate once everything is set
       // rb.freezeRotation = false;

        // Add to tracking list
        floatingDebrisList.Add(new FloatingDebrisData { obj = obj, direction = driftDir, speed = driftSpeed });
        currentDebrisCount++;
    }


    /// <summary>
    /// Returns item to the pool by checking the DebrisIdentifier of the object 
    /// </summary>
    /// <param name="obj"></param>
    void ReturnToPool(GameObject obj)
    {
        //Get ID from object 
        DebrisIdentifier id = obj.GetComponent<DebrisIdentifier>();

        if (id != null)
        {
            //loop through each pool to check where to return the object to 
            foreach (var pool in floatingDebrisPools)
            {
                //Check the pool ID against the prefab ID 
                if (pool.prefabID == id.prefabID)
                {
                    //Return object to matching pool 
                    pool.Return(obj);
                    //decrease the count of current existing debris.
                    currentDebrisCount--;
                    return;
                }
            }
        }

        Debug.LogWarning($"Debris with prefabID '{id?.prefabID}' not found in pool list. Destroying.");
        Destroy(obj);
        currentDebrisCount--;
    }//end return to pool 
    

}//end DebisSpawner Class


/// <summary>
/// Object Pooling Class 
/// </summary>
[System.Serializable]
public class DebrisPool
{
    public GameObject prefab;
    public int initialSize = 10;
    public bool isNotBuoyency = false;

    //local pool 
    private Queue<GameObject> pool = new Queue<GameObject>();
    private Transform parent;

    [HideInInspector] public string prefabID;

    public void Initialize(Transform parentTransform , WaterSurface waterSurface)
    {
        parent = parentTransform;

        // Assign ID from the prefab's DebrisIdentifier
        var id = prefab.GetComponent<DebrisIdentifier>();
        if (id != null)
        {
            prefabID = id.prefabID;
        }
        else
        {
            Debug.LogWarning($"Prefab '{prefab.name}' is missing DebrisIdentifier!");
        }

        //Create a Parent GameObject to store Each Pool Gameobject as a child:
        GameObject poolParentObject = new GameObject();
        poolParentObject.name = $"Pool_{id}"; // Set the name of the GameObject

        // Set poolParentObject as a child of this script's GameObject
        poolParentObject.transform.parent = parentTransform;

        //GameObject.Instantiate(poolParentObject, parent);

        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = GameObject.Instantiate(prefab, poolParentObject.transform);
            //link to the waterline:

            if (obj.TryGetComponent<FitToWaterSurface>(out FitToWaterSurface fit))
            {
                fit.targetSurface = waterSurface;
            }
            if (obj.TryGetComponent<Buoyancy>(out Buoyancy buoyancy))
            {
                buoyancy.targetSurface = waterSurface;
            }

            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }//edn Initialize


    /// <summary>
    /// Method to get the game object from the pool (used instead of Instantiate)
    /// </summary>
    /// <returns></returns>
    public GameObject Get()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        else
        {
            GameObject obj = GameObject.Instantiate(prefab, parent);
            return obj;
        }
    }

    /// <summary>
    /// Method to return object to the pool (Used instead of Destroy)
    /// </summary>
    /// <param name="obj"></param>
    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }


}
