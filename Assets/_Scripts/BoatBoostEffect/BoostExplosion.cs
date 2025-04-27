using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(TugboatMovementWFloat))]
public class BoostExplosion : MonoBehaviour
{
    [Header("Boost Explosion Settings")]
    [SerializeField] private Transform rightBootsPoint;
    [SerializeField] private Transform leftBootsPoint;
    [SerializeField] private Transform normalBootsPoint;
    [SerializeField] private float explosionRadius = 1f;
    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private AudioClip outOfEnergyClip;


    [Space(10)]
    [Header("Boost Delay Config's")]
    [SerializeField] private Slider energyBar;
    [SerializeField] private GameObject energyDepletedVFX;
    [SerializeField] private float cooldownDuration = 2f; // time in seconds before energy refills
    private bool canBoost = true;


    private Rigidbody rb;
    private BoatInputActions controls;
    private AudioSource _audioSource;
    private TugboatMovementWFloat _movementScript;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        _movementScript = GetComponent<TugboatMovementWFloat>();

        if (energyBar != null)
        {
            energyBar.maxValue = cooldownDuration;
            energyBar.value = cooldownDuration;
        }
    }

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        //add it if null 
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnEnable()
    {
        controls = new BoatInputActions();
        controls.Boat.Enable();

        // Register event handlers
        controls.Boat.BoostRight.performed += OnBoostRight;
        controls.Boat.BoostLeft.performed += OnBoostLeft;
        controls.Boat.BoostNormal.performed += OnBoostNormal;

    }

    private void OnDisable()
    {
        // Unregister event handlers
        controls.Boat.BoostRight.performed -= OnBoostRight;
        controls.Boat.BoostLeft.performed -= OnBoostLeft;
        controls.Boat.BoostNormal.performed -= OnBoostNormal;
        // Disable the action map
        controls.Boat.Disable();

    }

    private void OnBoostRight(InputAction.CallbackContext context) => TriggerExplosion(rightBootsPoint.position);
    private void OnBoostLeft(InputAction.CallbackContext context) => TriggerExplosion(leftBootsPoint.position);
    private void OnBoostNormal(InputAction.CallbackContext context) => TriggerExplosion(normalBootsPoint.position);


    private void Update()
    {
        //if(Keyboard.current.jKey.wasPressedThisFrame || triggerLeftBoost)
        //{
        //    Debug.Log("Left Boost Triggered");
        //    //Collider[] colliders = Physics.OverlapSphere(leftBootsPoint.position, explosionRadius);
        //    //foreach (Collider hit in colliders)
        //    //{
        //    //Rigidbody rb = hit.GetComponent<Rigidbody>();

        //    if (rb != null)
        //    {
        //        Debug.Log("Left Explosion Triggered");
        //        rb.AddExplosionForce(explosionForce, leftBootsPoint.position, explosionRadius);
        //    }
        //    //}

        //}

        //if (Keyboard.current.kKey.wasPressedThisFrame || triggerRightBoost)
        //{
        //    Debug.Log("Right Boost Triggered");
        //    //Collider[] colliders = Physics.OverlapSphere(leftBootsPoint.position, explosionRadius);
        //    //foreach (Collider hit in colliders)
        //    //{
        //    //Rigidbody rb = hit.GetComponent<Rigidbody>();

        //    if (rb != null)
        //    {
        //        Debug.Log("Right Explosion Triggered");
        //        rb.AddExplosionForce(explosionForce, rightBootsPoint.position, explosionRadius);
        //    }
        //    //}
        //    triggerRightBoost = false;
        //}
    }


    //private void OnBoostRight(InputAction.CallbackContext context)
    //{
    //    Debug.Log("Boost Right Triggered");
    //    TriggerExplosion(rightBootsPoint.position);
    //}

    //private void OnBoostLeft(InputAction.CallbackContext context)
    //{
    //    Debug.Log("Boost Left Triggered");
    //    TriggerExplosion(leftBootsPoint.position);
    //}

    //private void OnBoostNormal(InputAction.CallbackContext context)
    //{
    //    Debug.Log("Boost Normal Triggered");
    //    TriggerExplosion(normalBootsPoint.position);
    //}

    private void TriggerExplosion(Vector3 direction)
    {
        if (!canBoost)
        {
            PlayDepletedFeedback();
            return;
        } // only allows boost after energy is full 

        //_movementScript.maxSpeed = _movementScript.boostMaxSpeed;
        _movementScript.maxSpeed *= 3;

        Debug.Log($"Explosion triggered in direction: {direction}");
        // Example: GetComponent<Rigidbody>().AddForce(direction * forceAmount, ForceMode.Impulse);
        
        rb.AddExplosionForce(explosionForce * 2f, direction, explosionRadius, 3.0f);

        //Cooldown + energy refill
        canBoost = false;
        StartCoroutine(RefillEnergyBar());
    }

    private void PlayDepletedFeedback()
    {
        Debug.Log("Energy is depleted!");
        if (_audioSource && outOfEnergyClip) _audioSource.PlayOneShot(outOfEnergyClip);
        if (energyDepletedVFX)
        {
            Instantiate(energyDepletedVFX, transform.position, Quaternion.identity);
        }
    }


    private IEnumerator RefillEnergyBar()
    {
        float elapsed = 0f;
        if (energyBar != null) energyBar.value = 0f;

        while (elapsed < cooldownDuration)
        {
            elapsed += Time.deltaTime;
            if (energyBar != null) energyBar.value = elapsed;
            yield return null;
        }
        _movementScript.maxSpeed /= 3;

        if (energyBar != null) energyBar.value = cooldownDuration;
        canBoost = true;
        Debug.Log("Energy refilled. Boost ready!");
    }
    //private IEnumerator RefillEnergy()
    //{
    //    Debug.Log("Energy depleted. Refilling...");
    //    yield return new WaitForSeconds(cooldownDuration);
    //    canBoost = true;
    //    Debug.Log("Energy refilled. Boost ready!");
    //}

    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.DrawSphere(leftBootsPoint.position, explosionRadius);
    //    Gizmos.DrawSphere(rightBootsPoint.position, explosionRadius);
    //    Gizmos.DrawSphere(normalBootsPoint.position, explosionRadius);

    //}
}
