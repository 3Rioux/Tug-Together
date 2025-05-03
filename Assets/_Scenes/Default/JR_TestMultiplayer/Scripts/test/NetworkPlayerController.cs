using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed;

    private BoatInputActions boatActions;

    Vector3 finalMove;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        boatActions = new BoatInputActions();
        boatActions.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        //Only calculate if we are the Owner->

        var move = boatActions.Boat.Move.ReadValue<Vector2>();
        finalMove = new Vector3(-move.x, 0, -move.y) * Time.deltaTime * moveSpeed;

        MoveMe(move);

    }


    public void MoveMe(Vector3 move)
    {
        transform.Translate(move);
        //transform.position = new Vector3(transform.position.x, 0, transform.position.z);
        RotateMe(transform.position);
    }

    public void RotateMe(Vector3 move)
    {
       // Quaternion rotate = Quaternion.LookRotation(move);
        transform.rotation = Quaternion.LookRotation(move);
    }

}
