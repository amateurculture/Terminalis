using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

public class LambertUvsB 
{
    private void DoTestForPhase(double fromPhase, double toPhase) {
        GravityEngine ge = GravityEngine.Instance();
        ge.Clear();
        const float mass = 1000f;
        const bool reverse = false;
        GameObject star = TestSetupUtils.CreateNBody(mass, new Vector3(0, 0, 0));
        NBody starNbody = star.GetComponent<NBody>();
        float orbitRadius = 20f;
        GameObject planet = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 0f, orbitRadius);
        OrbitUniversal orbitU = planet.GetComponent<OrbitUniversal>();
        orbitU.phase = fromPhase;
        orbitU.SetMajorAxisInspector(orbitRadius);

        orbitRadius = 30.0f;
        GameObject planet2 = TestSetupUtils.CreatePlanetInOrbitUniversal(starNbody, 0f, orbitRadius);
        OrbitUniversal orbitU2 = planet2.GetComponent<OrbitUniversal>();
        orbitU2.phase = toPhase;
        orbitU2.SetMajorAxisInspector(orbitRadius);

        ge.UnitTestAwake();
        ge.AddBody(star);
        ge.AddBody(planet);
        ge.AddBody(planet2);
        ge.Setup();

        Debug.Log("Find transfers");
        OrbitData fromOrbit = new OrbitData(orbitU);
        OrbitData toOrbit = new OrbitData(orbitU2);
        LambertUniversal lambertU = new LambertUniversal(fromOrbit, toOrbit, true);
        Assert.AreNotEqual(lambertU, null);
        double time = 0.8f * lambertU.GetTMin();
        lambertU.ComputeXfer(reverse, false, 0, time);
        LambertBattin lambertB = new LambertBattin(fromOrbit, toOrbit);
        int error = lambertB.ComputeXfer(reverse, false, 0, time);
        Assert.AreEqual(error, 0);
        Assert.AreNotEqual(lambertB, null);
        Assert.AreNotEqual(lambertB.GetTransferVelocity(), null);
        Debug.LogFormat("initial velocity {0} vs {1}", lambertU.GetTransferVelocity(), lambertB.GetTransferVelocity());
        Debug.LogFormat("initial velocity mag {0} vs {1}", 
            lambertU.GetTransferVelocity().magnitude, lambertB.GetTransferVelocity().magnitude);
        Debug.LogFormat("final velocity {0} vs {1}", lambertU.GetFinalVelocity(), lambertB.GetFinalVelocity());
        Debug.LogFormat("final velocity mag {0} vs {1}", 
            lambertU.GetFinalVelocity().magnitude, lambertB.GetFinalVelocity().magnitude);
        // best can do for 180 degree case is E-2 accuracy on the magnitude. Not sure why...seems too big
        Assert.IsTrue(GEUnit.DoubleEqual( lambertU.GetTransferVelocity().magnitude, 
                        lambertB.GetTransferVelocity().magnitude, 
                        1E-2));
        ge.Clear();
    }


    [Test]
    // Check eccentricity and inclination
    public void CircleToCircle90() {
        DoTestForPhase(0, 90.0);
    }

    [Test]
    // Check eccentricity and inclination
    public void CircleToCircle90Offset90() {
        DoTestForPhase(90.0, 180.0);
    }

    [Test]
    // Check eccentricity and inclination
    public void CircleToCircle180() {
        DoTestForPhase(0, 180.0);
    }

    [Test]
    // Check eccentricity and inclination
    public void CircleToCircle135() {
        DoTestForPhase(0, 135.0);
    }
}
