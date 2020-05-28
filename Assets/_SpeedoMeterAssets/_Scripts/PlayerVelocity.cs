using UnityEngine;

public class PlayerVelocity : MonoBehaviour {
    [HideInInspector] public float oldVelocity;
    private float newVelocity;
    private float minVelocity = 0.0f;
    private float maxVelocity = 240.0f;

    private void Update() {
        newVelocity = this.GetComponent<Rigidbody>().velocity.magnitude * 2.237f;
        newVelocity = Mathf.Clamp(newVelocity, minVelocity, maxVelocity);
        oldVelocity = Mathf.Lerp(oldVelocity, newVelocity, Time.deltaTime * 10.0f);
    }
}
