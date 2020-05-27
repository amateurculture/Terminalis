using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EarthImpact : ImpactTrigger
{

    private bool impactFlag;
    private bool handledImpact;
    private NBody nbody;

    /// <summary>
    /// Need to handle GE changes in the update thread and not during a callback due to impact.
    /// </summary>
    /// <param name="nbody"></param>

    override
    public void Impact(NBody nbody, GravityState gravityState) {
        if (!impactFlag) {
            Vector3 vel = GravityEngine.Instance().GetScaledVelocity(nbody);
            Vector3 pos = gravityState.GetPhysicsPosition(nbody);
            pos = GravityEngine.Instance().physToWorldFactor * pos;
            Debug.LogFormat("Set pos={0} |pos| = {1}", pos, pos.magnitude);
            GravityEngine.Instance().InactivateBody(nbody.gameObject);
            nbody.GEUpdate(pos, vel, GravityEngine.Instance());

            this.nbody = nbody;
            impactFlag = true;
        }
    }

    void Update() {
        if (impactFlag && !handledImpact) {
            Vector3 vel = GravityEngine.Instance().GetScaledVelocity(nbody);
            Vector3 pos = GravityEngine.Instance().GetPhysicsPosition(nbody);
            Debug.LogFormat("Impact at r={0} |r|={1} v={2}  |v|={3} ", pos, pos.magnitude, vel, vel.magnitude);
            handledImpact = true;
        }
    }
}
