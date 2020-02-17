using UnityEngine;

public class FacingSign : MonoBehaviour
{
	void FixedUpdate ()
	{
        transform?.LookAt(Camera.main?.transform);
        transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);
    }
}
