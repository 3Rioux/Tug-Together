using UnityEngine;


public class ResetCinemachinePosition : MonoBehaviour
{
    public Vector3 defaultPosition;
    public Vector3 defaultRotation;

    private void Start()
    {
        defaultPosition = transform.position;
        defaultRotation = transform.eulerAngles;
    }

    private void OnEnable()
    {
        transform.position = defaultPosition;
        transform.eulerAngles = defaultRotation;
    }
}
