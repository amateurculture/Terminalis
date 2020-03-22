using UnityEngine;

public class Rotate : MonoBehaviour
{
    public float scalingFactor = 4;
    public bool xAxis;
    public bool yAxis;
    public bool zAxis;

    void FixedUpdate()
    {
        transform.Rotate(new Vector3(
            ((xAxis) ? Time.fixedUnscaledDeltaTime * scalingFactor : 0), 
            ((yAxis) ? Time.fixedUnscaledDeltaTime * scalingFactor : 0), 
            ((zAxis) ? Time.fixedUnscaledDeltaTime * scalingFactor : 0)), 
            Space.World);
    }
}
