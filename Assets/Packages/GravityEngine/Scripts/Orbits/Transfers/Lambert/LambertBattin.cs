using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LambertBattin : OrbitTransfer
{


    private double twopi = 2.0 * Mathd.PI;
    private double pi = Mathd.PI;

    private Vector3d r1;
    private Vector3d r2;

    //! Initial velocity at point 1
    private Vector3d r1crossv1; 

    private NBody fromNBody;

    //! flag indicating if ship (fromOrbit) is transfering to an outer orbit
    private bool innerToOuter;

    private double mu;

    // center position of the orbit. r1 and r2 are with respect to this position
    private Vector3d center3d;

    public LambertBattin(OrbitData _fromOrbit, OrbitData _toOrbit) : base(_fromOrbit, _toOrbit) {

        name = "LambertBattin";
        // Fundamentals of Astrodynamics and Applications, Vallado, 4th Ed., Algorithm 56 p475 
        // Take r0, r => a_min, e_min, t_min, v0

        GravityEngine ge = GravityEngine.Instance();

        // If Nbody is available get position directly. If not (target marker) then 
        // use orbit phase to compute position
        
        center3d = ge.GetPositionDoubleV3(_fromOrbit.centralMass);

        if (fromOrbit.nbody != null) {
            r1 = ge.GetPositionDoubleV3(fromOrbit.nbody);
            r1crossv1 = _fromOrbit.GetAxis();
        } else {
            r1 = new Vector3d( toOrbit.GetPhysicsPositionforEllipse(fromOrbit.phase));
            Debug.LogError("Code incomplete need to get v1");
        }

        if (toOrbit.nbody != null) {
            r2 = ge.GetPositionDoubleV3(toOrbit.nbody);
        } else {
            r2 = new Vector3d(toOrbit.GetPhysicsPositionforEllipse(toOrbit.phase));
        }

        r1 = r1 - center3d;
        r2 = r2 - center3d;

        if (fromOrbit.a < toOrbit.a) {
            innerToOuter = true;
        } else {
            innerToOuter = false;
        }
        fromNBody = fromOrbit.nbody;
        mu = ge.GetPhysicsMass(fromOrbit.centralMass);
    }


    public LambertBattin(NBody fromBody, NBody centerNBody, Vector3d r_from, Vector3d r_to, Vector3d fromAxis) : base() {
        name = "LambertBattin";

        fromNBody = fromBody;
        GravityEngine ge = GravityEngine.Instance();
        centerBody = centerNBody;

        center3d = ge.GetPositionDoubleV3(centerNBody);
        mu = ge.GetPhysicsMass(centerNBody);

        r1 = r_from - center3d;
        r1crossv1 = fromAxis;
        r2 = r_to - center3d;

        innerToOuter = true;
    }


    private double Fmod(double a, double b) {
        return a % b;
    }

    /* ----------------------- lambert techniques -------------------- */

    /* utility functions for lambertbattin, etc */
    /* -------------------------------------------------------------------------- */
    // ------------------------------------------------------------------------------
    //                           function lambhodograph
    //
    // this function accomplishes 180 deg transfer(and 360 deg) for lambert problem.
    //
    //  author        : david vallado                  719 - 573 - 2600   22 may 2017
    //
    //  inputs          description                    range / units
    //    r1 - ijk position vector 1          km
    //    r2 - ijk position vector 2          km
    //    dtsec - time between r1 and r2         s
    //    dnu - true anomaly change            rad
    //
    //  outputs       :
    //    v1t - ijk transfer velocity vector   km / s
    //    v2t - ijk transfer velocity vector   km / s
    //
    //  references :
    //    Thompson JGCD 2013 v34 n6 1925
    // Thompson AAS GNC 2018
    // [v1t, v2t] = lambhodograph(r1, v1, r2, p, a, ecc, dnu, dtsec)
    // ------------------------------------------------------------------------------

    private void LambHodograph
            (
            double p,
            double ecc,
            double dnu,
            double dtsec
            ) {
        double eps, magr1, magr2, a, b, x1, x2, y2a, y2b, ptx;
        Vector3d rcrv, rcrr, nvec;

        eps = 1.0e-8;  // -14

        magr1 = r1.magnitude;
        magr2 = r2.magnitude;

        a = mu * (1.0 / magr1 - 1.0 / p);  // not the semi - major axis
        b = Mathd.Pow(mu * ecc / p, 2) - a * a;
        if (b <= 0.0)
            x1 = 0.0;
        else
            x1 = -Mathd.Sqrt(b);

        // 180 deg, and multiple 180 deg transfers
        if (Mathd.Abs(Mathd.Sin(dnu)) < eps) {
            nvec = r1crossv1;
            if (ecc < 1.0) {
                ptx = twopi * Mathd.Sqrt(p * p * p / Mathd.Pow(mu * (1.0 - ecc * ecc), 3));
                if (Fmod(dtsec, ptx) > ptx * 0.5)
                    x1 = -x1;
            }
        } else {
            y2a = mu / p - x1 * Mathd.Sin(dnu) + a * Mathd.Cos(dnu);
            y2b = mu / p + x1 * Mathd.Sin(dnu) + a * Mathd.Cos(dnu);
            if (Mathd.Abs(mu / magr2 - y2b) < Mathd.Abs(mu / magr2 - y2a))
                x1 = -x1;

            // depending on the cross product, this will be normal or in plane,
            // or could even be a fan
            rcrr = Vector3d.Cross(r1, r2);
            nvec = rcrr.normalized; // if this is r1, v1, the transfer is coplanar!
            if (Fmod(dnu, twopi) > pi) {
                nvec = -1.0 * nvec;
            }
        }

        rcrv = Vector3d.Cross(nvec, r1);
        rcrr = Vector3d.Cross(nvec, r2);
        x2 = x1 * Mathd.Cos(dnu) + a * Mathd.Sin(dnu);
        v1t = (Mathd.Sqrt(mu * p) / magr1) * ((x1 / mu) * r1 + rcrv) / magr1;
        v2t = (Mathd.Sqrt(mu * p) / magr2) * ((x2 / mu) * r2 + rcrr) / magr2;
    }  // lambhodograph


    private static double KBattin
    (
    double v
    ) {
        double[] d = new double[21]
        {
        1.0 / 3.0, 4.0 / 27.0,
            8.0 / 27.0, 2.0 / 9.0,
            22.0 / 81.0, 208.0 / 891.0,
            340.0 / 1287.0, 418.0 / 1755.0,
            598.0 / 2295.0, 700.0 / 2907.0,
            928.0 / 3591.0, 1054.0 / 4347.0,
            1330.0 / 5175.0, 1480.0 / 6075.0,
            1804.0 / 7047.0, 1978.0 / 8091.0,
            2350.0 / 9207.0, 2548.0 / 10395.0,
            2968.0 / 11655.0, 3190.0 / 12987.0,
            3658.0 / 14391.0
        };
        double del, delold, term, termold, sum1;
        int i;

        /* ---- process forwards ---- */
        sum1 = d[0];
        delold = 1.0;
        termold = d[0];
        i = 1;
        while ((i <= 20) && (Mathd.Abs(termold) > 0.00000001)) {
            del = 1.0 / (1.0 - d[i] * v * delold);
            term = termold * (del - 1.0);
            sum1 = sum1 + term;
            i++;
            delold = del;
            termold = term;
        }
        //return sum1;

        int ktr = 20;
        double sum2 = 0.0;
        double term2 = 1.0 + d[ktr] * v;
        for (i = 1; i <= ktr - 1; i++) {
            sum2 = d[ktr - i] * v / term2;
            term2 = 1.0 + sum2;
        }

        return (d[0] / term2);
    }  // double kbattin


    /* -------------------------------------------------------------------------- */

    static double SeeBattin(double v2) {
        double[] c = new double[21]

            {
            0.2,
            9.0 / 35.0, 16.0 / 63.0,
            25.0 / 99.0, 36.0 / 143.0,
            49.0 / 195.0, 64.0 / 255.0,
            81.0 / 323.0, 100.0 / 399.0,
            121.0 / 483.0, 144.0 / 575.0,
            169.0 / 675.0, 196.0 / 783.0,
            225.0 / 899.0, 256.0 / 1023.0,
            289.0 / 1155.0, 324.0 / 1295.0,
            361.0 / 1443.0, 400.0 / 1599.0,
            441.0 / 1763.0, 484.0 / 1935.0

            };
        // first term is diff, indices are offset too
        double[] c1 = new double[20]

        {
            9.0 / 7.0, 16.0 / 63.0,
            25.0 / 99.0, 36.0 / 143.0,
            49.0 / 195.0, 64.0 / 255.0,
            81.0 / 323.0, 100.0 / 399.0,
            121.0 / 483.0, 144.0 / 575.0,
            169.0 / 675.0, 196.0 / 783.0,
            225.0 / 899.0, 256.0 / 1023.0,
            289.0 / 1155.0, 324.0 / 1295.0,
            361.0 / 1443.0, 400.0 / 1599.0,
            441.0 / 1763.0, 484.0 / 1935.0

        };

        double term, termold, del, delold, sum1, eta, sqrtopv;
        int i;

        sqrtopv = Mathd.Sqrt(1.0 + v2);
        eta = v2 / Mathd.Pow(1.0 + sqrtopv, 2);

        /* ---- process forwards ---- */
        delold = 1.0;
        termold = c[0];  // * eta
        sum1 = termold;
        i = 1;
        while ((i <= 20) && (Mathd.Abs(termold) > 0.000001)) {
            del = 1.0 / (1.0 + c[i] * eta * delold);
            term = termold * (del - 1.0);
            sum1 = sum1 + term;
            i++;
            delold = del;
            termold = term;
        }

        //   return ((1.0 / (8.0 * (1.0 + sqrtopv))) * (3.0 + sum1 / (1.0 + eta * sum1)));
        // double seebatt = 1.0 / ((1.0 / (8.0 * (1.0 + sqrtopv))) * (3.0 + sum1 / (1.0 + eta * sum1)));

        int ktr = 19;
        double sum2 = 0.0;
        double term2 = 1.0 + c1[ktr] * eta;
        for (i = 0; i <= ktr - 1; i++) {
            sum2 = c1[ktr - i] * eta / term2;
            term2 = 1.0 + sum2;
        }

        return (8.0 * (1.0 + sqrtopv) /
            (3.0 +
            (1.0 /
            (5.0 + eta + ((9.0 / 7.0) * eta / term2)))));
    }  // double seebattin


    /*------------------------------------------------------------------------------
   *
   *                           procedure lamberbattin
   *
   *  this procedure solves lambert's problem using battins method. the method is
   *    developed in battin (1987).
   *
   *  author        : david vallado                  719-573-2600   22 jun 2002
   *
   *  inputs          description                    range / units
   *    r1          - ijk position vector 1          km
   *    r2           - ijk position vector 2          km
   *   dm          - direction of motion            'l','s'
   *    dtsec        - time between r1 and r2         sec
   *
   *  outputs       :
   *    v1          - ijk velocity vector            er / tu
   *    v2           - ijk velocity vector            er / tu
   *    error       - error flag                     1, 2, 3, ... use numbers since c++ is so horrible at strings
   *        error = 1;   // a = 0.0
   *
   *  locals        :
   *    i           - index
   *    loops       -
   *    u           -
   *    b           -
   *    sinv        -
   *    cosv        -
   *    rp          -
   *    x           -
   *    xn          -
   *    y           -
   *    l           -
   *    m           -
   *    cosdeltanu  -
   *    sindeltanu  -
   *    dnu         -
   *    a           -
   *    tan2w       -
   *    ror         -
   *    h1          -
   *    h2          -
   *    tempx       -
   *    eps         -
   *    denom       -
   *    chord       -
   *    k2          -
   *    s           -
   *
   *  coupling      :
   *    arcsin      - arc sine function
   *    arccos      - arc cosine function
   *    astMath::mag         - astMath::magnitude of a vector
   *    arcsinh     - inverse hyperbolic sine
   *    arccosh     - inverse hyperbolic cosine
   *    sinh        - hyperbolic sine
   *    power       - raise a base to a power
   *    atan2       - arc tangent function that resolves quadrants
   *
   *  references    :
   *    vallado       2013, 494, Alg 59, ex 7-5
   -----------------------------------------------------------------------------*/

    //! Initial velocity for maneuver
    private Vector3d v1t;

    //! velocity when final point is reached
    private Vector3d v2t;

    public Vector3d GetTransferVelocity() { 
        return v1t;
    }

    public Vector3d GetFinalVelocity() {
        return v2t;
    }

    public int ComputeXfer(
            // r1, r2, v1 set by constructor
            bool reverse, // was dm 'l' or 's'
            bool df,      // 'r' for retro case "alt approach for high energy(long way, retro multi - rev) case"
            int nrev, 
            double dtsec
            ) 
    {
        const double small = 0.000001;
        Vector3d rcrossr;
        int loops;
        double u, b, x, xn, y, L, m, cosdeltanu, sindeltanu, dnu, a,
            ror, h1, h2, tempx, eps, denom, chord, k2, s,
            p, ecc, f, A;
        double magr1, magr2, magrcrossr, lam, temp, temp1, temp2;

        y = 0;

        int error = 0; // PM - added an error code for loop not converging. Seems Vallado did not implement any?
        magr1 = r1.magnitude;
        magr2 = r2.magnitude;

        cosdeltanu = Vector3d.Dot(r1, r2) / (magr1 * magr2);
        // make sure it's not more than 1.0
        if (Mathd.Abs(cosdeltanu) > 1.0)
            cosdeltanu = 1.0 * Mathd.Sign(cosdeltanu);

        rcrossr = Vector3d.Cross(r1, r2);
        magrcrossr = rcrossr.magnitude;
        if (!reverse)
            sindeltanu = magrcrossr / (magr1 * magr2);
        else
            sindeltanu = -magrcrossr / (magr1 * magr2);

        dnu = Mathd.Atan2(sindeltanu, cosdeltanu);
        // the angle needs to be positive to work for the long way
        if (dnu < 0.0)
            dnu = 2.0 * pi + dnu;

        // these are the same
        //chord = Mathd.Sqrt(magr1 * magr1 + magr2 * magr2 - 2.0 * magr1 * magr2 * cosdeltanu);
        chord = (r2 - r1).magnitude;

        s = (magr1 + magr2 + chord) * 0.5;
        ror = magr2 / magr1;
        eps = ror - 1.0;

        lam = 1.0 / s * Mathd.Sqrt(magr1 * magr2) * Mathd.Cos(dnu * 0.5);
        L = Mathd.Pow((1.0 - lam) / (1.0 + lam), 2);
        m = 8.0 * mu * dtsec * dtsec / (s * s * s * Mathd.Pow(1.0 + lam, 6));

        // initial guess
        if (nrev > 0)
            xn = 1.0 + 4.0 * L;
        else
            xn = L;   //l    // 0.0 for par and hyp, l for ell

        // alt approach for high energy(long way, retro multi - rev) case
        if (df  && (nrev > 0)) {
            xn = 1e-20;  // be sure to reset this here!!
            x = 10.0;  // starting value
            loops = 1;
            while ((Mathd.Abs(xn - x) >= small) && (loops <= 20)) {
                x = xn;
                temp = 1.0 / (2.0 * (L - x * x));
                temp1 = Mathd.Sqrt(x);
                temp2 = (nrev * pi * 0.5 + Mathd.Atan(temp1)) / temp1;
                h1 = temp * (L + x) * (1.0 + 2.0 * x + L);
                h2 = temp * m * temp1 * ((L - x * x) * temp2 - (L + x));

                b = 0.25 * 27.0 * h2 / (Mathd.Pow(temp1 * (1.0 + h1), 3));
                if (b < -1.0) // reset the initial condition
                    f = 2.0 * Mathd.Cos(1.0 / 3.0 * Mathd.Acos(Mathd.Sqrt(b + 1.0)));
                else {
                    A = Mathd.Pow(Mathd.Sqrt(b) + Mathd.Sqrt(b + 1.0), (1.0 / 3.0));
                    f = A + 1.0 / A;
                }

                y = 2.0 / 3.0 * temp1 * (1.0 + h1) * (Mathd.Sqrt(b + 1.0) / f + 1.0);
                xn = 0.5 * ((m / (y * y) - (1.0 + L)) - Mathd.Sqrt(Mathd.Pow(m / (y * y) - (1.0 + L), 2) - 4.0 * L));
                // fprintf(outfile, " %3i yh %11.6f x %11.6f h1 %11.6f h2 %11.6f b %11.6f f %11.7f \n", loops, y, x, h1, h2, b, f);
                loops = loops + 1;
            }  // while
            x = xn;
            a = s * Mathd.Pow(1.0 + lam, 2) * (1.0 + x) * (L + x) / (8.0 * x);
            p = (2.0 * magr1 * magr2 * (1.0 + x) * Mathd.Pow(Mathd.Sin(dnu * 0.5), 2)) / (s * Mathd.Pow(1.0 + lam, 2) * (L + x));  // thompson
            ecc = Mathd.Sqrt(1.0 - p / a);
            LambHodograph(p, ecc, dnu, dtsec);
            // fprintf(outfile, "high v1t %16.8f %16.8f %16.8f %16.8f\n", v1t, astMath::mag(v1t));
        } else {
            // standard processing
            // note that the dr nrev = 0 case is not represented
            loops = 1;
            x = 10.0;  // starting value
            while ((Mathd.Abs(xn - x) >= small) && (loops <= 30)) {
                if (nrev > 0) {
                    x = xn;
                    temp = 1.0 / ((1.0 + 2.0 * x + L) * (4.0 * x));
                    temp1 = (nrev * pi * 0.5 + Mathd.Atan(Mathd.Sqrt(x))) / Mathd.Sqrt(x);
                    h1 = temp * Mathd.Pow(L + x, 2) * (3.0 * Mathd.Pow(1.0 + x, 2) * temp1 - (3.0 + 5.0 * x));
                    h2 = temp * m * ((x * x - x * (1.0 + L) - 3.0 * L) * temp1 + (3.0 * L + x));
                } else {
                    x = xn;
                    tempx = SeeBattin(x);
                    denom = 1.0 / ((1.0 + 2.0 * x + L) * (4.0 * x + tempx * (3.0 + x)));
                    h1 = Mathd.Pow(L + x, 2) * (1.0 + 3.0 * x + tempx) * denom;
                    h2 = m * (x - L + tempx) * denom;
                }

                // ---------------------- - evaluate cubic------------------
                b = 0.25 * 27.0 * h2 / (Mathd.Pow(1.0 + h1, 3));

                u = 0.5 * b / (1.0 + Mathd.Sqrt(1.0 + b));
                k2 = KBattin(u);
                y = ((1.0 + h1) / 3.0) * (2.0 + Mathd.Sqrt(1.0 + b) / (1.0 + 2.0 * u * k2 * k2));
                xn = Mathd.Sqrt(Mathd.Pow((1.0 - L) * 0.5, 2) + m / (y * y)) - (1.0 + L) * 0.5;

                loops = loops + 1;
            }  // while

        }

        if (loops < 30) {
            // blair approach use y from solution
            //       lam = 1.0 / s * sqrt(magr1*magr2) * cos(dnu*0.5);
            //       m = 8.0*mu*dtsec*dtsec / (s ^ 3 * (1.0 + lam) ^ 6);
            //       L = ((1.0 - lam) / (1.0 + lam)) ^ 2;
            //a = s*(1.0 + lam) ^ 2 * (1.0 + x)*(lam + x) / (8.0*x);
            // p = (2.0*magr1*magr2*(1.0 + x)*sin(dnu*0.5) ^ 2) ^ 2 / (s*(1 + lam) ^ 2 * (lam + x));  % loechler, not right ?
            p = (2.0 * magr1 * magr2 * y * y * Mathd.Pow(1.0 + x, 2) * Mathd.Pow(Mathd.Sin(dnu * 0.5), 2)) / 
                    (m * s * Mathd.Pow(1.0 + lam, 2));  // thompson
            ecc = Mathd.Sqrt((eps * eps + 4.0 * magr2 / magr1 * Mathd.Pow(Mathd.Sin(dnu * 0.5), 2) * 
                    Mathd.Pow((L - x) / (L + x), 2)) / (eps * eps + 4.0 * magr2 / magr1 * Mathd.Pow(Mathd.Sin(dnu * 0.5), 2)));
            LambHodograph(p, ecc, dnu, dtsec);

            // Battin solution to orbital parameters(and velocities)
            // thompson 2011, loechler 1988
            if (dnu > pi)
                lam = -Mathd.Sqrt((s - chord) / s);
            else
                lam = Mathd.Sqrt((s - chord) / s);

            // loechler pg 21 seems correct!
            Vector3d v1dvl = 1.0 / (lam * (1.0 + lam)) * Mathd.Sqrt(mu * (1.0 + x) / (2.0 * s * s * s * (L + x))) * ((r2 - r1) 
                                + s * Mathd.Pow(1.0 + lam, 2) * (L + x) / (magr1 * (1.0 + x)) * r1);
            // added v2
            Vector3d v2dvl = 1.0 / (lam * (1.0 + lam)) * Mathd.Sqrt(mu * (1.0 + x) / (2.0 * s * s * s * (L + x))) * ((r2 - r1) 
                                - s * Mathd.Pow(1.0 + lam, 2) * (L + x) / (magr2 * (1.0 + x)) * r2);

            // Seems these are the answer. Not at all sure what the point of the Hodograph calls was...
            // but for FRGeneric the Hodograph answer seems ok. In that case lam = 0 and v1vdl is NaN. Wha??
            // Add code to use hodograph if lam=0
            //Debug.LogFormat("lam={0}", lam);
            if (Mathd.Abs(lam) > 1E-8) {
                v1t = v1dvl;
                v2t = v2dvl;
            }

            //fprintf(1, 'loe v1t %16.8f %16.8f %16.8f %16.8f\n', v1dvl, mag(v1dvl));
            //fprintf(1, 'loe v2t %16.8f %16.8f %16.8f %16.8f\n', v2dvl, mag(v2dvl));
            // Debug.LogFormat("v1dvl={0} v2dvl={1}", v1dvl, v2dvl);
        } else {
            error = 2;
        }


        // Determine maneuvers needed for ship (fromOrbit)
        if (error == 0) {
            maneuvers.Clear();
            // Departure
            Maneuver departure = new Maneuver();
            departure.mtype = Maneuver.Mtype.vector;
            departure.nbody = fromNBody;
            departure.physPosition = r1 + center3d;
            if (innerToOuter) {
                departure.velChange = v1t.ToVector3() - GravityEngine.Instance().GetVelocity(fromNBody);
            } else {
                // Need to establish arrival velocity
                departure.velChange = v2t.ToVector3() - GravityEngine.Instance().GetVelocity(fromNBody);
            }
            departure.worldTime = (float)GravityEngine.Instance().GetGETime();
            maneuvers.Add(departure);
            deltaV = departure.velChange.magnitude;

            // Arrival (will not be required if intercept)
            if (toOrbit != null) {
                Maneuver arrival = new Maneuver();
                arrival.nbody = fromNBody;
                arrival.physPosition = r2 + center3d;
                arrival.worldTime = departure.worldTime + (float)dtsec;
                arrival.mtype = Maneuver.Mtype.vector;
                arrival.velChange = toOrbit.GetPhysicsVelocityForEllipse(toOrbit.phase) - v2t.ToVector3();
                maneuvers.Add(arrival);
                deltaV += arrival.velChange.magnitude;
            }
        }

        return error;
    }  // lambertbattin



}
