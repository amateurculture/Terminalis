using UnityEngine;
using System.Collections;

/// <summary>
/// Three body solution.
///
/// Configure the three NBody children attached with the initial conditions of the 
/// specified ThreeBody solution
/// </summary>
public class ThreeBodySolution : MonoBehaviour {

	public string solutionSet; 
	public string solutionName;

	public GameObject body1;
	public GameObject body2;
	public GameObject body3;

	private GravityEngine gravityEngine;
	private bool running; 

	void Start () {
		
		gravityEngine = GravityEngine.instance;
		if (gravityEngine == null) {
			Debug.LogError("No GravityEngine in scene");
			return;
		}
		// check we have the bodies we need
		if (body1 == null || body2 == null || body3 == null) {
			Debug.LogError("Require 3 GameObjects with NBody scripts. ");
			return;
		}
		GameObject[] bodies = new GameObject[]{body1, body2, body3};
		NBody[] nbodies = new NBody[3];
		for (int i=0; i < 3; i++) {
			nbodies[i] = bodies[i].GetComponent<NBody>();
			if (nbodies[i] == null) {
				Debug.LogError("Body " + i + " does not have an NBody script attached.");
				return;
			}
		}
		SetBodies(nbodies);
		if (gravityEngine.massScale != 1.0f) {
			Debug.LogError("Three body solutions will not work as expected when massScale != 1");
		}
	}

	void FixedUpdate() {
		if (!running) {
			running = true; 
			gravityEngine.SetEvolve(true);
		}
	}

	private void SetBodies(NBody[] nbodies) {
		double[,] x = new double[3,3];
		double[,] v = new double[3,3];
		GravityEngine.Algorithm algorithm = GravityEngine.Algorithm.AZTRIPLE;
		// Scale is a suggested visual scale based on how solution evolves. 
		// Currently not used. 
		float scale = 1.0f; 	// visual scale to display solution
		if ( !SolutionServer.Instance().GetData(solutionSet, solutionName,ref x, ref v, ref algorithm, ref scale)) {
				Debug.LogError("Could not find set " + solutionSet + " soln=" + solutionName);
				return;
		}
		for (int i=0; i < 3; i++) {
			nbodies[i].vel = new Vector3((float) v[i,0],(float) v[i,1],(float) v[i,2]);
			// Awkward: Inflate positions by the NBE scale factor so that when NBE adds them (and removes the scale
			// factor) it comes out as a wash. This is because the literature uses [-1..1] and this awkward approach
			// is easier than scaling the velocities and masses. 
			Vector3 position = new Vector3((float) x[i,0], (float) x[i,1], (float) x[i,2]) * gravityEngine.physToWorldFactor;
			nbodies[i].initialPos = position;
			nbodies[i].transform.position = position;
		}
		// solution server provides a per-solution algorithm
		GravityEngine.instance.algorithm = algorithm;
	}

}
