using UnityEngine;

/// <summary>
/// Simple script to rotate an object around y by speed
/// </summary>
public class BuoyLight : MonoBehaviour
{

    [SerializeField] private float rotationSpeed = 30f; // Degrees per second

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

}
