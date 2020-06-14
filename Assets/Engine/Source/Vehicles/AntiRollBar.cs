using UnityEngine;

public class AntiRollBar : MonoBehaviour
{
    public WheelCollider WheelL;
    public WheelCollider WheelR;
    Rigidbody rigid;
    float AntiRoll = 5000.0f;

    void FixedUpdate()
    {
        WheelHit hit;
        float travelL = 1.0f;
        float travelR = 1.0f;

        var groundedL = WheelL.GetGroundHit(out hit);
        if (groundedL) travelL = (-WheelL.transform.InverseTransformPoint(hit.point).y - WheelL.radius) / WheelL.suspensionDistance;

        var groundedR = WheelR.GetGroundHit(out hit);
        if (groundedR) travelR = (-WheelR.transform.InverseTransformPoint(hit.point).y - WheelR.radius) / WheelR.suspensionDistance;

        float antiRollForce = (travelL - travelR) * AntiRoll;

        rigid = GetComponent<Rigidbody>();

        if (groundedL) rigid.AddForceAtPosition(WheelL.transform.up * -antiRollForce, WheelL.transform.position);
        if (groundedR) rigid.AddForceAtPosition(WheelR.transform.up * antiRollForce, WheelR.transform.position);
    }
}
