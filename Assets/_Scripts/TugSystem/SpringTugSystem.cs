using GogoGaga.OptimizedRopesAndCables;
using TMPro;
using UnityEngine;


public class SpringTugSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI distanceText;


    [SerializeField] private SpringJoint springJoint;
    [SerializeField] private Rope visualRope;

    [SerializeField] private Transform attachPoint;

    private void Start()
    {
       // springJoint.autoConfigureConnectedAnchor = false;
        //springJoint.connectedAnchor = attachPoint.position;
    }

    private void Update()
    {
        float distance = Vector3.Distance(transform.position, attachPoint.position);

        if (distance >= springJoint.maxDistance)
        {
            //set to max lenght to simulate tention 
            visualRope.ropeLength = springJoint.maxDistance;
        }
        else
        {
            visualRope.ropeLength = distance;
        }

        distanceText.text = distance.ToString()+ " m";
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, attachPoint.position);

        Gizmos.DrawSphere(attachPoint.position, 0.1f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(springJoint.connectedAnchor, 5f);
    }

}
