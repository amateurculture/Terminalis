using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manuever.
/// Holds a future course change for the spaceship. Will be triggered based on world time.
/// 
/// In some cases (orbital transfers) the value of the change will be recorded as a
/// scalar. In trajectory intercept cases, a vector velocity change will be provided.
/// 
/// Manuevers are added to the GE. This allows them to be run at the closest possible time
/// step (in general GE will do multiple time steps per FixedUpdate). Due to time precision
/// the resulting trajectory may not be exactly as desired. More timesteps in the GE will
/// reduce this error. 
/// 
/// Manuevers support sorting based on earliest worldTime. 
/// 
/// Maneuvers are always expressed in internal physics units of distance and velocity. 
/// 
/// </summary>
public class Maneuver : IComparer<Maneuver>  {

	//! time at which the maneuver is to occur (physical time in GE)
	public float worldTime;

	//! velocity change vector to be applied (if mtype is vector)
	public Vector3 velChange;

	//! scalar value of the velocity change in physics units (+ve means in-line with motion). Use GetDvScaled() for scaled value.    		
	public float dV;

	//! position at which maneuver should occur (if known, trajectories only)
	public Vector3 r;		

	//! NBody to apply the course correction to
	public NBody nbody;

    //! Position of the maneuver in internal physics units (used for error estimates and ManeuverRenderer)
    public Vector3d physPosition;

    /// <summary>
    /// Set of info to allow a Maneuver to be used to define additions to a Kepler Sequence. Only valid if
    /// the relativeTo field is non-NULL. 
    /// </summary>
    public Vector3d relativePos;
    public Vector3d relativeVel;
    public NBody relativeTo; 

    /// <summary>
    /// Type of maneuver:
    /// vector: apply a dV vector
    /// scalar: apply the scalar dV amount to the vector at time of maneuver
    /// setv: set the absolute velocity at the time of the maneuver
    /// </summary>
    public enum Mtype {vector, scalar, setv};

	//! Type of information about manuever provided
	public Mtype mtype = Mtype.vector; 

    //! template for the callback to be run when the maneuver is executed
	public delegate void OnExecuted(Maneuver m); 

	//! Delegate to be called when the maneuver is executed (optional)
	public OnExecuted onExecuted;

    /// <summary>
    /// Create a vector maneuver at the intercept point to match trajectory
    /// that was intercepted. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="intercept"></param>
	public Maneuver(NBody nbody, TrajectoryData.Intercept intercept) {
		this.nbody = nbody;
		worldTime = intercept.tp1.t;
		velChange = intercept.tp2.v - intercept.tp1.v;
		r = intercept.tp1.r;
		dV = intercept.dV;
	}

	// Empty constructor when caller wishes to fill in field by field
	public Maneuver() {

	}

    /// <summary>
    /// Return the dV in scaled units. 
    /// </summary>
    /// <returns></returns>
    public float GetDvScaled() {
        return dV / GravityScaler.GetVelocityScale();
    }

    /// <summary>
    /// Set the deltaV for a scalar maneuver in world units (e.g. in ORBITAL
    /// units set velocity in km/hr)
    /// </summary>
    /// <param name="newDv"></param>
    public void SetDvScaled(float newDv) {
        dV = newDv * GravityScaler.GetVelocityScale();
    }

    /// <summary>
    /// Set the velocity change vector in world units (e.g. in ORBITAL
    /// units set velocity in km/hr)
    /// </summary>
    /// <param name="newVel"></param>
    public void SetVelScaled(Vector3 newVel) {
        velChange = newVel * GravityScaler.GetVelocityScale();
    }

    /// <summary>
    /// Set the maneuver time in world units (e.g. in ORBITAL units in 
    /// hours). 
    /// </summary>
    /// <param name="time"></param>
    public void SetTimeScaled(float time) {
        worldTime = time / GravityScaler.GetGameSecondPerPhysicsSecond();
    }

    /// <summary>
    /// Execute the maneuver. Called automatically by Gravity Engine for maneuvers that
    /// have been added to the GE via AddManeuver(). 
    /// 
    /// Unusual to call this method directly. 
    /// </summary>
    /// <param name="ge"></param>
    public void Execute(GravityState gs)
    {
        double[] vel = new double[3] { 0, 0, 0 };
        gs.GetVelocityDouble(nbody, ref vel);
        switch(mtype)
        {
            case Mtype.vector:
                vel[0] += velChange[0];
                vel[1] += velChange[1];
                vel[2] += velChange[2];
                break;

            case Mtype.scalar:
                // scalar: adjust existing velocity by dV
                Vector3d vel3d = new Vector3d(ref vel);
                Vector3d change = vel3d.normalized * dV ;
                vel[0] += change[0];
                vel[1] += change[1];
                vel[2] += change[2];
                break;

            case Mtype.setv:
                // direct assignement (this mode normally used for debug)
                vel[0] = velChange[0];
                vel[1] = velChange[1];
                vel[2] = velChange[2];
                break;

        }
#pragma warning disable 162        // disable unreachable code warning
        if (GravityEngine.DEBUG) {
            if (!gs.isAsync) {
                Debug.Log("Applied manuever: " + LogString() + " engineRef.index=" + nbody.engineRef.index +
                    " engineRef.bodyType=" + nbody.engineRef.bodyType + " timeError=" + (worldTime - gs.time));
                Debug.LogFormat("r={0} v=({1},{2},{3}) ", Vector3.Magnitude(nbody.transform.position),
                            vel[0], vel[1],vel[2]);
            }
        }
#pragma warning restore 162        // enable unreachable code warning

        gs.SetVelocityDouble(nbody, ref vel);
    }

	public int Compare(Maneuver m1, Maneuver m2) {
			if (m1 == m2) {
				return 0;
			}
			if (m1.worldTime < m2.worldTime) {
				return -1;
			}
			return 1;
	}

	public string LogString() {
		return string.Format("Maneuver {0} t={1} type={2} dV={3} vel={4}", nbody.gameObject.name, worldTime, mtype, dV, velChange);
	}

}
