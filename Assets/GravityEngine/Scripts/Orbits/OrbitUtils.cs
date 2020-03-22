using UnityEngine;
using System.Collections;

public class OrbitUtils  {

    public const double small = 1E-3;

	/// <summary>
	/// Calculates the Hill Radius (radius at which the secondary's gravity becomes dominant, when the 
	/// secondary is in orbit around the primary). 
	/// </summary>
	/// <returns>The radius.</returns>
	/// <param name="primary">Primary.</param>
	/// <param name="secondary">Secondary. In orbit around primary</param>
	static public float HillRadius(GameObject primary, GameObject secondary) {

		NBody primaryBody = primary.GetComponent<NBody>(); 
		NBody secondaryBody = secondary.GetComponent<NBody>(); 
		EllipseBase orbit = secondary.GetComponent<EllipseBase>();
		if ((primaryBody == null) || (secondaryBody == null) || (orbit == null)) {
			return 0;
		}
		float denom = 3f*(secondaryBody.mass + primaryBody.mass);
		if (Mathf.Abs(denom) < 1E-6) {
			return 0;
		}
		return Mathf.Pow(secondaryBody.mass/denom, 1/3f) * orbit.a_scaled * (1-orbit.ecc);

	}

    /// <summary>
    /// Get the center
    /// </summary>
    /// <param name="objectInOrbit"></param>
    /// <returns></returns>
    static public NBody GetCenterNbody(Transform objectInOrbit, GameObject centerObject) {
        // If parent has an Nbody assume it is the center
        NBody centerNbody = null;
        if (centerObject == null) {
            if (objectInOrbit.parent != null) {
                centerNbody = objectInOrbit.parent.gameObject.GetComponent<NBody>();
                if (centerNbody != null) {
                    centerObject = objectInOrbit.parent.gameObject;
                } else {
                    Debug.LogError("Parent object must have NBody attached");
                    return null;
                }
            } else {
                Debug.Log("Warning - Require a parent object (with NBody)");
                // This path when init-ed via Instantiate() script will need to 
                // call Init() explicily once orbit params and center are set
                return null;
            }
        } else {
            centerNbody = centerObject.GetComponent<NBody>();
            if (centerNbody == null) {
                Debug.LogError("CenterObject must have an NBody attached");
            }
        }
        return centerNbody;
    }

    /// <summary>
    /// Calculate the semi-major axis for the required period
    /// </summary>
    /// <param name="period"></param>
    /// <param name="centerMass"></param>
    /// <returns></returns>
    public static double CalcAForPeriod(double period, double centerMass) {
        double p_over_2pi = period / (2.0 * Mathd.PI);
        return Mathd.Pow( p_over_2pi * p_over_2pi * centerMass, 1.0 / 3.0) ;
    }

    /// <summary>
    /// Determine how many parents/grandparents etc. have Kepler mode. 
    /// GE uses this to ensure evolution starts at the most central body in a heirarchy
    /// and works out to the leaves. 
    /// </summary>
    public static int CalcKeplerDepth(IFixedOrbit fixedBody) {

        if (!fixedBody.IsOnRails()) {
            return 0;
        }

        int depth = 0;
        bool done = false;
        NBody center = fixedBody.GetCenterNBody();
        while (!done && (center != null)) {
            IFixedOrbit parent = center.GetComponent<IFixedOrbit>();
            if ((parent != null) && parent.IsOnRails()) {
                depth++;
                center = parent.GetCenterNBody();
            } else {
                done = true;
            }
        }

        return depth;
    }

    /// <summary>
    /// Determine the SOI radius in internal physics units.
    /// </summary>
    /// <param name="planet"></param>
    /// <param name="moon"></param>
    /// <returns></returns>
    public static float SoiRadius(NBody planet, NBody moon) {
        // to allow to run before GE is up, use Ellipse component to get radius
        OrbitEllipse moonEllipse = moon.gameObject.GetComponent<OrbitEllipse>();
        float a;
        if (moonEllipse != null) {
            a = moonEllipse.a_scaled;
        } else {
            OrbitUniversal orbitU = moon.GetComponent<OrbitUniversal>();
            if (orbitU != null) {
                a = (float) orbitU.GetApogee();
            } else {
                Debug.LogWarning("Could not get moon orbit size");
                return float.NaN;
            }
        }
        // mass scaling will cancel in this ratio
        return Mathf.Pow(moon.mass / planet.mass, 0.4f) * a;
    }

    public static bool IsOnRails(NBody nbody) {
        bool isOnRails = false;
        IFixedOrbit ifOrbit = nbody.GetComponent<IFixedOrbit>();
        if (ifOrbit != null)
            isOnRails = ifOrbit.IsOnRails();
        return isOnRails; 
    }

    // Taken from Vallado source code site. (Also used in LambertUniversal)
    public static void FindC2C3(double znew, out double c2new, out double c3new) {
        double small, sqrtz;
        small = 0.00000001;

        // -------------------------  implementation   -----------------
        if (znew > small) {
            sqrtz = System.Math.Sqrt(znew);
            c2new = (1.0 - System.Math.Cos(sqrtz)) / znew;
            c3new = (sqrtz - System.Math.Sin(sqrtz)) / (sqrtz * sqrtz * sqrtz);
        } else {
            if (znew < -small) {
                sqrtz = System.Math.Sqrt(-znew);
                c2new = (1.0 - System.Math.Cosh(sqrtz)) / znew;
                c3new = (System.Math.Sinh(sqrtz) - sqrtz) / (sqrtz * sqrtz * sqrtz);
            } else {
                c2new = 0.5;
                c3new = 1.0 / 6.0;
            }
        }
    }  // findc2c3

    /// <summary>
    /// Determine the time of flight in physics time units (GE internal time) that it takes for the body
    /// in orbit to go from position r0 to position r1 in an orbit with parameter p.
    /// 
    /// The angle between two 3D vectors cannot be greater than 180 degrees unless the orientation of the
    /// plane they define is also specified. The normal parameter is used for this. If an angle less than
    /// 180 is desired then the cross product of r0 and v0 can be used as the normal. 
    /// Calling TOF via the OrbitUniversal wrapper will handle all that automatically. 
    /// 
    /// </summary>
    /// <param name="r0">from point (with respect to center)</param>
    /// <param name="r1">to point (with respect to center)</param>
    /// <param name="p">orbit semi-parameter</param>
    /// <param name="mu">centerbody mass</param>
    /// <param name="normal">normal to orital plane</param>
    /// <returns>time to travel from r0 to r1 in GE time</returns>
    public static double TimeOfFlight(Vector3d r0, Vector3d r1, double p, double mu, Vector3d normal) {
        // Vallado, Algorithm 11, p126
        double tof = 0;
        double r0r1 = r0.magnitude * r1.magnitude;
        double cos_dnu = Vector3d.Dot(r0, r1) / r0r1;
        double sin_dnu = Vector3d.Cross(r0, r1).magnitude / r0r1;
        // use the normal to determine if angle is > 180
        if (Vector3d.Dot(Vector3d.Cross(r0, r1), normal) < 0.0) {
            sin_dnu *= -1.0;
        }
         // GE - precision issue at 180 degrees. Simply return 1/2 the orbit period.
        if (Mathd.Abs(1.0+cos_dnu) < 1E-5) {
            double a180 = 0.5f * (r0.magnitude + r1.magnitude);
            return Mathd.Sqrt(a180 * a180 * a180 / mu) * Mathd.PI;
        }
        // sin_nu: Need to use direction of flight to pick sign per Algorithm 53
        double k = r0r1 * (1.0 - cos_dnu);
        double l = r0.magnitude + r1.magnitude;
        double m = r0r1 * (1 + cos_dnu);
        double a = (m * k * p) / ((2.0 * m - l * l) * p * p + 2.0 * k * l * p - k * k);
        double f = 1.0 - (r1.magnitude / p) * (1.0 - cos_dnu);
        double g = r0r1 * sin_dnu / (Mathd.Sqrt(mu * p));

        double alpha = 1 / a;
        if (alpha > 1E-7) {
            // ellipse
            double delta_nu = Mathd.Atan2(sin_dnu, cos_dnu);
            double fdot = Mathd.Sqrt(mu / p) * Mathd.Tan(0.5 * delta_nu) *
                ((1 - cos_dnu) / p - (1 / r0.magnitude) - (1.0 / r1.magnitude));
            double cos_deltaE = 1 - r0.magnitude / a * (1.0 - f);
            double sin_deltaE = -r0r1 * fdot / (Mathd.Sqrt(mu * a));
            double deltaE = Mathd.Atan2(sin_deltaE, cos_deltaE);
            tof = g + Mathd.Sqrt(a * a * a / mu) * (deltaE - sin_deltaE);
        } else if (alpha < -1E-7) {
            // hyperbola
            double cosh_deltaH = 1.0 + (f - 1.0) * r0.magnitude / a;
            double deltaH = GEMath.Acosh(cosh_deltaH);
            tof = g + Mathd.Sqrt(-a * a * a / mu) * (GEMath.Sinh(deltaH) - deltaH);
        } else {
            // parabola
            double c = Mathd.Sqrt(r0.magnitude * r0.magnitude + r1.magnitude * r1.magnitude - 2.0 * r0r1 * cos_dnu);
            double s = 0.5 * (r0.magnitude + r1.magnitude + c);
            tof = 2/3*Mathd.Sqrt(s*s*s/(2.0*mu))*(1-Mathd.Pow(((s-c)/s), 1.5));
        }
        return tof;
    }

    // From Vallado source code site. Adpated for GE/C#

    /// <summary>
    /// A "struct-like" class that holds all the orbital elements determined by
    /// RVtoCOE and used by COEtoRV
    /// </summary>
    public class OrbitElements {
        public enum TypeOrbit { ELLIPTICAL_INCLINED,
                                CIRCULAR_EQUATORIAL,
                                CIRCULAR_INCLINED,
                                ELLIPTICAL_EQUATORIAL
        };
        public double p;
        public double a;
        public double ecc;
        public Vector3d ecc_vec;
        public double incl;
        public double raan;
        public double argp;
        public double nu;
        public double m;
        public double eccanom;
        public double arglat;
        public double truelon;
        public double lonper;
        public TypeOrbit typeOrbit;

        public bool IsInclined() {
            return (typeOrbit == TypeOrbit.ELLIPTICAL_INCLINED) || (typeOrbit == TypeOrbit.CIRCULAR_INCLINED);
        }

        public bool IsCircular() {
            return (typeOrbit == TypeOrbit.CIRCULAR_INCLINED) || (typeOrbit == TypeOrbit.CIRCULAR_EQUATORIAL);
        }

        public override string ToString() {
            return string.Format("type={0} p={1} a={2} ecc={3} incl={4} raan={5} argp={6} nu={7} m={8}",
                typeOrbit, p, a, ecc, incl, raan, argp, nu, m);
        }
    }
    /* -----------------------------------------------------------------------------
    *
    *                           function rv2coe
    *
    *  this function finds the classical orbital elements given the geocentric
    *    equatorial position and velocity vectors.
    *
    *  author        : david vallado                  719-573-2600   21 jun 2002
    *
    *  revisions
    *    vallado     - fix special cases                              5 sep 2002
    *    vallado     - delete extra check in inclination code        16 oct 2002
    *    vallado     - add constant file use                         29 jun 2003
    *
    *  inputs          description                    range / units
    *    r           - ijk position vector            km
    *    v           - ijk velocity vector            km / s
    *
    *  outputs       :
    *    p           - semilatus rectum               km
    *    a           - semimajor axis                 km
    *    ecc         - eccentricity
    *    incl        - inclination                    0.0  to pi rad
    *    raan       - longitude of ascending node    0.0  to 2pi rad
    *    argp        - argument of perigee            0.0  to 2pi rad
    *    nu          - true anomaly                   0.0  to 2pi rad
    *    m           - mean anomaly                   0.0  to 2pi rad
    *    eccanom     - eccentric, parabolic,
    *                  hyperbolic anomaly             rad
    *    arglat      - argument of latitude      (ci) 0.0  to 2pi rad
    *    truelon     - true longitude            (ce) 0.0  to 2pi rad
    *    lonper      - longitude of periapsis    (ee) 0.0  to 2pi rad
    *
    *  locals        :
    *    hbar        - angular momentum h vector      km2 / s
    *    ebar        - eccentricity     e vector
    *    nbar        - line of nodes    n vector
    *    c1          - v**2 - u/r
    *    rdotv       - r dot v
    *    hk          - hk unit vector
    *    sme         - specfic mechanical energy      km2 / s2
    *    i           - index
    *    temp        - temporary variable
    *    typeorbit   - type of orbit                  ee, ei, ce, ci
    *
    *  coupling      :
    *    mag         - magnitude of a vector
    *    cross       - cross product of two vectors
    *    angle       - find the angle between two vectors
    *    newtonnu    - find the mean anomaly
    *
    *  references    :
    *    vallado       2013, 113, alg 9, ex 2-5
    * --------------------------------------------------------------------------- */

    // Vector3 version 4.0
    public static OrbitElements RVtoCOE(Vector3 r_in,
                            Vector3 v_in,
                            NBody centerBody,
                            float mu,
                            bool relativePos) {
        return RVtoCOE(new Vector3d(r_in), new Vector3d(v_in), centerBody, mu, relativePos);
    }

    public static OrbitElements RVtoCOE(Vector3 r_in,
                        Vector3 v_in,
                        NBody centerBody,
                        bool relativePos) {
        double mu = GravityEngine.Instance().GetMass(centerBody);
        return RVtoCOE(new Vector3d(r_in), new Vector3d(v_in), centerBody, mu, relativePos);
    }

    /// <summary>
    /// Pre-4.0 method signature without explicit use of mu
    /// </summary>
    /// <param name="r_in"></param>
    /// <param name="v_in"></param>
    /// <param name="centerBody"></param>
    /// <param name="relativePos"></param>
    /// <returns></returns>
    public static OrbitElements RVtoCOE(Vector3d r_in,
                     Vector3d v_in,
                     NBody centerBody,
                     bool relativePos) {
        double mu = GravityEngine.Instance().GetMass(centerBody);
        return RVtoCOE(r_in, v_in, centerBody, mu, relativePos);
    }

    public static OrbitElements RVtoCOE(Vector3d r_in,
                        Vector3d v_in,
                        NBody centerBody, 
                        double mu,
                        bool relativePos) {

        double  magr, magv, magn, sme, rdotv, temp, c1, hk, magh;

        Vector3d r = r_in;
        Vector3d v = v_in; 
        if (!relativePos) {
            r = r - GravityEngine.Instance().GetPositionDoubleV3(centerBody);
            v = v - GravityEngine.Instance().GetVelocityDoubleV3(centerBody);
        }
        OrbitElements oe = new OrbitElements();
        oe.eccanom = 0.0;

        // -------------------------  implementation   -----------------
        magr = r.magnitude;
        magv = v.magnitude;

        // ------------------  find h n and e vectors   ----------------
        Vector3d hbar = Vector3d.Cross(r, v);
        magh = hbar.magnitude;
        if (magh > small) {
            Vector3d nbar = new Vector3d(-hbar.y, hbar.x, 0.0);
            magn = nbar.magnitude;
            c1 = magv * magv - mu / magr;
            rdotv = Vector3d.Dot(r, v);
            temp = 1.0 / mu;
            Vector3d ebar = new Vector3d((c1 * r.x - rdotv * v.x) * temp,
                                         (c1 * r.y - rdotv * v.y) * temp,
                                         (c1 * r.z - rdotv * v.z) * temp);
            oe.ecc_vec = ebar;
            oe.ecc = ebar.magnitude;

            // ------------  find a e and semi-latus rectum   ----------
            sme = (magv * magv * 0.5) - (mu / magr);
            if (Mathd.Abs(sme) > small)
                oe.a = -mu / (2.0 * sme);
            else
                oe.a = double.NaN;
            oe.p = magh * magh * temp;

            // -----------------  find inclination   -------------------
            hk = hbar.z/ magh;
            oe.incl = Mathd.Acos(Mathd.Clamp(hk, -1.0, 1.0));

            oe.typeOrbit = OrbitElements.TypeOrbit.ELLIPTICAL_INCLINED;

            if (oe.ecc < small) {
                // ----------------  circular equatorial ---------------
                if ((oe.incl < small) || (Mathd.Abs(oe.incl - Mathd.PI) < small)) {
                    oe.typeOrbit = OrbitElements.TypeOrbit.CIRCULAR_EQUATORIAL;
                } else {
                    oe.typeOrbit = OrbitElements.TypeOrbit.CIRCULAR_INCLINED;
                }
            } else {
                // - elliptical, parabolic, hyperbolic equatorial --
                if ((oe.incl < small) || (Mathd.Abs(oe.incl - Mathd.PI) < small)) {
                     oe.typeOrbit = OrbitElements.TypeOrbit.ELLIPTICAL_EQUATORIAL;
                }
            }

            // ----------  find right ascension of the ascending node ------------
            if (magn > small) {
                temp = nbar.x / magn;
                if (Mathd.Abs(temp) > 1.0)
                    temp = Mathd.Sign(temp);
                oe.raan = Mathd.Acos(Mathd.Clamp(temp, -1.0, 1.0));
                if (nbar.y < 0.0)
                    oe.raan = 2.0 * Mathd.PI - oe.raan;
            } else
                oe.raan = double.NaN;

            // ---------------- find argument of perigee ---------------
            if (oe.typeOrbit == OrbitElements.TypeOrbit.ELLIPTICAL_INCLINED) {
                oe.argp = Vector3d.Angle(nbar, ebar) * Mathd.Deg2Rad; ;
                if (ebar.z < 0.0)
                    oe.argp = 2.0 * Mathd.PI - oe.argp;
            } else
                oe.argp = double.NaN;

            // ------------  find true anomaly at epoch    -------------
            if (!oe.IsCircular()) {
                oe.nu = Vector3d.Angle(ebar, r) * Mathd.Deg2Rad;
                if (rdotv < 0.0)

                    oe.nu = 2.0 * Mathd.PI - oe.nu;
            } else
                oe.nu = double.NaN;

            // ----  find argument of latitude - circular inclined -----
            if (oe.typeOrbit == OrbitElements.TypeOrbit.CIRCULAR_INCLINED) {
                oe.arglat = Vector3d.Angle(nbar, r) * Mathd.Deg2Rad; ;
                if (r.z < 0.0)
                    oe.arglat = 2.0 * Mathd.PI - oe.arglat;
                oe.m = oe.arglat;
            } else
                oe.arglat = double.NaN;

            // -- find longitude of perigee - elliptical equatorial ----
            if ((oe.ecc > small) && (oe.typeOrbit == OrbitElements.TypeOrbit.ELLIPTICAL_EQUATORIAL)) {
                temp = ebar.x / oe.ecc;
                if (Mathd.Abs(temp) > 1.0)
                    temp = Mathd.Sign(temp);
                oe.lonper = Mathd.Acos(Mathd.Clamp(temp, -1.0, 1.0));
                if (ebar.y < 0.0)
                    oe.lonper = 2.0 * Mathd.PI - oe.lonper;
                if (oe.incl > 0.5 * Mathd.PI)
                    oe.lonper = 2.0 * Mathd.PI - oe.lonper;
            } else
                oe.lonper = double.NaN;

            // -------- find true longitude - circular equatorial ------
            if ((magr > small) && (oe.typeOrbit == OrbitElements.TypeOrbit.CIRCULAR_EQUATORIAL)) {
                temp = r.x / magr;
                if (Mathd.Abs(temp) > 1.0)
                    temp = Mathd.Sign(temp);
                oe.truelon = Mathd.Acos(Mathd.Clamp(temp, -1.0, 1.0));
                if (r.y < 0.0)
                    oe.truelon = 2.0 * Mathd.PI - oe.truelon;
                if (oe.incl > 0.5 * Mathd.PI)
                    oe.truelon = 2.0 * Mathd.PI - oe.truelon;
                oe.m = oe.truelon;
            } else
                oe.truelon = double.NaN;

            // ------------ find mean anomaly for all orbits -----------
            if (!oe.IsCircular())
                NewtonNu(oe);
        } else {
            oe.p = double.NaN;
            oe.a = double.NaN;
            oe.ecc = double.NaN;
            oe.incl = double.NaN;
            oe.raan = double.NaN;
            oe.argp = double.NaN;
            oe.nu = double.NaN;
            oe.m = double.NaN;
            oe.arglat = double.NaN;
            oe.truelon = double.NaN;
            oe.lonper = double.NaN;
        }
        return oe;
    }  // rv2coe

 
    /* ------------------------------------------------------------------------------
	*
	*                           function coe2rv
	*
	*  this function finds the position and velocity vectors in geocentric
	*    equatorial (ijk) system given the classical orbit elements.
	*
	*  author        : david vallado                  719-573-2600    1 mar 2001
	*
	*  inputs          description                    range / units
	*    p           - semilatus rectum               km
	*    ecc         - eccentricity
	*    incl        - inclination                    0.0 to pi rad
	*    raan       - longitude of ascending node    0.0 to 2pi rad
	*    argp        - argument of perigee            0.0 to 2pi rad
	*    nu          - true anomaly                   0.0 to 2pi rad
	*    arglat      - argument of latitude      (ci) 0.0 to 2pi rad
	*    lamtrue     - true longitude            (ce) 0.0 to 2pi rad
	*    lonper      - longitude of periapsis    (ee) 0.0 to 2pi rad
	*
	*  outputs       :
	*    r           - ijk position vector            km
	*    v           - ijk velocity vector            km / s
	*
	*  locals        :
	*    temp        - temporary real*8 value
	*    rpqw        - pqw position vector            km
	*    vpqw        - pqw velocity vector            km / s
	*    sinnu       - sine of nu
	*    cosnu       - cosine of nu
	*    tempvec     - pqw velocity vector
	*
	*  coupling      :
	*    rot3        - rotation about the 3rd axis
	*    rot1        - rotation about the 1st axis
	*
	*  references    :
	*    vallado       2013, 118, alg 10, ex 2-5
	* --------------------------------------------------------------------------- */

    public static void COEtoRV(OrbitElements oe, 
                                NBody centerBody, 
                                ref Vector3d r, 
                                ref Vector3d v, 
                                bool relativePos) {
        double temp, sinnu, cosnu;
        Vector3d rpqw, vpqw;

        double mu = GravityEngine.Instance().GetMass(centerBody);
 
        // --------------------  implementation   ----------------------
        //       determine what type of orbit is involved and set up the
        //       set up angles for the special cases.
        // -------------------------------------------------------------
        if (oe.ecc < small) {
            // ----------------  circular equatorial  ------------------

            if ((oe.incl < small) | (Mathd.Abs(oe.incl - Mathd.PI) < small)) {
                oe.argp = 0.0;
                oe.raan = 0.0;
                oe.nu = oe.truelon;
            } else {
                // --------------  circular inclined  ------------------
                oe.argp = 0.0;
                oe.nu = oe.arglat;
            }
        } else {
            // ---------------  elliptical equatorial  -----------------
            if ((oe.incl < small) | (Mathd.Abs(oe.incl - Mathd.PI) < small)) {
                oe.argp = oe.lonper;
                oe.raan = 0.0;
            }
        }

        // ----------  form pqw position and velocity vectors ----------
        cosnu = Mathd.Cos(oe.nu);
        sinnu = Mathd.Sin(oe.nu);
        temp = oe.p / (1.0 + oe.ecc * cosnu);
        rpqw = new Vector3d(temp * cosnu, temp * sinnu, 0.0);
        if (Mathd.Abs(oe.p) < 0.00000001)
            oe.p = 0.00000001;
        vpqw = new Vector3d(-sinnu * Mathd.Sqrt(mu / oe.p),
                            (oe.ecc + cosnu) * Mathd.Sqrt(mu / oe.p),
                                        0.0);

        // ----------------  perform transformation to ijk  ------------
        r = GEMath.Rot3(rpqw, -oe.argp);
        r = GEMath.Rot1(r, -oe.incl);
        r = GEMath.Rot3(r, -oe.raan);

        v = GEMath.Rot3(vpqw, -oe.argp);
        v = GEMath.Rot1(v, -oe.incl);
        v = GEMath.Rot3(v, -oe.raan);
        if (!relativePos) {
            r += GravityEngine.Instance().GetPositionDoubleV3(centerBody);
            v += GravityEngine.Instance().GetVelocityDoubleV3(centerBody);
        }
    }  // coe2rv

    public static void COEtoRVMirror(OrbitElements oe,
                                NBody centerBody,
                                ref Vector3d r,
                                ref Vector3d v,
                                bool relativePos) {

        double temp, sinnu, cosnu;
        Vector3d rpqw, vpqw;

        double mu = GravityEngine.Instance().GetMass(centerBody);

        // --------------------  implementation   ----------------------
        //       determine what type of orbit is involved and set up the
        //       set up angles for the special cases.
        // -------------------------------------------------------------
        if (oe.ecc < small) {
            // ----------------  circular equatorial  ------------------
            if ((oe.incl < small) | (Mathd.Abs(oe.incl - Mathd.PI) < small)) {
                oe.argp = 0.0;
                oe.raan = 0.0;
                oe.nu = oe.truelon;
            } else {
                // --------------  circular inclined  ------------------
                oe.argp = 0.0;
                oe.nu = oe.arglat;
            }
        } else {
            // ---------------  elliptical equatorial  -----------------
            if ((oe.incl < small) | (Mathd.Abs(oe.incl - Mathd.PI) < small)) {
                oe.argp = oe.lonper;
                oe.raan = 0.0;
            }
        }

        // ----------  form pqw position and velocity vectors ----------
        cosnu = Mathd.Cos(oe.nu);
        sinnu = Mathd.Sin(oe.nu);
        temp = oe.p / (1.0 + oe.ecc * cosnu);
        // flip Y
        rpqw = new Vector3d(temp * cosnu, -temp * sinnu, 0.0);
        if (Mathd.Abs(oe.p) < 0.00000001)
            oe.p = 0.00000001;

        // flip X (not Y)
        vpqw = new Vector3d(sinnu * Mathd.Sqrt(mu / oe.p),
                            (oe.ecc + cosnu) * Mathd.Sqrt(mu / oe.p),
                                        0.0);

        // ----------------  perform transformation to ijk  ------------
        r = GEMath.Rot3(rpqw, -oe.argp);
        r = GEMath.Rot1(r, -oe.incl);
        r = GEMath.Rot3(r, -oe.raan);

        v = GEMath.Rot3(vpqw, -oe.argp);
        v = GEMath.Rot1(v, -oe.incl);
        v = GEMath.Rot3(v, -oe.raan);

        if (!relativePos) {
            r += GravityEngine.Instance().GetPositionDoubleV3(centerBody);
            v += GravityEngine.Instance().GetVelocityDoubleV3(centerBody);
        }

    }  // coe2rvMirror

    /// <summary>
    /// Returns the current phase in radian
    /// </summary>
    /// <param name="oe"></param>
    /// <returns></returns>
    public static double GetPhaseFromOE(OrbitUtils.OrbitElements oe) {
        if (oe.ecc < small) {
            // ----------------  circular equatorial  ------------------

            if ((oe.incl < small) | (Mathd.Abs(oe.incl - Mathd.PI) < small)) {
               return oe.truelon;
            } else {
                // --------------  circular inclined  ------------------
                return oe.arglat;
            }
        }
        return oe.nu;
    }

    /* -----------------------------------------------------------------------------
	*
	*                           function newtonnu
	*
	*  this function solves keplers equation when the true anomaly is known.
	*    the mean and eccentric, parabolic, or hyperbolic anomaly is also found.
	*    the parabolic limit at 168ø is arbitrary. the hyperbolic anomaly is also
	*    limited. the hyperbolic sine is used because it's not double valued.
	*
	*  author        : david vallado                  719-573-2600   27 may 2002
	*
	*  revisions
	*    vallado     - fix small                                     24 sep 2002
	*
	*  inputs          description                    range / units
	*    ecc         - eccentricity                   0.0  to
	*    nu          - true anomaly                   -2pi to 2pi rad
	*
	*  outputs       :
	*    e0          - eccentric anomaly              0.0  to 2pi rad       153.02 deg
	*    m           - mean anomaly                   0.0  to 2pi rad       151.7425 deg
	*
	*  locals        :
	*    e1          - eccentric anomaly, next value  rad
	*    sine        - sine of e
	*    cose        - cosine of e
	*    ktr         - index
	*
	*  coupling      :
	*    arcsinh     - arc hyperbolic sine
	*    sinh        - hyperbolic sine
	*
	*  references    :
	*    vallado       2013, 77, alg 5
	* --------------------------------------------------------------------------- */

    private static void NewtonNu(OrbitElements oe) {
        double small, sine, cose, cosnu, temp;

        double ecc = oe.ecc;
        double nu = oe.nu;
        double e0, m;

        // ---------------------  implementation   ---------------------
        e0 = 999999.9;
        m = 999999.9;
        small = 0.00000001;

        // --------------------------- circular ------------------------
        if (Mathd.Abs(ecc) < small) {
            m = nu;
            e0 = nu;
        } else
        // ---------------------- elliptical -----------------------
        if (ecc < 1.0 - small) {
            cosnu = Mathd.Cos(nu);
            temp = 1.0 / (1.0 + ecc * cosnu);
            sine = (Mathd.Sqrt(1.0 - ecc * ecc) * Mathd.Sin(nu)) * temp;
            cose = (ecc + cosnu) * temp;
            e0 = Mathd.Atan2(sine, cose);
            m = e0 - ecc * Mathd.Sin(e0);
        } else
        // -------------------- hyperbolic  --------------------
        if (ecc > 1.0 + small) {
            if ((ecc > 1.0) && (Mathd.Abs(nu) + 0.00001 < Mathd.PI - Mathd.Acos(Mathd.Clamp(1.0 / ecc, -1.0, 1.0)))) {
                sine = (Mathd.Sqrt(ecc * ecc - 1.0) * Mathd.Sin(nu)) / (1.0 + ecc * Mathd.Cos(nu));
                e0 = GEMath.Asinh(sine);
                m = ecc * GEMath.Sinh(e0) - e0;
            }
        } else
        // ----------------- parabolic ---------------------
        if (Mathd.Acos(Mathd.Clamp(nu, -1.0, 1.0)) < 168.0 * Mathd.PI / 180.0) {
            e0 = Mathd.Tan(nu * 0.5);
            m = e0 + (e0 * e0 * e0) / 3.0;
        }

        if (ecc < 1.0) {
            m = System.Math.Truncate(m / (2.0 * Mathd.PI));
            if (m < 0.0)
                m = m + 2.0 * Mathd.PI;
            e0 = System.Math.Truncate(e0 / (2.0 * Mathd.PI));
        }
        oe.m = m;
        oe.eccanom = e0;
    }  // newtonnu


}
