using UnityEngine;
using System.Collections;

/// <summary>
/// Orbit renderer.
/// Create the positions for a parents Orbit script (OrbitEllipse or OrbitHyper) and create the 
/// positions for the attached LineRenderer. (Use parent since a Line Renderer wants to be on a gameobject
/// of its own).
///
/// The position array is calculated in the associted editor script when a parameter change in the parent
/// orbit object is detected.
///
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class OrbitRenderer : MonoBehaviour {

	public int numPoints = 100;

    private NBody centerNBody;
	private IOrbitPositions orbitP; 
	private LineRenderer lineR;

	void Start () {
		// calculate positions for the LineRenderer (cannot assume Editor script has been invoked to do this)
		GameObject parent = transform.parent.gameObject;
 		if (parent != null) {
			EllipseBase ellipseBase = parent.GetComponent<EllipseBase>();
			if (ellipseBase != null) {
                centerNBody = ellipseBase.centerObject.GetComponent<NBody>();
                orbitP = ellipseBase;
			} else {

                OrbitUniversal orbitU = parent.GetComponent<OrbitUniversal>();
                if (orbitU != null) {
                    centerNBody = orbitU.GetCenterNBody();
                    orbitP = orbitU;
                } else {
                    OrbitHyper orbitHyper = parent.GetComponent<OrbitHyper>();

                    if (orbitHyper != null) {
                        centerNBody = orbitHyper.centerObject.GetComponent<NBody>();
                        orbitP = orbitHyper;
                    } else {
                        Debug.LogWarning("Parent object must have OrbitEllipse or OrbitHyper - cannot compute positions for line");
                    }
                }
			}
		} else {
			Debug.LogWarning("No parent object - cannot compute positions for line");
		}
        if (centerNBody == null) {
            Debug.LogError("Parent must have an NBody");
        }
        lineR = GetComponent<LineRenderer>();
		lineR.positionCount = numPoints;
	}

	// The center of the orbit may be moving, so need to update each cycle
	void FixedUpdate() {
        Vector3 centerPos = GravityEngine.Instance().GetPhysicsPosition(centerNBody);
        lineR.SetPositions(orbitP.OrbitPositions(numPoints, centerPos, true));
	}
}
