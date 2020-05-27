using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hohmann transfer or rendezvous between any two circular orbits. Handles diffences in radius, inclination and 
/// RAAN (Omega). 
/// 
/// Rendezvous in general requires an intermediate transfer orbit to adjust the phasing of the chasing ship in order that
/// it arrives at the common node with the correct phase relationship for rendezvous. This intermediate
/// orbit is "free" in dV (since the velocity will help it get to the target). 
/// 
/// The general case follows Vallado Algorithm 46 (p368) with additional code to handle the case of outer orbit to
/// inner orbit. 
/// 
/// Coplanar transfer/rdvs is handled by the same code that was used in the original HohmannXfer class. 
/// 
/// </summary>
public class HohmannGeneral : OrbitTransfer
{
    private float transfer_time;

    private const float TWO_PI = 2f * Mathf.PI;

    private const float SMALL = 1E-2f;

    private double time_start; 

    public HohmannGeneral(OrbitData fromOrbit, OrbitData toOrbit, bool rendezvous) : base(fromOrbit, toOrbit) {

        name = "Hohmann General transfer";

        if (fromOrbit.ecc > SMALL) {
            Debug.LogError("Require a circular orbit. fromOrbit ecc=" + fromOrbit.ecc);
            return;
        }

        if (toOrbit.ecc > SMALL) {
            Debug.LogError("Require a circular orbit. toOrbit ecc=" + toOrbit.ecc);
            return;
        }

        bool inclinationChange = Mathf.Abs(fromOrbit.inclination - toOrbit.inclination) > SMALL;
        bool sameRadius = Mathf.Abs(fromOrbit.a - toOrbit.a) < SMALL;
        time_start = GravityEngine.Instance().GetPhysicalTimeDouble();

        if (sameRadius) {
            if (!inclinationChange) {
                if (rendezvous) {
                    SameOrbitPhasing(fromOrbit, toOrbit);
                } else {
                    Debug.LogWarning("Already in the required orbit. Nothing to do. ");
                }
            } else {
                CircularInclinationAndAN(fromOrbit, toOrbit, rendezvous);
            }
        } else {
            if (!inclinationChange) {
                CoplanarHohmannVallado(fromOrbit, toOrbit, rendezvous);
            } else {
                bool innerToOuter = (fromOrbit.a < toOrbit.a);
                if (innerToOuter)
                    InnerToOuter(fromOrbit, toOrbit, rendezvous);
                else
                    OuterToInner(fromOrbit, toOrbit, rendezvous);
            }
        }
    }

    // Easier to follow without all the if(innerToOuter) stuff, so implement two versions for non-coplanar with rdvs.
    // Differ in when the plane change is done (outer orbit since less dV cost)
    // and some differences in phasing calculations. 

    private void InnerToOuter(OrbitData fromOrbit, OrbitData toOrbit, bool rendezvous) {

        double w_target = toOrbit.GetOmegaAngular();
        double w_int = fromOrbit.GetOmegaAngular();
        float a_transfer = 0.5f * (fromOrbit.a + toOrbit.a);
        float t_transfer = Mathf.PI * Mathf.Sqrt(a_transfer * a_transfer * a_transfer / fromOrbit.mu);

        // Debug.LogFormat("int: a={0} w={1}  target: a={2} w={3}", fromOrbit.a, w_int, toOrbit.a, w_target);

        // lead angle required by target
        double alpha_L = w_target * t_transfer;

        double u_initial = 0;
        double u_final = 0;
        double delta_theta_int = 0; 
        FindClosestNode(fromOrbit, toOrbit, ref u_initial, ref u_final, ref delta_theta_int);

        double deltat_node = delta_theta_int / w_int;

        // Debug.LogFormat("Node at: {0} (deg), distance to node (deg)={1}", u_initial * Mathd.Rad2Deg, delta_theta_int * Mathd.Rad2Deg);

        // Algorithm uses lambda_true. See the definition in Vallado (2-92). Defined for circular equitorial
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
        double alpha_new = theta_new % (2.0 * Mathd.PI);

        //Debug.LogFormat("lambda_tgt1={0} theta_new={1} toOrbit.phase={2} t_node={3} w_target={4}",
        //    lambda_tgt1 * Mathd.Rad2Deg, theta_new * Mathd.Rad2Deg, toOrbit.phase, deltat_node, w_target);

        // k_target: number of revolutions in transfer orbit. Provided as input
        // k_int: number of revs in phasing orbit. Want to ensure a_phase < a_target to not
        //        waste deltaV.
        double mu = fromOrbit.mu;
        double k_target = 0.0;
        double two_pi_k_target = k_target * 2.0 * Mathd.PI;
        double P_phase = (alpha_new - alpha_L + two_pi_k_target) / w_target;
        while (P_phase < 0) {
            // Debug.Log("Pphase < 0. Bumping k_target");
            k_target += 1.0;
            two_pi_k_target = k_target * 2.0 * Mathd.PI;
            P_phase = (alpha_new - alpha_L + two_pi_k_target) / w_target;
        }
        double k_int = 1.0;
        double two_pi_k_int = k_int * 2.0 * Mathd.PI;
        double a_phase = Mathd.Pow(mu * (P_phase * P_phase / (two_pi_k_int * two_pi_k_int)), 1.0 / 3.0);
        //Debug.LogFormat("alpha_new={0} alpha_L={1} Pphase={2}", alpha_new * Mathd.Rad2Deg, alpha_L * Mathd.Rad2Deg, P_phase);

        // For outer to inner modify both target and phase orbits
        int loopCnt = 0;
        while (rendezvous && ((a_phase < fromOrbit.a) || (a_phase > toOrbit.a))) {
            if (a_phase < fromOrbit.a) {
                // Debug.Log("Adjust: a_phase < toOrbit - add a target lap. a_phase=" + a_phase);
                k_target += 1.0;
                two_pi_k_target = k_target * 2.0 * Mathd.PI;
            } else if (a_phase > toOrbit.a) {
                // Debug.Log("Adjust: a_phase > fromOrbit - add a phase lap. a_phase=" + a_phase);
                k_int += 1.0;
                two_pi_k_int = k_int * 2.0 * Mathd.PI;
            }
            P_phase = (alpha_new - alpha_L + two_pi_k_target) / w_target;
            a_phase = Mathd.Pow(mu * (P_phase * P_phase / (two_pi_k_int * two_pi_k_int)), 1.0 / 3.0);
            //Debug.LogFormat("alpha_new={0} alpha_L={1} Pphase={2} a_phase={3} k_int={4} k_tgt={5}",
            //    alpha_new * Mathd.Rad2Deg, alpha_L * Mathd.Rad2Deg, P_phase, a_phase, k_int, k_target);
            if (loopCnt++ > 10) {
                Debug.LogWarning("Failed to find transfer. Rendezvous phasing issue. ");
                return;
            }
        }

        double t_phase = k_int * 2.0 * Mathd.PI * Mathd.Sqrt(a_phase * a_phase * a_phase / fromOrbit.mu);

        double deltaV_phase = 0;
        double deltaV_trans1, deltaV_trans2;

        double delta_incl = (toOrbit.inclination - fromOrbit.inclination) * Mathd.Deg2Rad;
            if (rendezvous) {
                if (fromOrbit.a > 2.0 * a_phase) {
                    Debug.LogError("Phasing orbit is not an ellipse. Cannot proceed. a_phase=" + a_phase);
                    return;
                }
                deltaV_phase = Mathd.Sqrt(2.0 * mu / fromOrbit.a - mu / a_phase)
                                                    - Mathd.Sqrt(mu / fromOrbit.a);
                deltaV_trans1 = Mathd.Sqrt(2.0 * mu / fromOrbit.a - mu / a_transfer)
                                                    - Mathd.Sqrt(2.0 * mu / fromOrbit.a - mu / a_phase);
            } else {
                deltaV_trans1 = Mathd.Sqrt(2.0 * mu / fromOrbit.a - mu / a_transfer)
                                                    - Mathd.Sqrt( mu / fromOrbit.a);
            }
            // Note: deltaV_trans2 value is just for the accounting of dV. Manuever does a setv to 
            // place to ensure correct orientation. 
            deltaV_trans2 = Mathd.Sqrt(2.0 * mu / toOrbit.a - mu / a_transfer
                                              + mu / toOrbit.a
                                              - 2.0 * Mathd.Sqrt(2.0 * mu / toOrbit.a - mu / a_transfer)
                                                    * Mathd.Sqrt(mu / toOrbit.a) * Mathd.Cos(delta_incl));

        //Debug.LogFormat("T1: a_int={0} a_phase={1} a_tgt={2} dt={3} dV_phase={4} dv1={5} dv2={6}",
        //    fromOrbit.a, a_phase, toOrbit.a,
        //    deltat_node, deltaV_phase, deltaV_trans1, deltaV_trans2);

        Vector3d xferStart = new Vector3d(fromOrbit.GetPhysicsPositionforEllipse((float)(u_initial * Mathd.Rad2Deg)));
        if (rendezvous) {
            // phasing burn: in same plane as the orbit at the node, use scalar maneuver
            Maneuver m_phase = new Maneuver();
            m_phase.mtype = Maneuver.Mtype.scalar;
            m_phase.dV = (float)deltaV_phase;
            m_phase.worldTime = (float)(time_start + deltat_node);
            m_phase.nbody = fromOrbit.nbody;
            m_phase.physPosition = xferStart;
            maneuvers.Add(m_phase);
        } else {
            t_phase = 0; 
        }

        // transfer burn - stay in initial orbit plane
        Maneuver m_trans1 = new Maneuver();
        m_trans1.mtype = Maneuver.Mtype.scalar;
        m_trans1.dV = (float) deltaV_trans1;
        m_trans1.worldTime = (float)(time_start + deltat_node + t_phase);
        m_trans1.nbody = fromOrbit.nbody;
        m_trans1.physPosition = xferStart;
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

    private void OuterToInner(OrbitData fromOrbit, OrbitData toOrbit, bool rendezvous) {

        double w_target = toOrbit.GetOmegaAngular();
        double w_int = fromOrbit.GetOmegaAngular();
        float a_transfer = 0.5f * (fromOrbit.a + toOrbit.a);
        float t_transfer = Mathf.PI * Mathf.Sqrt(a_transfer * a_transfer * a_transfer / fromOrbit.mu);

        //Debug.LogFormat("int: a={0} w={1}  target: a={2} w={3} a_transfer={4}",
        //    fromOrbit.a, w_int, toOrbit.a, w_target, a_transfer);

        // lead angle required by target
        double alpha_L = w_target * t_transfer;

        double u_initial = 0;
        double u_final = 0;
        double delta_theta_int = 0; 
        FindClosestNode(fromOrbit, toOrbit, ref u_initial, ref u_final, ref delta_theta_int);
        double deltat_node = delta_theta_int / w_int;

        double t_phase = 0;
        double a_phase = 0;
        double deltaV_trans1 = 0;
        double mu = fromOrbit.mu;
        if (rendezvous) {

            //Debug.LogFormat("Node at: {0} rad/{1} (deg), distance to node (deg)={2}", u_initial, u_initial * Mathd.Rad2Deg, delta_theta_int * Mathd.Rad2Deg);

            // Algorithm uses lambda_true. See the definition in Vallado (2-92). Defined for circular equitorial
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
            double alpha_new = theta_new; 

            //Debug.LogFormat("lambda_tgt1={0} theta_new={1} toOrbit.phase={2} t_node={3} w_target={4}",
            //    lambda_tgt1 * Mathd.Rad2Deg, theta_new * Mathd.Rad2Deg, toOrbit.phase, deltat_node, w_target);

            // k_target: number of revolutions in transfer orbit. 
            // k_int: number of revs in phasing orbit. Want to ensure a_phase > a_target to not
            //        waste deltaV (outer to inner).
            double k_target = 0.0;
            double two_pi_k_target = k_target * 2.0 * Mathd.PI;
            double P_phase = (alpha_new - alpha_L + two_pi_k_target) / w_target;
            while (P_phase < 0) {
                //Debug.Log("Pphase < 0. Bumping k_target");
                k_target += 1.0;
                two_pi_k_target = k_target * 2.0 * Mathd.PI;
                P_phase = (alpha_new - alpha_L + two_pi_k_target) / w_target;
            }
            double k_int = 1.0;
            double two_pi_k_int = k_int * 2.0 * Mathd.PI;
            a_phase = Mathd.Pow(mu * (P_phase * P_phase / (two_pi_k_int * two_pi_k_int)), 1.0 / 3.0);
            //Debug.LogFormat("alpha_new={0} alpha_L={1} Pphase={2} a_phase={3}",
            //    alpha_new * Mathd.Rad2Deg, alpha_L * Mathd.Rad2Deg, P_phase, a_phase);


            // For outer to inner modify both target and phase orbits
            int loopCnt = 0;
            while (rendezvous && ((a_phase < toOrbit.a) || (a_phase > fromOrbit.a))) {
                if (a_phase < toOrbit.a) {
                    //Debug.Log("Adjust: a_phase < toOrbit - add a target lap. a_phase=" + a_phase);
                    k_target += 1.0;
                    two_pi_k_target = k_target * 2.0 * Mathd.PI;
                } else if (a_phase > fromOrbit.a) {
                    //Debug.Log("Adjust: a_phase > fromOrbit - add a phase lap. a_phase=" + a_phase);
                    k_int += 1.0;
                    two_pi_k_int = k_int * 2.0 * Mathd.PI;
                }
                P_phase = (alpha_new - alpha_L + two_pi_k_target) / w_target;
                a_phase = Mathd.Pow(mu * (P_phase * P_phase / (two_pi_k_int * two_pi_k_int)), 1.0 / 3.0);
                //Debug.LogFormat("alpha_new={0} alpha_L={1} Pphase={2} a_phase={3} k_int={4} k_tgt={5}",
                //    alpha_new * Mathd.Rad2Deg, alpha_L * Mathd.Rad2Deg, P_phase, a_phase, k_int, k_target);
                if (loopCnt++ > 10) {
                    Debug.LogWarning("Failed to find transfer. Rendezvous phasing issue. ");
                    return;
                }
            }

            t_phase = k_int * 2.0 * Mathd.PI * Mathd.Sqrt(a_phase * a_phase * a_phase / fromOrbit.mu);
 
            if (fromOrbit.a > 2.0 * a_phase) {
                Debug.LogError("Phasing orbit is not an ellipse. Cannot proceed. a_phase=" + a_phase);
                return;
            }
            deltaV_trans1 = Mathd.Sqrt(2.0 * mu / fromOrbit.a - mu / a_transfer)
                                                - Mathd.Sqrt(2.0 * mu / fromOrbit.a - mu / a_phase);
        }

        // To change plane at start, need velocity vector in plane of destination. Build this orbit. 
        OrbitData xferData = new OrbitData(toOrbit);

        float u_initialDeg = (float)(u_initial * Mathd.Rad2Deg);
        float u_finalDeg = (float)(u_final * Mathd.Rad2Deg);
        Vector3d xferStart = new Vector3d(fromOrbit.GetPhysicsPositionforEllipse(u_initialDeg));
        if (rendezvous) {
            // have apogee and a_phase, need ecc:
            xferData.a = (float)a_phase;
            float r_perigee = 2.0f * (float)a_phase - fromOrbit.a;
            xferData.ecc = (fromOrbit.a - r_perigee) / (fromOrbit.a + r_perigee);
            // Debug.LogFormat("ecc={0} a={1}", xferData.ecc, xferData.a);
            // phasing burn: In outer to inner do the plane change here, less dV
            Maneuver m_phase = new Maneuver();
            m_phase.mtype = Maneuver.Mtype.setv;
            // uFinal is node location since xferData is a copy of toOrbit BUT want the velocity of apoapsis
            // (this phase is peri)
            float v_xfer = (float)Mathd.Sqrt(2.0 * mu / fromOrbit.a - mu / a_phase);
            m_phase.velChange = v_xfer * xferData.GetPhysicsVelocityForEllipse(u_finalDeg).normalized;
            m_phase.dV = (m_phase.velChange - fromOrbit.GetPhysicsVelocityForEllipse(u_finalDeg)).magnitude;
            m_phase.worldTime = (float)(time_start + deltat_node);
            m_phase.nbody = fromOrbit.nbody;
            m_phase.physPosition = xferStart;
            maneuvers.Add(m_phase);

            // transfer burn - phasing orbit did plane change
            Maneuver m_trans1 = new Maneuver();
            m_trans1.mtype = Maneuver.Mtype.scalar;
            m_trans1.dV = (float)deltaV_trans1;
            m_trans1.worldTime = (float)(time_start + deltat_node + t_phase);
            m_trans1.nbody = fromOrbit.nbody;
            m_trans1.physPosition = xferStart;
            maneuvers.Add(m_trans1);
        } else {
            // no rendezvous - direct xfer
            t_phase = 0;
            // have apogee and a_transfer, need ecc:
            float r_perigee = 2.0f * (float)a_transfer - fromOrbit.a;
            xferData.a = (float)a_transfer;
            xferData.ecc = (fromOrbit.a - r_perigee) / (fromOrbit.a + r_perigee);
            // transfer burn - do inclination change here
            Maneuver m_trans1 = new Maneuver();
            m_trans1.mtype = Maneuver.Mtype.setv;
            float v_xfer = (float) Mathd.Sqrt(2.0 * mu / fromOrbit.a - mu / a_transfer); 
            m_trans1.velChange = v_xfer * xferData.GetPhysicsVelocityForEllipse(u_finalDeg).normalized;
            m_trans1.dV = (m_trans1.velChange - fromOrbit.GetPhysicsVelocityForEllipse(u_finalDeg)).magnitude;
            m_trans1.worldTime = (float)(time_start + deltat_node);
            m_trans1.nbody = fromOrbit.nbody;
            m_trans1.physPosition = xferStart;
            maneuvers.Add(m_trans1);
        }

        // Arrival burn
        float finalPhase = (float)(u_final * Mathd.Rad2Deg + 180f);
        Vector3 finalV = toOrbit.GetPhysicsVelocityForEllipse(finalPhase);
        Vector3 finalPos = toOrbit.GetPhysicsPositionforEllipse(finalPhase);
        Maneuver m_trans2 = new Maneuver();
        m_trans2.mtype = Maneuver.Mtype.setv;
        m_trans2.velChange = finalV;
        m_trans2.dV = (m_trans2.velChange - xferData.GetPhysicsVelocityForEllipse(finalPhase)).magnitude;
        m_trans2.worldTime = (float)(time_start + deltat_node + t_phase + t_transfer);
        m_trans2.nbody = fromOrbit.nbody;
        m_trans2.physPosition = new Vector3d(finalPos);
        maneuvers.Add(m_trans2);
    }



    private void CoplanarHohmannVallado(OrbitData fromOrbit, OrbitData toOrbit, bool rendezvous) {

        double mu = fromOrbit.mu;
        double a_target = toOrbit.a;
        double a_int = fromOrbit.a;
        bool innerToOuter = (a_target > a_int);
        double a_transfer = 0.5 * (a_int + a_target);
        double t_transfer = Mathd.PI * Mathd.Sqrt((a_transfer * a_transfer * a_transfer)/mu);
        double t_wait = 0; 
        if (rendezvous) {
            // determine the phasing required
            double w_target = Mathd.Sqrt(mu / (a_target * a_target * a_target));
            double w_int = Mathd.Sqrt(mu / (a_int * a_int * a_int));
            double alpha_L = w_target * t_transfer;
            // theta is required seperation (target - interceptor)
            double theta = alpha_L - Mathd.PI;
            double w_delta = w_int - w_target;
            double phaseDelta = NUtils.AngleDeltaDegrees(toOrbit.phase, fromOrbit.phase);
            if (!innerToOuter) {
                // target is moving away during xfer
                theta = Mathd.PI - alpha_L;
                w_delta *= -1.0;
                phaseDelta = NUtils.AngleDeltaDegrees(fromOrbit.phase, toOrbit.phase);
            }
            phaseDelta *= Mathd.Deg2Rad;
            double KLIMIT = 10.0;
            double k = 0.0;
            while ((t_wait <= 0) && (k < KLIMIT)) {
                t_wait = (theta - phaseDelta + 2.0 * Mathd.PI * k) /w_delta;
                k += 1.0;
            }
            if (k >= KLIMIT) {
                Debug.LogError("Transfer required too many orbits");
                return;
            }
        }

        double dV1 = Mathd.Sqrt(2.0 * mu / a_int - mu / a_transfer) - Mathd.Sqrt(mu / a_int);

        // Create two maneuvers for xfer
        Maneuver m1 = new Maneuver();
        m1.mtype = Maneuver.Mtype.scalar;
        m1.dV = (float) dV1;
        m1.worldTime = (float)(time_start + t_wait);
        m1.nbody = fromOrbit.nbody;
        m1.physPosition = Vector3d.zero;
        maneuvers.Add(m1);

        double dV2 = Mathd.Sqrt(mu / a_target) - Mathd.Sqrt(2.0 * mu / a_target - mu / a_transfer);

        Maneuver m2 = new Maneuver();
        m2.mtype = Maneuver.Mtype.scalar;
        m2.dV = (float)dV2;
        m2.worldTime = (float)(time_start + t_wait + t_transfer);
        m2.nbody = fromOrbit.nbody;
        m2.physPosition = Vector3d.zero;
        maneuvers.Add(m2);

        // maneuver positions and info for KeplerSeq conversion and velocity directions
        Vector3d h_unit = fromOrbit.GetAxis();
        float phaseAtXfer = (float)(fromOrbit.phase + (TWO_PI / fromOrbit.period) * t_wait * Mathf.Rad2Deg);
        m1.physPosition = new Vector3d(fromOrbit.GetPhysicsPositionforEllipse(phaseAtXfer));
        m1.relativePos = new Vector3d(fromOrbit.GetPhysicsPositionforEllipseRelative(phaseAtXfer));
        m1.relativeVel = Vector3d.Cross(h_unit, m1.relativePos).normalized;
        m1.relativeTo = fromOrbit.centralMass;

        m2.physPosition = new Vector3d(toOrbit.GetPhysicsPositionforEllipse(phaseAtXfer + 180f));
        m2.relativePos = new Vector3d(toOrbit.GetPhysicsPositionforEllipseRelative(phaseAtXfer + 180f));
        m2.relativeVel = Vector3d.Cross(h_unit, m2.relativePos).normalized;
        m2.relativeTo = fromOrbit.centralMass;

        // Determine the relative velocities
        m1.relativeVel *= Mathd.Sqrt(mu/fromOrbit.a) + m1.dV;
        m2.relativeVel *= Mathd.Sqrt(mu/toOrbit.a);
    }

    /// <summary>
    /// Rendezvous two ships in the same orbit. 
    /// 
    /// 
    /// </summary>
    /// <param name="fromOrbit"></param>
    /// <param name="toOrbit"></param>
    /// <param name="rendezvous"></param>
    private void SameOrbitPhasing(OrbitData fromOrbit, OrbitData toOrbit) {

        double mu = fromOrbit.mu;
        double a = fromOrbit.a;
        double w_target = Mathd.Sqrt(mu / (a * a * a));

        double theta = NUtils.AngleDeltaDegrees(fromOrbit.phase, toOrbit.phase) * Mathd.Deg2Rad;
        // Debug.LogFormat("Theta={0} \nfrom={1} \nto={2}", theta * Mathd.Rad2Deg, fromOrbit.LogString(), toOrbit.LogString() );
        double t_phase = 0;
        double a_phase = 0; 
        double k_target = 1.0;
        double k_int = 1.0;
        int loopCnt = 0;
        const int LOOP_LIMIT = 20;
        // target leads, want to do one (or more) orbits minus phase lead
        t_phase = (2.0 * Mathd.PI * k_target - theta) / w_target;
        if (theta > Mathd.PI) {
            theta = 2.0 * Mathd.PI - theta;
            t_phase = (2.0 * Mathd.PI * k_target + theta) / w_target;
        }

        while (((a_phase > 2.0*a) || (a_phase < 0.5 * a)) && loopCnt < LOOP_LIMIT) {
            double two_pi_k_int = k_int * 2.0 * Mathd.PI;
            a_phase = Mathd.Pow(mu * (t_phase * t_phase) / (two_pi_k_int * two_pi_k_int), 1.0 / 3.0);
            if (a_phase > 3.0 * a) {
                k_int += 1.0;
            } else if (a_phase < 0.5 * a) {
                k_target += 1.0;
            }
            loopCnt++;
        }
        if (loopCnt >= LOOP_LIMIT) {
            Debug.LogError("Loop count exceeded");
        }
        double t_transfer = 2.0 * Mathd.PI * Mathd.Sqrt(a_phase * a_phase * a_phase / mu);


        double dV1 = Mathd.Sqrt(2.0 * mu / a - mu / a_phase) - Mathd.Sqrt(mu / a);

        // Debug.LogFormat("a_phase={0} t_phase={1} k_int={2} k_tgt={3} dv1={4}", a_phase, t_transfer, k_int, k_target, dV1);

        // Create two maneuvers for xfer
        Maneuver m1 = new Maneuver();
        m1.mtype = Maneuver.Mtype.scalar;
        m1.dV = (float)dV1;
        m1.worldTime = (float)(time_start);
        m1.nbody = fromOrbit.nbody;
        m1.physPosition = Vector3d.zero;
        maneuvers.Add(m1);

        double dV2 = Mathd.Sqrt(mu / a) - Mathd.Sqrt(2.0 * mu / a - mu / a_phase);

        Maneuver m2 = new Maneuver();
        m2.mtype = Maneuver.Mtype.scalar;
        m2.dV = (float)dV2;
        m2.worldTime = (float)(time_start + t_transfer);
        m2.nbody = fromOrbit.nbody;
        m2.physPosition = Vector3d.zero;
        maneuvers.Add(m2);

        // maneuver positions and info for KeplerSeq conversion and velocity directions
        Vector3d h_unit = fromOrbit.GetAxis();
        float phaseAtXfer = (float)(fromOrbit.phase + (TWO_PI / fromOrbit.period) * time_start * Mathf.Rad2Deg);
        m1.physPosition = new Vector3d(fromOrbit.GetPhysicsPositionforEllipse(phaseAtXfer));
        m1.relativePos = new Vector3d(fromOrbit.GetPhysicsPositionforEllipseRelative(phaseAtXfer));
        m1.relativeVel = Vector3d.Cross(h_unit, m1.relativePos).normalized;
        m1.relativeTo = fromOrbit.centralMass;

        m2.physPosition = new Vector3d(toOrbit.GetPhysicsPositionforEllipse(phaseAtXfer + 180f));
        m2.relativePos = new Vector3d(toOrbit.GetPhysicsPositionforEllipseRelative(phaseAtXfer + 180f));
        m2.relativeVel = Vector3d.Cross(h_unit, m2.relativePos).normalized;
        m2.relativeTo = fromOrbit.centralMass;

        // Determine the relative velocities
        m1.relativeVel *= Mathd.Sqrt(mu / fromOrbit.a) + m1.dV;
        m2.relativeVel *= Mathd.Sqrt(mu / toOrbit.a);
    }

    /// <summary>
    /// Find the points where two circular orbits intersect. Only the orientation and phase portions of from/to are used so
    /// can be used if orbits are not the same radius. 
    /// 
    /// This code assumes the phase of the fromOrbit represents the position of a ship about to xfer and returns the node
    /// closest to this phase in the direction of orbit. 
    /// 
    /// </summary>
    /// <param name="fromOrbit"></param>
    /// <param name="orbitData"></param>
    /// <param name="u_initial">Phase of intersection point with respect to fromOrbit.</param>
    /// <param name="u_final">Phase of intersection point with respect to toOrbit.</param>
    private void FindClosestNode(OrbitData fromOrbit, OrbitData orbitData, ref double u_initial, ref double u_final, ref double delta_theta_int) {

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
        u_initial = Mathd.Acos(Mathd.Clamp(numer / denom, -1.0, 1.0));
        // u_final: phase of intersection in the final orbit
        numer = Mathd.Cos(i_initial) * Mathd.Sin(i_final) - Mathd.Sin(i_initial) * Mathd.Cos(i_final) * Mathd.Cos(dOmega);
        if (Mathd.Abs(Mathd.Sin(theta)) < 1E-6) {
            Debug.LogError("u_final: about to divide by zero (small theta)");
            return;
        }
        u_final = Mathd.Acos(Mathd.Clamp(numer / Mathd.Sin(theta), -1.0, 1.0));

        // how far is fromOrbit from a node? 
        // double delta_theta_int = u_initial - fromOrbit.phase * Mathd.Deg2Rad;
        delta_theta_int = NUtils.AngleDeltaRadians(fromOrbit.phase * Mathf.Deg2Rad, u_initial);
        // if node is more than a half-rev away, use the opposing node
        if (delta_theta_int > Mathd.PI) {
            delta_theta_int -= Mathd.PI;
            u_initial += Mathd.PI;
            u_final += Mathd.PI;
        }
    }

    public void CircularInclinationAndAN(OrbitData fromOrbit, OrbitData toOrbit, bool rendezvous) { 

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
        double u_initial = 0;
        double u_final = 0;
        // not used in same radius case.
        double delta_theta_int = 0; 
        FindClosestNode(fromOrbit, toOrbit, ref u_initial, ref u_final, ref delta_theta_int);

        double u_initialDeg = u_initial * Mathd.Rad2Deg;
        double u_finalDeg = u_final * Mathd.Rad2Deg;
        double time_to_crossing = fromOrbit.period * (u_initialDeg - fromOrbit.phase) / 360f;

        // Determine velocity change required
        Vector3 dV = toOrbit.GetPhysicsVelocityForEllipse((float)u_finalDeg) -
                        fromOrbit.GetPhysicsVelocityForEllipse((float)u_initialDeg);

        // Create a maneuver object
        Maneuver m = new Maneuver();
        m.physPosition = new Vector3d(fromOrbit.GetPhysicsPositionforEllipse((float)(u_initialDeg)));
        m.mtype = Maneuver.Mtype.setv;
        m.dV = dV.magnitude;
        m.velChange = toOrbit.GetPhysicsVelocityForEllipse((float)u_finalDeg);
        m.worldTime = (float)(time_start + time_to_crossing);
        m.nbody = fromOrbit.nbody;
        maneuvers.Add(m);

        if (rendezvous) {
            // Can do the co-planar phasing at any time. Apply a small time_offset amount to ensure the plane change
            // and sync maneuver are well separated
            double a = toOrbit.a;
            double mu = fromOrbit.mu;
            double w_to = Mathd.Sqrt(mu / (a * a * a));
            // make copies toOrbit and update phases as required
            OrbitData fromCopy = new OrbitData(toOrbit);
            OrbitData toCopy = new OrbitData(toOrbit);
            // advance phases 
            fromCopy.phase = (float) u_finalDeg;
            fromCopy.nbody = fromOrbit.nbody;
            toCopy.phase += (float)(w_to * time_to_crossing * Mathd.Rad2Deg);
            // Same orbit phasing will add two maneuvers
            SameOrbitPhasing(fromCopy, toCopy);
            const float time_offset = 0.5f;
            maneuvers[1].worldTime += (float) time_to_crossing + time_offset;
            maneuvers[2].worldTime += (float) time_to_crossing + time_offset;
            // TODO: Need to adjust physPos/physVel at maneuver time
        }
    }


}
