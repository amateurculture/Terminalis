using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple script to demonstrate/test the get position and velocity functions
/// </summary>
public class LogPositionVelocity : MonoBehaviour {

	private NBody nbody; 

	void Awake() {
		nbody = GetComponent<NBody>();
		if (nbody == null) {
			Debug.LogWarning("Requires an NBody");
		}
	}
	// Update is called once per frame
	void Update () {
		if (nbody == null)
			return;

		if (Input.GetKeyDown(KeyCode.L)) {
			double[] p = new double[3];
			double[] v = new double[3];
			GravityEngine.instance.GetPositionVelocityScaled(nbody, ref p, ref v);
			Debug.Log(string.Format("GetPositionVelocityScaled p={0} {1} {2} v={3} {4} {5}", 
								p[0], p[1], p[2], v[0], v[1], v[2]));
			double r = System.Math.Sqrt(p[0]*p[0]+ p[1]*p[1]+ p[2]*p[2]);
			double vmag = System.Math.Sqrt(v[0]*v[0]+ v[1]*v[1]+ v[2]*v[2]);
			Debug.Log(string.Format("r={0} vsq={1}", r, vmag));
		}

	}
}
