using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatchedConicXfer : OrbitTransfer
{

    private const double TWO_PI = 2f * System.Math.PI;

    public const double LAMBDA1_DEFAULT = 75f;

    private double t_flight;

    /// <summary>
    /// Calculate the transfer maneuver from a circular initial orbit to the sphere of influence (SOI) of a smaller mass
    /// orbiting the same body as the spaceship (e.g. Earth to Moon transfer). 
    /// 
    /// Assumes the moon orbit is spherical and co-planar with the spaceship initial orbit. 
    /// 
    /// Patched conic is an approximation that assumes the ship moves in only the field of the central body until
    /// it gets to the SOI. In reality (and GE evolution) there will be an influence from the Moon and the actual result
    /// will differ slightly. 
    /// 
    /// </summary>
    /// <param name="fromOrbit"></param>
    /// <param name="toOrbit"></param>
    /// <param name="lambda1">Angle of arrival wrt planet-moon line (0..90 degrees)</param>
    public PatchedConicXfer(OrbitData fromOrbit, OrbitData toOrbit, double lambda1Deg) : base(fromOrbit, toOrbit) {
        name = "PatchedConicXfer";

        // Patched conic xfer is via an ellipse from one circle to another. The ellipse is uniquely
        // defined by the radius of from and to. 
        // Equations from Chobotov Ch 5.4
        double r_inner = 0f;
        double r_outer = 0f;
        if (fromOrbit.a < toOrbit.a) {
            r_inner = fromOrbit.a;
            r_outer = toOrbit.a;
        } else {
            r_inner = toOrbit.a;
            r_outer = fromOrbit.a;
        }
        // Start with assumption of xfer from inner more massive to outer
        // patched conic follows 7.4 in Fundamentals of Astrodynamics (Bate/Mueller/White) 1971
        // Algorithm takes as input (r0, v0, phi0, lambda1)
        // and determines r1, v1, delta0, delta1
        // See Fig 7.4-1 p336
        double D = r_outer - r_inner;

        // radius of sphere of influence
        double Rs = D * System.Math.Pow(toOrbit.nbody.mass / toOrbit.centralMass.mass, 0.4f);
        double lambda1 = Mathf.Deg2Rad * lambda1Deg; 

        // 1. r0 is given (current circular orbit). Find a v0 that is energetic enough to cross the moons orbit. 
        // (Can use a non-rdzv Hohmann)
        HohmannXfer hohmann = new HohmannXfer(fromOrbit, toOrbit, false);
        maneuvers = hohmann.GetManeuvers();

        double r0 = r_inner;
        double v0 = System.Math.Sqrt(fromOrbit.mu / r0) + maneuvers[0].dV;

        // assume we thrust along orbit: phi0 = 0
        double E = v0 * v0 / 2f - fromOrbit.mu / r_inner;
        double h = v0 * r0; // cos(phi0) = 1
        double r1 = System.Math.Sqrt(D * D + Rs * Rs - 2f * D * Rs * System.Math.Cos(lambda1));

        // xfer orbit details
        double p = h * h / fromOrbit.mu;
        double a = -0.5f * fromOrbit.mu / E;
        double ecc = System.Math.Sqrt(1 - p / a);

        double cos_nu0 = System.Math.Min((p - r0) / (r0 * ecc), 1f);
        double cos_nu1 = System.Math.Min((p - r1) / (r1 * ecc), 1f);

        // eccentric anomolies
        double cos_E0 = (ecc + cos_nu0) / (1 + ecc * cos_nu0);
        double E0 = System.Math.Acos(cos_E0);  // quadrant issues?
        double cos_E1 = (ecc + cos_nu1) / (1 + ecc * cos_nu1);
        double E1 = System.Math.Acos(cos_E1);  // quadrant issues ?
        //Debug.LogFormat("PatchedConic ({0}): p={1} a={2} ecc={3} cos_nu0={4} cos_nu1={5} E0={6} E1={7}", 
        //    lambda1Deg, p, a, ecc, cos_nu0, cos_nu1, E0, E1);

        // time of flight
        t_flight = System.Math.Sqrt(a * a * a / fromOrbit.mu) * (E1 - ecc * System.Math.Sin(E1)) - (E0 - ecc * System.Math.Sin(E0));

        // delta0 is phase angle at departure
        double nu0 = System.Math.Acos(cos_nu0);
        double nu1 = System.Math.Acos(cos_nu1);
        double gamma1 = System.Math.Asin(Rs / r1 * System.Math.Sin(lambda1)); // quadrant?
        double w_m = 2f * System.Math.PI / toOrbit.period;
        double gamma0 = nu1 - nu0 - gamma1 - w_m * t_flight;
        // Debug.LogFormat("PatchedConic: nu0={0} nu1={1} g0(deg)={2} g1(deg)={3}", nu0, nu1, System.Math.Rad2Deg*gamma0, System.Math.Rad2Deg*gamma1);

        // gamma0 is the phase delta initial burn needs to have wrt to the phase of body 2
        // find current angular seperation
        double phase_gap = (toOrbit.phase + toOrbit.omega_lc) - (fromOrbit.phase + fromOrbit.omega_lc);
        if (phase_gap < 0)
            phase_gap += 360f;
        // need seperation to be delta0 
        double dTheta = Mathf.Deg2Rad * phase_gap - gamma0;
        if (dTheta < 0) {
            dTheta += TWO_PI;
        }
        // need to wait for phase_gap to reduce to this value. It reduces at a speed based on the difference 
        // in the angular velocities. 
        double dOmega = TWO_PI / fromOrbit.period - TWO_PI / toOrbit.period;
        double tWait = dTheta / dOmega;

        // Debug.LogFormat("PatchedConic: r1={0} E={1} t_flight={2} delta0={3} tWait={4}", r1, E, t_flight, delta0, tWait);
        // adjust time of the first Hohmann burn
        maneuvers[0].worldTime += (float) tWait;
        // remove the second maneuver to allow PatchedConicSOI to trigger when it detects SOI
        maneuvers.RemoveAt(1);
    }

    public double GetTimeOfFlight() {
        return t_flight;
    }

    public PatchedConicXfer CreateTransferCopy(double lambda1Deg) {

        PatchedConicXfer newXfer = new PatchedConicXfer(this.fromOrbit, this.toOrbit, lambda1Deg);
        return newXfer;
    }


    public override string ToString() {
        return name;
    }
}
