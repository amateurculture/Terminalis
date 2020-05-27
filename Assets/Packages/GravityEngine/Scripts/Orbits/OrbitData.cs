using UnityEngine;
using System.Collections;

/// <summary>
/// Orbit data.
/// Hold the traditional orbit parameters for an elliptic/hyperbolic orbit. 
///
/// Provide utility methods to derive the orbit parameters from the position, velocity and centerBody
/// of an NBody object pair. This orbit prediction is based only on the two bodies (and assumes the central
/// body mass dominates) - the influence of other bodies in the scene may result in an actual path that is
/// different from the predicted path. 
/// </summary>
[System.Serializable]
public class OrbitData {

    public const float OD_ZERO_LIMIT = 0.001f;

    // Orbit parameters (user control via FixedEllipseEditor)
    // These parameters are in world space. 
    //! eccentricity (0..1, 0=circle, 1=linear)
    public float ecc;

    //! eccentricity vector (direction to point of closest approach when e != 0)
    public Vector3d ecc_vec;

    // Allow EITHER a or p to specify size (JPL data uses a for planets and p for comets, so both are useful)
    //! semi-major axis - based on paramBy user can specify a OR p. a = p/(1-ecc)
    public float a = 10f;
    //! perihelion 
    public float perihelion;
    //! "longitude of ascending node" - angle from x-axis to line from focus to pericenter
    public float omega_uc;
    //! "argument of perienter" - angle from ascending node to pericenter
    public float omega_lc;
    //! inclination (degrees!)
    public float inclination;

    //! initial TRUE anomoly (angle wrt line from focus to closest approach)
    public float phase;

    //! period of orbit in game time (uses GravityScalar.GameSecToWorldTime for world units)
    public float period;

    private double omegaAngular;

    //! time to periapsis (point of closest approach) in game seconds (use GravityScalar.GameSecToWorldTime for world units)
    public float tau;

    //! Hyperbola - initial distance from focus
    public float r_initial = 10f;

    public NBody centralMass;

    public NBody nbody;

    //! Scaled mass of central body
    public float mu;

    private const float MIN_ANGULAR_MOMTM = 0.001f;

    private const float MIN_VECTOR_LEN = 0.01f;

    // Empty constructor
    public OrbitData() {
        // empty
    }

    public OrbitData(OrbitData od) {
        this.centralMass = od.centralMass;
        this.a = od.a;
        this.perihelion = od.perihelion;
        this.ecc = od.ecc;
        this.inclination = od.inclination;
        this.omega_lc = od.omega_lc;
        this.omega_uc = od.omega_uc;
        this.phase = od.phase;
        this.nbody = od.nbody;
        this.mu = od.mu;
        this.period = od.period;
    }

    /// <summary>
    /// Construct an orbit data routine from an existing orbit ellipse by copying the orbital elements
    /// </summary>
    /// <param name="orbitEllipse"></param>
    public OrbitData(OrbitEllipse orbitEllipse) {
        GravityEngine ge = GravityEngine.Instance();
        a = orbitEllipse.a_scaled;
        omega_lc = orbitEllipse.omega_lc;
        omega_uc = orbitEllipse.omega_uc;
        inclination = orbitEllipse.inclination;
        ecc = orbitEllipse.ecc;
        phase = orbitEllipse.phase;
        centralMass = orbitEllipse.centerObject.GetComponent<NBody>();
        // TODO: Should be m1 + m2 
        mu = (float)(ge.GetMass(centralMass) );
        period = CalcPeriod();
    }

    /// <summary>
    /// Construct an orbit data routine from an existing orbit universal by copying the orbital elements
    /// </summary>
    /// <param name="orbitU"></param>
    public OrbitData(OrbitUniversal orbitU) {
        a = (float) orbitU.GetMajorAxis();
        omega_lc = (float) orbitU.omega_lc;
        omega_uc = (float) orbitU.omega_uc;
        inclination = (float) orbitU.inclination;
        ecc = (float) orbitU.eccentricity;
        phase = (float) orbitU.phase;
        nbody = orbitU.GetNBody();
        centralMass = orbitU.centerNbody;
        mu = (float)(orbitU.GetMu());
        period = CalcPeriod();
    }


    /// <summary>
    /// Computes the orbit parameters for a specified velocity with respect to a central body.
    /// This method assumes the orbit is not a Kepler-mode orbit ellipse. If it is (or not sure) then use
    /// SetOrbit() below.
    /// </summary>
    /// <param name="forNbody"></param>
    /// <param name="aroundNBody"></param>
    /// <param name="velocity"></param>
    public void SetOrbitForVelocity(NBody forNbody, NBody aroundNBody, Vector3 velocity) {
        nbody = forNbody;
        centralMass = aroundNBody;
        GravityEngine ge = GravityEngine.Instance();
        Vector3 r_for = ge.GetPhysicsPosition(forNbody);
        mu = (float)(ge.GetMass(centralMass) + ge.GetMass(forNbody));
        RVtoCOEWrapper(aroundNBody, r_for, velocity);
        //Debug.LogFormat("SetOrbitForVelocity: a={0} perih={1} e={2} i={3} Omega={4} omega={5} V={6}",
        //                a, perihelion, ecc, inclination, omega_uc, omega_lc, velocity);
    }


    /// <summary>
    /// Computes the orbit parameters for a specified velocity (from NBody) with respect to a central body.
    /// This method assumes the orbit is not a Kepler-mode orbit ellipse. If it is (or not sure) then use
    /// SetOrbit() below.
    /// </summary>
    /// <param name="forNbody"></param>
    /// <param name="aroundNBody"></param>
    public void SetOrbitForVelocity(NBody forNbody, NBody aroundNBody) {
        nbody = forNbody;
        nbody.UpdateVelocity();
        centralMass = aroundNBody;
        aroundNBody.UpdateVelocity();
        SetOrbitForVelocity(forNbody, aroundNBody, forNbody.vel_phys);
    }

    /// <summary>
    /// SetOrbit
    /// Determine orbit params from an attached orbit component (if Kepler) otherwise use the velocity to 
    /// determine the orbit
    /// </summary>
    /// <param name="forNbody"></param>
    /// <param name="aroundNBody"></param>
    public void SetOrbit(NBody forNbody, NBody aroundNBody) {
        nbody = forNbody;
        centralMass = aroundNBody;
        // Kepler Seq? (Fix from Petri)
        KeplerSequence keplerSeq = nbody.GetComponent<KeplerSequence>();
        if (keplerSeq != null) {
            OrbitUniversal orbitUKep = keplerSeq.GetCurrentOrbit();
            if (orbitUKep != null) {
                ecc = (float)orbitUKep.eccentricity;
                // Might need to make a > 0 for hyperbola since OrbitData got this wrong??
                a = (float)orbitUKep.p / (1 - ecc * ecc);
                omega_lc = (float)orbitUKep.omega_lc;
                omega_uc = (float)orbitUKep.omega_uc;
                inclination = (float)orbitUKep.inclination;
                mu = GravityEngine.Instance().GetPhysicsMass(aroundNBody);
                period = CalcPeriod();
                // TODO:  tau
                phase = (float)orbitUKep.phase;
                return;
            }
        }       
        // is this a Kepler body
        if (nbody.engineRef.fixedBody != null) {
            OrbitEllipse orbitEllipse = nbody.GetComponent<OrbitEllipse>();
            if (orbitEllipse != null) {
                ecc = orbitEllipse.ecc;
                a = orbitEllipse.a * GravityEngine.Instance().GetLengthScale();
                omega_lc = orbitEllipse.omega_lc;
                omega_uc = orbitEllipse.omega_uc;
                inclination = orbitEllipse.inclination;
                mu = GravityEngine.Instance().GetPhysicsMass(aroundNBody);
                period = CalcPeriod();
                // TODO:  tau
                phase = orbitEllipse.phase;
                return;
            }
            OrbitUniversal orbitU = nbody.GetComponent<OrbitUniversal>();
            if (orbitU != null) {
                ecc = (float) orbitU.eccentricity;
                // Might need to make a > 0 for hyperbola since OrbitData got this wrong??
                a = (float) orbitU.p / (1 - ecc * ecc);
                omega_lc = (float) orbitU.omega_lc;
                omega_uc = (float) orbitU.omega_uc;
                inclination = (float) orbitU.inclination;
                mu = GravityEngine.Instance().GetPhysicsMass(aroundNBody);
                period = CalcPeriod();
                // TODO:  tau
                phase = (float) orbitU.phase;
                return;
            }
            OrbitHyper orbitHyper = nbody.GetComponent<OrbitHyper>();
            if (orbitHyper != null) {
                ecc = orbitHyper.ecc;
                perihelion = orbitHyper.perihelion * GravityEngine.Instance().GetLengthScale();
                omega_lc = orbitHyper.omega_lc;
                omega_uc = orbitHyper.omega_uc;
                inclination = orbitHyper.inclination;
                // need phase, tau, period
                return;
            }
        }
        SetOrbitForVelocity(forNbody, aroundNBody);
    }

    /// <summary>
    /// Compute the axis of the orbit by taking r x v and normalizing. 
    /// </summary>
    /// <returns></returns>
    public Vector3d GetAxis() {
        GravityEngine ge = GravityEngine.Instance();
        Vector3d r = ge.GetPositionDoubleV3(nbody) - ge.GetPositionDoubleV3(centralMass);
        Vector3d v = GravityEngine.Instance().GetVelocityDoubleV3(nbody) - ge.GetVelocityDoubleV3(centralMass);
        return Vector3d.Normalize(Vector3d.Cross(r, v));
    }

    /// <summary>
    /// Get the angular velocity in rad/time
    /// </summary>
    /// <returns></returns>
    public double GetOmegaAngular() {
        return omegaAngular;
    }

    /// <summary>
    /// Calculate the period in worldTime
    /// </summary>
    /// <returns></returns>
    private float CalcPeriod() {
        // this equation has a G but we set G=1
        omegaAngular = Mathf.Sqrt(mu / (a * a * a));
        float _period = 2f * Mathf.PI / (float)omegaAngular;
        return _period; 
    }


    private void RVtoCOEWrapper(NBody aroundNBody, Vector3 r, Vector3 v) {
        // This breaks a LOT of unit tests!
        OrbitUtils.OrbitElements oe = OrbitUtils.RVtoCOE(r, v, aroundNBody, mu,  false);
        // OrbitUtils.OrbitElements oe = OrbitUtils.RVtoCOE(r, v, aroundNBody, false);
        ecc = (float)oe.ecc;
        a = (float)oe.a;
        // awkward, but hyperbola expects -a (TODO clean this all out!)
        if (ecc > 1.0f) { 
            a = -a;
        }
        perihelion = a * (ecc - 1);
        inclination = Mathf.Rad2Deg * (float) oe.incl;
        omega_uc = 0;
        omega_lc = 0; 
        if (oe.IsInclined()) {
            if (oe.IsCircular()) {
                omega_uc = Mathf.Rad2Deg * (float)oe.raan;
            } else { 
                omega_uc = Mathf.Rad2Deg * (float)oe.raan;
                omega_lc = Mathf.Rad2Deg * (float)oe.argp;
            }
        } else {
            if (!oe.IsCircular()) {
                omega_lc = Mathf.Rad2Deg * (float)oe.lonper;
            }
            // if not inclined and circular, no omega_uc or omega_lc, just phase.
        }
        phase = (float) OrbitUtils.GetPhaseFromOE(oe) * Mathf.Rad2Deg;
        ecc_vec = oe.ecc_vec;

        if (ecc < 1.0f) {
            CalcPeriod();
            CalcTau( r,  v);
        }
    }

    /// <summary>
    /// Determine the time to periapsis in physics time. 
    /// </summary>
    /// <param name="r"></param>
    /// <param name="v"></param>
    public void CalcTau(Vector3 r, Vector3 v) {
        // Determine time to periapsis (M&D 2.140)
        // Eqn assumes E=0 when t=tau
        float E = Mathf.Acos((1f - r.magnitude / a) / ecc);
        // this equation has a G but we set G=1
        period = CalcPeriod();

        float M = (E - ecc * Mathf.Sin(E));
        tau = M * Mathf.Sqrt(a * a * a / mu);
        // tau is giving time to/from apoapsis, need to find out which
        float vdotr = Vector3.Dot(v, r);
        if (vdotr > 0) {
            tau = period - tau;
        }
    }


    public string LogString() {
        return string.Format("a={0} ecc={1} incl={2} Omega={3} omega={4} phase ={5} period={6} tau={7}", 
                        a, ecc, inclination, omega_uc, omega_lc, phase, period, tau);
    }


    /// <summary>
    /// Get the absolute physics position for a specified phase for the ellipse defined by this OrbitData.
    /// </summary>
    /// <param name="phaseDeg"></param>
    /// <returns></returns>
    public Vector3 GetPhysicsPositionforEllipse(float phaseDeg) {

        return GetPhysicsPositionforEllipse(phaseDeg, false); 
    }

    /// <summary>
    /// Get position in the ellipse relative to the center body
    /// </summary>
    /// <param name="phaseDeg"></param>
    /// <returns></returns>
    public Vector3 GetPhysicsPositionforEllipseRelative(float phaseDeg) {

        return GetPhysicsPositionforEllipse(phaseDeg, true);
    }

    /// <summary>
    /// Get the physics position for a specified phase for the ellipse defined by this OrbitData.
    /// </summary>
    /// <param name="phaseDeg"></param>
    /// <param name="relativePos">Return the position relative to the center object, otherwise absolute wolrd value.</param>
    /// <returns></returns>
    private Vector3 GetPhysicsPositionforEllipse(float phaseDeg, bool relativePos) {

        // C&P from EllipseBase - make common someday
        Quaternion ellipse_orientation = Quaternion.AngleAxis(omega_uc, GEConst.zunit) *
                      Quaternion.AngleAxis(inclination, GEConst.xunit) *
                      Quaternion.AngleAxis(omega_lc, GEConst.zunit);

        float phaseRad = phaseDeg * Mathf.Deg2Rad;
        // position object using true anomoly (angle from  focus)
        // a is really a_scaled when constructed from OrbitEllipse (so scaling has been done)
        float r = a * (1f - ecc * ecc) / (1f + ecc * Mathf.Cos(phaseRad));

        Vector3 pos = new Vector3(r * Mathf.Cos(phaseRad), r * Mathf.Sin(phaseRad), 0);
        // move from XY plane to the orbital plane
        Vector3 new_p = ellipse_orientation * pos;
        // orbit position is WRT center. Could be adding dynamically to an object in motion, so need current position. 
        if (!relativePos) { 
            Vector3 centerPos = Vector3.zero;
            // used by widgets - so need to get explcitly
            if (centralMass.engineRef != null) {
                centerPos = GravityEngine.Instance().GetPhysicsPosition(centralMass);
            } else {
                // setup - not yet added to GE
                centerPos = centralMass.initialPhysPosition;
            }
            new_p += centerPos;
        }
        return new_p;
    }

    /// <summary>
    /// Determine velocity in physics units at the indicated phase angle (in degrees)
    /// 
    /// </summary>
    /// <param name="phaseDeg"></param>
    /// <returns></returns>
    public Vector3 GetPhysicsVelocityForEllipse(float phaseDeg) {
        /// Uses Vallado, Algorithm 10 for (x,y) plane and then rotates into place

        Quaternion ellipse_orientation = Quaternion.AngleAxis(omega_uc, GEConst.zunit) *
                       Quaternion.AngleAxis(inclination, GEConst.xunit) *
                       Quaternion.AngleAxis(omega_lc, GEConst.zunit);
        float p = a * (1f - ecc * ecc);
        float phaseRad = phaseDeg * Mathf.Deg2Rad;
        float vx = -Mathf.Sqrt(mu / p) * Mathf.Sin(phaseRad);
        float vy = Mathf.Sqrt(mu / p) * (ecc + Mathf.Cos(phaseRad));
        return ellipse_orientation * new Vector3(vx, vy, 0);
    }

}
