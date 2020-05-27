using UnityEngine;
using System.Collections;

/// <summary>
/// Spaceship.
/// A simple example of an object that is subject to the gravitational field and makes changes in 
/// own motion based on user input. 
///
/// Controls:
///   Arrow Keys: spacecraft pitch and yaw
///   Space: Pause/Resume
///   F: Fire engine
///
/// When paused the -/= keys can be used to set a course correction that will be applied when resume is pressed.
///
/// This script is to be attached to a model that is the child of an NBody object. 
/// The GravityEngine will perform the physical updates to position and velocity. 
///
/// Changes to the spaceship motion are via impulse changes to the velocity. 
///
/// </summary>

public class Spaceship : MonoBehaviour {

	//! Thrust scale 
	public float thrustPerKeypress = 1f;
	//! Rate of spin when arrow keys pressed (degress/update cycle)
	public float spinRate = 1f;
	//! Forward direction of the ship model. Thrust us applies in opposite direction to this vector.
	public Vector3 axis = Vector3.forward;  //! Axis of thrust w.r.t. model

	public GameObject thrustCone;

    public OrbitPredictor orbitPredictor;

	private Vector3 axisN; // normalized axis
	private NBody nbody; 

	private Vector3 coneScale; // initial scale of thrust cone

	private float thrustSize; // thrust size set when paused. 

	private bool running; 

	// Use this for initialization
	void Start () {
		if (transform.parent == null) {
			Debug.LogError("Spaceship script must be applied to a model that is a child of an NBody object.");
			return;
		}
		nbody = transform.parent.GetComponent<NBody>();
		if (nbody == null) {
			Debug.LogError("Parent must have an NBody script attached.");
		}
		axisN = Vector3.Normalize(axis);
		running = true;
		//GravityEngine.instance.SetEvolve(running);
		//GravityEngine.instance.Setup();

		coneScale = thrustCone.transform.localScale;

	}


	// Update is called once per frame
	void Update () {
		if (nbody == null) {
			return; // misconfigured
		}
		Vector3 thrust = axisN;
		if (Input.GetKeyDown(KeyCode.Space)) {
			running = !running;
			if (running && thrustSize > 0) {
				thrust = thrustSize * axisN;
				thrust = transform.rotation * thrust;
				GravityEngine.instance.ApplyImpulse(nbody, thrust);
				// reset thrust
				thrustSize = 0f;
				SetThrustCone(thrustSize);
			}
			GravityEngine.instance.SetEvolve(running);

		} else if (Input.GetKey(KeyCode.F)) {
			thrust = thrustPerKeypress * axisN;
			thrust = transform.rotation * thrust;
			GravityEngine.instance.ApplyImpulse(nbody, thrust);

		} else {
			Quaternion rotation = Quaternion.identity;
			if (Input.GetKey(KeyCode.A)) {
				rotation = Quaternion.AngleAxis( spinRate, Vector3.forward);

			} else if (Input.GetKey(KeyCode.D)) {
				rotation = Quaternion.AngleAxis( -spinRate, Vector3.forward);

			} else if (Input.GetKey(KeyCode.W)) {
				rotation = Quaternion.AngleAxis( spinRate, Vector3.right);

			} else if (Input.GetKey(KeyCode.S)) {
				rotation = Quaternion.AngleAxis( -spinRate, Vector3.right);

			} 
			transform.rotation *= rotation;
		}
		// When paused check for course correction
		if (!running) {
			if (Input.GetKeyDown(KeyCode.Minus)) {
				thrustSize -= thrustPerKeypress; 
				if (thrustSize < 0)
					thrustSize = 0f; 
			} else 	if (Input.GetKeyDown(KeyCode.Equals)) {
                thrustSize += thrustPerKeypress; 
			}

			SetThrustCone(thrustSize);
			// In order for change in orbit to be see by the predictor, need to 
			// determine the new NBody velocity - but not set it (or all updates will
			// be cumulative). This way can see impact of rotating with a specific thrust setting.
			thrust = thrustSize * axisN;
			thrust = transform.rotation * thrust;
            // Orbit predictor will use this vel_phys value and update the orbit in the scene
			nbody.vel_phys = GravityEngine.instance.VelocityForImpulse(nbody, thrust);
		}

	}

	private const float thrustScale = 3f; // adjust for desired visual sensitivity

	private void SetThrustCone(float size) {
		Vector3 newConeScale = coneScale;
		newConeScale.z = coneScale.z * size * thrustScale;
		thrustCone.transform.localScale = newConeScale;
		// as cone grows, need to offset
		Vector3 localPos = thrustCone.transform.localPosition;
		// move cone center along spacecraft axis as cone grows
		localPos = -(size*thrustScale)/2f*axisN;
		thrustCone.transform.localPosition = localPos;
	}
}
