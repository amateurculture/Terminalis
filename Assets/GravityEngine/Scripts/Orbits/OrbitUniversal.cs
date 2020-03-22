using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A generic double precision orbit class. Can be used in place of OrbitEllipse and OrbitHyper. 
/// 
/// Maintains all orbital elements in double precision in internal GE units. 
/// 
/// Supports two evolve modes:
///     GRAVITY_ENGINE: Set initial velocity and poistion based on orbital elements and then let
///                     GE integrators move the body as time evolves. 
///                     
///     KEPLER: Use Kepler's equation (position, velocity as a function of time) to determine when
///             the body is each GE update. GE will still use the mass of this body to influence
///             other bodies in the scene. 
///             
/// Arbitrary nesting of these modes is expected. (i.e. Kepler motion of a moon, around a GE planet in
/// orbit around a Sun etc.)
/// 
/// Since the semi-major axis (a) can change sign as eccentricy changes, specify the scale of the 
/// orbit with p (semi-latus rectum). 
/// 
/// 
/// 
/// </summary>
public class OrbitUniversal: MonoBehaviour, INbodyInit, IFixedOrbit, IOrbitPositions, IOrbitScalable
{
    //! Mode for evolution; NBody simulation in GE or "on-rails" using Kepler equation. 
    public enum EvolveMode { GRAVITY_ENGINE, KEPLERS_EQN };

    private NBody nbody;

    public EvolveMode evolveMode;

    //! semi-parameter that defines orbit size in GE internal units. 
    //! For a universal orbit p is needed. See SetMajorAxis() if a is needed.
    public double p = 10.0;

    //! Value of semi-parameter p from inspector (i.e. in GE selected units such as ORBITAL or SOLAR)
    public double p_inspector = 10.0;

    [SerializeField]
    public double eccentricity;

    //! inclinataion in degrees (0..180)
    public double inclination;

    //! Omega in degress (0..360)
    public double omega_uc;

    //! omega in degress (0..360)
    public double omega_lc;

    //! Phase in orbit in degrees. 
    public double phase = 0;

    //! Object influencing this bodies motion.
    public NBody centerNbody;

    private double mu;

    protected Quaternion conic_orientation;

    // ellipse
    private double orbit_period;
    private double mean_anomoly_phase;

    // Scaling:
    // In orbitEllipse the imnplementation used a, a_phys and a_scaled and it became confusing. 
    // Here the user physical size is entered in the units chosen in the GE units selector (e.g. ORBITAL, SOLAR, DL)


    // COE in radians
    private double omega_u_rad;
    private double omega_l_rad;
    private double incl_rad;
    // normal to the orbital plane
    private Vector3d h_unit; 

    // anomoly
    private double nu;

    // Initial conditions. Kepler evolution is driven from these values. These are with respect to an 
    // origin (0,0,0) in the centerBody frame of reference.
    private Vector3d r0;
    private Vector3d v0;
    private double time0;

    //! Time at which evolve was last called (and for which pos and vel are valid)
    private double t_last;
    Vector3d centerPosLast;
    Vector3d centerVelLast;

    private const double SMALL = 1E-6;

    //! position in GE world space in physics units. Updated after each Evolve call. 
    private Vector3d pos;
    //! velocity in GE world space in physics units. Updated after each Evolve call. 
    private Vector3d vel;

    private GravityEngine ge;

    //! Flag to indicate R,V,T set explcitly. No need to refer to COE.
    private bool initFromRVT;

    //! Used by LockAtTime()/UnlockTime()
    private bool timeLocked; 

#if UNITY_EDITOR
    // Editor script allows different ways to specify the size and shape (eccentricity, p or a).
    // Not all modes can work for all cases (e.g if parabola, a is infinite and need to use p)
    public enum InputMode { DOUBLE, DOUBLE_ELLIPSE, ELLIPSE_MAJOR_AXIS_A, ELLIPSE_APOGEE_PERIGEE, ECC_PERIGEE };
    public InputMode inputMode = InputMode.DOUBLE;
#endif

    // No "real code" in Awake() or Start(). Init is via GE or OrbitPredictor (or via OnDrawGizmos in Editor mode)

    void Awake() {
        ge = GravityEngine.Instance();
    }

    // Interface INBodyInit
    public void InitNBody(float physicalScale, float massScale) {
        time0 = GravityEngine.Instance().GetPhysicalTimeDouble();
        p = p_inspector / physicalScale * GravityEngine.Instance().GetLengthScale();
        Init();
    }

    /// <summary>
    /// Use an active NBody that is live in GE to initialize an orbit. 
    /// 
    /// Can be used in Kepler mode to determine the future position of the object by subsequently calling Evolve(). 
    /// Used by LambertPhasing to propogate the target for the time required for the transfer. 
    /// </summary>
    public void InitFromActiveNBody(NBody activeNbody, NBody center, EvolveMode mode) {
        ge = GravityEngine.Instance();
        evolveMode = mode;
        nbody = activeNbody;
        Vector3d r = ge.GetPositionDoubleV3(activeNbody);
        Vector3d v = ge.GetVelocityDoubleV3(activeNbody);
        double t = ge.GetPhysicalTimeDouble();
        InitFromRVT(r, v, t, center, false);
    }

    public void Init() {

        ge = GravityEngine.Instance();

        if (nbody == null) {
            nbody = GetComponent<NBody>();
        }

        if (centerNbody == null)
            centerNbody = OrbitUtils.GetCenterNbody(transform, null);
        // @TODO: use one body value?
        // 4.0: Use M+m (precision is required for Lagrange points)
        mu = ge.GetMass(centerNbody) + ge.GetMass(nbody);

        if (initFromRVT) {
            RVtoCOEWrapper();
            nbody.initialPhysPosition = r0.ToVector3();
            nbody.vel_phys = v0.ToVector3();
        } else {
            omega_u_rad = omega_uc * Mathd.Deg2Rad;
            omega_l_rad = omega_lc * Mathd.Deg2Rad;
            incl_rad = inclination * Mathd.Deg2Rad;
            InitFromCOE();
        }
        CalculateRotation();

        // force update of r and vel for Kepler mode
        if (evolveMode == EvolveMode.KEPLERS_EQN) {
            double[] r_dummy = new double[] { 0, 0, 0 };
            Evolve(ge.GetPhysicalTimeDouble(), ref r_dummy);
        }
    }

    /// <summary>
    /// For ellipses can specify size using major-axis. Typically used by editor script, but may have other uses.
    /// </summary>
    /// <param name="a"></param>
    public void SetMajorAxisInspector(double a) {
        if (eccentricity < 1.0) {
            p_inspector = a * (1 - eccentricity * eccentricity);
        } else {
            Debug.LogWarning("Cannot set size via major axis if not an ellipse.");
        }
    }

    /// <summary>
    /// Get the major axis for the orbit using the inspector value (world units). 
    /// - for a hyperbola this is negative
    /// - for a parabola this is NaN
    /// </summary>
    /// <returns></returns>
    public double GetMajorAxisInspector() {
        if (Mathd.Abs(eccentricity-1.0) < 1E-6) {
            // parabola
            return double.NaN;
        } 
        return p_inspector / (1 - eccentricity * eccentricity);
    }

    /// <summary>
    /// Get the Major Axis is interal physical units. 
    /// </summary>
    /// <returns></returns>
    public double GetMajorAxis() {
        return p / (1 - eccentricity * eccentricity);
    }

    /// <summary>
    /// Get the apogee (point of greatest distance from focus) for the orbit in Scaled units
    /// (e.g. ORBITAL). This is typically used in the inspector prior to the game starting. 
    /// 
    /// Typically used for an ellipse. For a hyperbola will be a negative number. 
    /// </summary>
    /// <returns></returns>
    public double GetApogeeInspector() {
        if (Mathd.Abs(eccentricity - 1.0) < 1E-6) {
            // parabola
            return double.NaN;
        }
        double a = p_inspector / (1 - eccentricity * eccentricity);
        // will be negative for a hyperbola (a < 0)
        return a * (1 + eccentricity);
    }

    /// <summary>
    /// Determine the orbit apogee (apoapsis) is internal physics units. 
    /// 
    /// Typically used for an ellipse. For a hyperbola will be a negative number. 
    /// </summary>
    /// <returns>apogee value in internal physics units</returns>
    public double GetApogee() {
        if (Mathd.Abs(eccentricity - 1.0) < 1E-6) {
            // parabola
            return double.NaN;
        }
        double a = p / (1 - eccentricity * eccentricity);
        // will be negative for a hyperbola (a < 0)
        return a * (1 + eccentricity);
    }

    public double GetSemiParam() {
        return p; 
    }

    /// <summary>
    /// Get the perigee (point of closest approach to the focus) for the orbit in Scaled units
    /// (e.g. ORBITAL). This is typically used in the inspector prior to the game starting. 
    /// 
    /// Valid for all orbit types.
    /// 
    /// </summary>
    /// <returns></returns>
    public double GetPerigeeInspector() {
        if (Mathd.Abs(eccentricity - 1.0) < 1E-6) {
            // parabola
            return p_inspector/2.0;
        }
        double a = p_inspector / (1 - eccentricity * eccentricity);
        return a * (1 - eccentricity);
    }

    /// <summary>
    /// Determine the orbit apogee (periapsis) is internal physics units. 
    /// 
    /// </summary>
    /// <returns>perigee value in internal physics units</returns>
    public double GetPerigee() {
        if (Mathd.Abs(eccentricity - 1.0) < 1E-6) {
            // parabola
            return p / 2.0;
        }
        double a = p / (1 - eccentricity * eccentricity);
        return a * (1 - eccentricity);
    }

    /// <summary>
    /// Set the size and eccentricity of the ellipse by using values for apogee and perigee. 
    /// (To be precise really should call them apoapsis and periapsis, but more people will 
    /// know apogee/perigee).
    /// 
    /// Typically only used for ellipses (but can work for hyperbolas) by the Editor script for
    /// this component.
    /// 
    /// Values provided are in GE scaled units (e.g. ORBITAL)
    /// 
    /// </summary>
    /// <param name="apogee"></param>
    /// <param name="perigee"></param>
    public void SetSizeWithApogeePerigee(double apogee, double perigee) {
        double a_new = 0.5 * (apogee + perigee);
        eccentricity = apogee / a_new - 1.0;
        SetMajorAxisInspector(a_new);
    }

    public void SetSizeWithEccPerigee(double ecc, double perigee) {
        double a_new = perigee / (1 - ecc); 
        eccentricity = ecc;
        p_inspector = a_new * (1 - ecc * ecc);
    }

    public double GetMu() {
        return mu;
    }

    public NBody GetNBody() {
        return nbody;
    }

    public Vector3d GetAxis() {
        return Vector3d.Cross(r0, v0).normalized;
    }

    private Vector3d ApplyRotations(Vector3d v) {
        double[] v_out = new double[] { 0, 0, 0 };

        v_out[0] = (Mathd.Cos(omega_u_rad) * Mathd.Cos(omega_l_rad) -
                    Mathd.Sin(omega_u_rad) * Mathd.Sin(omega_l_rad) * Mathd.Cos(incl_rad)) * v.x -
                   (Mathd.Cos(omega_u_rad) * Mathd.Sin(omega_l_rad) +
                    Mathd.Sin(omega_u_rad) * Mathd.Cos(omega_l_rad) * Mathd.Cos(incl_rad)) * v.y +
                    (Mathd.Sin(omega_u_rad) * Mathd.Sin(incl_rad)) * v.z;

        v_out[1] = (Mathd.Sin(omega_u_rad) * Mathd.Cos(omega_l_rad) +
            Mathd.Cos(omega_u_rad) * Mathd.Sin(omega_l_rad) * Mathd.Cos(incl_rad)) * v.x -
           (Mathd.Sin(omega_u_rad) * Mathd.Sin(omega_l_rad) -
            Mathd.Cos(omega_u_rad) * Mathd.Cos(omega_l_rad) * Mathd.Cos(incl_rad)) * v.y -
            (Mathd.Cos(omega_u_rad) * Mathd.Sin(incl_rad)) * v.z;

        v_out[2] = (Mathd.Sin(omega_l_rad) * Mathd.Sin(incl_rad)) * v.x +
                    (Mathd.Cos(omega_l_rad) * Mathd.Sin(incl_rad)) * v.y +
                    Mathd.Cos(incl_rad) * v.z;

        return new Vector3d(v_out[0], v_out[1], v_out[2]);
    }

    /// <summary>
    /// Determine the initial position and velocity (r0, v0) from the Classical Orbital Elements. 
    /// To be general (all types of orbits) use semi-parameter p instead of a.
    /// 
    /// To preserve double throughout use the explicit form of the rotation matrix, instead of creating 
    /// a double Quaternion. 
    /// 
    /// Use Algorithm 10 from Vallado. p118
    /// </summary>
    private void InitFromCOE() {

        nu = phase * Mathd.Deg2Rad; ;

        double r_denom = 1 + eccentricity * Mathd.Cos(nu);
        Vector3d r_pqw = new Vector3d(p * Mathd.Cos(nu) / r_denom, p * Mathd.Sin(nu) / r_denom, 0);
        r0 = ApplyRotations(r_pqw);
        pos = r0;

        double v_coeef = Mathd.Sqrt(mu / p);
        Vector3d v_pqw = new Vector3d(-v_coeef * Mathd.Sin(nu), v_coeef * (eccentricity + Mathd.Cos(nu)), 0);
        v0 = ApplyRotations(v_pqw);

        Vector3d h = Vector3d.Cross(r0, v0);
        h_unit = h.normalized;

        // TODO: Want to preserve double pos/velocity through NBody into GE during init
        Vector3 centerPos = Vector3.zero;
        // used by widgets - so need to get explcitly
        if (centerNbody.engineRef != null) {
            centerPos = GravityEngine.Instance().GetPhysicsPosition(centerNbody);
        } else {
            // setup - not yet added to GE
            centerPos = centerNbody.initialPhysPosition;
        }
        pos += new Vector3d(centerPos);
        nbody.initialPhysPosition = pos.ToVector3();
        vel = v0;

        // This is awkward - refactor
        nbody.vel_phys = v0.ToVector3();
    }

    /// <summary>
    /// Initialize the Orbit from orbital elements contained in an OrbitData object. 
    /// </summary>
    /// <param name="od"></param>
    public void InitFromOrbitData(OrbitData od, double time) {
        eccentricity = od.ecc;
        omega_lc = od.omega_lc;
        omega_l_rad = od.omega_lc * Mathf.Deg2Rad;
        omega_uc = od.omega_uc;
        omega_u_rad = od.omega_uc * Mathf.Deg2Rad;
        inclination = od.inclination;
        incl_rad = od.inclination * Mathf.Deg2Rad;
        phase = od.phase;
        p = od.a * (1 -od.ecc * od.ecc);
        p_inspector = p;
        time0 = time;
        centerNbody = od.centralMass;
        Init();
    }

    /// <summary>
    /// Initialize the orbit using position, velocity and time. 
    /// 
    /// Position and velocity are relative to the center object. (This is because
    /// when eg. adding a segement arount a moon in free return calculations cannot
    /// assume we mean the current position of the center).
    /// 
    /// </summary>
    /// <param name="r">relative position wrt center</param>
    /// <param name="v">relative velocity wrt center</param>
    /// <param name="time"></param>
    /// <param name="center"></param>
    public void InitFromRVT(Vector3d r, Vector3d v, double time, NBody center, bool relativePos) {
        centerNbody = center;
        r0 = r ;
        v0 = v ;
        // pos/vel include center stuff
        pos = r0;
        vel = v0;
        if (!relativePos) {
            r0 = r - ge.GetPositionDoubleV3(center);
            v0 = v - ge.GetVelocityDoubleV3(center);
        }
        time0 = time;
        initFromRVT = true;
        Init();
    }

    /// <summary>
    /// Inits from solar body. This will always be an ellipse. 
    /// </summary>
    /// <param name="sbody">Sbody.</param>
    public void InitFromSolarBody(SolarBody sbody) {
        SetMajorAxisInspector(sbody.a);
        eccentricity = sbody.ecc;
        omega_lc = sbody.omega_lc;
        omega_uc = sbody.omega_uc;
        inclination = sbody.inclination;
        phase = sbody.longitude;
        Init();
        ApplyScale(GravityEngine.Instance().GetLengthScale());
    }


    // Simliar to code in OrbitData but not quite the same...
    private void RVtoCOEWrapper() {
        Vector3d h = Vector3d.Cross(r0, v0);
        h_unit = h.normalized;
        // Don't duplicate all of RVtoCOE here...
        OrbitUtils.OrbitElements oe = OrbitUtils.RVtoCOE(r0, v0, centerNbody, mu, true);
        p = oe.p;
        p_inspector = p / GravityEngine.Instance().lengthScale;
        eccentricity = oe.ecc;
        inclination = Mathf.Rad2Deg * (float)oe.incl;
        omega_uc = 0;
        omega_lc = 0; 
        if (oe.IsInclined()) {
            if (!oe.IsCircular()) {
                omega_uc = Mathf.Rad2Deg * (float)oe.raan;
                omega_lc = Mathf.Rad2Deg * (float)oe.argp;
            } else {
                omega_uc = Mathf.Rad2Deg * (float)oe.raan;
            }
        } else {
            // equatorial
            if (!oe.IsCircular()) {
                omega_uc = 0;
                omega_lc = Mathf.Rad2Deg * (float)oe.lonper;
            }
        }
        if (omega_uc >= 360.0)
            omega_uc -= 360.0;
        if (omega_lc >= 360.0)
            omega_lc -= 360.0;
        omega_u_rad = omega_uc * Mathd.Deg2Rad;
        omega_l_rad = omega_lc * Mathd.Deg2Rad;
        phase = Mathf.Rad2Deg * OrbitUtils.GetPhaseFromOE(oe);
    }

    public void ApplyScale(float scale) {
        nbody = GetComponent<NBody>();
        p = p_inspector / GravityEngine.Instance().physToWorldFactor * GravityEngine.Instance().GetLengthScale();

        if (centerNbody == null)
            centerNbody = OrbitUtils.GetCenterNbody(transform, null);
        if (!initFromRVT) {
            InitFromCOE();
        }
        SetInitialPosition(nbody, centerNbody.gameObject);
    }


    //-----------------------------------------------------
    // Interface: IFixedOrbit
    //-----------------------------------------------------

    public bool IsOnRails() {
        return evolveMode == EvolveMode.KEPLERS_EQN;
    }

    public void PreEvolve(float physicalScale, float massScale) {
        if (!initFromRVT)
            InitFromCOE();
    }

    // Orignal comments from Vallado
    /* -----------------------------------------------------------------------------
	*
	*                           function kepler
	*
	*  this function solves keplers problem for orbit determination and returns a
	*    future geocentric equatorial (ijk) position and velocity vector.  the
	*    solution uses universal variables.
	*
	*  author        : david vallado                  719-573-2600   22 jun 2002
	*
	*  revisions
	*    vallado     - fix some mistakes                             13 apr 2004
	*
	*  inputs          description                    range / units
	*    ro          - ijk position vector - initial  km
	*    vo          - ijk velocity vector - initial  km / s
	*    dtsec       - length of time to propagate    s
	*
	*  outputs       :
	*    r           - ijk position vector            km
	*    v           - ijk velocity vector            km / s
	*    error       - error flag                     'ok', ...
	*
	*  locals        :
	*    f           - f expression
	*    g           - g expression
	*    fdot        - f dot expression
	*    gdot        - g dot expression
	*    xold        - old universal variable x
	*    xoldsqrd    - xold squared
	*    xnew        - new universal variable x
	*    xnewsqrd    - xnew squared
	*    znew        - new value of z
	*    c2new       - c2(psi) function
	*    c3new       - c3(psi) function
	*    dtsec       - change in time                 s
	*    timenew     - new time                       s
	*    rdotv       - result of ro dot vo
	*    a           - semi or axis                   km
	*    alpha       - reciprocol  1/a
	*    sme         - specific mech energy           km2 / s2
	*    period      - time period for satellite      s
	*    s           - variable for parabolic case
	*    w           - variable for parabolic case
	*    h           - angular momentum vector
	*    temp        - temporary real*8 value
	*    i           - index
	*
	*  coupling      :
	*    mag         - magnitude of a vector
	*    findc2c3    - find c2 and c3 functions
	*
	*  references    :
	*    vallado       2013, 93, alg 8, ex 2-4
	---------------------------------------------------------------------------- */
    /// <summary>
    /// Evolve the orbit to the time indicated. The algorithm used requires some internal
    /// iteration but in general converges very quicky. (All solutions to Kepler's equation
    /// use some iteration, since the equation is not closed form).
    /// 
    /// The evolution sets pos and vel internally.
    /// 
    /// Universal Kepler evolution using KEPLER (algorithm 8) from Vallado, p93
    /// Code taken from book companion site and adapted to C#/Unity.
    /// </summary>
    /// <param name="physicsTime"></param>
    /// <param name="r_new">Position at the specified time (returned by ref)</param>
    public void Evolve(double physicsTime, ref double[] r_new) {

        t_last = physicsTime;

        int ktr, numiter;
        double f, g, fdot, gdot, rval, xold, xoldsqrd,
            xnewsqrd, znew, pp, dtnew, rdotv, a, dtsec,
            alpha, sme, s, w, temp,
            magro, magvo, magr;
        double c2new = 0.0;
        double c3new = 0.0;
        double xnew = 0.0;
        double small,  halfpi;

        if (timeLocked) {
            r_new[0] = pos.x;
            r_new[1] = pos.y;
            r_new[2] = pos.z;
            return;
        }

        // can have a weird precision issue when same time used in Init and first evolve.
        if (((physicsTime - time0) < 0) && (Mathd.Abs(physicsTime - time0) > 1E-5)) {
            Debug.LogWarning(string.Format("evolution time {0} is before time0 reference {1} for {2}",
                physicsTime, time0, gameObject.name));
            return;
        }
        // evolution time is relative to time0
        double dtseco = physicsTime - time0;
        dtsec = dtseco;

        small = 0.00000001;
        halfpi = Mathd.PI * 0.5;

        centerPosLast = GravityEngine.Instance().GetPositionDoubleV3(centerNbody);
        centerVelLast = GravityEngine.Instance().GetVelocityDoubleV3(centerNbody);

        // -------------------------  implementation   -----------------
        // set constants and intermediate printouts
        numiter = 100;

        // --------------------  initialize values   -------------------
        ktr = 0;
        xold = 0.0;
        znew = 0.0;

        if (Mathd.Abs(dtseco) > small) {
            magro = r0.magnitude;
            magvo = v0.magnitude;
            rdotv = Vector3d.Dot(r0, v0);

            // -------------  find sme, alpha, and a  ------------------
            sme = ((magvo * magvo) * 0.5) - (mu / magro);
            alpha = -sme * 2.0 / mu;

            if (Mathd.Abs(sme) > small)
                a = -mu / (2.0 * sme);
            else
                a = double.NaN;
            if (Mathd.Abs(alpha) < small)   // parabola
                alpha = 0.0;

            // ------------   setup initial guess for x  ---------------
            // -----------------  circle and ellipse -------------------
            if (alpha >= small) {
                //period = 2.0 * Mathd.PI * Mathd.Sqrt(Mathd.Abs(a * a * a) / mu);
                if (Mathd.Abs(alpha - 1.0) > small)
                    xold = Mathd.Sqrt(mu) * dtsec * alpha;
                else
                    // - first guess can't be too close. ie a circle, r=a
                    xold = Mathd.Sqrt(mu) * dtsec * alpha * 0.97;
            } else {
                // --------------------  parabola  ---------------------
                if (Mathd.Abs(alpha) < small) {
                    Vector3d h = Vector3d.Cross(r0, v0);
                    pp = h.sqrMagnitude / mu;
                    s = 0.5 * (halfpi - Mathd.Atan(3.0 * Mathd.Sqrt(mu / (pp * pp * pp)) * dtsec));
                    w = Mathd.Atan(Mathd.Pow(Mathd.Tan(s), (1.0 / 3.0)));
                    xold = Mathd.Sqrt(p) * (2.0 * GEMath.Cot(2.0 * w));
                    alpha = 0.0;
                } else {
                    // ------------------  hyperbola  ------------------
                    temp = -2.0 * mu * dtsec /
                        (a * (rdotv + Mathd.Sign(dtsec) * Mathd.Sqrt(-mu * a) * (1.0 - magro * alpha)));
                    xold = Mathd.Sign(dtsec) * Mathd.Sqrt(-a) * Mathd.Log(temp);
                }
            } // if alpha

            ktr = 1;
            dtnew = -10.0;
            // conv for dtsec to x units
            double tmp = 1.0 / Mathd.Sqrt(mu);

            while ((Mathd.Abs(dtnew * tmp - dtsec) >= small) && (ktr < numiter)) {
                xoldsqrd = xold * xold;
                znew = xoldsqrd * alpha;

                // ------------- find c2 and c3 functions --------------
                OrbitUtils.FindC2C3(znew, out c2new, out c3new);

                // ------- use a newton iteration for new values -------
                rval = xoldsqrd * c2new + rdotv * tmp * xold * (1.0 - znew * c3new) +
                    magro * (1.0 - znew * c2new);
                dtnew = xoldsqrd * xold * c3new + rdotv * tmp * xoldsqrd * c2new +
                    magro * xold * (1.0 - znew * c3new);

                // ------------- calculate new value for x -------------
                xnew = xold + (dtsec * Mathd.Sqrt(mu) - dtnew) / rval;

                // ----- check if the univ param goes negative. if so, use bissection
                if (xnew < 0.0)
                    xnew = xold * 0.5;

                ktr = ktr + 1;
                xold = xnew;
            }  // while

            if (ktr >= numiter) {
                Debug.LogWarning(string.Format("{0} not converged in {1} iterations. dtnew={2} tmp={3} dto={4} expr={5}", 
                    gameObject.name, numiter, dtnew, tmp, dtsec, Mathd.Abs(dtnew * tmp - dtsec)));
                Debug.LogFormat("alpha={0} a={1}", alpha, a);
                // Mitigation: use last known position
                ge.GetPositionDouble(nbody, ref r_new);
            } else {
                // --- find position and velocity vectors at new time --
                xnewsqrd = xnew * xnew;
                f = 1.0 - (xnewsqrd * c2new / magro);
                g = dtsec - xnewsqrd * xnew * c3new / Mathd.Sqrt(mu);
                r_new[0] = f * r0.x + g * v0.x;
                r_new[1] = f * r0.y + g * v0.y;
                r_new[2] = f * r0.z + g * v0.z;
                magr = Mathd.Sqrt(r_new[0] * r_new[0] + r_new[1] * r_new[1] + r_new[2] * r_new[2]);
                gdot = 1.0 - (xnewsqrd * c2new / magr);
                fdot = (Mathd.Sqrt(mu) * xnew / (magro * magr)) * (znew * c3new - 1.0);
                temp = f * gdot - fdot * g;
                if (Mathd.Abs(temp - 1.0) > 0.00001)
                    Debug.LogWarning(string.Format("consistency check failed {0}", (temp - 1.0)));
                // Add centerPos to value we ref back
                r_new[0] += centerPosLast.x;
                r_new[1] += centerPosLast.y;
                r_new[2] += centerPosLast.z;
                pos = new Vector3d(ref r_new);
                // update velocity
                vel = new Vector3d(fdot * r0.x + gdot * v0.x,
                            fdot * r0.y + gdot * v0.y,
                            fdot * r0.z + gdot * v0.z) + centerVelLast;
            }
        } // if fabs
        else { 
            // ----------- set vectors to incoming since 0 time --------
            r_new[0] = r0.x;
            r_new[1] = r0.y;
            r_new[2] = r0.z;
            // Add centerPos to value we ref back
            r_new[0] += centerPosLast.x;
            r_new[1] += centerPosLast.y;
            r_new[2] += centerPosLast.z;
            pos = new Vector3d(ref r_new);
            vel = new Vector3d(v0.x, v0.y, v0.z) + centerVelLast;
        }

    }   // kepler

    /// <summary>
    /// Use in On-Rails mode to lock an object at a specific time. Any evolve calls will not re-compute the 
    /// position/velocity but instead leave them unchanged. 
    /// </summary>
    /// <param name="lockTime"></param>
    public void LockAtTime(double lockTime) {
        if (evolveMode != EvolveMode.KEPLERS_EQN) {
            Debug.LogError("Cannot lock time in GE mode " + gameObject.name);
        }
        double[] dummy = new double[3] { 0, 0, 0 };
        timeLocked = false;
        if (lockTime < time0)
            time0 = lockTime;
        Evolve(lockTime, ref dummy);
        timeLocked = true;
    }

    public void UnlockTime() {
        timeLocked = false;
    }

    public double GetPeriod() {
        if (centerNbody == null)
            return 0;
        // Use Find to allow Editor to use method. 
        if (ge == null) {
            ge = (GravityEngine)FindObjectOfType(typeof(GravityEngine));
            if (ge == null) {
                Debug.LogError("Need GravityEngine in the scene");
                return 0;
            }
            Init();
        }
        if (eccentricity < 1.0) {
            double a = GetMajorAxis() / ge.GetPhysicalScale();
            orbit_period = 2f * Mathd.PI * Mathd.Sqrt(a * a * a / mu); // G=1
            return orbit_period;
        } else {
            return double.NaN;
        }

    }

    public double GetStartTime() {
        return time0;
    }

    /// <summary>
    /// Get the last velocity computed by the Evolve function when in Kepler mode.
    /// </summary>
    /// <returns></returns>    
    public Vector3 GetVelocity() {
        return vel.ToVector3();
    }

    /// <summary>
    /// Get the last velocity computed by the Evolve function when in Kepler mode.
    /// </summary>
    /// <returns></returns>    
    public Vector3d GetVelocityDouble() {
        return vel;
    }

    /// <summary>
    /// Get the last position computed by the Evolve function when in Kepler mode.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetPosition() {
        return pos.ToVector3();
    }

    /// <summary>
    /// Get the last position computed by the Evolve function when in Kepler mode.
    /// </summary>
    /// <returns></returns>    
    public Vector3d GetPositionDouble() {
        return pos;
    }

    /// <summary>
    /// Get the intial conditions for the orbit. 
    /// 
    /// These value are always available (even if orbitU was not inited with InitRVT). 
    /// </summary>
    /// <param name="r0"></param>
    /// <param name="v0"></param>
    /// <param name="time0"></param>
    public void GetRVT(ref Vector3d r0, ref Vector3d v0, ref double time0) {
        r0 = this.r0;
        v0 = this.v0;
        time0 = this.time0;
    }

    /// <summary>
    /// Get the position, velocity and time from the last Kepler mode evolve. 
    /// </summary>
    /// <param name="r0"></param>
    /// <param name="v0"></param>
    /// <param name="time0"></param>
    public void GetRVTLastEvolve(ref Vector3d r0, ref Vector3d v0, ref double time0) {
        r0 = pos;
        v0 = vel;
        time0 = t_last;
    }
    public void GEUpdate(GravityEngine ge) {
        nbody.GEUpdate(pos.ToVector3(), vel.ToVector3(), ge);
    }

    public void Move(Vector3 position) {
        pos += new Vector3d(position);
    }

    public void SetNBody(NBody nbody) {
        this.nbody = nbody;
    }

    // Wrapper for IOrbitPositions used only by the OrbitRenderer
    public Vector3[] OrbitPositions(int numPoints, Vector3 centerPos, bool doSceneMapping) {
        return OrbitPositions(numPoints, centerPos, doSceneMapping, 0);
    }

    public Vector3[] OrbitPositions(int numPoints, Vector3 centerPos, bool doSceneMapping, float hyperRadius) {
        Vector3[] positions;
        if (double.IsNaN(eccentricity)) {
            // an orbit with no angular momtm (free fall) will have NaN for all orbital elements
            // make path a line to the center
            positions = new Vector3[numPoints];
            Vector3 bodypos = ge.GetPhysicsPosition(nbody);
            if (doSceneMapping) {
                bodypos = ge.MapToScene(bodypos);
            }
            positions[0] = bodypos;
            for (int i = 1; i < numPoints; i++)
                positions[i] = centerPos;
        } else if (eccentricity < 1.0) {
            positions = EllipsePositions(numPoints, centerPos, doSceneMapping); 
        } else {
            positions = HyperSegmentSymmetric(numPoints, centerPos, hyperRadius, doSceneMapping);
        }
        return positions;
    }

    /// <summary>
    /// Get the position for the specified phase in physics co-ordinates. 
    /// 
    /// (If map to scene is being used it up to the caller to do the conversion using ge.MapToScene() )
    /// 
    /// </summary>
    /// <param name="phaseDeg"></param>
    /// <returns></returns>
    public Vector3 PositionForPhase(float phaseDeg) {
        return GetPositionForThetaRadians(phaseDeg * Mathf.Deg2Rad, ge.GetPhysicsPosition(centerNbody));
    }

    public Vector3 VelocityForPhase(float phaseDeg) {
        /// Uses Vallado, Algorithm 10 for (x,y) plane and then rotates into place
        float phaseRad = phaseDeg * Mathf.Deg2Rad;
        double vx = -Mathd.Sqrt(mu / p) * Mathd.Sin(phaseRad);
        double vy = Mathd.Sqrt(mu / p) * (eccentricity + Mathd.Cos(phaseRad));
        return conic_orientation * new Vector3((float) vx, (float) vy, 0);
    }

    /// <summary>
    /// Given a position on the orbit (or a position that is used to establish a phase wrt the center)
    /// determine the velocity vector at that point on the orbit. 
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public Vector3 VelocityForPosition(Vector3 position) {
        // determine the phase and use that to get the velocity
        Vector3 toPos = position - ge.GetPhysicsPosition(centerNbody);
        Vector3 toPeri = PositionForPhase(0);
        float angle = Vector3.Angle(toPeri, toPos);
        // sign check 
        if ( Vector3.Dot(Vector3.Cross(toPeri, toPos), GetAxis().ToVector3()) < 0) {
            angle = 360f - angle;
        }
        return VelocityForPhase(angle);
    }

    public bool CanApplyImpulse() {
        return true;
    }

    public NBody GetCenterNBody() {
        return centerNbody;
    }

    /// <summary>
    /// Set a new start position for orbit evolution with respect to the current center object. 
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="vel"></param>
    public void UpdatePositionAndVelocity(Vector3 pos, Vector3 vel) {
        Vector3 cPos = ge.GetPhysicsPosition(centerNbody);
        Vector3 cVel = ge.GetVelocity(centerNbody);
        r0 = new Vector3d(pos - cPos);
        v0 = new Vector3d(vel - cVel);
        Debug.Log("pos=" + pos + " r0=" + r0);
        time0 = ge.GetPhysicalTimeDouble();
        // Update transform position
        this.pos = new Vector3d(pos);
        this.vel = new Vector3d(vel);
        GEUpdate(ge);
    }

    /*********************************************************************************************
    *   Methods that are unique to OrbitUniversal
    *********************************************************************************************/

    /// <summary>
    /// Set a new center object. Used when doing Kepler ("on-rails") evolution and the object enters
    /// the sphere of influence of a new body. The inital conditions (r0,v0, t0) are updated to be with respect
    /// to the new center object allowing Kepler evolution wrt the new center
    /// </summary>
    /// <param name="newCenter"></param>
    public void SetNewCenter(NBody newCenter) {

        centerNbody = newCenter;
        if (nbody.engineRef != null) {
            GravityEngine.Instance().UpdateKeplerDepth(nbody, this);
            mu = GravityEngine.Instance().GetMass(centerNbody);
            Vector3d r = ge.GetPositionDoubleV3(nbody);
            Vector3d v = ge.GetVelocityDoubleV3(nbody);
            Vector3d r_center = ge.GetPositionDoubleV3(newCenter);
            Vector3d v_center = ge.GetVelocityDoubleV3(newCenter);

            r0 = r - r_center;
            v0 = v - v_center;
            time0 = ge.GetPhysicalTimeDouble();
        }
    }

    /// <summary>
    /// Change the evolve mode between KEPLER and GRAVITY_ENGINE. i.e. go from off-rails to on-rails.
    /// </summary>
    /// <param name="newMode"></param>
    public void ChangeEvolveMode(EvolveMode newMode) {
        Debug.LogError("Not Implemented");
    }

    /// <summary>
    /// Apply an impulse to change the Kepler evolution. OrbitUniversal supports seamless changes from ellipse to 
    /// parabola to hyperbola. This updates  the initial conditions and start time (r0, v0, time0)
    /// 
    /// This method is usually called from GravityEngine ApplyImpulse(). Not commonly called from game code. 
    /// 
    /// </summary>
    /// <param name="impulse">Adjusted Impulse</param>
    public Vector3 ApplyImpulse(Vector3 impulse) {

        v0 = vel + new Vector3d(impulse) - centerVelLast;
        time0 = t_last;
        r0 = pos - centerPosLast;
        // update the classical orbit elements (not essential but might be useful later to avoid an OrbitData?)
        RVtoCOEWrapper();
        return v0.ToVector3();
    }

    /// <summary>
    /// Set the velocity and update initial conditions for the OrbitUniversal. This method is used by Maneuvers
    /// when the body is in Kepler mode. 
    /// 
    /// This technique is not reversable (cannot jump time to earlier than this) since it is not recorded. 
    /// 
    /// A transfer that is time reversable can be implemented with a KeplerSequence 
    /// (see LambertDemoController::TransferOnRails) 
    /// </summary>
    /// <param name="vel"></param>
    public void SetVelocityDouble(Vector3d vel) {
        v0 = vel - ge.GetVelocityDoubleV3(centerNbody);
        r0 = pos - ge.GetPositionDoubleV3(centerNbody);
        time0 = t_last;
        RVtoCOEWrapper();
    }

    /// <summary>
    /// Determine the time of flight in physics time units (GE internal time) that it takes for the body
    /// in orbit to go from relative position r0 to position r1. 
    /// 
    /// 
    /// </summary>
    /// <param name="r0">relative position (assumes center is (0,0,0)</param>
    /// <param name="r1">relative velocity (assume center v=0)</param>
    /// <returns>time to travel from r0 to r1 in GE time</returns>
    public double TimeOfFlight(Vector3d r0, Vector3d r1) {
        
        double tof = OrbitUtils.TimeOfFlight(r0, r1, p, mu, h_unit);
        if ((eccentricity < 1.0) && (tof < 0)) {
            if (orbit_period == 0)
                GetPeriod();
            tof += orbit_period;
        } 
        // for hyperbola want to do Abs. Just do it always
        return Mathd.Abs(tof);
    }

    /// <summary>
    /// Sets the initial physics position based on the orbit parameters. Used in the init phase to set the NBody in the
    /// correct position in the scene before handing control GE. 
    /// </summary>
    private void SetInitialPosition(NBody nbody, GameObject centerObject) {

        if (initFromRVT)
            nbody.initialPhysPosition = r0.ToVector3();

        float phaseRad = (float)(phase * Mathd.Deg2Rad);
        // position object using true anomoly (angle from  focus)
        float r = (float)(p / (1f + eccentricity * Mathf.Cos(phaseRad)));

        Vector3 pos = new Vector3(r * Mathf.Cos(phaseRad), r * Mathf.Sin(phaseRad), 0);
        // move from XY plane to the orbital plane
        Vector3 new_p = conic_orientation * pos;
        // orbit position is WRT center. Could be adding dynamically to an object in motion, so need current position. 
        Vector3 centerPos = Vector3.zero;
        // used by widgets - so need to get explcitly
        centerNbody = OrbitUtils.GetCenterNbody(transform, centerObject);
        if (centerNbody.engineRef != null) {
            centerPos = GravityEngine.Instance().GetPhysicsPosition(centerNbody);
        } else {
            // setup - not yet added to GE
            centerPos = centerNbody.initialPhysPosition;
        }
        nbody.initialPhysPosition = new_p + centerPos;

    }

    public bool IsCircular() {
        // Use same limit as OrbitUtils
        return (eccentricity < OrbitUtils.small);
    }


#if UNITY_EDITOR
    /*********************************************************************************************
     *               GIZMO CODE
     *********************************************************************************************/

 
    /// <summary>
    /// Displays the path of the orbit when the object is selected in the editor. 
    /// 
    /// No need for full double precision, so use as convenient.
    /// 
    /// Simpler to use specific code for ellipse vs hyperbola and nudge a true parabola into a hyperbola.
    /// </summary>
    void OnDrawGizmosSelected() {

        // only display if this object or parent is selected
        bool selected = Selection.Contains(transform.gameObject);
        if (transform.parent != null)
            selected |= Selection.Contains(transform.parent.gameObject);
        if (!selected) {
            return;
        }

        // When part of OrbitPredictor, there will be no NBody to grab, bail out.
        NBody testForNbody = this.GetComponent<NBody>();
        if (testForNbody == null) {
            return;
        } 

        if (nbody == null)
            nbody = testForNbody;

        // need to have a center to draw gizmo.
        if (centerNbody == null) {
            centerNbody = OrbitUtils.GetCenterNbody(transform, null);
        }

        // Init() needs the object active in GE (to get mass), so just update params here
        omega_u_rad = omega_uc * Mathd.Deg2Rad;
        omega_l_rad = omega_lc * Mathd.Deg2Rad;
        incl_rad = inclination * Mathd.Deg2Rad;


        // Center object may need to determine it's position in an orbit
        // and update it's intialPhyPosition
        GravityEngine ge = GravityEngine.Instance();
        centerNbody.InitPosition(ge);
        centerNbody.EditorUpdate(ge);
        Vector3 centerPos = centerNbody.transform.position;
        CalculateRotation();

        if (!Application.isPlaying) {
            SetInitialPosition(nbody, centerNbody.gameObject);
        }

        if (eccentricity < 1.0) {
            DrawEllipseGizmo(centerPos);
        } else {
            DrawHyperGizmo(centerPos);
        }
    }

#endif

    private void CalculateRotation() {
        // Following Murray and Dermot Ch 2.8 Fig 2.14
        // Quaternions go L to R (matrices are R to L)
        conic_orientation = Quaternion.AngleAxis((float)omega_uc, GEConst.zunit) *
                              Quaternion.AngleAxis((float)inclination, GEConst.xunit) *
                              Quaternion.AngleAxis((float)omega_lc, GEConst.zunit);
    }

    public Quaternion GetConicOrientation() {
        return conic_orientation;
    }

    const int NUM_STEPS = 100;
    const int STEPS_PER_RAY = 10;

    /// <summary>
    /// Calculate an array of points that describe the specified orbit
    /// </summary>
    /// <returns>The positions.</returns>
    /// <param name="numPoints">Number points.</param>
    private Vector3[] EllipsePositions(int numPoints, Vector3 centerPos, bool doSceneMapping) {

        Vector3[] points = new Vector3[numPoints];

        CalculateRotation();

        float dtheta = 2f * Mathf.PI / numPoints;
        float theta = 0;

        // add a fudge factor to ensure we go all the way around the circle
        for (int i = 0; i < numPoints; i++) {
            points[i] = GetPointForTheta(theta, centerPos, doSceneMapping);
            theta += dtheta;
        }
        // close the path (credit for fix to R. Vincent)
        points[numPoints - 1] = points[0];
        return points;
    }

    private Vector3 GetPointForTheta(float theta, Vector3 centerPos, bool doSceneMapping) {
        Vector3 point = GetPositionForThetaRadians(theta, centerPos);
        if (NUtils.VectorNaN(point)) {
            string opDetails = "";
            OrbitPredictor op = GetComponent<OrbitPredictor>();
            if (op != null) {
                opDetails = string.Format(" OrbitPredictor: {0} around {1}", op.body.name, op.centerBody.name);
            }
            Debug.LogError("Vector NaN + " + point + " in " + gameObject.name + opDetails);
            point = Vector3.zero;
        } else if (doSceneMapping && ge.mapToScene) {
            point = ge.MapToScene(point);
        }
        return point;
    }

    private Vector3[] HyperOrbitPositions(int numPoints, Vector3 centerPos, bool doSceneMapping) {

        CalculateRotation();

        Vector3[] points = new Vector3[numPoints];
        float theta = -1f * branchDisplayFactor * Mathf.PI;
        float dTheta = 2f * Mathf.Abs(theta) / (float)numPoints;
        GravityEngine ge = GravityEngine.Instance();
        for (int i = 0; i < numPoints; i++) {
            points[i] = GetPositionForThetaRadians(theta, centerPos);
            if (NUtils.VectorNaN(points[i])) {
                points[i] = Vector3.zero;
            } else if (doSceneMapping && ge.mapToScene) {
                points[i] = ge.MapToScene(points[i]);
            }
            theta += dTheta;
        }
        return points;
    }

    /// <summary>
    /// Calculate points suitable for a line renderer that show a segment of the hyperbola for a specified
    /// radius with respect to the center. 
    /// </summary>
    /// <param name="numPoints"></param>
    /// <param name="centerPos"></param>
    /// <param name="radius"></param>
    /// <param name="doSceneMapping"></param>
    /// <returns></returns>
    public Vector3[] HyperSegmentSymmetric(int numPoints, Vector3 centerPos, float radiusOrZero, bool doSceneMapping) {
        float radius = radiusOrZero;
        if (radiusOrZero < 1E-6) {
            radius = (pos.ToVector3() - centerPos).magnitude;
        }
        CalculateRotation();

        // solve hyperbola equation for theta:
        double cos_theta = Mathd.Clamp((p / radius - 1) / eccentricity, -1.0, 1.0);
        float theta_for_r = (float) Mathd.Acos(cos_theta);

        Vector3[] points = new Vector3[numPoints];
        float dTheta = 2f * Mathf.Abs(theta_for_r) / (float)numPoints;
        float theta = -theta_for_r;
        for (int i = 0; i < numPoints; i++) {
            points[i] = points[i] = GetPointForTheta(theta, centerPos, doSceneMapping);
            theta += dTheta;
        }
        // explicitly include the end point to avoid slight miss
        points[numPoints-1] = GetPointForTheta(theta_for_r, centerPos, doSceneMapping);
        return points;

    }

    // same code as EllipseBase
    /// <summary>
    /// Generate the points for an ellipse segment given the start and end positions. If shortPath then 
    /// the short path between the points will be shown, otherwise the long way around. 
    /// 
    /// The points are used to determine an angle from the main axis of the ellipse and although they 
    /// should be on the ellipse for best results, the code will do it's best if they are not. 
    /// </summary>
    /// <param name="numPoints"></param>
    /// <param name="centerPos"></param>
    /// <param name="pos"></param>
    /// <param name="destPoint"></param>
    /// <param name="shortPath"></param>
    /// <returns></returns>
    public Vector3[] EllipseSegment(int numPoints, Vector3 centerPos, Vector3 pos, Vector3 destPoint, bool shortPath) {

        Vector3[] points = new Vector3[numPoints];
        CalculateRotation();
        float dtheta = 2f * Mathf.PI / numPoints;
        float theta = 0;

        // find the vector to theta=0 on the ellipse, with no offset
        Vector3 ellipseAxis = GetPositionForThetaRadians(0f, Vector3.zero);
        Vector3 normal = conic_orientation * Vector3.forward;
        float theta1 = NUtils.AngleFullCircleRadians(ellipseAxis, pos - centerPos, normal);
        float theta2 = NUtils.AngleFullCircleRadians(ellipseAxis, destPoint - centerPos, normal);

        if (inclination > 90) {
            float temp = theta1;
            theta1 = theta2;
            theta2 = temp;
        }
        if (theta1 > theta2) {
            float temp = theta1;
            theta1 = theta2;
            theta2 = temp;
        }
        if (!shortPath) {
            if ((theta2 - theta1) < Mathf.PI) {
                float temp = theta1;
                theta1 = theta2;
                // ok to go beyond 2 Pi, since will increment to theta2
                theta2 = temp + 2f * Mathf.PI;
            }
        } else {
            // shortpath=true
            if ((theta2 - theta1) > Mathf.PI) {
                float temp = theta1;
                theta1 = theta2;
                // ok to go beyond 2 Pi, since will increment to theta2
                theta2 = temp + 2f * Mathf.PI;
            }
        }
        // Debug.LogFormat("theta1={0} theta2={1} start={2} end={3} axis={4}", theta1, theta2, startPos, endPos, ellipseAxis);

        int i = 0;
        // TODO: Add to API
        bool doSceneMapping = true; 
        for (theta = theta1; theta < theta2; theta += dtheta) {
            points[i] = points[i] = GetPointForTheta(theta, centerPos, doSceneMapping);
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

    public string DumpInfo() {
        return string.Format("  OrbitU: p={0:0.00} e={1:0.00}, i={2:0.00} Om={3:0.00} om={4:0.00}\n        center={5}\n",
            p, eccentricity, inclination, omega_uc, omega_lc, centerNbody.name);
    }
    //-----------------------------------------------
    // Ellipse Gizmo stuff
    //-----------------------------------------------

    private void DrawEllipseGizmo(Vector3 centerPos) {
        int rayCount = 0;
        Gizmos.color = Color.white;
        GravityEngine ge = GravityEngine.Instance();

        GameObject centerObject = centerNbody.gameObject;

        Vector3[] positions = EllipsePositions(NUM_STEPS, centerPos, false); // do not apply mapToScene
        for (int i = 1; i < NUM_STEPS; i++) {
            Gizmos.DrawLine(positions[i], positions[i - 1]);
            rayCount = (rayCount + 1) % STEPS_PER_RAY;
            if (rayCount == 0) {
                Gizmos.DrawLine(centerObject.transform.position, positions[i]);
            }
        }
        // close the circle
        Gizmos.DrawLine(positions[NUM_STEPS - 1], positions[0]);

        // Draw the axes in a different color
        Gizmos.color = Color.red;
        Gizmos.DrawLine(GetPositionForThetaRadians(0.5f * Mathf.PI, centerPos), GetPositionForThetaRadians(-0.5f * Mathf.PI, centerPos));
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(GetPositionForThetaRadians(0f, centerPos), GetPositionForThetaRadians(Mathf.PI, centerPos));

        // move body to location specified by parameters but only if GE not running
        if (!Application.isPlaying) {
            NBody nbody = GetComponent<NBody>();
            if (nbody != null) {
                nbody.EditorUpdate(ge);
            }
        }
        // Draw the Hill sphere
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, OrbitUtils.HillRadius(centerObject, transform.gameObject));
    }

    /// <summary>
    /// Determine orbit position in physics space (with rotation and center offset) for the specified
    /// angle in the orbit (in radians)
    /// 
    /// Common code works for ellipse and hyperbola, but not a parabola. Nudge a parabola into hyperbola
    /// </summary>
    /// <param name="thetaRadians"></param>
    /// <param name="centerPos"></param>
    /// <returns></returns>
    private Vector3 GetPositionForThetaRadians(float thetaRadians, Vector3 centerPos) {
        float ecc = (float)eccentricity;
        if (Mathf.Abs(ecc - 1f) < 1E-6)
            ecc = (float)(1 + 1E-5);
        float r = (float)(p / (1f + ecc * Mathf.Cos(thetaRadians)));
        Vector3 position = new Vector3(r * Mathf.Cos(thetaRadians), r * Mathf.Sin(thetaRadians), 0);
        // move from XY plane to the orbital plane and add offset for center
        return conic_orientation * position + centerPos; ;
    }


    /// <summary>
    /// Get the positions where the orbit is at a specified radius. In general there are two. The result
    /// is positions in world space. These can then be used in e.g. TimeOfFlight
    /// 
    /// Use the general equation for hyperbola and ellipse and nudge a parabola into a hyperbola. 
    /// </summary>
    /// <param name="radius"></param>
    /// <returns></returns>
    public Vector3[] GetPositionsForRadius(double radius, Vector3 centerPos) {
        float theta = GetPhaseDegForRadius(radius) * Mathf.Deg2Rad;
        return new Vector3[2] { GetPositionForThetaRadians(theta, centerPos), GetPositionForThetaRadians(-theta, centerPos) };

    }

    /// <summary>
    /// Get the phase for the specified radius/altitude. 
    /// 
    /// In the case of an ellipse if the altitute larger/smaller than those allowed just return the max. 
    /// An ellipse will have two values that match the altitude. To get the alternative one use (360 - phase). 
    /// 
    /// A circular orbit will return a phase of zero for any altitude. 
    /// 
    /// </summary>
    /// <param name="radius"></param>
    /// <returns></returns>
    public float GetPhaseDegForRadius(double radius) {
        double ecc = eccentricity;
        // Nudge eccentricity if needed to avoid a divide by zero
        if (Mathd.Abs(ecc - 1f) < 1E-6)
            ecc = (float)(1 + 1E-5);
        float theta = 0;
        if (ecc > 1E-8) {
            double thetaEqn = (p / radius - 1) / ecc;
            theta = (float)Mathd.Acos(Mathd.Clamp(thetaEqn, -1.0, 1.0));
        }
        return theta * Mathf.Rad2Deg;
    }

    /// <summary>
    /// Get the current orbital elements using the live position and velocity from GE. 
    /// </summary>
    /// <returns></returns>
    public OrbitUtils.OrbitElements GetCurrentOrbitalElements() {
        return OrbitUtils.RVtoCOE(ge.GetPositionDoubleV3(nbody),
                                  ge.GetVelocityDoubleV3(nbody),
                                  centerNbody,
                                  false /* relative position */);

    }

    /// <summary>
    /// Get the current phase (degrees) of the body based on the position and velocity retreived from GE
    /// </summary>
    /// <returns></returns>
    public double GetCurrentPhase() {
        return OrbitUtils.GetPhaseFromOE(GetCurrentOrbitalElements()) * Mathd.Rad2Deg;
    }


    /// <summary>
    /// Get the positions where the orbit is at a specified radius. In general there are two. The result
    /// is positions in world space. These can then be used in e.g. TimeOfFlight
    /// 
    /// Use the general equation for hyperbola and ellipse and nudge a parabola into a hyperbola. 
    /// </summary>
    /// <param name="radius"></param>
    /// <returns></returns>
    public Vector3[] GetPositionsForRadius(double radius) {
        return GetPositionsForRadius(radius, ge.GetPhysicsPosition(centerNbody));
    }

    //-----------------------------------------------
    // Hyperbola Gizmo stuff
    //-----------------------------------------------
    // fraction of the branch of the hyperbola to display in OrbitPositions
    private float branchDisplayFactor = 0.5f;
    private float b; 

    private void DrawHyperGizmo(Vector3 centerPos) {
        GravityEngine ge = GravityEngine.Instance();
        int rayCount = 0;
        Gizmos.color = Color.white;
        Vector3[] points = HyperOrbitPositions(NUM_STEPS, centerPos, false);
        GameObject centerObject = centerNbody.gameObject;

        for (int i = 1; i < NUM_STEPS; i++) {
            Gizmos.DrawLine(points[i - 1], points[i]);
            // draw rays from focus
            rayCount = (rayCount + 1) % STEPS_PER_RAY;
            if (rayCount == 0) {
                Gizmos.DrawLine(centerPos, points[i]);
            }
        }
        Gizmos.color = Color.white;
        // Draw the axes in a different color
        Gizmos.color = Color.red;
        Gizmos.DrawLine(GetPositionForThetaRadians(0.5f * Mathf.PI, centerPos), 
                        GetPositionForThetaRadians(-0.5f * Mathf.PI, centerPos));
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(HyperPositionForY(0f, centerPos), centerObject.transform.position);

        // move body to location specified by parameters
        if (!Application.isPlaying) {
            NBody nbody = GetComponent<NBody>();
            if (nbody != null) {
                nbody.EditorUpdate(ge);
            }
        }

    }


 
    /// <summary>
    /// Determine the position in physics space given a Y position wrt the focus.
    /// for the hyperbola. Use Cartesian co-ords since angles are very twitchy for hyperbolas.
    /// </summary>
    /// <param name="y"></param>
    /// <param name="cPos"></param>
    /// <returns></returns>
	private Vector3 HyperPositionForY(float y, Vector3 cPos) {
        float a = (float)(p / (1 - eccentricity * eccentricity));
        float b = (float) p;
        float x = (float)( a * Mathf.Sqrt(1 + y * y / (b * b)));
        // focus is at x = -(a*e), want to translate to origin is at focus
        // -ve x to take the left branch
        Vector3 position = new Vector3((float)(-x + a * eccentricity), y, 0);
        // move from XY plane to the orbital plane
        Vector3 newPosition = conic_orientation * position;
        // orbit position is WRT center
        newPosition += cPos;
        return newPosition;
    }

    public void SetPositionDouble(Vector3d pos) {
        throw new NotImplementedException();
    }
}
