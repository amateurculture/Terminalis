using UnityEngine;

public class BlackBox : MonoBehaviour 
{
    [HideInInspector] public float oldVelocity;
    [HideInInspector] public float oldAltitude;

    private float newAltitude;
    private float minAltitude = 0;
    private float maxAltitude = 99999;

    private float newVelocity;
    private float minVelocity = 0.0f;
    private float maxVelocity = 240.0f;

    private void Update() {
        newAltitude = transform.position.y * 3.28084f;
        newAltitude = Mathf.Clamp(newAltitude, minAltitude, maxAltitude);
        oldAltitude = Mathf.Lerp(oldAltitude, newAltitude, Time.deltaTime * 10.0f);

        newVelocity = this.GetComponent<Rigidbody>().velocity.magnitude * 2.237f;
        newVelocity = Mathf.Clamp(newVelocity, minVelocity, maxVelocity);
        oldVelocity = Mathf.Lerp(oldVelocity, newVelocity, Time.deltaTime * 10.0f);
    }
}
