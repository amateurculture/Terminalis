using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HohmannXfer : OrbitTransfer {

    private const float TWO_PI = 2f * Mathf.PI;

    private float transfer_time;

    public HohmannXfer(OrbitData fromOrbit, OrbitData toOrbit, bool rendezvous) : base(fromOrbit, toOrbit) {
        name = "Hohmann";
        if (rendezvous) {
            name += " Rendezvous";
        } else {
            name += " Transfer";
        }

        // Check both objects are orbiting in the same direction
        if (Vector3d.Dot(fromOrbit.GetAxis(), toOrbit.GetAxis()) < 0) {
            Debug.LogWarning("Objects orbiting in different directions. Will not proceed.");
            return;
        }

        // Hohmann xfer is via an ellipse from one circle to another. The ellipse is uniquely
        // defined by the radius of from and to. 
        // Equations from Chobotov Ch 5.4
        float r_inner = 0f;
        float r_outer = 0f;
        // From orbit result is in physics quantities
        if (fromOrbit.a < toOrbit.a) {
            r_inner = fromOrbit.a;
            r_outer = toOrbit.a;
        } else {
            r_inner = toOrbit.a;
            r_outer = fromOrbit.a;
        }

        // (mass scale was applied in Orbit data)
        float v_inner = Mathf.Sqrt(fromOrbit.mu / r_inner);
        float rf_ri = r_outer / r_inner;
        float dV_inner = v_inner * (Mathf.Sqrt(2f * rf_ri / (1f + rf_ri)) - 1f);
        // Debug.LogFormat("dv_iner={0} v_inner={1} r_i={2} r_o={3}", dV_inner, v_inner, r_inner, r_outer);

        float v_outer = Mathf.Sqrt(fromOrbit.mu / r_outer);
        //simplify per Roy (12.22)
        float dV_outer = v_outer * (1f - Mathf.Sqrt(2 / (1 + rf_ri)));

        // Debug.LogFormat("r_in={0} r_out={1}  v_inner={2} v_outer={3}", r_inner, r_outer, v_inner, v_outer);

        // transfer time
        // Need to flip rf_ri for inner orbits to get the correct transfer_time
        // (should re-derive for this case sometime to see why)
        transfer_time = 0f;
        // time to wait for first maneuver (rendezvous case)
        float tWait = 0f;

        // Build the manuevers required
        deltaV = 0f;
        float worldTime = GravityEngine.Instance().GetPhysicalTime();

        Maneuver m1;
        m1 = new Maneuver();
        m1.nbody = fromOrbit.nbody;
        m1.mtype = Maneuver.Mtype.scalar;
        m1.worldTime = worldTime;
        Maneuver m2;
        m2 = new Maneuver();
        m2.nbody = fromOrbit.nbody;
        m2.mtype = Maneuver.Mtype.scalar;
        // If the orbit is almost circular, then can be some omega which will impact rendezvous phasing
        float fromPhase = fromOrbit.phase + fromOrbit.omega_lc;
        float toPhase = toOrbit.phase + toOrbit.omega_lc;


        if (fromOrbit.a < toOrbit.a) {
            // inner to outer
            float subexpr = 1f + rf_ri;
            transfer_time = fromOrbit.period / Mathf.Sqrt(32) * Mathf.Sqrt(subexpr * subexpr * subexpr);
            if (rendezvous) {
                // need to determine wait time for first maneuver to phase the arrival
                // find angle by which outer body must lead (radians)
                // Chobotov 7.3
                float subexpr2 = 0.5f * (1f + r_inner / r_outer);
                float theta_h = Mathf.PI * (1f - Mathf.Sqrt(subexpr2 * subexpr2 * subexpr2));
                // find current angular seperation
                float phase_gap = toPhase - fromPhase;
                if (phase_gap < 0)
                    phase_gap += 360f;
                // need seperation to be theta_h 
                float dTheta = Mathf.Deg2Rad * phase_gap - theta_h;
                if (dTheta < 0) {
                    dTheta += TWO_PI;
                }
                // need to wait for phase_gap to reduce to this value. It reduces at a speed based on the difference 
                // in the angular velocities. 
                float dOmega = TWO_PI / fromOrbit.period - TWO_PI / toOrbit.period;
                tWait = dTheta / dOmega;
                //Debug.LogFormat("inner_phase= {0} out_phase={1} phase_gap(deg)={2} thetaH={3} dTheta(rad)={4} dOmega={5} tWait={6}", 
                //    fromOrbit.phase, toOrbit.phase, phase_gap, theta_h, dTheta, dOmega, tWait);

            }
            // from inner to outer
            // first maneuver is to a higher orbit
            m1.dV = dV_inner;
            deltaV += dV_inner;
            maneuvers.Add(m1);
            // second manuever is opposite to velocity
            m2.dV = dV_outer;
            deltaV += dV_outer;
            maneuvers.Add(m2);
        } else {
            // outer to inner
            float subexpr_in = 1f + r_inner / r_outer;
            transfer_time = fromOrbit.period / Mathf.Sqrt(32) * Mathf.Sqrt(subexpr_in * subexpr_in * subexpr_in);
            if (rendezvous) {
                // Chobotov 7.2/7.3 (modified for outer to inner, use (Pi+Theta) and Pf not Pi
                float subexpr2 = 0.5f * (1f + r_outer / r_inner);
                float theta_h = Mathf.PI * (1f + Mathf.Sqrt(subexpr2 * subexpr2 * subexpr2));
                // find current angular seperation
                float phase_gap = fromPhase - toPhase;
                if (phase_gap < 0)
                    phase_gap += 360f;
                // need seperation to be -theta_h 
                float dTheta = Mathf.Deg2Rad * phase_gap - theta_h;
                // Can need inner body to go around more than once...
                while (dTheta < 0) {
                    dTheta += TWO_PI;
                }
                // larger (inner) omega first 
                float dOmega = TWO_PI / toOrbit.period - TWO_PI / fromOrbit.period;
                tWait = dTheta / dOmega;
                //Debug.LogFormat("inner_phase= {0} out_phase={1} phase_gap(deg)={2} thetaH(deg)={3} dTheta(rad)={4} dOmega={5} tWait={6}",
                //    toOrbit.phase, fromOrbit.phase, phase_gap, Mathf.Rad2Deg*theta_h, dTheta, dOmega, tWait);

            }
            // from outer to inner
            // first maneuver is to a lower orbit
            m1.dV = -dV_outer;
            deltaV += dV_outer;
            maneuvers.Add(m1);
            // second manuever is opposite to velocity
            m2.dV = -dV_inner;
            deltaV += dV_outer;
            maneuvers.Add(m2);
        }
        m1.worldTime = worldTime + tWait;
        m2.worldTime = worldTime + tWait + transfer_time;
        deltaT = tWait + transfer_time;
        // maneuver positions and info for KeplerSeq conversion and velocity directions
        Vector3d h_unit = fromOrbit.GetAxis();
        float phaseAtXfer = fromOrbit.phase + (TWO_PI / fromOrbit.period) * tWait * Mathf.Rad2Deg;
        m1.physPosition = new Vector3d(fromOrbit.GetPhysicsPositionforEllipse(phaseAtXfer));
        m1.relativePos = new Vector3d(fromOrbit.GetPhysicsPositionforEllipseRelative(phaseAtXfer));
        m1.relativeVel = Vector3d.Cross(h_unit, m1.relativePos).normalized;
        m1.relativeTo = fromOrbit.centralMass;

        m2.physPosition = new Vector3d(toOrbit.GetPhysicsPositionforEllipse(phaseAtXfer + 180f));
        m2.relativePos = new Vector3d(toOrbit.GetPhysicsPositionforEllipseRelative(phaseAtXfer + 180f));
        m2.relativeVel = Vector3d.Cross(h_unit, m2.relativePos).normalized;
        m2.relativeTo = fromOrbit.centralMass;

        // Determine the relative velocities
        if (fromOrbit.a < toOrbit.a) {
            // inner to outer
            m1.relativeVel *= v_inner + m1.dV;
            m2.relativeVel *= v_outer;
        } else {
            // outer to inner
            m1.relativeVel *= v_outer + m1.dV;
            m2.relativeVel *= v_inner;
        }
    }

    /// <summary>
    /// Provide the transfer time in GE internal units. 
    /// </summary>
    /// <returns></returns>
    public float GetTransferTime() {
        return transfer_time;
    }

    public override string ToString() {
		return name;
	}

    public HohmannXfer CreateTransferCopy(bool rendezvous) {

        HohmannXfer newXfer = new HohmannXfer(this.fromOrbit, this.toOrbit, rendezvous);
        return newXfer;
    }

    /// <summary>
    /// For the given Hohmann transfer determine the launch windows (e.g. times the from object would
    /// start the transfer). 
    /// 
    /// Times returned are times from the present GE time in GE time units. 
    /// </summary>
    /// <param name="numWindows"></param>
    /// <returns></returns>
    public double[] LaunchTimes(int numWindows) {
        // Following Vallado, Algorithm 45, p363
        double[] times = new double[numWindows];

        double omegaFrom = fromOrbit.GetOmegaAngular();
        double omegaTo = toOrbit.GetOmegaAngular();

        double a_trans = 0.5 * (fromOrbit.a + toOrbit.a);
        // The PI here ties it to a Hohmann xfer. 
        double tau = Mathd.PI * Mathd.Sqrt(a_trans * a_trans * a_trans / fromOrbit.mu);

        // Initial phase difference from target TO interceptor
        double theta_i = (toOrbit.phase - fromOrbit.phase) * Mathd.Deg2Rad;


        if (theta_i < 0) {
            theta_i += 2.0 * Mathd.PI;
        }

        double omegaDelta;
        double alpha_lead;
        double theta;
        if (fromOrbit.a < toOrbit.a) {
            omegaDelta = omegaFrom - omegaTo;
            // phase change of destination while in transit
            alpha_lead = omegaTo * tau;
            theta = alpha_lead - Mathd.PI;
        } else {
            omegaDelta = omegaTo - omegaFrom;
            alpha_lead = omegaFrom * tau;
            theta = alpha_lead - Mathd.PI;
        }

        // find only non-negative transfer times
        int k = 0;
        int window = 0;
        do {
            times[window] = (theta - theta_i + 2.0 * Mathd.PI * k) / omegaDelta;
            if (times[window] > 0) {
                window++;
            }
            k++;
        } while ((window < numWindows) && (k < numWindows*10));

        if (window != numWindows) {
            Debug.LogWarning("Failed to find launch windows");
        }
        return times;
    }

}
