using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Set the position to be on the line from origin to target at the specified fraction of the distance. 
/// 
/// Optionally can add a rotation to align the X-axis with the line between origin and target.
/// 
/// Used in the OrbitXfer demo to provide a label to an object out of view. 
/// 
/// </summary>
public class OrbitLabel : MonoBehaviour {

    //! target for orbit label
    public GameObject target; 

     //! Origin for target direction
    public GameObject originObject;

    public float distanceFromOrigin; 

    public bool rotateToLine = true; 

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 toTarget = Vector3.Normalize(target.transform.position - originObject.transform.position);
        transform.position = originObject.transform.position + distanceFromOrigin * toTarget;

        if (rotateToLine) {
            transform.rotation = Quaternion.FromToRotation(Vector3.right, toTarget);
        }
    }
}
