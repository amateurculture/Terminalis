using UnityEngine;
using System.Collections;

/// <summary>
/// SpaceshipWithTrajectory.
/// A simple example of an object that is subject to the gravitational field and makes changes in 
/// own motion based on user input. A trajectory component is updated when there is change in the
/// spacecraft's path.
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
/// As changes are made to the spaceship velocity it is necessary to re-compute the predicted trajectory. 
///
/// Changes to the spaceship motion are via impulse changes to the velocity. 
///
/// </summary>

public class SpaceshipWithTrajectory : MonoBehaviour {

	//! Thrust scale 
	public float thrustPerKeypress = 1f;
	//! Rate of spin when arrow keys pressed (degress/update cycle)
	public float spinRate = 1f;

	//! Optional element to mark trajectory intercepts
	public TrajectoryIntercepts trajIntercepts;

	//! Forward direction of the ship model. Thrust us applies in opposite direction to this vector.
	public Vector3 axis = Vector3.forward;  //! Axis of thrust w.r.t. model

	public GameObject thrustCone;

	private Vector3 axisN; // normalized axis
	private NBody nbody; 

	private Vector3 coneScale; // initial scale of thrust cone

	private float thrustSize; // thrust size set when paused. 

	private bool running = true; 

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
		GravityEngine.instance.SetEvolve(running);
		coneScale = thrustCone.transform.localScale;

	}

	private Vector3 shipVelocity; 
	private Vector3 shipPosition; 

	// Update is called once per frame
	void Update () {
		if (nbody == null) {
			return; // misconfigured
		}
		Vector3 thrust = axisN;
		bool rotationChanged = false;
		if (Input.GetKeyDown(KeyCode.Space)) {
			running = !running;
			if (running && thrustSize > 0) {
				thrust = thrustSize * axisN;
				thrust = transform.rotation * thrust;
				GravityEngine.instance.ApplyImpulse(nbody, thrust);
				// reset thrust
				thrustSize = 0f;
				SetThrustCone(thrustSize);
			} else {
				// into not running:
				// - get the spaceship velocity
				shipVelocity = GravityEngine.instance.GetVelocity(transform.parent.gameObject);
				shipPosition = transform.parent.position;
			}
			GravityEngine.instance.SetEvolve(running);

		} else if (Input.GetKey(KeyCode.F)) {
			thrust = thrustPerKeypress * axisN;
			thrust = transform.rotation * thrust;
			GravityEngine.instance.ApplyImpulse(nbody, thrust);

		} else if (Input.GetKeyDown(KeyCode.R)) {
				// debug code to assess cost of restart
				GravityEngine.instance.TrajectoryRestart();

		} else if (Input.GetKeyDown(KeyCode.T)) {
				// toggle trajectory prediction
				bool trajPrediction = GravityEngine.instance.trajectoryPrediction;
				GravityEngine.instance.SetTrajectoryPrediction(!trajPrediction);
		} else if (Input.GetKeyDown(KeyCode.M)) {
			if (trajIntercepts != null) {
				trajIntercepts.ComputeAndMarkIntercepts(1f, 2f, 1f);
			}
		} else {
			// check for rotations
			Quaternion rotation = Quaternion.identity;
			if (Input.GetKey(KeyCode.A)) {
				rotation = Quaternion.AngleAxis( spinRate, Vector3.forward);
				rotationChanged = true;
			} else if (Input.GetKey(KeyCode.D)) {
				rotation = Quaternion.AngleAxis( -spinRate, Vector3.forward);
				rotationChanged = true;
			} else if (Input.GetKey(KeyCode.W)) {
				rotation = Quaternion.AngleAxis( spinRate, Vector3.right);
				rotationChanged = true;
			} else if (Input.GetKey(KeyCode.S)) {
				rotation = Quaternion.AngleAxis( -spinRate, Vector3.right);
				rotationChanged = true;
			}

			if (rotationChanged) {
				transform.rotation *= rotation;
			}
		}

        // When paused check for course correction
        if (!running) {
			bool thrustChanged = false;
			if (Input.GetKeyDown(KeyCode.Minus)) {
				thrustSize -= thrustPerKeypress; 
				if (thrustSize < 0)
					thrustSize = 0f; 
				thrustChanged = true;
			} else 	if (Input.GetKeyDown(KeyCode.Equals)) {
				thrustSize += thrustPerKeypress; 
				thrustChanged = true;
			}
			if (thrustChanged || (rotationChanged && (thrustSize > 0))) {
				SetThrustCone(thrustSize);
				// To update the orbit prediction, need set the velocity
				thrust =  transform.rotation * (thrustSize * axisN);
				// force back to initial velocity to add the current thrust
				GravityEngine.instance.UpdatePositionAndVelocity(nbody, shipPosition, shipVelocity);
				GravityEngine.instance.ApplyImpulse(nbody, thrust);
			}
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
