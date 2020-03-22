using UnityEngine;
using System.Collections;

/// <summary>
/// Calculate the correct initial conditions for a specified elliptical orbit around a designated body OR
/// moves the object in a fixed ellipse determined by Kepler's equation. 
///
/// In Gravity Engine mode the orbit will evolve based on the global gravitation field and due to interactions with other bodies
/// the actual orbit may not be the ellipse shown in the Unity editor due to these perturbations. 
///
/// In KEPLER mode the orbit will be constrained to move on the ellipse specified. 
///
/// This script must be attached to a gameObject with an NBody component. 
///
/// </summary>

[RequireComponent(typeof(NBody))]
public class OrbitEllipse : EllipseBase, INbodyInit, IFixedOrbit, IOrbitScalable {

	public enum evolveType {GRAVITY_ENGINE, KEPLERS_EQN};

	//! Use GRAVITY_ENGINE to evolve or move in a fixed KEPLER orbit. 
	public evolveType evolveMode = evolveType.GRAVITY_ENGINE;

    //! Current world position
    private Vector3 position; 

    //! Current velocity in scaled units
    private Vector3 velocityScaled;
    private NBody nbody;

	public bool IsOnRails() {
		return (evolveMode == evolveType.KEPLERS_EQN);
	}

    // Scaling:
    // Setting the size of the ellipse is done via the ellipse base "a". This is
    // entered by the user in the units selected in the GE units chooser. Additionaly the
    // GE inspector may indicate a Unity Units per [km|AU] and this needs to be taken into account
    // in the Gizmo that draws the orbit. 
    //

	/// <summary>
	/// Inits the N body position and velocity based on the ellipse parameters and the 
	/// position and velocity of the parent. 
	/// </summary>
	/// <param name="physicalScale">Physical scale.</param>
	public void InitNBody(float physicalScale, float massScale) {

        base.Init();
		float a_phy = a_scaled/physicalScale;
        if (nbody == null) {
            nbody = GetComponent<NBody>();
        }
		
		// Phase is TRUE anomoly f
		float f = phase * Mathf.Deg2Rad;
        // Murray and Dermot 
        // (2.26)
        // This should really be (M+m), but assume m << M
        // (massScale is added in at the GE level)
        float mu = (centerNbody.mass + nbody.mass) * massScale;
		float n = Mathf.Sqrt( (float)(mu)/(a_phy*a_phy*a_phy));
		// (2.36)
		float denom = Mathf.Sqrt( 1f - ecc*ecc);
		float xdot = -1f * n * a_phy * Mathf.Sin(f)/denom;
		float ydot = n * a_phy * (ecc + Mathf.Cos(f))/denom;

		// Init functions are called in the engine by SetupOneBody and calls of parent vs children/grandchildren etc.
		// can be in arbitrary order. A child will need info from parent for position and velocity. Ensure parent
		// has inited. Init may get called more than once. 
        centerNbody.InitPosition(GravityEngine.Instance());

		Vector3 v_xy = new Vector3( xdot, ydot, 0);
        // if we're evolving, get the latest velocity
        if (centerNbody.engineRef != null) {
            centerNbody.UpdateVelocity();
        }
		Vector3 vphy = ellipse_orientation * v_xy + centerNbody.vel_phys;
		nbody.vel_phys = vphy;
		SetInitialPosition(nbody);
        if (evolveMode == evolveType.KEPLERS_EQN) {
            if (Application.isPlaying) {
                // Need to force an evolve to update position and velocity so that 
                // dependent objects get the correct values. 
                GravityEngine ge = GravityEngine.Instance();
                double[] r = new double[] { 0, 0, 0 };
                PreEvolve(ge.GetLengthScale(), ge.massScale);
                Evolve(ge.GetPhysicalTime(),  ref r);
            } else {
                // if in the editor/DrawGizmos all we care about is position and above clause
                // causes exceptions, since it asks GE for position/velocity
                position = nbody.initialPhysPosition;
            }
        }
	}	

	/// <summary>
	/// Inits from solar body.
	/// </summary>
	/// <param name="sbody">Sbody.</param>
	public void InitFromSolarBody(SolarBody sbody) {
		a = sbody.a; 
		ecc = sbody.ecc; 
		omega_lc = sbody.omega_lc;
		omega_uc = sbody.omega_uc; 
		inclination = sbody.inclination;
		phase = sbody.longitude;
		Init();
		ApplyScale(GravityEngine.Instance().GetLengthScale());
	}

	// Fixed motion code

	private float a_phy; 	// semi-major axis scaled for physics space
	private float orbitPeriod; 
	private float mean_anomoly_phase;
	private GravityEngine gravityEngine; 

	public float GetPeriod() {
		if (centerNbody == null)
			return 0;
		// Use Find to allow Editor to use method. 
		if (gravityEngine == null) {
			gravityEngine = (GravityEngine) FindObjectOfType(typeof(GravityEngine));
			if (gravityEngine == null) {
				Debug.LogError("Need GravityEngine in the scene");
				return 0;
			}
			base.Init();
		}
		a_phy = a_scaled/gravityEngine.GetPhysicalScale();
		float massScale = gravityEngine.massScale;
		orbitPeriod = 2f * Mathf.PI * Mathf.Sqrt(a_phy*a_phy*a_phy/((float)centerNbody.mass * massScale)); // G=1
		return orbitPeriod;
	}

    /// <summary>
    /// Called by GravityEngine to setup physics info prior to simulation
    /// Do not call this method directly. Instead ensure this object is added to the GravityEngine
    /// either by adding in a script or to the public bodies list.
    /// </summary>
    /// <param name="physicalScale">Physical scale.</param>
    public void PreEvolve(float physicalScale, float massScale) {
		if (ecc >= 1f) {
			ecc = 0.99f;
		}
		// Convert a to physical scale so the correct orbit time results w.r.t evolving bodies
		// Don't know this factor until GravityEngine calls here. 
		a_phy = a_scaled/physicalScale;
		base.CalculateRotation();
		// evolution relies on orbital period to determine current mean anomoly
		orbitPeriod = GetPeriod();

		// convert phase in true anomoly into mean anomoly phase
		float phase_rad = Mathf.Deg2Rad * phase;
		float phase_E = 2f * Mathf.Atan( Mathf.Sqrt((1-ecc)/(1+ecc))*Mathf.Tan(phase_rad/2f));
		mean_anomoly_phase = phase_E - ecc * Mathf.Sin(phase_E);
	}
		
	/// <summary>
	/// Called from the GravityEngine on FixedUpdate cycles to determine current position of body given
	/// the physics time evolution only when mode=KEPLERS_EQN.
	///
	/// This routine updates the game object position in game space and physics space. 
	///
	/// Do not call this method directly. 
	/// </summary>
	/// <param name="physicsTime">Physics time.</param>
	/// <param name="physicalScale">Physical scale.</param>
	/// <param name="r">Reference to array into which new position is placed.</param>
	public void Evolve (double physicsTime,  ref double[] r_new) {
		//  There is no simple expression for the position in an Elliptical orbit as a function of time. 
		//	(Expressions exist that describe position as a function of the angle in the orbit, but determining
		//	angle as a function of time results in an expression that is not elementary - Kepler's equation). 
		//			
		//	Here we follow the excellent development of the equations in "Gravity" (Poisson and Will, 2014). 
		//
		// following Gravity (Poisson & Will) Box 3.1
		// 1) First use Newton's root-finding method to determine eccentic anomoly u for a given t
		// The mean anomoly (angle from center of ellipse if motion was circular and uniform) is determined
		// from the orbit period and time evolved so far. 
		int loopCount = 0; 
		// mean_anomoly is the angle around the circle of radius a if the body moved uniformly
		// (which it does not)
		float mean_anomoly = 2f * Mathf.PI * (float)physicsTime/orbitPeriod;
		mean_anomoly += mean_anomoly_phase;
		float u = mean_anomoly; // seed with mean anomoly
		float u_next = 0;
		const int LOOP_LIMIT = 20;
		while(loopCount++ < LOOP_LIMIT) {
			// this should always converge in a small number of iterations - but be paranoid
			u_next = u + (mean_anomoly - (u - ecc * Mathf.Sin(u)))/(1 - ecc * Mathf.Cos(u));
			if (Mathf.Abs(u_next - u) < 1E-5)
				break;
			u = u_next;
		}
        if (loopCount >= LOOP_LIMIT) {
            u_next = u + (mean_anomoly - (u - ecc * Mathf.Sin(u))) / (1 - ecc * Mathf.Cos(u));
            Debug.LogWarning(string.Format("Failed to converge (use best estimate) err={0:E5}", Mathf.Abs(u_next - u))); 
            // keep going anyway
        }
        // 2) eccentric anomoly is angle from center of ellipse, not focus (where centerObject is). Convert
		//    to true anomoly, f - the angle measured from the focus. (see Fig 3.2 in Gravity) 
		float cos_f = (Mathf.Cos(u) - ecc)/(1f - ecc * Mathf.Cos(u));
        float sin_f = (Mathf.Sqrt(1 - ecc*ecc) * Mathf.Sin (u))/(1f - ecc * Mathf.Cos(u));
		float r = a_phy * (1f - ecc*ecc)/(1f + ecc * cos_f);
		position = new Vector3( r * cos_f, r * sin_f, 0);

        // Need a precise (right now) value for centerBody for best results, get from the engine
		// move from XY plane to the orbital plane and scale to world space
		// orbit position is WRT center
		position =  ellipse_orientation * position + GravityEngine.Instance().GetPhysicsPosition(centerNbody);
        // fill in r. GE will use this position.
        r_new[0] = position.x;
		r_new[1] = position.y;
		r_new[2] = position.z;

        // determine velocity 
        // (C&P from above for performance)
        // Murray and Dermot 
        // (2.26)
        // This should really be (M+m), but assume m << M
        // (massScale is added in at the GE level)
        float n = Mathf.Sqrt((float)(centerNbody.mass * GravityEngine.Instance().massScale) / (a_phy * a_phy * a_phy));
        // (2.36)
        float denom = Mathf.Sqrt(1f - ecc * ecc);
        float xdot = -1f * n * a_phy * sin_f / denom;
        float ydot = n * a_phy * (ecc + cos_f) / denom;
        Vector3 v_xy = new Vector3(xdot, ydot, 0);
        velocityScaled = ellipse_orientation * v_xy + GravityEngine.Instance().GetScaledVelocity(centerNbody);
        phase = Mathf.Atan2(sin_f, cos_f) * Mathf.Rad2Deg;
    }


    public void Move(Vector3 moveBy) {
        position += moveBy;
    }

    public void GEUpdate(GravityEngine ge) {
        nbody.GEUpdate(position, velocityScaled, ge);
    }

    public Vector3 GetVelocity() {
        return velocityScaled;
    }

    /// <summary>
    /// Return the scene position 
    /// </summary>
    /// <returns></returns>
    public Vector3 GetPosition() {
        return position;
    }

	/// <summary>
	/// Convert Mean Anomoly to True Anomoly for an ellipse with eccentricity e. 
	/// </summary>
	/// <returns>True Anomoly in degrees.</returns>
	/// <param name="m">Mean Anomoly. (degrees)</param>
	/// <param name="e">Eccentricty.</param>
	public static float MeanToTrueAnomoly(float m, float e) {
		int loopCount = 0; 
		float u = m * Mathf.Deg2Rad; // seed with mean anomoly
		float u_next = 0;
		// some extreme comet orbits (e.g. Halley) need a LOT of iterations
		const int LOOP_LIMIT = 200;
		while(loopCount++ < LOOP_LIMIT) {
			// this should always converge in a small number of iterations - but be paranoid
			u_next = u + (m - (u - e * Mathf.Sin(u)))/(1 - e * Mathf.Cos(u));
			if (Mathf.Abs(u_next - u) < 1E-6)
				break;
			u = u_next;
		}
		if (loopCount >= LOOP_LIMIT)	
			Debug.LogError("Failed to converge u_n=" + u_next);	// keep going anyway

		// 2) eccentric anomoly is angle from center of ellipse, not focus (where centerObject is). Convert
		//    to true anomoly, f - the angle measured from the focus. (see Fig 3.2 in Gravity) 
		float cos_f = (Mathf.Cos(u) - e)/(1f - e * Mathf.Cos(u));
        float sin_f = (Mathf.Sqrt(1 - e*e) * Mathf.Sin (u))/(1f - e * Mathf.Cos(u));
		float f_deg = Mathf.Atan2(sin_f, cos_f) * Mathf.Rad2Deg;
		//Debug.Log("m=" + m + " E=" + u * Mathf.Deg2Rad + " f=" + f_deg);
        return f_deg;
	}

	/// <summary>
	/// Apply scale to the orbit. This is used by the inspector scripts during
	/// scene setup. Do not use at run-time.
	/// </summary>
	/// <param name="scale">Scale.</param>
	public void ApplyScale(float scale) {
		if (paramBy == ParamBy.AXIS_A){
			a_scaled = a * scale;
            p_scaled = a_scaled * (1 - ecc);
		} else if (paramBy == ParamBy.CLOSEST_P) {
			p_scaled = p * scale; 
			a_scaled = p * scale/(1-ecc);
		}
		UpdateOrbitParams();
        nbody = GetComponent<NBody>();
        SetInitialPosition(nbody);
	}

	public void Log(string prefix) {
		Debug.Log(string.Format("orbitEllipse: {0} a={1} e={2} i={3} Omega={4} omega={5} phase={6}", 
								prefix, a, ecc, inclination, omega_uc, omega_lc, phase));
	}

    public void SetNBody(NBody nbody) {
        this.nbody = nbody;
    }

    public NBody GetCenterNBody() {
        return centerNbody;
    }

    public Vector3 ApplyImpulse(Vector3 impulse) {
        Debug.LogWarning("Not supported");
        return new Vector3(float.NaN, float.NaN, float.NaN);
    }

    public void UpdatePositionAndVelocity(Vector3 pos, Vector3 vel) {
        throw new System.NotImplementedException();
    }

    public string DumpInfo() {
        return string.Format("      Ellipse: a={0:0.00} e={1:0.00}, i={2:0.00} Om={3:0.00} om={4:0.00} center={5}\n",
            a_scaled, ecc, inclination, omega_uc, omega_lc, centerNbody.name);
    }

}
