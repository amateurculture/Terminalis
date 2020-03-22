using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

public class OrbitUniversalTest  {


    [Test]
    // Check eccentricity and inclination
    public void CheckTestRV() {
        const float mass = 1000f;
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0, 0, 0));
        NBody starNbody = star.GetComponent<NBody>();
        const float orbitRadius = 10f;
        GameObject planet = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 1f, orbitRadius);
        OrbitUniversal orbitU = planet.GetComponent<OrbitUniversal>();
        orbitU.eccentricity = .25f;
        orbitU.inclination = 25f;
        orbitU.omega_uc = 10f;
        orbitU.omega_lc = 20f;
        orbitU.phase = 190f;
        orbitU.SetMajorAxisInspector(orbitRadius);

        OrbitData od = new OrbitData();
        od.a = orbitRadius;
        od.ecc = 0.25f;
        od.inclination = 25f;
        od.omega_uc = 10f;
        od.omega_lc = 20f;
        od.phase = 190f;
        od.centralMass = starNbody;
        GravityEngine.Instance().UnitTestAwake();
        Debug.LogFormat("major-axis: {0} vs {1}", orbitU.GetMajorAxisInspector(), orbitRadius);
        Assert.AreEqual(orbitU.GetMajorAxisInspector(), orbitRadius);
        TestRV(od, planet, starNbody, orbitRadius);
    }

    [Test]
    // Create an NBody and check it's mass
    public void CircleA() {
        const float mass = 1000f;
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0, 0, 0));
        NBody starNbody = star.GetComponent<NBody>();
        const float orbitRadius = 10f;
        GameObject planet = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 1f, orbitRadius);
        TestSetupUtils.SetupGravityEngine(star, planet);
        // confirm planet is in correct location
        Assert.AreEqual(Vector3.Distance(planet.transform.position, new Vector3(10f, 0f, 0)), 0);
        // take the velocity and check 
        OrbitData orbitData = new OrbitData();
        orbitData.SetOrbitForVelocity(planet.GetComponent<NBody>(), starNbody);
        GEUnit.FloatEqual(orbitData.a, orbitRadius);
        GEUnit.FloatEqual(orbitData.omega_uc, 0f);
        TestRV(orbitData, planet, starNbody, orbitRadius);
    }

    [Test]
    // Check eccentricity and inclination
    public void EccentricityInclTest() {
        const float mass = 1000f;
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0, 0, 0));
        NBody starNbody = star.GetComponent<NBody>();
        const float orbitRadius = 10f;
        GameObject planet = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 1f, orbitRadius);
        OrbitUniversal orbitU = planet.GetComponent<OrbitUniversal>();

        float eccentricity = 0.3f;
        // MUST reset the scale after ecc is changed, since -> p
        orbitU.eccentricity = eccentricity;
        orbitU.SetMajorAxisInspector(orbitRadius);
        TestSetupUtils.SetupGravityEngine(star, planet);

        // Try some values of inclination and ecc
        float[] eccValues = { 0f, .1f, .2f, 0.5f, 0.9f };
        float[] inclinationValues = { 0f, 30f, 45f, 60f, 90f, 135f, 180f };

        foreach (float inc in inclinationValues) {
            foreach (float ecc in eccValues) {
                Debug.Log("====EccentricityInclTest====    ecc=" + ecc + " incl = " + inc);
                orbitU.inclination = inc;
                orbitU.eccentricity = ecc;
                orbitU.SetMajorAxisInspector(orbitRadius);      // can only use for ellipses
                TestSetupUtils.SetupGravityEngine(star, planet);
                OrbitData od = new OrbitData();
                od.SetOrbitForVelocity(planet.GetComponent<NBody>(), starNbody);
                Debug.Log("TEST: incl = " + orbitU.inclination + " ecc=" + orbitU.eccentricity + " od:" + od.LogString());
                Debug.LogFormat("Check ecc: {0} vs {1}", ecc, od.ecc);
                Assert.IsTrue(GEUnit.FloatEqual(ecc, od.ecc, 1E-3));
                float axis = (float) orbitU.GetMajorAxisInspector();
                Debug.LogFormat("Check axis: {0} vs {1}", axis , od.a);
                Assert.IsTrue(GEUnit.FloatEqual( axis, od.a, 1E-3));
                Debug.LogFormat("Check incl: {0} vs {1}", inc, od.inclination);
                Assert.IsTrue(GEUnit.FloatEqual(inc, od.inclination, 1E-3));
                // TestRV(od, planet, starNbody, orbitRadius);
            }
        }
    }

    [Test]
    // Check eccentricity and inclination
    public void EllipseInclination() {
        const float mass = 1000f;
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0, 0, 0));
        NBody starNbody = star.GetComponent<NBody>();
        const float orbitRadius = 10f;
        GameObject planet = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 1f, orbitRadius);
        OrbitUniversal orbitU = planet.GetComponent<OrbitUniversal>();

        float eccentricity = 0.3f;
        // MUST reset the scale after ecc is changed, since -> p
        orbitU.eccentricity = eccentricity;
        orbitU.SetMajorAxisInspector(orbitRadius);
        TestSetupUtils.SetupGravityEngine(star, planet);

        // take the velocity and check 
        OrbitData orbitData = new OrbitData();
        orbitData.SetOrbitForVelocity(planet.GetComponent<NBody>(), star.GetComponent<NBody>());
        Debug.LogFormat("check a {0} vs {1}, check ecc: {2} vs {3}",
            orbitRadius, orbitData.a,
            eccentricity, orbitData.ecc);
        Assert.IsTrue(GEUnit.FloatEqual(orbitRadius, orbitData.a, 1E-3));
        Assert.IsTrue(GEUnit.FloatEqual(eccentricity, orbitData.ecc, 1E-3));

        // Try some values of inclination
        float[] inclinationValues = { 0f, 30f, 45f, 60f, 90f, 135f, 180f };
        foreach (float inc in inclinationValues) {
            Debug.Log("====EclipseInclination====    inc=" + inc);
            orbitU.inclination = inc;
            TestSetupUtils.SetupGravityEngine(star, planet);
            OrbitData od = new OrbitData();
            od.SetOrbitForVelocity(planet.GetComponent<NBody>(), starNbody);
            Debug.Log("TEST: incl = " + orbitU.inclination + " ecc=" + orbitU.eccentricity + " od:" + od.LogString());
            Debug.LogFormat("Check incl: {0} vs {1}", inc, od.inclination);
            Assert.IsTrue(GEUnit.FloatEqual(inc, od.inclination, 1E-3));
            TestRV(od, planet, starNbody, orbitRadius);
        }
    }

    // Do omega_uc with NO inclination. This will come back as omega_lc, since
    // Omega is angle to line of nodes - and does not apply if inclination=0
    [Test]
    public void OmegaUNoInclination() {
        const float mass = 1000f;
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0, 0, 0));
        NBody starNbody = star.GetComponent<NBody>();
        const float orbitRadius = 10f;
        GameObject planet = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 1f, orbitRadius);
        OrbitUniversal orbitU = planet.GetComponent<OrbitUniversal>();
        orbitU.eccentricity = 0.1f;
        orbitU.SetMajorAxisInspector(orbitRadius);
        // Try some values of om
        float[] omegaValues = { 0f, 30f, 45f, 60f, 90f, 135f, 180f, 210f, 320f };
        foreach (float omega in omegaValues) {
            orbitU.omega_uc = omega;
            TestSetupUtils.SetupGravityEngine(star, planet);

            OrbitData od = new OrbitData();
            od.SetOrbitForVelocity(planet.GetComponent<NBody>(), star.GetComponent<NBody>());
            Debug.Log("Omega = " + omega + " od.omega_lc=" + od.omega_lc + " od:" + od.LogString());
            Assert.IsTrue(GEUnit.FloatEqual(omega, od.omega_lc, 0.4));
        }
    }

    // Do omega_uc with NO inclination. This will come back as omega_lc, since
    // Omega is angle to line of nodes - and does not apply if inclination=0
    [Test]
    public void OmegaUCircleInclination() {
        const float mass = 1000f;
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0, 0, 0));
        NBody starNbody = star.GetComponent<NBody>();
        const float orbitRadius = 10f;
        GameObject planet = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 1f, orbitRadius);
        OrbitUniversal orbitU = planet.GetComponent<OrbitUniversal>();
        orbitU.eccentricity = 0.0f;
        orbitU.inclination = 5;
        orbitU.SetMajorAxisInspector(orbitRadius);
        // Try some values of om
        float[] omegaValues = { 0f, 30f, 45f, 60f, 90f, 135f, 180f, 210f, 320f };
        foreach (float omega in omegaValues) {
            orbitU.omega_uc = omega;
            TestSetupUtils.SetupGravityEngine(star, planet);

            OrbitData od = new OrbitData();
            od.SetOrbitForVelocity(planet.GetComponent<NBody>(), star.GetComponent<NBody>());
            Debug.Log("Omega = " + omega + " od.omega_lc=" + od.omega_lc + " od:" + od.LogString());
            Assert.IsTrue(GEUnit.FloatEqual(omega, od.omega_uc, 0.1));
        }
    }
    // Do omega_uc with NO inclination. This will come back as omega_lc, since
    // Omega is angle to line of nodes - and does not apply if inclination=0
    [Test]
    public void KeplerVsTimeOfFlight() {
        // Need to make sure TOF < 1 period
        const float mass = 100f;
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0, 0, 0));
        NBody starNbody = star.GetComponent<NBody>();
        const float orbitRadius = 10f;
        GameObject planet = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 1f, orbitRadius);
        OrbitUniversal orbitU = planet.GetComponent<OrbitUniversal>();
        // Parabola (ecc=1.0 fails, need to investigate)
        float[] ecc_values = { 0.0f, 0.1f, 0.5f, 0.9f,  1.2f, 1.5f };
        foreach (float ecc in ecc_values) {
            Debug.LogFormat("======= ecc={0}  =======", ecc);
            orbitU.eccentricity = ecc;
            orbitU.p = 10f;
            orbitU.evolveMode = OrbitUniversal.EvolveMode.KEPLERS_EQN;
            // Evolve to position r1
            double time = 5.0;
            TestSetupUtils.SetupGravityEngine(star, planet);
            double[] r1 = new double[] { 0, 0, 0 };
            // orbitU.PreEvolve(pscale, mscale);
            // Ugh. Need to do this before call evolve, since it caches the value. 
            Vector3d r0_vec = GravityEngine.Instance().GetPositionDoubleV3(planet.GetComponent<NBody>());
            orbitU.Evolve(time, ref r1);
            Vector3d r1_vec = new Vector3d(ref r1);
            // check time to r1
            double time_test = orbitU.TimeOfFlight(r0_vec, r1_vec);
            Debug.LogFormat("check r0={0} to r1={1} p ={2} after t={3} TOF => {4}",
                r0_vec, r1_vec, orbitU.p, time, time_test);
            Assert.IsTrue(GEUnit.DoubleEqual(time, time_test, 1E-4));
        }
    }

    [Test]
    public void PositionForRadius() {
        const float mass = 1000f;
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0, 0, 0));
        NBody starNbody = star.GetComponent<NBody>();
        const float orbitRadius = 10f;
        GameObject planet = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 1f, orbitRadius);
        OrbitUniversal orbitU = planet.GetComponent<OrbitUniversal>();
        orbitU.inclination = 5;
        orbitU.SetMajorAxisInspector(orbitRadius);
        float r = 10.0f;
        float[] eccValues = {  0f, 0.1f, 0.9f, 1.1f };
        foreach (float ecc in eccValues) {
            Debug.LogFormat("======= ecc={0}  =======", ecc);
            orbitU.eccentricity = ecc;
            orbitU.SetMajorAxisInspector(orbitRadius); // updates p
            TestSetupUtils.SetupGravityEngine(star, planet);

            Vector3[] positions = orbitU.GetPositionsForRadius(r, new Vector3(0,0,0));
            Debug.LogFormat("pos[0]={0} pos[1]={1}", positions[0], positions[1]);
            foreach( Vector3 p in positions) {
                Debug.LogFormat("Position error={0}", Mathf.Abs(p.magnitude - r));
                Assert.IsTrue(GEUnit.FloatEqual(p.magnitude, r, 1E-2));
            }
        }
    }
    private void TestRV(OrbitData od, GameObject planet, NBody starNbody, float orbitRadius) {

        GameObject testPlanet = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 1f, orbitRadius);
        OrbitUniversal orbitU = testPlanet.GetComponent<OrbitUniversal>();
        // Run init explicitly to update transform details
        orbitU.InitFromOrbitData(od, 0);

        // Awkward but previously could not add a new object to GE when it is stopped, so re-add all three
        // Leave as is, since it works!
        GravityEngine ge = GravityEngine.Instance();
        ge.Clear();
        ge.AddBody(starNbody.gameObject);
        ge.AddBody(planet);
        ge.AddBody(testPlanet);
        ge.Setup();
        ge.LogDump();
        Vector3 r_od = ge.GetPhysicsPosition(testPlanet.GetComponent<NBody>());
        Vector3 v_od = ge.GetVelocity(testPlanet);
        Vector3 r_i = ge.GetPhysicsPosition(planet.GetComponent<NBody>());
        Vector3 v_i = ge.GetVelocity(planet);
        Debug.Log(" r_i=" + r_i + " r_od=" + r_od + " delta=" + Vector3.Distance(r_i, r_od));
        Debug.Log(" v_i=" + v_i + " v_od=" + v_od + " delta=" + Vector3.Distance(v_i, v_od));
        Assert.IsTrue(GEUnit.FloatEqual(Vector3.Distance(r_i, r_od), 0f, 1E-2));
        Assert.IsTrue(GEUnit.FloatEqual(Vector3.Distance(v_i, v_od), 0f, 1E-2));

    }

}
