using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HohmannPlaneChange : OrbitTransfer {

	public HohmannPlaneChange(OrbitData fromOrbit, OrbitData toOrbit) : base(fromOrbit, toOrbit) {
		name = "Hohmann";

		// Hohmann xfer is via an ellipse from one circle to another. The ellipse is uniquely
		// defined by the radius of from and to. 
		// Equations from Chobotov Ch 5.4
		float r_inner = 0f;
		float r_outer = 0f; 
		// TODO: scaling needed? Check
		if (fromOrbit.a < toOrbit.a) {
			r_inner = fromOrbit.a;
			r_outer = toOrbit.a;
		} else {
			r_inner = toOrbit.a;
			r_outer = fromOrbit.a;
		}
		Debug.Log(string.Format("r_inner={0} r_outer={1}", r_inner, r_outer));

		// mass scale applied in Orbit data
		float v_inner = Mathf.Sqrt(fromOrbit.mu/r_inner);
		float rf_ri = r_outer/r_inner;
		float dV_inner = v_inner * (Mathf.Sqrt(2f*rf_ri/(1f+rf_ri)) -1f);

		float v_outer = Mathf.Sqrt(fromOrbit.mu/r_outer);
        //simplify per Roy (12.22)
		float dV_outer = v_outer * (1f - Mathf.Sqrt(2/(1+rf_ri)));

		// transfer time
        // Need to flip rf_ri for inner orbits to get the correct transfer_time
        // (should re-derive for this case sometime to see why)
		float transfer_time = 0f;

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
		if (fromOrbit.a < toOrbit.a) {
            float subexpr = 1f + rf_ri;
            transfer_time = fromOrbit.period / Mathf.Sqrt(32) * Mathf.Sqrt(subexpr * subexpr * subexpr);
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
            float subexpr_in = 1f + r_inner/r_outer;
            transfer_time = fromOrbit.period / Mathf.Sqrt(32) * Mathf.Sqrt(subexpr_in * subexpr_in * subexpr_in);
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
        m2.worldTime = worldTime + transfer_time;

    }

    public override string ToString() {
		return name;
	}

}
