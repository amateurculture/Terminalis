using UnityEngine;
using UnityEditor;
using NUnit.Framework;

/// <summary>
/// Test the conversion of times from GE to epoch to C# DateTime
/// </summary>
public class SolarTest {

	private static bool FloatEqual(float a, float b) {
		return (Mathf.Abs(a-b) < 1E-3); 
	}

	private static bool FloatEqual(float a, float b, double error) {
		return (Mathf.Abs(a-b) < error); 
	}

     [Test]
    // Create an NBody and check it's mass
    public void FindSolar()
    {
        SolarSystem solar = GameObject.Find("SolarSystem").GetComponent<SolarSystem>();
        Assert.That(solar != null, "Could not get Solar system. Did you open SolarUnitTest scene?");
        //Assert.AreEqual(nbody.mass, nbody.mass);
    }


    [Test]
    // Check can go through epoch time and still get a sensible DateTime (same day)
    public void DTtoEpochtoDT() {
        SolarSystem solar = GameObject.Find("SolarSystem").GetComponent<SolarSystem>();
        Assert.That(solar != null, "Could not get Solar system. Did you open SolarUnitTest scene?");

        System.DateTime[] times = { new System.DateTime(2000, 1, 1),
                                    new System.DateTime(1963, 6, 15) };
        foreach (System.DateTime t in times) {
            float epoch = SolarUtils.DateTimeToEpoch(t);
            System.DateTime newTime = SolarUtils.DateForEpoch(epoch);
            Assert.AreEqual(t.Year, newTime.Year);
            Assert.AreEqual(t.Month, newTime.Month);
            Assert.AreEqual(t.Day, newTime.Day);
        }
    }

    [Test]
    // Check can go through phys time and still get a sensible DateTime (same day)
    public void SolarTimeNow() {
        SolarSystem solar = GameObject.Find("SolarSystem").GetComponent<SolarSystem>();
        Assert.That(solar != null, "Could not get Solar system. Did you open SolarUnitTest scene?");

        System.DateTime newTime = SolarUtils.DateForEpoch( solar.GetStartEpochTime());
        newTime += GravityScaler.GetTimeSpan(GravityEngine.Instance().GetPhysicalTimeDouble(), GravityScaler.Units.SOLAR);
        Assert.AreEqual(newTime.Year, 2016);
        Assert.AreEqual(newTime.Month, 1);
        Assert.AreEqual(newTime.Day, 1);
    }

}
