using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Dynamically renders a rope with chain link prefabs and procedural slack curve.
/// </summary>
public class ChainLinkRopeRenderer : MonoBehaviour
{
    public Transform ropeStart;
    public Transform ropeEnd;
    public GameObject chainLinkPrefab;
    public int segments = 10;
    public float slackFactor = 0.3f;


    private readonly List<GameObject> links = new();

    public void RebuildRope()
    {
        ClearLinks();

        for (int i = 0; i < segments; i++)
        {
            GameObject link = Instantiate(chainLinkPrefab, transform);
            links.Add(link);
        }

        UpdateChainLinkPositions();
    }

    public void UpdateChainLinkPositions()
    {
        if (ropeStart == null || ropeEnd == null || links.Count == 0)
            return;

        Vector3 start = ropeStart.position;
        Vector3 end = ropeEnd.position;
        float length = Vector3.Distance(start, end);

        for (int i = 0; i < links.Count; i++)
        {
            float t = i / (float)(segments - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);

            // Slack curve offset
            Vector3 sag = Vector3.down * Mathf.Sin(t * Mathf.PI) * length * slackFactor;
            pos += sag;

            links[i].transform.position = pos;

            // Face toward next link
            if (i < links.Count - 1)
            {
                Vector3 dir = (links[i + 1].transform.position - pos).normalized;
                links[i].transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }

    public void ClearLinks()
    {
        foreach (var link in links)
        {
            if (link) Destroy(link);
        }
        links.Clear();
    }

    public void SetSegmentsFromLength(float ropeLength, float linkSpacing = 0.5f)
    {
        segments = Mathf.Max(3, Mathf.RoundToInt(ropeLength / linkSpacing));
        RebuildRope();
    }



}
