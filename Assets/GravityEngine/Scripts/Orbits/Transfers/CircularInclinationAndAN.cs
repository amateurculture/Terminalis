using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Determine the maneuver to change a circular orbit to a new circular orbit of the same radius with 
/// a different inclination and ascending node. The orbits will interesect at two common points and
/// the maneuver will use the current phase of the fromOrbit to select the closest common point. 
/// 
/// The code follows Vallado Algorithm 41 p348.
/// 
/// </summary>
public class CircularInclinationAndAN : OrbitTransfer {

    public CircularInclinationAndAN(OrbitData fromOrbit, OrbitData toOrbit) : base(fromOrbit, toOrbit) {

        name = "Circular Change Inclination and Ascending Node";

        // check the orbits are circular and have the same radius
        if (fromOrbit.ecc > GEConst.small) {
            Debug.LogWarning("fromOrbit is not circular. ecc=" + fromOrbit.ecc);
            return;
        }
        if (toOrbit.ecc > GEConst.small) {
            Debug.LogWarning("toOrbit is not circular. ecc=" + toOrbit.ecc);
            return;
        }
        if (Mathf.Abs(fromOrbit.a - toOrbit.a) > GEConst.small) {
            Debug.LogWarning("Orbits do not have the same radius delta=" + Mathf.Abs(fromOrbit.a - toOrbit.a));
            return;
        }

        double dOmega = (toOrbit.omega_uc - fromOrbit.omega_uc) * Mathd.Deg2Rad;
        double i_initial = fromOrbit.inclination * Mathd.Deg2Rad;
        double i_final = toOrbit.inclination * Mathd.Deg2Rad;

        // u_initial = omega_lc + nu (i.e. phase of circular orbit)
        // eqn (6-25)
        double cos_theta = Mathd.Cos(i_initial) * Mathd.Cos(i_final) +
                        Mathd.Sin(i_initial) * Mathd.Sin(i_final) * Mathd.Cos(dOmega);
        // Quadrant check
        double theta = Mathd.Acos(Mathd.Clamp(cos_theta, -1.0, 1.0));
        if (dOmega < 0) {
            theta = -theta;
        }

        // u_initial: phase of intersection in the initial orbit
        double numer = Mathd.Sin(i_final) * Mathd.Cos(dOmega) - cos_theta * Mathd.Sin(i_initial);
        double denom = Mathd.Sin(theta) * Mathd.Cos(i_initial);
        if (Mathd.Abs(denom) < 1E-6) {
            Debug.LogError("u_initial: about to divide by zero (small theta)");
            // return;
        }
        double u_initial = Mathd.Acos(Mathd.Clamp(numer / denom, -1.0, 1.0));

        // u_final: phase of intersection in the final orbit
        numer = Mathd.Cos(i_initial) * Mathd.Sin(i_final) - Mathd.Sin(i_initial) * Mathd.Cos(i_final) * Mathd.Cos(dOmega);
        if (Mathd.Abs(Mathd.Sin(theta)) < 1E-6) {
            Debug.LogError("u_final: about to divide by zero (small theta)");
            return;
        }
        double u_final = Mathd.Acos(Mathd.Clamp(numer / Mathd.Sin(theta), -1.0, 1.0));

        double u_initialDeg = u_initial * Mathd.Rad2Deg;
        double u_finalDeg = u_final * Mathd.Rad2Deg;

        // Orbits cross at two places, pick the location closest to the current position of the fromOrbit
        double time_to_crossing = fromOrbit.period * (u_initialDeg - fromOrbit.phase) / 360f;
        if (time_to_crossing < 0) {
            u_initialDeg += 180f;
            u_finalDeg += 180f;
            time_to_crossing += 0.5f*fromOrbit.period;
        }

        // Determine velocity change required
        Vector3 dV = toOrbit.GetPhysicsVelocityForEllipse((float)u_finalDeg) - 
                        fromOrbit.GetPhysicsVelocityForEllipse((float)u_initialDeg);

        // Create a maneuver object
        Maneuver m = new Maneuver();
        m.physPosition = new Vector3d(fromOrbit.GetPhysicsPositionforEllipse((float)(u_initialDeg)));
        m.mtype = Maneuver.Mtype.vector;
        m.dV = dV.magnitude;
        m.velChange = dV;
        m.worldTime = GravityEngine.instance.GetPhysicalTime() + (float) time_to_crossing;
        m.nbody = fromOrbit.nbody;
        maneuvers.Add(m);

        //Debug.LogFormat("u_initial = {0} u_final={1} (deg) dOmega={2} (deg) timeToCrossing={3} fromPhase={4} cos_theta={5} theta={6}",
        //    u_initialDeg,
        //    u_finalDeg,
        //    dOmega * Mathd.Rad2Deg, 
        //    time_to_crossing, 
        //    fromOrbit.phase, 
        //    cos_theta, 
        //    theta);
    }


    public override string ToString() {
        return name;
    }
}
