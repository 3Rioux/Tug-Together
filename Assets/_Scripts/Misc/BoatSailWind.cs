using UnityEngine;

[RequireComponent(typeof(Cloth))]
public class BoatSailWind : MonoBehaviour
{
    [Tooltip("Strength of the wind force on the sail")]
    [SerializeField] private float _windStrength = 5f;

    private Cloth _cloth;

    private void Awake()
    {
        _cloth = GetComponent<Cloth>();
    }

    private void Update()
    {
        // Boat’s local back direction in world space
        Vector3 windDir = transform.TransformDirection(Vector3.up).normalized;
        
        // Apply it every frame
        _cloth.externalAcceleration = windDir * _windStrength;
    }
}
