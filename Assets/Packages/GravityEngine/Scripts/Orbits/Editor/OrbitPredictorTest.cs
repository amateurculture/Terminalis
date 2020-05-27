using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

public class OrbitPredictorTest
{

    private void CompareOrbits(OrbitUniversal orbit1, OrbitUniversal orbit2, double precision) {
        if (Mathd.Abs(orbit1.p - orbit2.p) > precision) {
            Debug.LogFormat("** FAIL ** p compare failed {0} vs {1}", orbit1.p, orbit2.p);
            Debug.LogFormat("** orbit1={0}\n**  orbit2={1}", orbit1.DumpInfo(), orbit2.DumpInfo());
            Assert.Fail();
        }
        if (Mathd.Abs(orbit1.eccentricity - orbit2.eccentricity) > precision) {
            Debug.LogFormat("** FAIL ** ecc compare failed {0} vs {1}", orbit1.eccentricity, orbit2.eccentricity);
            Debug.LogFormat("** orbit1={0}\n**  orbit2={1}", orbit1.DumpInfo(), orbit2.DumpInfo());
            Assert.Fail();
        }
        if (Mathd.Abs(orbit1.inclination - orbit2.inclination) > precision) {
            Debug.LogFormat("** FAIL ** incl compare failed {0} vs {1}", orbit1.inclination, orbit2.inclination);
            Debug.LogFormat("** orbit1={0}\n**  orbit2={1}", orbit1.DumpInfo(), orbit2.DumpInfo());
            Assert.Fail();
        }
        double diff = orbit1.omega_lc - orbit2.omega_lc;
        if ((Mathd.Abs(diff) > precision) && 
            (Mathd.Abs(diff - 360.0) > precision) &&
            (Mathd.Abs(diff + 360.0) > precision)) {
            Debug.LogFormat("** FAIL ** Omega LC compare failed {0} vs {1}", orbit1.omega_lc, orbit2.omega_lc);
            Debug.LogFormat("** orbit1={0}\n**  orbit2={1}", orbit1.DumpInfo(), orbit2.DumpInfo());
            Assert.Fail();
        }
        diff = orbit1.omega_uc - orbit2.omega_uc;
        if ((Mathd.Abs(diff) > precision) &&
            (Mathd.Abs(diff - 360.0) > precision) &&
            (Mathd.Abs(diff + 360.0) > precision)) {
            Debug.LogFormat("** FAIL ** omega UC compare failed {0} vs {1}", orbit1.omega_uc, orbit2.omega_uc);
            Debug.LogFormat("** orbit1={0}\n**  orbit2={1}", orbit1.DumpInfo(), orbit2.DumpInfo());
            Assert.Fail();
        }
    }

    const double small = 1E-4;

    [Test]
    // Create an NBody and check it's mass
    public void CirclePrediction() {
        const float mass = 1000f;
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0, 0, 0));
        NBody starNbody = star.GetComponent<NBody>();
        const float orbitRadius = 10f;
        GameObject planet = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 1f, orbitRadius);
        OrbitUniversal orbitU = planet.GetComponent<OrbitUniversal>();

        OrbitPredictor op = TestSetupUtils.AddOrbitPredictor(planet, star);
        Assert.NotNull(op);

        double[] p_values = { 10, 100, 1000 };
        foreach (double p in p_values) {
            Debug.LogFormat("#### CirclePrediction p={0}", p);
            orbitU.p = p;
            TestSetupUtils.SetupGravityEngine(star, planet);
            op.TestRunnerSetup();

            OrbitUniversal predictedOrbit = op.GetOrbitUniversal();
            Assert.NotNull(predictedOrbit);
            CompareOrbits(orbitU, predictedOrbit, small);
        }
    }

    [Test]
    // Create an NBody and check it's mass
    public void EccentricityPrediction() {
        const float mass = 1000f;
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0, 0, 0));
        NBody starNbody = star.GetComponent<NBody>();
        const float orbitRadius = 10f;
        GameObject planet = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 1f, orbitRadius);
        OrbitUniversal orbitU = planet.GetComponent<OrbitUniversal>();

        OrbitPredictor op = TestSetupUtils.AddOrbitPredictor(planet, star);
        Assert.NotNull(op);

        double[] ecc_values = { 0, 0.1, 0.7, 0.9, 0.99, 1.01, 1.5, 2 };
        foreach (double ecc in ecc_values) {
            Debug.LogFormat("#### EccentricityPrediction ecc={0}", ecc);
            orbitU.eccentricity = ecc;
            TestSetupUtils.SetupGravityEngine(star, planet);
            op.TestRunnerSetup();

            OrbitUniversal predictedOrbit = op.GetOrbitUniversal();
            Assert.NotNull(predictedOrbit);
            CompareOrbits(orbitU, predictedOrbit, small);
        }
    }

    [Test]
    // Create an NBody and check it's mass
    public void InclinationPrediction() {
        const float mass = 1000f;
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0, 0, 0));
        NBody starNbody = star.GetComponent<NBody>();
        const float orbitRadius = 10f;
        GameObject planet = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 1f, orbitRadius);
        OrbitUniversal orbitU = planet.GetComponent<OrbitUniversal>();

        OrbitPredictor op = TestSetupUtils.AddOrbitPredictor(planet, star);
        Assert.NotNull(op);

        double[] incl_values = { 0, 1, 5, 10, 30, 45, 60, 90, 145, 179, 180 };
        foreach (double incl in incl_values) {
            Debug.LogFormat("#### InclinationPrediction incl={0}", incl);
            orbitU.inclination = incl;
            TestSetupUtils.SetupGravityEngine(star, planet);
            op.TestRunnerSetup();

            OrbitUniversal predictedOrbit = op.GetOrbitUniversal();
            Assert.NotNull(predictedOrbit);
            CompareOrbits(orbitU, predictedOrbit, small);
        }
    }

    [Test]
    // Create an NBody and check it's mass
    public void OmegaLPrediction() {
        const float mass = 1000f;
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0, 0, 0));
        NBody starNbody = star.GetComponent<NBody>();
        const float orbitRadius = 10f;
        GameObject planet = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 1f, orbitRadius);
        OrbitUniversal orbitU = planet.GetComponent<OrbitUniversal>();
        // Need a bit of ecc, otherwise omega comes back as phase. 
        orbitU.eccentricity = 0.1;

        OrbitPredictor op = TestSetupUtils.AddOrbitPredictor(planet, star);
        Assert.NotNull(op);

        double[] ol_values = { 0, 1, 5, 10, 30, 60, 90, 180, 270, 355, 360 };
        foreach (double ol in ol_values) {
            Debug.LogFormat("#### OmegaLPrediction ou={0}", ol);
            orbitU.omega_lc = ol;
            TestSetupUtils.SetupGravityEngine(star, planet);
            op.TestRunnerSetup();

            OrbitUniversal predictedOrbit = op.GetOrbitUniversal();
            Assert.NotNull(predictedOrbit);
            CompareOrbits(orbitU, predictedOrbit, small);
        }
    }

    [Test]
    // Create an NBody and check it's mass
    public void OmegaUCircularPrediction() {
        const float mass = 1000f;
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0, 0, 0));
        NBody starNbody = star.GetComponent<NBody>();
        const float orbitRadius = 10f;
        GameObject planet = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 1f, orbitRadius);
        OrbitUniversal orbitU = planet.GetComponent<OrbitUniversal>();
        // Need a bit of incl, otherwise omegaU comes back as omegaL. 
        orbitU.inclination = 10;

        OrbitPredictor op = TestSetupUtils.AddOrbitPredictor(planet, star);
        Assert.NotNull(op);

        double[] ou_values = { 0, 1, 5, 10, 30, 60, 90, 180, 270, 355, 360 };
        foreach (double ou in ou_values) {
            Debug.LogFormat("#### OmegaUPrediction ou={0}", ou);
            orbitU.omega_uc = ou;
            TestSetupUtils.SetupGravityEngine(star, planet);
            op.TestRunnerSetup();

            OrbitUniversal predictedOrbit = op.GetOrbitUniversal();
            Assert.NotNull(predictedOrbit);
            CompareOrbits(orbitU, predictedOrbit, small);
        }
    }

    [Test]
    // Create an NBody and check it's mass
    public void OmegaUEllipsePrediction() {
        const float mass = 1000f;
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0, 0, 0));
        NBody starNbody = star.GetComponent<NBody>();
        const float orbitRadius = 10f;
        GameObject planet = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 1f, orbitRadius);
        OrbitUniversal orbitU = planet.GetComponent<OrbitUniversal>();
        // Need a bit of incl, otherwise omegaU comes back as omegaL. 
        orbitU.inclination = 10;
        orbitU.eccentricity = 0.05;

        OrbitPredictor op = TestSetupUtils.AddOrbitPredictor(planet, star);
        Assert.NotNull(op);

        double[] ou_values = { 0, 1, 5, 10, 30, 60, 90, 180, 270, 355, 360 };
        foreach (double ou in ou_values) {
            Debug.LogFormat("#### OmegaUPrediction ou={0}", ou);
            orbitU.omega_uc = ou;
            TestSetupUtils.SetupGravityEngine(star, planet);
            op.TestRunnerSetup();

            OrbitUniversal predictedOrbit = op.GetOrbitUniversal();
            Assert.NotNull(predictedOrbit);
            CompareOrbits(orbitU, predictedOrbit, small);
        }
    }

    // TODO: center offset with drift velocity
    // TODO: Hyper and parabola tests
}
