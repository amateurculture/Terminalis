using UnityEngine;

public class Rotate : MonoBehaviour
{
    public float scalingFactor = 4;

    void FixedUpdate()
    {
        transform.Rotate(new Vector3(0, Time.fixedUnscaledDeltaTime * scalingFactor, 0), Space.World);
    }
}
