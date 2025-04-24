using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Connects a towable Rigidbody with a ConfigurableJoint and visualizes a rope using LineRenderer.
/// Tugboat towing system with rope visualization, tension-based snapping, and runtime reattachment.
/// </summary>
public class TugTowSystem : MonoBehaviour
{
    [Header("Tow Settings")]
    public Rigidbody towedObject;
    public Transform towAnchor;
    public Transform targetAttachPoint;
    public float ropeSlack = 0.5f;
    [Range(1f, 20f)] public float towingDistance = 5f;
    public float maxTensionDistance = 25f; // amount of tention before the rope breaks 

    [Header("Rope Visual")]
    public LineRenderer ropeRenderer;
    public GameObject chainLinkPrefab;
    public int ropeSegments = 10;
    public AnimationCurve ropeSag = AnimationCurve.EaseInOut(0, 0, 1, -0.3f);

    [Header("Chain Link Visual")]
    public ChainLinkRopeRenderer chainRenderer;

    private List<Transform> chainLinks = new();
    private ConfigurableJoint joint;
    private bool isAttached = false; // allow re-attaching and detattaching

    void Start()
    {
        //if (towedObject && targetAttachPoint)
        //{
        //    SetupJoint();
        //}
        if (towedObject && targetAttachPoint)
        {
            Attach();
        }
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!isAttached)
            {
                Attach();
            }else
            {
                Detach();
            }
        }

        if (isAttached && chainRenderer != null)
            chainRenderer.UpdateChainLinkPositions();

        if (isAttached && towedObject)
        {
            CheckTension();

            if (ropeRenderer)
                DrawRope();
        }
        else
        {
            ropeRenderer.positionCount = 0;
        }
    }

    public float currentRopeLength = 25f;




    public void Attach()
    {
        if (towedObject == null || targetAttachPoint == null)
            return;

        if (joint != null)
            Destroy(joint);

        joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = towedObject;

        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = transform.InverseTransformPoint(towAnchor.position);
        joint.connectedAnchor = towedObject.transform.InverseTransformPoint(targetAttachPoint.position);

        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;

        //SoftJointLimit limit = new SoftJointLimit { limit = ropeSlack };
        SoftJointLimit limit = new SoftJointLimit { limit = maxTensionDistance };
        joint.linearLimit = limit;

        //SetRopeLength(currentRopeLength); // Ensures visuals and joint match

        joint.enableCollision = true;
        joint.enablePreprocessing = false;

        JointDrive drive = new JointDrive
        {
            positionSpring = 1000f,
            positionDamper = 100f,
            maximumForce = Mathf.Infinity
        };

        joint.xDrive = joint.yDrive = joint.zDrive = drive;

        joint.targetPosition = new Vector3(0, 0, -towingDistance);

        isAttached = true;
    }

    public void Detach()
    {
        if (joint != null)
            Destroy(joint);

        isAttached = false;
    }

    public void SetRopeLength(float newLength)
    {
        currentRopeLength = newLength;

        if (joint)
        {
            SoftJointLimit limit = new SoftJointLimit { limit = newLength };
            joint.linearLimit = limit;
        }

        //if (chainRenderer)
        //    chainRenderer.SetSegmentsFromLength(currentRopeLength);
    }

    private void CheckTension()
    {
        float currentDistance = Vector3.Distance(towAnchor.position, targetAttachPoint.position);

        if (currentDistance > maxTensionDistance)
        {
            Debug.LogWarning("Rope snapped due to excessive tension!");
            Detach();
        }
    }

    //OLD attach only code 
    //void SetupJoint()
    //{
    //    joint = gameObject.AddComponent<ConfigurableJoint>();
    //    joint.connectedBody = towedObject;

    //    joint.autoConfigureConnectedAnchor = false;
    //    joint.anchor = transform.InverseTransformPoint(towAnchor.position);
    //    joint.connectedAnchor = towedObject.transform.InverseTransformPoint(targetAttachPoint.position);

    //    // Limit motion to simulate rope pull, not rigid rod
    //    joint.xMotion = ConfigurableJointMotion.Limited;
    //    joint.yMotion = ConfigurableJointMotion.Limited;
    //    joint.zMotion = ConfigurableJointMotion.Limited;

    //    SoftJointLimit limit = new SoftJointLimit { limit = ropeSlack };
    //    joint.linearLimit = limit;

    //    joint.enableCollision = true;
    //    joint.enablePreprocessing = false;

    //    // Springy rope feeling
    //    JointDrive drive = new JointDrive
    //    {
    //        positionSpring = 1000f,
    //        positionDamper = 100f,
    //        maximumForce = Mathf.Infinity
    //    };

    //    joint.xDrive = drive;
    //    joint.yDrive = drive;
    //    joint.zDrive = drive;
    //}

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
