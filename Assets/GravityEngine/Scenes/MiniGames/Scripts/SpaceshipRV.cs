using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SpaceshipRV
/// </summary>
public class SpaceshipRV : MonoBehaviour {

	//! graphic object to indicate the direction and magnitude of thrust
	public GameObject thrustCone;

	public string shipName;

    public RocketEngine rocketEngine; 

	private Vector3 axisN; // normalized axis for thrust direction

	private float thrustSize; // thrust size set when paused. 

	//! parent game object must have an NBody component
	private NBody nbody; 

	private Vector3 shipVelocity; 
	private Vector3 shipPosition; 
	private Vector3 coneScale; // initial scale of thrust cone

	private bool last_running; 

	private Trajectory trajectory;

	// Keep a copy of maneuvers for display purpose. Need to register callback to 
	// be notified when are cleaned up. 
	private List<Maneuver> maneuverList;

    private Vector3 initialOrientation; 

	// Use this for initialization
	void Start () {
		nbody = transform.parent.GetComponent<NBody>();
		if (nbody == null) {
			Debug.Log("Configuration error. Parent of SpaceshipRV must have NBody");
		}
		// Consistency checks. Each spaceship needs to have a Trajectory and data collection 
		// enabled, so that trajectory intercepts can be plotted.
		trajectory = nbody.GetComponentInChildren<Trajectory>();
		if (trajectory == null) {
			Debug.LogError("SpaceshipRV requires a child with a trajectory component");
		} 

		if (thrustCone == null) {
			Debug.LogError("Thrust cone member not set");
		} else {
			coneScale = thrustCone.transform.localScale;
		}
		// convention for alignment of the Spaceship model. Used to determine thrust direction
		axisN = Vector3.up;

		maneuverList = new List<Maneuver>();

        initialOrientation = transform.rotation * axisN;
	}

	public NBody GetNBody() {
		return nbody;
	}

	public Trajectory GetTrajectory() {
		return trajectory;
	}

	public void Rotate(float dTheta, Vector3 axis) {

		transform.rotation *= Quaternion.AngleAxis( dTheta, axis);

        // If there is a rocket engine attached, update it's thrust engine
        // (This assumes that engine is fixed with ship and not gimbaled - could allow some engine movement
        // independent of ship movement for more realism)
        // Convey the rotation w.r.t. where we started (not absolute rotation)
        if (rocketEngine != null) {
            Vector3 orientation = transform.rotation * axisN;
            rocketEngine.SetRotation(Quaternion.FromToRotation(initialOrientation, orientation));
        }
        if (thrustSize != 0) {
            // force trajectory re-compute and thrust vector update due to rotation
            UpdateThrust(0); 
        }
    }

    public void RotateToVector(Vector3 newAxis) {
        transform.rotation = Quaternion.FromToRotation(axisN, newAxis);
    }
    //--------------On-board Engine Control-----------------------------

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

	/// <summary>
	/// Updates the thrust when the Gravity Engine is paused by using the updated thrust
	/// and the ships velocity when the scene was paused. 
	/// </summary>
	/// <param name="thrustChange">Thrust change.</param>
	public void UpdateThrust(float thrustChange) {
		thrustSize += thrustChange;
		Vector3 thrust = thrustSize * axisN;
		thrust = transform.rotation * thrust;
		SetThrustCone(thrustSize);
		// force back to initial velocity to add the current thrust to undo any past changes
		GravityEngine.instance.UpdatePositionAndVelocity(nbody, shipPosition, shipVelocity);
		// apply impulse will update trajectories
		GravityEngine.instance.ApplyImpulse(nbody, thrust);
        Debug.Log("Update thrust from " + shipVelocity + " with impulse " + thrust);
        // Orbit predictor will use this vel_phys value and update the orbit in the scene
        nbody.vel_phys = GravityEngine.instance.VelocityForImpulse(nbody, thrust);
    }

    /// <summary>
    /// Set thrust to a specific value
    /// </summary>
    /// <param name="thrust"></param>
    public void ResetThrust() {
        thrustSize = 0f;
        SetThrustCone(thrustSize);
    }

    /// <summary>
    /// Fires the engine.
    /// Used when the Gravity Engine is running.
    /// </summary>
    /// <param name="thrustPerKeypress">Thrust per keypress.</param>
    public void FireEngine(float thrustPerKeypress) {
		Vector3 thrust = thrustPerKeypress * axisN;
		thrust = transform.rotation * thrust;
		GravityEngine.instance.ApplyImpulse(nbody, thrust);
	}

	// Called once per frame from TI_Controller Update()

	public void Run(bool running, float worldTime) {

		if (last_running != running) {
			if (!running) {
				// Need to record the pos/vel, so can modify the velocity based on thrust changes
				shipVelocity = GravityEngine.instance.GetVelocity(transform.parent.gameObject);
				shipPosition = transform.parent.position;
			} 
			last_running = running;
		}

	}

	protected void ManeuverExecutedCallback(Maneuver m) {
		maneuverList.Remove(m);
	}

	/// <summary>
	/// Add the manuever specified by the intercept to add a course correction
	/// at the intercept time to have the spaceship match the target course. 
	/// 
	/// Maneuvers are passed on to the GravityEngine to be executed on the correct
	/// integrator timeslice. The SpaceshipRV maintains a copy so they can be displayed/tracked. 
	/// </summary>
	/// <param name="intercept">Intercept.</param>
	public void SetManeuver(TrajectoryData.Intercept intercept) {
		Maneuver m = new Maneuver(nbody, intercept);
		m.onExecuted = ManeuverExecutedCallback;
		GravityEngine.Instance().AddManeuver(m);
		maneuverList.Add(m);
	}

	/// <summary>
	/// Sets a series of maneuvers required to execute an orbital transfer. 
	/// 
	/// Maneuvers are passed on to the GravityEngine to be executed on the correct
	/// integrator timeslice. 
	/// </summary>
	///
	/// <param name="transfer">The transfer</param>
	public void SetTransfer(OrbitTransfer transfer) {
		foreach (Maneuver m in transfer.GetManeuvers()) 
		{
			m.onExecuted = ManeuverExecutedCallback;
			GravityEngine.Instance().AddManeuver(m);
			maneuverList.Add(m);
		}
	}

	public string[] ManeuverString() {
		string[] str = new string[maneuverList.Count];
		int i = 0; 
		foreach(Maneuver m in maneuverList) {
			str[i++] = string.Format("T={0:F1} dV={1:F2}",  m.worldTime, m.dV);
		}
		return str;
	}

	public void LogManuevers() {
		foreach (string s in ManeuverString() ) 
		{
			Debug.Log(s);
		}
	}

}
