using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Define a docking port on a spaceship model. 
/// 
/// The position and orientation come form the transform components. The direction of the docking port
/// is based on the local z-axis. 
/// 
/// </summary>
public class DockingPort : MonoBehaviour {

    [SerializeField]
    [Tooltip("Position delta required for docking capture (in Unity scene units)")]
    private float captureDeltaPos = 1;

    [SerializeField]
    [Tooltip("Orientation delta (degrees) required for docking capture")]
    private float captureDeltaAngleDeg = 1;

    [SerializeField]
    [Tooltip("Velocity limit required for docking capture (in RigidBody velocity units")]
    private float velocityLimit = 1;

    [SerializeField]
    private Mesh gizmoMesh = null;

    [SerializeField]
    private Vector3 gizmoScale = Vector3.one;

    private const int cubeLen = 1;

    private Rigidbody shipRigidbody;

    void Start() {
        shipRigidbody = gameObject.transform.parent.gameObject.GetComponent<Rigidbody>();
        if (shipRigidbody == null) {
            Debug.LogError("Expect docking port parent to be a rigidbody.");
        }
    }

    public Rigidbody GetRigidbody() {
        return shipRigidbody;
    }

    /// <summary>
    /// Determine the separation distance between the docking ports in Unity scene units. 
    /// </summary>
    /// <param name="matePort"></param>
    /// <returns></returns>
    public Vector3 SeparationDistance(DockingPort matePort) {
        return transform.position - matePort.gameObject.transform.position;
    }

    /// <summary>
    /// Check to see if this docking port can capture another given the position, angle and velocity  limits
    /// specified. 
    /// </summary>
    /// <param name="matePort"></param>
    /// <returns></returns>
    public bool Capture(DockingPort matePort ) {

        float deltaPos = Vector3.Distance(transform.position, matePort.gameObject.transform.position);
        Vector3 mateDirection = matePort.gameObject.transform.rotation * Vector3.forward;
        Vector3 myDirection = transform.rotation * Vector3.forward;
        float deltaAngle = Vector3.Angle(mateDirection, myDirection);
        float dV = (shipRigidbody.velocity - matePort.GetRigidbody().velocity).magnitude;

        if (deltaPos > captureDeltaPos)
            return false;

         if ( deltaAngle > captureDeltaAngleDeg)
            return false;

        // velocity limit
        if (dV > velocityLimit)
            return false;

        Debug.Log("Capture");
        return true;
    }


#if UNITY_EDITOR
    /// <summary>
    /// Draw a simple sphere and box to show to location and orientation of the docking port in the
    /// Unity editor. 
    /// </summary>
    void OnDrawGizmosSelected() {
        Gizmos.DrawMesh(gizmoMesh, transform.position, transform.rotation, gizmoScale);
    }
#endif
}
