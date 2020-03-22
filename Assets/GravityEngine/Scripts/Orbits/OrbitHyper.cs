using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

/// <summary>
///
/// Class used to handle the hyperbolic orbital parameters and draw the orbit path in the editor using Gizmos. 
///
/// How to specify an hyperbola in 3D:
///
///    p - pericenter - distance from focus of hyperbola to point of closest approach to that focus
///
/// shape: controlled by ecc (eccentricity) 0 for a circle, 0.99 for a very long thin ellipse
///
/// orientation:
///  The standard orbit parameters are used. You can develop some intuition for these by chaging them in the Editor and observing
///  the change in the orbit. 
///
///  Orientation is defined with respect to the positive X axis. 
///    omega (lower case) - is a rotation in the plane of the orbit 
///    Inclination - is the tilt of the closest approach vector to the XY plance
///    Omega (capital Omega) - is the rotation around Z after preceeding rotations
///
/// </summary>
public class OrbitHyper : MonoBehaviour, INbodyInit, IOrbitPositions, IOrbitScalable, IFixedOrbit  {

	public enum EvolveType {GRAVITY_ENGINE, KEPLERS_EQN};
	//! Use GRAVITY_ENGINE to evolve or move in a fixed KEPLER orbit. 
	public EvolveType evolveMode = EvolveType.GRAVITY_ENGINE;

	//! object to orbit around (if null, will take parent game object)
	public GameObject centerObject; 
	
	// Orbit parameters (user control via FixedEllipseEditor)
	// These parameters are in world space. 
	//! eccentricity (0..1, 0=circle, 1=linear)
	public float ecc = 2.0f; 			

	/// <summary>
	/// Hyperbola parameters:
	/// The definition is typically in terms of semi-parameter p (semi-latus recturm) but using the
    /// perispase (closest approach) is more user-friendly. 
	///
	/// a,p  are calculated from the periapse and used for orbital calculations internally.
	///
	/// </summary>

	//! point of closest approach in selected units (should really be called periapse, but for backwards compat. leave it)
	public float perihelion = 10f; 		
	//! point of closest approach in internal physics units
	private float perihelion_phys = 10f;

    //! fraction of the branch of the hyperbola to display in OrbitPositions
    public float branchDisplayFactor = 0.5f;

	//! semi-major axis - based on paramBy user can specify a OR p. a = peri/(1-ecc)
	private float a_scaled;

	//! "longitude of ascending node" - angle from x-axis to line from focus to pericenter
	public float omega_uc; 		
	//! "argument of perienter" - angle from ascending node to pericenter
	public float omega_lc; 		
	//! inclination (degrees!)
	public float inclination; 	
	//! initial distance from focus
	public float r_initial = 10f;
    private float r_initial_phy = 10f; 

	//! initial distance on outbound leg
	public bool r_initial_outbound = false;

    public bool r_start_flip = false;

	private float b; // hyperbola semi-minor axis

    private Vector3 centerPos;

	protected Quaternion hyper_orientation;

	protected Vector3 xunit = Vector3.right;
	protected Vector3 yunit = Vector3.up;
	protected Vector3 zunit = Vector3.forward;

	protected NBody centerNbody;

    private float mean_anomoly_phase;
    private float orbit_period;

    // true anomoly in radian (nu)
    private float phase_nu; 

    //! Current world position
    private Vector3 position;

    //! Current velocity in scaled units
    private Vector3 velocityScaled;
    private NBody nbody;

    // Ugh. Can we avoid a mode flag?
    private bool initFromOrbitData = false;

    // center body mass in internal units. Only valid during Kepler evolution
    private float mu;

    // semi-parameter/semi-latus rectum
    private float p; 

    public void SetNBody(NBody nbody) {
        this.nbody = nbody;
    }

    /// <summary>
    /// Init the hyperbola, verify a center body is present and determine orientation.
    /// </summary>
    public void Init () {
        CalcOrbitParams();
        centerNbody = OrbitUtils.GetCenterNbody(transform, centerObject);
		CalculateRotation();

        NBody nbody = GetComponent<NBody>();
        // particle ring would not have an NBody
        if (nbody != null) {
            SetInitialPosition(nbody);
        }
    }

    public void InitFromOrbitData(OrbitData od) {
        // a is inited from perihilion 
		perihelion_phys = od.perihelion;
        // @TODO: This should be in selected units!
        perihelion = GravityScaler.ScaleDistancePhyToScene( od.perihelion);
		ecc = od.ecc;
        omega_lc = od.omega_lc;
		omega_uc = od.omega_uc; 
		inclination = od.inclination;
        phase_nu = od.phase * Mathf.Deg2Rad;
        // normally a should be negative, but we made it positive...
        p = od.a * (1 - ecc * ecc);
        // 
        float denom = 1 + ecc * Mathf.Cos(phase_nu);
        Vector3 r_pqw = new Vector3(p * Mathf.Cos(phase_nu) / denom, p * Mathf.Sin(phase_nu) / denom, 0);
        r_initial_phy = r_pqw.magnitude;

        initFromOrbitData = true;
		Init();
        PreEvolve(GravityEngine.Instance().physToWorldFactor, GravityEngine.Instance().massScale);
	}

	/// <summary>
	/// Sets the center body and initializes the hyperbola configuration.
	/// </summary>
	/// <param name="centerBody">Center body.</param>
	public void SetCenterBody(GameObject centerBody) {
		centerObject = centerBody; 
		Init(); 
	}
	
	// Update the derived orbit parameters
	protected void CalcOrbitParams() {
		// Protect against scripts changing ecc to < 1
		if (ecc < 1f) {
		    Debug.LogWarning("Detected ecc < 1. Set to 1.01");
		    ecc = 1.01f; 
		}
		// Roy has this as p = a(e^2-1) but says when f=0, r=a(e-1)
		// Wolfram has p = a (e^2-1)/e
        // p is semi-latus rectum/semi-parameter, not periapse
		// p is the distance at f = +/- Pi/2  i.e. where the line x=const through the focus
		// intercepts the hyperbola. The point of closest approach is when f=0, cosf=1, r=perihelion/(1+e)
		a_scaled = perihelion_phys/(ecc-1f); // flipped so a_scaled > 0. Awkward.
		b = a_scaled * Mathf.Sqrt( ecc*ecc-1f);
        // Also flipped 
        p = a_scaled * (ecc * ecc - 1f);
    }

    /// <summary>
    /// Get the semi-parameter p.
    /// </summary>
    /// <returns></returns>
    public float GetSemiParam() {
        return p;
    }

    protected void CalculateRotation() {
		// Following Murray and Dermot Ch 2.8 Fig 2.14
		// Quaternions go L to R (matrices are R to L)
		hyper_orientation = Quaternion.AngleAxis(omega_uc, zunit ) *
							  Quaternion.AngleAxis(inclination, xunit) * 
							  Quaternion.AngleAxis(omega_lc, zunit);
	}

    // Vallado Algorithm 10, p118
    // (generic, will work for Ellipse as well, provided special cases are handled)
    // Not used yet.
    private void SetInitialPosition(NBody nbody) {
        // phase is in 
        float denom = 1 + ecc * Mathf.Cos(phase_nu);
        Vector3 r_pqw = new Vector3(p * Mathf.Cos(phase_nu)/denom, p * Mathf.Sin(phase_nu)/denom, 0);
        r_initial_phy = r_pqw.magnitude;
        Vector3 r = hyper_orientation * r_pqw;

        // orbit position is WRT center. Could be adding dynamically to an object in motion, so need current position. 
        Vector3 centerPos = Vector3.zero;
        // used by widgets - so need to get explcitly
        centerNbody = OrbitUtils.GetCenterNbody(this.transform, centerObject);
        if (centerNbody.engineRef != null) {
            centerPos = GravityEngine.Instance().GetPhysicsPosition(centerNbody);
        } else {
            // setup - not yet added to GE
            centerPos = centerNbody.initialPhysPosition;
        }
        nbody.initialPhysPosition = r + centerPos;

    }

    private float ThetaForR(float r) {
		float theta = 0; 
		// solve r_i = a(e^2-1)/(1+eCos(theta)) for theta
		if (Mathf.Abs(r_initial_phy) > 1E-2) {
			float arg = ((a_scaled*(ecc*ecc-1))/ r_initial_phy - 1f)/ecc;
			arg = Mathf.Max(-1f, arg);
			arg = Mathf.Min(1f, arg);
			theta = Mathf.Acos(arg);
		}
		return theta;
	}

	private float RforTheta(float theta) {
		return a_scaled * ( ecc* ecc - 1)/(1f + ecc * Mathf.Cos(theta));
	}

	public bool IsOnRails() {
		return (evolveMode == EvolveType.KEPLERS_EQN);
	}

	/// <summary>
	/// Inits the N body position and velocity based on the hyperbola parameters and the 
	/// position and velocity of the parent. 
	/// </summary>
	/// <param name="physicalScale">Physical scale.</param>
	public void InitNBody(float physicalScale, float massScale) {

        Init();
		float a_phy = a_scaled/physicalScale;
        if (nbody == null) {
            nbody = GetComponent<NBody>();
        }
		
		// Phase is TRUE anomoly f
		float f = ThetaForR(r_initial_phy);
        // Murray and Dermot (2.20)
        float mu = (centerNbody.mass + nbody.mass) * massScale;
        float n = Mathf.Sqrt( mu/(a_phy*a_phy*a_phy));
		float denom = Mathf.Sqrt( ecc*ecc - 1f);
		// reverse sign from text to get prograde motion
		float xdot = 1f * n * a_phy * Mathf.Sin(f)/denom;
		float ydot = -1f * n * a_phy * (ecc + Mathf.Cos(f))/denom;
		if (!r_initial_outbound) {
			xdot *= -1f;
			ydot *= -1f;
		}

		// Init functions are called in the engine by SetupOneBody and calls of parent vs children/grandchildren etc.
		// can be in arbitrary order. A child will need info from parent for position and velocity. Ensure parent
		// has inited.
		INbodyInit centerInit = centerObject.GetComponent<INbodyInit>();
		if (centerInit != null) {
			centerInit.InitNBody(physicalScale, massScale);
		}
        if (centerNbody.engineRef != null) {
            centerPos = GravityEngine.Instance().GetPhysicsPosition(centerNbody);
        } else {
            // setup - not yet added to GE
            centerPos = centerNbody.initialPhysPosition;
        }

        Vector3 v_xy = new Vector3( xdot, ydot, 0);
		Vector3 vphy = hyper_orientation * v_xy + centerNbody.vel_phys;
		nbody.vel_phys = vphy;
	}	

	private Vector3 PositionForTheta(float theta) {
		float r = RforTheta(theta);
		Vector3 position = new Vector3( -r * Mathf.Cos (theta), r * Mathf.Sin (theta), 0);
		// move from XY plane to the orbital plane
		Vector3 newPosition = hyper_orientation * position; 
		// orbit position is WRT center
		newPosition += centerPos;
        return newPosition;
	}

    /// <summary>
    /// Given an angle (radians) determine the position on the left branch of the hyperbola. 
    /// </summary>
    /// <param name="theta"></param>
    /// <param name="centerPos"></param>
    /// <returns></returns>
	public Vector3 PositionForThetaLeftBranch(float theta, Vector3 centerPos) {
		float r = RforTheta(theta);
		// flip sign of X to get left branch
		Vector3 position = new Vector3( r * Mathf.Cos (theta), r * Mathf.Sin (theta), 0);
		// move from XY plane to the orbital plane
		Vector3 newPosition = hyper_orientation * position; 
		// orbit position is WRT center
		newPosition += centerPos;
		return newPosition;
	}

    /// <summary>
    /// Determine the position in physics space given a Y position wrt the focus.
    /// for the hyperbola. Use Cartesian co-ords since angles are very twitchy for hyperbolas.
    /// </summary>
    /// <param name="y"></param>
    /// <param name="cPos"></param>
    /// <returns></returns>
	private Vector3 PositionForY(float y, Vector3 cPos) {
		float x = a_scaled * Mathf.Sqrt( 1 + y*y/(b*b));
		// focus is at x = -(a*e), want to translate to origin is at focus
        // -ve x to take the left branch
		Vector3 position = new Vector3( -x + a_scaled*ecc, y, 0);
		// move from XY plane to the orbital plane
		Vector3 newPosition = hyper_orientation * position; 
		// orbit position is WRT center
		newPosition += cPos;
        return newPosition;
	}

	/// <summary>
	/// Calculate an array of orbit positions. Used by the OrbitPredictor, OrbitRenderer and Editor
	/// Gimzo to illustrate the hyperbola. 
	/// </summary>
	/// <returns>The positions.</returns>
	/// <param name="numPoints">Number points.</param>
	public Vector3[] OrbitPositions(int numPoints, Vector3 centerPos, bool doSceneMapping) {

        CalculateRotation();

        Vector3[] emptyArray = {new Vector3(0,0,0), new Vector3(0,0,0)};
		// need to have a center to create positions.
		if (centerObject == null) {
			centerObject = transform.parent.gameObject;
			if (centerObject == null) {
				return emptyArray;
			}
		}
		Vector3[] points = new Vector3[numPoints];
		float theta = -1f*branchDisplayFactor * Mathf.PI;
        float dTheta = 2f* Mathf.Abs(theta) / (float)numPoints;
        GravityEngine ge = GravityEngine.Instance();
		for (int i=0; i < numPoints; i++)
		{
			points[i] = PositionForThetaLeftBranch(theta, centerPos);
            if (NUtils.VectorNaN(points[i])) {
                points[i] = Vector3.zero;
            } else  if (doSceneMapping && ge.mapToScene) {
                points[i] = ge.MapToScene(points[i]);
            }
			theta += dTheta;
		}
		return points;
	}

    /// <summary>
    /// Determine the angle for a position on the hyperbola given the position.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public float ThetaForPosition(Vector3 position, Vector3 cPos) {
        Vector3 ellipseAxis = PositionForThetaLeftBranch(0f, cPos);
        Vector3 normal = hyper_orientation * Vector3.forward;
        return NUtils.AngleFullCircleRadians(ellipseAxis, position - centerPos, normal);
    }

    /// <summary>
    /// Given a position for an object in a hyperbolic orbit, determine the mirror position
    /// on the other side of the central body. 
    /// 
    /// Commonly used for patched conic hyperbolic segments around a moon/planet at the SOI. 
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="cPos"></param>
    /// <returns></returns>
    public Vector3 MirrorPhysPosition(Vector3 pos, Vector3 cPos) {
        // map pos into xy plane
        Vector3 pos_xy = Quaternion.Inverse(hyper_orientation) * (pos - cPos);
        return PositionForY(-pos_xy.y, cPos);
    }

    public Vector3 MirrorPhysVelocity(NBody body) {
        // map points into xy plane
        Vector3 vel = GravityEngine.Instance().GetVelocity(body);
        Vector3 vel_xy = Quaternion.Inverse(hyper_orientation) * vel;
        Vector3 velEnd_xy = new Vector3(-vel_xy.x, vel_xy.y, 0);
        return hyper_orientation * velEnd_xy;
    }

    /// <summary>
    /// Determine points from body through closest approach the same distance on the other side
    /// </summary>
    /// <param name="numPoints"></param>
    /// <param name="centerPos"></param>
    /// <param name="startPos"></param>
    /// <returns></returns>
	public Vector3[] OrbitSegmentSymmetricPositions(int numPoints, Vector3 centerPos,
                Vector3 startPos) {

        GravityEngine ge = GravityEngine.Instance();
        CalculateRotation();

        // map points into xy plane
        Vector3 start_xy = Quaternion.Inverse(hyper_orientation) * (startPos - centerPos);

        // symmetric around origin
        float start_y =-start_xy.y;
        float end_y = start_xy.y;
        if (start_y > end_y) {
            float temp = start_y;
            start_y = end_y;
            end_y = temp;
        }

        float dy = Mathf.Abs (end_y - start_y) / (float)numPoints;
        float y = start_y;
        int i = 0;
        Vector3[] points = new Vector3[numPoints];
        while (y < end_y) { 
            points[i] = PositionForY(y, centerPos);
            if (NUtils.VectorNaN(points[i])) {
                Debug.LogError(string.Format("Vector NaN = {0} y={1} ecc={2} ", points[i], y , ecc));
                points[i] = Vector3.zero;
            } else if (ge.mapToScene) {
                points[i] = ge.MapToScene(points[i]);
            }
            y += dy;
            i++;
            if (i > numPoints - 1)
                break;
        }
        // fill to end with last point
        int last = i - 1;
        if (last < 0)
            last = 0;
        while (i < numPoints) {
            points[i++] = points[last];
        }
        return points;
    }

    public void Log(string prefix) {
		Debug.Log(string.Format("orbitHyper: {0} a_scaled={1} ecc={2} peri={3} i={4} Omega={5} omega={6} r_initial={7}", 
								prefix, a_scaled, ecc, perihelion, inclination, omega_uc, omega_lc, r_initial));
	}

	/// <summary>
	/// Apply scale to the orbit. This is used by the inspector scripts during
	/// scene setup. Do not use at run-time.
	/// </summary>
	/// <param name="scale">Scale.</param>
	public void ApplyScale(float scale) {
		perihelion_phys = perihelion * scale;
        r_initial_phy = r_initial * scale;
        // This fixes T2 and breaks unit tests...
        CalcOrbitParams();
        if (!initFromOrbitData) {
            phase_nu = ThetaForR(r_initial_phy);
        }
        NBody nbody = GetComponent<NBody>();
        SetInitialPosition(nbody);
	}

    /// <summary>
    /// Return the center object around which this ellipse is defined.
    /// </summary>
    /// <returns>The center object.</returns>
    public GameObject GetCenterObject() {
        // need to have a center to draw gizmo.
        if (centerObject == null && transform.parent != null) {
            centerObject = transform.parent.gameObject;
        }
        centerNbody = centerObject.GetComponent<NBody>();
        if (centerNbody == null) {
            Debug.LogError("centerBody does not have NBody component: " + centerObject.name);
        }
        return centerObject;
    }

    // IFixedOrbit

    public void PreEvolve(float physicalScale, float massScale) {
        CalculateRotation();

        GravityEngine ge = GravityEngine.Instance();

        float a_phy = a_scaled / ge.GetPhysicalScale();
        mu = (float)GravityEngine.Instance().GetMass(centerNbody);
        orbit_period =  Mathf.Sqrt(a_phy * a_phy * a_phy / mu); // G=1
        // nu to anomoly (vallado algorithm 5, p77) It *really* is sin not sinh inside.
        float sinh_H = Mathf.Sin(phase_nu) * Mathf.Sqrt(ecc * ecc - 1) / (1 + ecc * Mathf.Cos(phase_nu));

        // ArcSinh(x) = ln(x + sqrt(x^2+1) )
        float H = Mathf.Log(sinh_H + Mathf.Sqrt(sinh_H * sinh_H + 1));
        mean_anomoly_phase = ecc * sinh_H - H;

        // Debug.LogFormat("PreEvolve: M0={0} H={1} nu0={1}", mean_anomoly_phase, H, nu);
    }

    private const int LOOP_LIMIT = 20;

    public float GetTimeToPeriapse() {
        // HACK: do we want a negative time? What if we are flying away?
        return Mathf.Abs(mean_anomoly_phase) * orbit_period;
    }

    public void Evolve(double physicsTime, ref double[] r_new) {

        // "period" is a a weird name, but need to set time scale and get to dimensionless value...
        float mean_anomoly = mean_anomoly_phase + (float) physicsTime/orbit_period;
        float H = 0;
        // KepEqntH (Vallado, algorithm 4, p71)
        // initial seed
        if (ecc < 1.6f) {
            if (((-Mathf.PI < mean_anomoly) && (mean_anomoly < 0)) || 
                 (mean_anomoly > Mathf.PI)) {
                H = mean_anomoly - ecc;
            } else {
                H = mean_anomoly + ecc;
            }
        } else {
            if ((ecc < 3.6) && (Mathf.Abs(mean_anomoly) > Mathf.PI)) {
                H = mean_anomoly - Mathf.Sign(mean_anomoly) * ecc;
            } else {
                H = mean_anomoly / (ecc - 1f);
            }
        }
        // iterate
        int loopCnt = 0;
        float H_next = float.MaxValue;
        while ( (loopCnt++ < LOOP_LIMIT)) {
            H_next = H + (mean_anomoly - ecc * (float)System.Math.Sinh(H) + H) / (ecc * (float)System.Math.Cosh(H) - 1);
            if (Mathf.Abs(H_next - H) < 1E-4) {
                break;
            }
            H = H_next;
        }
        if (loopCnt >= LOOP_LIMIT) {
            Debug.LogWarning("Did not converge ");
        }
        // determine nu (algorithm 5, p77)
        float denom = 1 - ecc * (float)System.Math.Cosh(H_next);
        float sin_nu = (float)(-System.Math.Sinh(H_next) * Mathf.Sqrt(ecc * ecc - 1)) / denom;
        float cos_nu = ((float)System.Math.Cosh(H_next) - ecc) / denom;
        float nu = Mathf.Atan2(sin_nu, cos_nu);


        // Rethink this position update stuff
        //denom = 1 + ecc * cos_nu;
        //float r_x = perihelion * cos_nu / denom;
        //float r_y = perihelion * sin_nu / denom;
        //position = new Vector3(r_x, r_y, 0);
        float r = RforTheta(nu);
        position = new Vector3(r * Mathf.Cos(nu), r * Mathf.Sin(nu), 0);
        // Debug.LogFormat("Evolve M={0} H={1} t={2} nu={3} r={4}", mean_anomoly, H_next, physicsTime, nu, position);

        position = hyper_orientation * position + GravityEngine.Instance().GetPhysicsPosition(centerNbody);
        // fill in r. NBE will use this position.
        r_new[0] = position.x;
        r_new[1] = position.y;
        r_new[2] = position.z;

        // determine velocity
        float coeef = Mathf.Sqrt(mu / p);
        Vector3 v_xy = new Vector3(-coeef * Mathf.Sin(nu), coeef * (ecc + Mathf.Cos(nu)), 0);
        velocityScaled = hyper_orientation * v_xy + GravityEngine.Instance().GetScaledVelocity(centerNbody);

    }

    public Vector3 GetVelocity() {
        return velocityScaled;
    }

    public Vector3 GetPosition() {
        return position;
    }

    public void GEUpdate(GravityEngine ge) {
        nbody.GEUpdate(position, Vector3.zero, ge);
    }

    public void Move(Vector3 moveBy) {
        position += moveBy;
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
        return string.Format("      Hyper: a={0:0.00} e={1:0.00}, i={2:0.00} Om={3:0.00} om={4:0.00} center={5}\n",
            a_scaled, ecc, inclination, omega_uc, omega_lc, centerNbody.name);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Displays the path of the elliptical orbit when the object is selected in the editor. 
    /// </summary>
    void OnDrawGizmosSelected()
	{
        // need to have a center to draw gizmo.
        if (GetCenterObject() == null) {
            return;
        }
        // need to have a center to draw gizmo.
        centerNbody = centerObject.GetComponent<NBody>();
        if (centerNbody == null) {
            return;
        }
        // only display if this object is directly selected
        if (Selection.activeGameObject != transform.gameObject) {
			return;
		}
		const int NUM_STEPS = 100; 
		const int STEPS_PER_RAY = 20; 
		int rayCount = 0; 
		Gizmos.color = Color.white;

        // Center object may need to determine it's position in an orbit
        // and update it's intialPhyPosition
        GravityEngine ge = GravityEngine.Instance();
        centerNbody.InitPosition(ge);
        centerNbody.EditorUpdate(ge);
        Vector3 centerPos = centerNbody.transform.position;
        Init();
        // use transform (can't ask GE it's not setup yet)
        Vector3[] points = OrbitPositions(NUM_STEPS, centerPos, false);

		for (int i=1; i < NUM_STEPS; i++) {
			Gizmos.DrawLine(points[i-1], points[i] );
			// draw rays from focus
			rayCount = (rayCount+1)%STEPS_PER_RAY;
			if (rayCount == 0) {
				Gizmos.DrawLine(centerPos, points[i] );
			}
		}
		Gizmos.color = Color.white;
		// Draw the axes in a different color
		Gizmos.color = Color.red;
		Gizmos.DrawLine(PositionForTheta(0.5f*Mathf.PI), PositionForTheta(-0.5f*Mathf.PI) );
		Gizmos.color = Color.blue;
		Gizmos.DrawLine(PositionForY(0f, centerPos), centerObject.transform.position );

        // move body to location specified by parameters
        if (!Application.isPlaying) {
            NBody nbody = GetComponent<NBody>();
            if (nbody != null) {
                nbody.EditorUpdate(ge);
            }
        }

    }


#endif
}

