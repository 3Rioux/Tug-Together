using TMPro;
using UnityEngine;


public class SpringTugSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private SpringJoint springJoint;

    [SerializeField] private Transform attachPoint;

    private void Start()
    {
       // springJoint.autoConfigureConnectedAnchor = false;
        //springJoint.connectedAnchor = attachPoint.position;
    }

    private void Update()
    {
        //Vector3.Distance(transform.position, attachPoint.position);

        distanceText.text = Vector3.Distance(transform.position, attachPoint.position).ToString();
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
