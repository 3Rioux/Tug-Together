using UnityEngine;


/// <summary>
/// Connects a towable Rigidbody with a ConfigurableJoint and visualizes a rope using LineRenderer.
/// </summary>
public class TugTowSystem : MonoBehaviour
{
    [Header("Tow Settings")]
    public Rigidbody towedObject;
    public Transform towAnchor;
    public Transform targetAttachPoint;
    public float ropeSlack = 0.5f;

    [Header("Rope Visual")]
    public LineRenderer ropeRenderer;
    public int ropeSegments = 10;
    public AnimationCurve ropeSag = AnimationCurve.EaseInOut(0, 0, 1, -0.3f);

    private ConfigurableJoint joint;

    void Start()
    {
        if (towedObject && targetAttachPoint)
        {
            SetupJoint();
        }
    }

    void Update()
    {
        if (ropeRenderer && towedObject)
        {
            DrawRope();
        }
    }

    void SetupJoint()
    {
        joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = towedObject;

        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = transform.InverseTransformPoint(towAnchor.position);
        joint.connectedAnchor = towedObject.transform.InverseTransformPoint(targetAttachPoint.position);

        // Limit motion to simulate rope pull, not rigid rod
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;

        SoftJointLimit limit = new SoftJointLimit { limit = ropeSlack };
        joint.linearLimit = limit;

        joint.enableCollision = true;
        joint.enablePreprocessing = false;

        // Springy rope feeling
        JointDrive drive = new JointDrive
        {
            positionSpring = 1000f,
            positionDamper = 100f,
            maximumForce = Mathf.Infinity
        };

        joint.xDrive = drive;
        joint.yDrive = drive;
        joint.zDrive = drive;
    }

    void DrawRope()
    {
        Vector3 start = towAnchor.position;
        Vector3 end = targetAttachPoint.position;
        ropeRenderer.positionCount = ropeSegments + 1;

        for (int i = 0; i <= ropeSegments; i++)
        {
            float t = i / (float)ropeSegments;
            Vector3 pos = Vector3.Lerp(start, end, t);
            Vector3 offset = Vector3.down * ropeSag.Evaluate(t) * Vector3.Distance(start, end);
            ropeRenderer.SetPosition(i, pos + offset);
        }
    }
}
