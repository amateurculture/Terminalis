using UnityEngine;
using UnityEditor;
using NUnit.Framework;

public class HyperDataTest {

	private static bool FloatEqual(float a, float b) {
		return (Mathf.Abs(a-b) < 1E-3); 
	}

	private static bool FloatEqual(float a, float b, double error) {
		return (Mathf.Abs(a-b) < error); 
	}

    private static bool FloatEqualMod360(float a_, float b_, double error) {
        float a = a_;
        if (a >= 360f)
            a -= 360f;
        else if (b_ < 0)
            b_ += 360f;
        float b = b_;
        if (b >= 360f)
            b -= 360f;
        else if (b < 0)
            b += 360f;
        if (Mathf.Abs(a - b) < error)
            return true;
        // can still have e.g. 0 and 359.9
        if (Mathf.Abs(a + 360 - b) < error)
            return true;
        if (Mathf.Abs(a - 360 - b) < error)
            return true;
        return false;
    }

    // HYPERBOLAS

    [Test]
    public void HyperBasic()
    {
        const float mass = 1000f; 
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0,0,0));
		GameObject planet = TestSetupUtils.CreatePlanetInHyper(star, 1f);
		OrbitHyper orbitHyper = planet.GetComponent<OrbitHyper>();
		// Try some values of om
		float[] eccValues = { 1.1f, 1.3f, 2f, 2.2f, 3f, 10f};
		foreach (float ecc in eccValues) {
			orbitHyper.ecc = ecc ;
            TestSetupUtils.SetupGravityEngine(star, planet);

            orbitHyper.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
			OrbitData od = new OrbitData();
			od.SetOrbitForVelocity(planet.GetComponent<NBody>(), star.GetComponent<NBody>());
			Debug.Log("ecc = " + ecc + " od.ecc=" + od.ecc);
			// Need a bit of leeway at 0 with error
			Assert.IsTrue( FloatEqual(ecc, od.ecc, 0.02) );
            TestRV(od, planet, star);
        }
    }

	[Test]
    public void HyperInclination()
    {
        const float mass = 1000f; 
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0,0,0));
		GameObject planet = TestSetupUtils.CreatePlanetInHyper(star, 1f);
		OrbitHyper orbitHyper = planet.GetComponent<OrbitHyper>();
		orbitHyper.ecc = 1.4f;
		orbitHyper.r_initial = 20f;

		// Try some values of om
		float[] inclValues = { 30f, 45f, 60f, 90f, 135f, 180f, 0f};
		foreach (float incl in inclValues) {
			orbitHyper.inclination = incl ;
            TestSetupUtils.SetupGravityEngine(star, planet);

            orbitHyper.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
			OrbitData od = new OrbitData();
			od.SetOrbitForVelocity(planet.GetComponent<NBody>(), star.GetComponent<NBody>());
			Debug.Log("incl = " + incl + " od.incl=" + od.inclination);
			// Need a bit of leeway at 0 with error
			Assert.IsTrue( FloatEqual(incl, od.inclination, 0.02) );
		}
    }

	[Test]
    public void HyperOmegaLNoInclNoPhase()
    {
        const float mass = 1000f; 
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0,0,0));
        const float orbitPeri = 15f; 
		GameObject planet = TestSetupUtils.CreatePlanetInHyper(star, 1f);
		OrbitHyper orbitHyper = planet.GetComponent<OrbitHyper>();
		orbitHyper.ecc = 1.4f;
		orbitHyper.perihelion = orbitPeri;
		orbitHyper.r_initial = orbitPeri;

		// Try some values of om
		float[] omegaValues = { 30f, 45f, 60f, 90f, 135f, 180f, 0f};
		foreach (float omega in omegaValues) {
			orbitHyper.omega_lc = omega ;
            TestSetupUtils.SetupGravityEngine(star, planet);

            orbitHyper.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
			OrbitData od = new OrbitData();
			od.SetOrbitForVelocity(planet.GetComponent<NBody>(), star.GetComponent<NBody>());
			Debug.Log("omega = " + omega + " od.omega_l=" + od.omega_lc);
			// Need a bit of leeway at 0 with error
			Assert.IsTrue( FloatEqualMod360(omega, od.omega_lc, 0.05) );
			TestRV(od, planet, star);
		}
    }

	[Test]
    public void HyperPhaseNoIncl()
    {
        const float mass = 1000f; 
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0,0,0));
        const float orbitPeri = 15f; 
		GameObject planet = TestSetupUtils.CreatePlanetInHyper(star, 1f);
		OrbitHyper orbitHyper = planet.GetComponent<OrbitHyper>();
		orbitHyper.ecc = 1.4f;
		orbitHyper.perihelion = orbitPeri;
		orbitHyper.r_initial = orbitPeri;

		// Try some values of om
		float[] rinit_values = { orbitPeri, orbitPeri+2f, orbitPeri+5f, orbitPeri+10f, orbitPeri+20f};
		foreach (float rinit in rinit_values) {
			orbitHyper.r_initial = rinit ;
            TestSetupUtils.SetupGravityEngine(star, planet);

            orbitHyper.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
			OrbitData od = new OrbitData();
			od.SetOrbitForVelocity(planet.GetComponent<NBody>(), star.GetComponent<NBody>());
			Debug.Log("rinit = " + rinit + " od.r_initial=" + od.r_initial);
			// Need a bit of leeway at 0 with error
			TestRV(od, planet, star);
        }
    }

	[Test]
    public void HyperOmegaLNoIncl()
    {
        const float mass = 1000f; 
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0,0,0));
		GameObject planet = TestSetupUtils.CreatePlanetInHyper(star, 1f);
		OrbitHyper orbitHyper = planet.GetComponent<OrbitHyper>();
		orbitHyper.ecc = 1.4f;
		orbitHyper.r_initial = 20f;

		// Try some values of om
		float[] omegaValues = { 30f, 45f, 60f, 90f, 135f, 180f, 0f};
		foreach (float omega in omegaValues) {
			orbitHyper.omega_lc = omega ;
            TestSetupUtils.SetupGravityEngine(star, planet);

            orbitHyper.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
			OrbitData od = new OrbitData();
			od.SetOrbitForVelocity(planet.GetComponent<NBody>(), star.GetComponent<NBody>());
            Debug.Log("OD:" + od.LogString());
			Debug.Log("omega = " + omega + " od.omega_l=" + od.omega_lc);
            // Need a bit of leeway at 0 with error
 			TestRV(od, planet, star);
            Assert.IsTrue(FloatEqualMod360(omega, od.omega_lc - 360f, 0.02));
        }
    }

	[Test]
    public void HyperOmegaLIncl()
    {
        const float mass = 1000f; 
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0,0,0));
		GameObject planet = TestSetupUtils.CreatePlanetInHyper(star, 1f);
		OrbitHyper orbitHyper = planet.GetComponent<OrbitHyper>();
		orbitHyper.ecc = 1.4f;
		orbitHyper.r_initial = 20f;
		orbitHyper.inclination = 40f;

		// Try some values of om
		float[] omegaValues = { 30f, 45f, 60f, 90f, 135f, 180f, 1f, 358f};
		foreach (float omega in omegaValues) {
			orbitHyper.omega_lc = omega ;
            TestSetupUtils.SetupGravityEngine(star, planet);

            orbitHyper.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
			OrbitData od = new OrbitData();
			od.SetOrbitForVelocity(planet.GetComponent<NBody>(), star.GetComponent<NBody>());
            Debug.Log(od.LogString());
			Debug.Log("omega = " + omega + " od.omega_l=" + od.omega_lc);
			// Need a bit of leeway at 0 with error
			Assert.IsTrue( FloatEqualMod360(omega, od.omega_lc, 0.02) );
		}
    }

	[Test]
    public void HyperOmegaUInclNoPhase()
    {
        const float mass = 1000f; 
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0,0,0));
		GameObject planet = TestSetupUtils.CreatePlanetInHyper(star, 1f);
		OrbitHyper orbitHyper = planet.GetComponent<OrbitHyper>();
		orbitHyper.ecc = 1.4f;
		orbitHyper.perihelion = 20f;
		orbitHyper.r_initial = 20f;
		orbitHyper.inclination = 35f;

		// Try some values of om
		float[] omegaValues = { 30f, 45f, 60f, 90f, 135f, 180f, 210f, 275f,  355f};
		foreach (float omega in omegaValues) {
			orbitHyper.omega_uc = omega ;
            TestSetupUtils.SetupGravityEngine(star, planet);

            orbitHyper.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
			OrbitData od = new OrbitData();
			od.SetOrbitForVelocity(planet.GetComponent<NBody>(), star.GetComponent<NBody>());
			Debug.Log("omega = " + omega + " od.omega_uc=" + od.omega_uc);
			// Need a bit of leeway at 0 with error
			Assert.IsTrue( FloatEqualMod360(omega, od.omega_uc, 0.05) );
		}
    }
	[Test]
    public void HyperOmegaUIncl()
    {
        const float mass = 1000f; 
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0,0,0));
		GameObject planet = TestSetupUtils.CreatePlanetInHyper(star, 1f);
		OrbitHyper orbitHyper = planet.GetComponent<OrbitHyper>();
		orbitHyper.ecc = 1.4f;
		orbitHyper.r_initial = 20f;
		orbitHyper.inclination = 35f;

		// Try some values of om
		float[] omegaValues = { 30f, 45f, 60f, 90f, 135f, 180f, 210f, 275f,  355f};
		foreach (float omega in omegaValues) {
			orbitHyper.omega_uc = omega ;
            TestSetupUtils.SetupGravityEngine(star, planet);

            orbitHyper.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
			OrbitData od = new OrbitData();
			od.SetOrbitForVelocity(planet.GetComponent<NBody>(), star.GetComponent<NBody>());
			Debug.Log("omega = " + omega + " od.omega_uc=" + od.omega_uc);
			// Need a bit of leeway at 0 with error
			Assert.IsTrue( FloatEqualMod360(omega, od.omega_uc, 0.02) );
		}
    }

	[Test]
    public void HyperInclOmega()
    {
        const float mass = 1000f; 
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0,0,0));
		GameObject planet = TestSetupUtils.CreatePlanetInHyper(star, 1f);
		OrbitHyper orbitHyper = planet.GetComponent<OrbitHyper>();
		orbitHyper.r_initial = 20f;
		orbitHyper.ecc = 2.5f;

		// Try some values of phase (incl=0 covered by another test)
		float[] inclinationValues = { 30f, 45f, 60f, 90f, 135f, 180f};
		float[] omegaUValues = { 0f, 30f, 45f, 60f, 90f, 135f, 180f, 210f, 320f};
		foreach (float incl in inclinationValues) { 
			foreach (float omegau in omegaUValues) {
				orbitHyper.inclination = incl ; 
				orbitHyper.omega_uc = omegau;
                TestSetupUtils.SetupGravityEngine(star, planet);
                Debug.LogFormat("Test for i={0} omegaU={1}", incl, omegau);
                orbitHyper.Log("Initial circle:");
				OrbitData od = new OrbitData();
				od.SetOrbitForVelocity(planet.GetComponent<NBody>(), star.GetComponent<NBody>());
                Debug.Log("OrbitData: " + od.LogString());
                TestRV(od, planet, star);
                Debug.Log("incl = " + incl + " od.incl=" + od.inclination);
				Debug.Log("omegaU = " + omegau + " od.omegau=" + od.omega_uc +" od.omega_lc=" + od.omega_lc );
                if (incl != 180f) {
                    // 180 comes back as omegaL = omegaU = 180, but TestRV is ok
                    Assert.IsTrue(FloatEqual(incl, od.inclination, 0.02));
                    Assert.IsTrue(FloatEqualMod360(omegau, od.omega_uc, 0.02));
                }
			}
		}
    }

    private void TestRV(OrbitData od, GameObject planet, GameObject star)  {

        GameObject testPlanet = TestSetupUtils.CreatePlanetInHyper(star, 1f);
        testPlanet.name = "TestPlanet";
        OrbitHyper testHyper = testPlanet.GetComponent<OrbitHyper>();
        testHyper.InitFromOrbitData(od);

        planet.name = "Planet";

        // Awkward but cannot add a new object to GE when it is stopped, so re-add all three
        GravityEngine ge = GravityEngine.Instance();
        ge.Clear();
        ge.AddBody(star);
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
        Assert.IsTrue(FloatEqual(Vector3.Distance(r_i, r_od), 0f, 5E-2));
        Assert.IsTrue(FloatEqual(Vector3.Distance(v_i, v_od), 0f, 5E-2));

    }

}
