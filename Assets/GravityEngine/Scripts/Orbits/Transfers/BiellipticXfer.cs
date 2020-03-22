using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bi-elliptic Transfer
/// 
/// Bi-Elliptic is a three transfer maneuver to an orbit outside the target orbit (assuming
/// target orbit larger that start), then a xfer back. It can be lower dV (but longer time)
/// than Hohmann for some rf/ri. e.g. if rf/ri > 11.94 and xfer_radius is very large will be better by less than
/// 10%. (see https://en.wikipedia.org/wiki/Bi-elliptic_transfer)
/// </summary>
public class BiellipticXfer : OrbitTransfer {

	public BiellipticXfer(OrbitData fromOrbit, OrbitData toOrbit, float xferOvershootPercent) : base(fromOrbit, toOrbit) {
        // call this beta until can better tune the deep space xfer. 
		name = "Bi-Elliptic (beta)"; 

		float r_inner = fromOrbit.a;
		float r_outer = toOrbit.a;

        if (r_inner > r_outer) {
            return;
        }

        float xfer_radius = r_outer * (1f + xferOvershootPercent / 100f);

        // mass scale applied in Orbit data
        float a1 = (r_inner + xfer_radius) / 2f;
        float a2 = (r_outer + xfer_radius) / 2f;
        float mu = fromOrbit.mu;
        float dV1 = Mathf.Sqrt(2f * mu / r_inner - mu / a1) - Mathf.Sqrt(mu / r_inner);
        float dV2 = Mathf.Sqrt(2f * mu / xfer_radius - mu / a2) -
                    Mathf.Sqrt(2f * mu / xfer_radius - mu / a1);
        float dV3 = Mathf.Sqrt(2f * mu / r_outer - mu / a2) - Mathf.Sqrt(mu / r_outer);

        // transfer time
        float t1 = Mathf.PI * Mathf.Sqrt(a1 * a1 * a1 / mu);
        float t2 = Mathf.PI * Mathf.Sqrt(a2 * a2 * a2 / mu);

		// Build the manuevers required
		deltaV = 0f;
		float worldTime = GravityEngine.Instance().GetPhysicalTime();

		Maneuver[] marray = new Maneuver[3];
        for (int i=0; i < marray.Length; i++ ) {
            marray[i] = new Maneuver();
            marray[i].nbody = fromOrbit.nbody;
            marray[i].mtype = Maneuver.Mtype.scalar;
            maneuvers.Add(marray[i]);
        }
        marray[0].worldTime = worldTime;
        marray[0].dV = dV1;
        marray[1].worldTime = worldTime + t1;
        marray[1].dV = dV2;
        marray[2].worldTime = worldTime + t1 + t2;
        marray[2].dV = -dV3;

        deltaV = (dV1 + dV2 + dV3);
        deltaT = t1 + t2;
    }

    /// <summary>
    /// Check to see if a bi-elliptic xfer can produce lower dV. In many cases the time
    /// required for the BE xfer may not be practical.
    /// 
    /// Routine imposes a limit of 13 for r2/r1 (for which transfer orbit is 48.9*r1)
    /// 
    /// </summary>
    /// <param name="fromOrbit"></param>
    /// <param name="toOrbit"></param>
    /// <returns></returns>
    public static bool HasLowerDv(OrbitData fromOrbit, OrbitData toOrbit)
    {
        bool valid = false;
        if (fromOrbit.a < toOrbit.a)
        {
            float ratio21 = toOrbit.a / fromOrbit.a;
            if (ratio21 > 13f)
            {
                valid = true;
            }
        }
        return valid;
    }

    public BiellipticXfer CreateTransferCopy(float xferRadius) {

        BiellipticXfer newXfer = new BiellipticXfer(this.fromOrbit, this.toOrbit, xferRadius);
        return newXfer;
    }

    public override string ToString() {
		return name;
	}

}
