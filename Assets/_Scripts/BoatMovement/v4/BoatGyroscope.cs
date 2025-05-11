using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class BoatGyroscope : MonoBehaviour
{
    [Header("Gyroscopic Settings")]
    [SerializeField] private float stability = 0.3f;            // How quickly the boat stabilizes
    [SerializeField] private float resistence = 2.0f;                // How aggressively it resists tilting

    private Rigidbody rb;
    private Vector3 previousUp;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = 10f;
        previousUp = transform.up;
    }

    void FixedUpdate()
    {
        Vector3 torqueVector = Vector3.Cross(transform.up, Vector3.up);
        Vector3 angularAcceleration = torqueVector * stability;

        if (torqueVector.x <= 10f || torqueVector.x <= -10f) angularAcceleration = new Vector3(0, 0, 0);

        rb.AddTorque(angularAcceleration * resistence * resistence);

        // Optional: Smooth the previous up vector (can be useful for visual feedback)
        previousUp = Vector3.Lerp(previousUp, transform.up, Time.fixedDeltaTime * resistence);
    }
}
