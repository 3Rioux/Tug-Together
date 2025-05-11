using UnityEngine;

/// <summary>
/// This script just makes the object its attached to float 
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class FloatingObject : MonoBehaviour
{

    //[Header("Buoyancy")]
    //public float buoyancyStrength = 10f;
    //public float buoyancyDamping = 0.5f;
    //public float waveAmplitude = 1f;
    //public float waveFrequency = 0.5f;
    //public float waveSpeed = 1f;

    [SerializeField] private float waterLevel = 0.0f;
    [SerializeField] private float floatThreshold = 2.0f;
    [SerializeField] private float waterDensity = 0.125f;
    [SerializeField] private float downForce = 4.0f;        // this is what controls the amount the GameObject floats 
    // [SerializeField] private float

    private float _forceFactor;
    private Vector3 _floatForce;

    private Rigidbody _rb;

    public float WaterLevel { get => waterLevel; set => waterLevel = value; } // allows me to change the water level of the game 

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    //using Fixed Update because we need to finish the calculations before running the methods 
    private void FixedUpdate()
    {
       // ApplyBuoyancy();

        _forceFactor = 1.0f - ((transform.position.y - WaterLevel) / floatThreshold);

        if (_forceFactor > 0.0f)
        {
            //_floatForce = -Physics.gravity * (_forceFactor - _rb.linearVelocity.y * waterDensity); //<- does NOT take into consideration the weight to the object 
            _floatForce = -Physics.gravity * _rb.mass * (_forceFactor - _rb.linearVelocity.y * waterDensity);
            _floatForce += new Vector3(0, -downForce, 0); // add downforce to the floatForce

            //Apply the force: to the rb 
            _rb.AddForceAtPosition(_floatForce, transform.position);
        }

    }


    //private void ApplyBuoyancy()
    //{
    //    float waveY = GetWaveHeight(transform.position.x, transform.position.z, Time.time);
    //    float submergedDepth = Mathf.Clamp01((waveY - transform.position.y));

    //    if (submergedDepth > 0f)
    //    {
    //        float upwardForce = buoyancyStrength * submergedDepth;
    //        Vector3 damping = -_rb.linearVelocity * buoyancyDamping;
    //        _rb.AddForce(Vector3.up * upwardForce + damping, ForceMode.Acceleration);
    //    }
    //}


    //private float GetWaveHeight(float x, float z, float t)
    //{
    //    return waterLevel + Mathf.Sin((x + t * waveSpeed) * waveFrequency) * waveAmplitude;
    //}

}
