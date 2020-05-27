using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple demonstration of origin re-location. This is commonly used to maintain a spaceship near the center
/// of the physics (and hence Unity) co-ordinates to reduce "numerical jitter".  Numerical jitter can occur when the spaceship
/// is at very large values of position and numerical precision issues cause local jumps in position from frame to frame. 
/// 
/// </summary>
public class MoveToDemo : MonoBehaviour {

    public NBody referenceObject;

    public float distanceTrigger;

    public GameObject cameraObject; 

    private GravityEngine ge; 
	// Use this for initialization
	void Start () {
        ge = GravityEngine.Instance();
	}
	
	void FixedUpdate () {
        Vector3 pos = ge.GetPhysicsPosition(referenceObject);
        if (pos.magnitude > distanceTrigger) {
            Vector3 unityPos = referenceObject.transform.position;
            ge.MoveAll(referenceObject);
            Debug.Log("====================================================================");
            Debug.LogFormat("Moving physics position={0} D={1} ", pos, pos.magnitude);

            // moving the camera need to move in Unity space (not physics space)
            if (cameraObject != null) {
                cameraObject.transform.position -= unityPos;
            }
            // ge.singleStep = true;
        }
    }
}
