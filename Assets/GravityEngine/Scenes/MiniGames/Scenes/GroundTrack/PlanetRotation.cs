using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rotate a planet at the rotation period specified. 
/// 
/// Ensure that the rotation is consistent as timeZoom and time reversal occur. 
/// </summary>
public class PlanetRotation : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Period of rotation in world units (e.g for orbital: hours)")]
    private float period = 1.0f;

    [SerializeField]
    [Tooltip("Initial phase for the rotation in degrees")]
    private float initPhase = 0f;

    [SerializeField]
    private Vector3 axis = Vector3.forward;

    //! angular frequency in radian per GE physics time
    private float omega;

    private Quaternion initialRotation;
    private GravityEngine ge;

    // Start is called before the first frame update
    void Start()
    {
        initialRotation = transform.rotation;
        ge = GravityEngine.Instance();

        omega = 2.0f * Mathf.PI / ((float) GravityScaler.WorldTimeToPhysTime(period));
        Debug.Log("omega=" + omega);
    }

    // Update is called once per frame
    void Update()
    {
        float angleDegrees = Mathf.Rad2Deg * (omega * ge.GetPhysicalTime()) + initPhase;
        transform.rotation = Quaternion.AngleAxis(angleDegrees, axis) * initialRotation;
    }

    public Vector3 RotatePoint(Vector3 point, float deltaTime) {
        float angleDegrees = Mathf.Rad2Deg * (omega * deltaTime);
        return Quaternion.AngleAxis(-angleDegrees, axis) * point;
    }
}
