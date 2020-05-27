using UnityEngine;
using System.Collections;

/// <summary>
/// Orbit simple decay.
/// This script causes a simple decay of a body in orbit by applying a small
/// decay factor to the velocity on each frame. 
///
/// The decay factor is logarithmic - larger values cause orbits that decay more slowly
/// </summary>
public class OrbitSimpleDecay : MonoBehaviour {

	public float logDecayFactor = 4f;

	private float decayFactor = 0.0001f;
	private bool initOk; 
	private NBody nbody;

	// Use this for initialization
	void Start () {
		if (transform.parent == null) {
			Debug.LogError("Object needs to be a child of NBody (no parent)");
			return;
		}
		if (transform.parent.GetComponent<NBody>() == null) {
			Debug.LogError("Object needs to be a child of NBody (parent does not have NBody)");
			return;
		}
		nbody = GetComponent<NBody>();
		if (nbody == null) {
			Debug.LogError("Object needs to have an NBody component");
			return;
		}
		initOk = true;
		decayFactor = Mathf.Pow(10f, -1f*Mathf.Max(1f,logDecayFactor));
	}
	
	void FixedUpdate () {
		if (initOk && GravityEngine.instance.IsSetup()) {
			Vector3 velocity = GravityEngine.instance.GetVelocity(transform.gameObject);
			GravityEngine.instance.ApplyImpulse(nbody, -1f * decayFactor * velocity);
		}
	}

}
