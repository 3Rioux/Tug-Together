using TMPro;
using UnityEngine;

/// <summary>
/// Points the compass needle toward the end goal of the scene.
/// </summary>
public class Compass : MonoBehaviour
{

    [SerializeField] private Transform player;              // The player or camera
    [SerializeField] private Transform target;              // The object to point to
    [SerializeField] private Transform needle;              // The rotating arrow
    [SerializeField] private float rotationSpeed = 5f;      // Smoothing factor

    //UI 
    [SerializeField] private TextMeshPro distanceText;  // Text to show distance

    //Private
    private Vector3 _targetDirection;                        // Save the direction of the target 

  



    private void Start()
    {
        //player = transform.parent.parent; // lol you can do that??? 
        if (LevelVariableManager.Instance != null)
        {
            target = LevelVariableManager.Instance.GlobalEndGameTrigger.transform;
        }
    }

    void Update()
    {
        if (player == null || target == null || needle == null) return;


        //// Make the compass face the player (like a HUD)
        //transform.LookAt(player);
        //transform.rotation = Quaternion.Euler(0, -transform.rotation.y, 0); // Optional: lock X and Z rotation


        // Calculate direction to target
        _targetDirection = target.position - player.position;
        _targetDirection.y = 0; // Ignore vertical difference


        // Calculate target rotation
        //Quaternion targetRotation = Quaternion.LookRotation(_targetDirection);
        float angle = Vector3.SignedAngle(player.forward, _targetDirection, Vector3.up);

        // Smoothly rotate the needle
        //needle.rotation = new Quaternion(needle.rotation.x, Quaternion.Slerp(needle.rotation, targetRotation, rotationSpeed * Time.deltaTime).z, needle.rotation.z, 0);
        needle.localEulerAngles = new Vector3(0, 0, -angle);


        // Update distance text
        float distance = Vector3.Distance(player.position, target.position);
        distanceText.text = $"{distance:F1}m";



        //float angle = Vector3.SignedAngle(player.forward, _targetDirection, Vector3.up);
        //compassNeedle.localEulerAngles = new Vector3(0, 0, -angle);
    }




}
