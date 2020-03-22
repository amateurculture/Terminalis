using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script to align the ship axis with the current velocity vector, to simulate appearance that a 
/// capsule is oriented the way it is travelling (or in the case of re-entry, the opposite direction)
/// </summary>
public class ReentryAttitude : MonoBehaviour
{

    [SerializeField]
    [Tooltip("Axis to align with NBody velocity")]
    private Vector3 axis = Vector3.forward;

    [SerializeField]
    [Tooltip("NBody to use as velocity reference")]
    private NBody nbody = null;

    private GravityEngine ge;

    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 v = ge.GetVelocity(nbody.gameObject);
        transform.rotation = Quaternion.FromToRotation(axis.normalized, v.normalized);
    }
}
