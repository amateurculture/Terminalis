using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircularizeXfer : OrbitTransfer
{

    public CircularizeXfer(OrbitData fromOrbit) : base(fromOrbit)
    {
        name = "Circularize";
        GravityEngine ge = GravityEngine.Instance();

        // find velocity vector perpendicular to r for circular orbit
        Vector3d r_ship = ge.GetPositionDoubleV3(fromOrbit.nbody);
        Vector3d v_ship = ge.GetVelocityDoubleV3(fromOrbit.nbody);
        Vector3d r_center = ge.GetPositionDoubleV3(fromOrbit.centralMass);
        Vector3d v_center = ge.GetVelocityDoubleV3(fromOrbit.centralMass);

        Vector3d r = r_ship - r_center;

        // want velocity relative to central mass (it could be moving)
        Vector3d v =  v_ship - v_center;

        // to get axis of orbit, can take r x v
        Vector3d axis = Vector3d.Cross(r, v).normalized;
        // vis visa for circular orbit
        double mu = GravityEngine.Instance().GetMass(centerBody);
        double v_mag = Mathd.Sqrt(mu / r.magnitude);
        // positive v is counter-clockwise
        Vector3d v_dir = Vector3d.Cross(axis, r).normalized;
        Vector3d v_circular = v_mag * v_dir;

        Maneuver m1;
        m1 = new Maneuver();
        m1.nbody = fromOrbit.nbody;
        m1.mtype = Maneuver.Mtype.vector;
        m1.velChange = (v_circular - v).ToVector3();
        m1.dV = Vector3.Magnitude(m1.velChange);

        // maneuver positions and info for KeplerSeq conversion and velocity directions
        Vector3d h_unit = fromOrbit.GetAxis();
        m1.physPosition = r_ship;
        m1.relativePos = r_ship - r_center;
        m1.relativeVel = v_circular.magnitude * Vector3d.Cross(h_unit, m1.relativePos).normalized;
        m1.relativeTo = fromOrbit.centralMass;

        //Debug.LogFormat("v_ship={0} v_center={1} v_c.x={2}", vel_ship, vel_center, v_center[0]);
        //Debug.Log(string.Format("v_ship={0} v_circular={1} axis={2} v_dir={3} velChange={4}", 
        //    vel_ship, v_circular, axis, v_dir, m1.velChange));
        m1.worldTime = ge.GetPhysicalTime();
        maneuvers.Add(m1);
    }

    public override string ToString()
    {
        return name;
    }
}
