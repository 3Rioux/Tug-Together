using UnityEngine;

public class PivotOffset : MonoBehaviour
{
    // Adjust this value to shift the model into the water.
    public Vector3 pivotOffset;

    private void Start()
    {
        if (transform.childCount > 0)
        {
            Transform model = transform.GetChild(0);
            // Offset the model relative to the container pivot.
            model.localPosition = pivotOffset;
        }
    }
}