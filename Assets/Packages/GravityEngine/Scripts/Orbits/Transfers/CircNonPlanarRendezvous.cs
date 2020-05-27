using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Calculate a rendezvous from one circular orbit to a different circular orbit for orbits in different
/// inclinations. 
/// 
/// This requires an intermediate transfer orbit to adjust the phasing of the chasing ship in order that
/// it arrives at the common node with the correct phase relationship for rendezvous. This intermediate
/// orbit is "free" in dV (since the velocity will help it get to the target). 
/// 
/// If the orbits are co-planar the HohmannXfer with rendezvous=true is more appropriate. 
/// 
/// This algorithm follows Vallado Algorithm 46 (p368). 
/// 
/// </summary>
public class CircNonPlanarRendezvous : OrbitTransfer
{

    public CircNonPlanarRendezvous(OrbitData fromOrbit, OrbitData toOrbit) : base(fromOrbit, toOrbit) {

        name = "Circular Non-Planar Rendezvous";

        double w_target = toOrbit.GetOmegaAngular();
        double w_int = fromOrbit.GetOmegaAngular();

        bool innerToOuter = (fromOrbit.a < toOrbit.a);

        if (!innerToOuter) {
            Debug.LogError("Fix me: algorithm does not support outer to inner yet!");
        }

        float a_transfer = 0.5f * (fromOrbit.a + toOrbit.a);
        float t_transfer = Mathf.PI * Mathf.Sqrt(a_transfer * a_transfer * a_transfer / fromOrbit.mu);

        Debug.LogFormat("int: a={0} w={1}  target: a={2} w={3}", fromOrbit.a, w_int, toOrbit.a, w_target);

        // lead angle required by target
        double alpha_L = w_target * t_transfer;

        // find the phase of the nearest node in the interceptor orbit 
        // (C&P from CircularInclinationAndAN, messy to extract)
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
            return;
        }
        double u_initial = Mathd.Acos(Mathd.Clamp(numer / denom, -1.0, 1.0));

        // u_final: phase of intersection in the final orbit
        numer = Mathd.Cos(i_initial) * Mathd.Sin(i_final) - Mathd.Sin(i_initial) * Mathd.Cos(i_final) * Mathd.Cos(dOmega);
        if (Mathd.Abs(Mathd.Sin(theta)) < 1E-6) {
            Debug.LogError("u_final: about to divide by zero (small theta)");
            return;
        }
        double u_final = Mathd.Acos(Mathd.Clamp(numer / Mathd.Sin(theta), -1.0, 1.0));

        // how far is interceptor from a node? 
        double delta_theta_int = u_initial - fromOrbit.phase * Mathd.Deg2Rad;
        if (delta_theta_int < -Mathd.PI) {
            delta_theta_int += 2.0 * Mathd.PI;
            u_initial += 2.0 * Mathd.PI;
            u_final += 2.00 * Mathd.PI;
        } else if (delta_theta_int < 0) {
            delta_theta_int += Mathd.PI;
            u_initial += Mathd.PI;
            u_final += Mathd.PI;
        }
        double deltat_node = delta_theta_int / w_int;

        Debug.LogFormat("Node at: {0} (deg), distance to node (deg)={1}", u_initial * Mathd.Rad2Deg, delta_theta_int * Mathd.Rad2Deg);

        // Algorithm uses lambda_true. See the definition in Vallada (2-92). defined for circular equitorial
        // orbits as angle from I-axis (x-axis) to the satellite.
        // This is not the same as u = argument of latitude, which is measured from the ascending node. 

        // Target moves to lambda_tgt1 as interceptor moves to node
        double lambda_tgt1 = toOrbit.phase * Mathd.Deg2Rad + w_target * deltat_node;
        // phase lag from interceptor to target (target is 1/2 revolution from u_initial)
        // Vallado uses the fact that node is at omega, which assumes destination orbit is equitorial?
        double lambda_int = u_final + Mathd.PI; // Mathd.PI + fromOrbit.omega_uc;
        double theta_new = lambda_int - lambda_tgt1;
        if (theta_new < 0)
            theta_new += 2.0 * Mathd.PI;
        // This is not working. Why??
        // double alpha_new = Mathd.PI + theta_new;
        // Keep in 0..2 Pi (my addition)
        double alpha_new =  theta_new % (2.0 * Mathd.PI);

        Debug.LogFormat("lambda_tgt1={0} theta_new={1} toOrbit.phase={2} t_node={3} w_target={4}", 
            lambda_tgt1 * Mathd.Rad2Deg, theta_new * Mathd.Rad2Deg, toOrbit.phase, deltat_node, w_target);

        // k_target: number of revolutions in transfer orbit. Provided as input
        // k_int: number of revs in phasing orbit. Want to ensure a_phase < a_target to not
        //        waste deltaV.
        double mu = fromOrbit.mu;
        double k_target = 0.0;
        double two_pi_k_target = k_target * 2.0 * Mathd.PI;
        double P_phase = (alpha_new - alpha_L + two_pi_k_target) / w_target;
        while (P_phase < 0) {
            Debug.Log("Pphase < 0. Bumping k_target");
            k_target += 1.0;
            two_pi_k_target = k_target * 2.0 * Mathd.PI;
            P_phase = (alpha_new - alpha_L + two_pi_k_target) / w_target;
        }
        double k_int = 1.0;
        double two_pi_k_int = k_int * 2.0 * Mathd.PI;
        double a_phase = Mathd.Pow(mu * (P_phase * P_phase / (two_pi_k_int * two_pi_k_int)), 1.0 / 3.0);
        Debug.LogFormat("alpha_new={0} alpha_L={1} Pphase={2}", alpha_new * Mathd.Rad2Deg, alpha_L*Mathd.Rad2Deg, P_phase);

        // if need a long time to phase do multiple phasing orbits
        int loopCnt = 0; 
        while (innerToOuter && (a_phase > toOrbit.a)) {
            Debug.Log("a_phase > toOrbit - add a lap");
            k_int += 1.0;
            two_pi_k_int = k_int * 2.0 * Mathd.PI;
            a_phase = Mathd.Pow(mu * (P_phase * P_phase / (two_pi_k_int * two_pi_k_int)), 1.0 / 3.0);
            if (loopCnt++ > 10)
                break;
        }

        double t_phase = 2.0 * Mathd.PI * Mathd.Sqrt(a_phase * a_phase * a_phase / fromOrbit.mu);

        // Book has Abs(). Why? Need to remove this otherwise some phase differences do not work
        // Only take Cos of delta_incl (no sign issues)

        double deltaV_phase, deltaV_trans1, deltaV_trans2;

        double delta_incl = (toOrbit.inclination - fromOrbit.inclination) * Mathd.Deg2Rad;
        if (innerToOuter) {
            deltaV_phase = Mathd.Sqrt(2.0 * mu / fromOrbit.a - mu / a_phase)
                                                - Mathd.Sqrt(mu / fromOrbit.a);
            deltaV_trans1 = Mathd.Sqrt(2.0 * mu / fromOrbit.a - mu / a_transfer)
                                                - Mathd.Sqrt(2.0 * mu / fromOrbit.a - mu / a_phase);
            deltaV_trans2 = Mathd.Sqrt(2.0 * mu / toOrbit.a - mu / a_transfer
                                              + mu / toOrbit.a
                                              - 2.0 * Mathd.Sqrt(2.0 * mu / toOrbit.a - mu / a_transfer)
                                                    * Mathd.Sqrt(mu / toOrbit.a) * Mathd.Cos(delta_incl));
        }  else {
            // FIX ME!! Get NaN, so need to review from first principles. 
            deltaV_phase = Mathd.Sqrt(mu / a_phase  - 2.0 * mu / fromOrbit.a)
                                                - Mathd.Sqrt(mu / fromOrbit.a);
            deltaV_trans1 = Mathd.Sqrt(mu / a_transfer - 2.0 * mu / fromOrbit.a)
                                                - Mathd.Sqrt(2.0 * mu / fromOrbit.a - mu / a_phase);
            deltaV_trans2 = Mathd.Sqrt(mu / a_transfer - 2.0 * mu / toOrbit.a
                                              + mu / toOrbit.a
                                              - 2.0 * Mathd.Sqrt(mu / a_transfer - 2.0 * mu / toOrbit.a)
                                                    * Mathd.Sqrt(mu / toOrbit.a) * Mathd.Cos(delta_incl));
        }

        Debug.LogFormat("T1: a_int={0} a_phase={1} a_tgt={2} dt={3} dV_phase={4} dv1={5} dv2={6}", 
            fromOrbit.a, a_phase, toOrbit.a,
            deltat_node, deltaV_phase, deltaV_trans1, deltaV_trans2);

        // phasing burn: in same plane as the orbit at the node, use scalar maneuver
        double time_start = GravityEngine.Instance().GetPhysicalTimeDouble();
        Maneuver m_phase = new Maneuver();
        m_phase.mtype = Maneuver.Mtype.scalar;
        m_phase.dV = (float)deltaV_phase;
        m_phase.worldTime = (float)(time_start + deltat_node);
        m_phase.nbody = fromOrbit.nbody;
        m_phase.physPosition = new Vector3d(fromOrbit.GetPhysicsPositionforEllipse((float)(u_initial * Mathd.Rad2Deg)));
        maneuvers.Add(m_phase);

        // transfer burn - stay in initial orbit plane
        Maneuver m_trans1 = new Maneuver();
        m_trans1.mtype = Maneuver.Mtype.scalar;
        m_trans1.dV = (float) deltaV_trans1;
        m_trans1.worldTime = (float)(time_start + deltat_node + t_phase);
        m_trans1.nbody = fromOrbit.nbody;
        m_trans1.physPosition = m_phase.physPosition;
        maneuvers.Add(m_trans1);

        // Arrival burn - do plane change here (just assign the correct velocity)
        // TODO: Need to reverse this when from outer to inner...do plane change at start
        float finalPhase = (float)(u_final * Mathd.Rad2Deg + 180f);
        Vector3 finalV = toOrbit.GetPhysicsVelocityForEllipse(finalPhase);
        Vector3 finalPos = toOrbit.GetPhysicsPositionforEllipse(finalPhase);
        Maneuver m_trans2 = new Maneuver();
        m_trans2.mtype = Maneuver.Mtype.setv;
        m_trans2.dV = (float) deltaV_trans2;
        m_trans2.velChange = finalV;
        m_trans2.worldTime = (float)(time_start + deltat_node + t_phase + t_transfer);
        m_trans2.nbody = fromOrbit.nbody;
        m_trans2.physPosition = new Vector3d(finalPos);
        maneuvers.Add(m_trans2);
    }

}
