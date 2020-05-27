using UnityEngine;
using NUnit.Framework;

public class OrbitUtilsTest  {

    private const double small = 1E-4;

    private class RVpair
    {
        public Vector3d r;
        public Vector3d v;

        public RVpair(double rx, double ry, double rz, double vx, double vy, double vz) {
            r = new Vector3d(rx, ry, rz);
            v = new Vector3d(vx, vy, vz);
        }
    }

    [Test]
    // Check eccentricity and inclination
    public void RVtoCOEtoRV() {

        GameObject star = TestSetupUtils.CreateNBody(10, new Vector3(0, 0, 0));
        TestSetupUtils.SetupGravityEngine(star, null);
        NBody starBody = star.GetComponent<NBody>();
        RVpair[] rvp = {
            new RVpair(10, 0, 0, 0, 1, 0),
            new RVpair(10, 0, 0, 0, 10, 0),
            new RVpair(10, 0, 0, 0, -1, 0),
            new RVpair(10, 0, 0, 0, -10, 0),
            new RVpair(-10, 10, 0, -4, 3, 0),
            new RVpair(-10, 10, 0, 4, -3, 0)
        };

        for (int i = 0; i < rvp.Length; i++) {
            OrbitUtils.OrbitElements oe = OrbitUtils.RVtoCOE(rvp[i].r, rvp[i].v, starBody, false);
            Vector3d r1 = new Vector3d();
            Vector3d v1 = new Vector3d();
            OrbitUtils.COEtoRV(oe, starBody, ref r1, ref v1, false);

            Debug.LogFormat("i={0} r_in={1} r_out={2}\n v_in={3} v_out={4}\n oe: {5}", 
                i, rvp[i].r, r1, rvp[i].v, v1, oe);
            Assert.IsTrue(GEUnit.Vec3dEqual(rvp[i].r, r1, small));
            Assert.IsTrue(GEUnit.Vec3dEqual(rvp[i].v, v1, small));
        }
    }
}
