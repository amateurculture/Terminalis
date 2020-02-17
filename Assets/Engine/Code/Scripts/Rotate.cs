using UnityEngine;

public class Rotate : MonoBehaviour
{
    void FixedUpdate()
    {
        transform.Rotate(new Vector3(0, Time.fixedUnscaledDeltaTime, 0), Space.World);
    }
}
