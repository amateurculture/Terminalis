using UnityEngine;
using UnityEditor;
using NUnit.Framework;

public class NutilsTest {


	[Test]
    public void Mod360()
    {
    	// check each quadrant
		float[] angles = { -90f, 20f, 355f, 270f+2*360f, 14f*360f+5f};
		float[] answer = { 270f, 20f, 355f, 270f, 5f};
        for (int i=0; i < angles.Length; i++) {
			float a = NUtils.DegreesMod360( angles[i]);
			Debug.Log( "angle=" + angles[i] + " a=" + a);
			Assert.IsTrue( Mathf.Abs(a - answer[i]) < 1E-2);
        }
    }

    [Test]
    public void AngleDelta() {
        Assert.AreEqual(NUtils.AngleDeltaDegrees(1.0, 359.0), 358.0);
        Assert.AreEqual(NUtils.AngleDeltaDegrees(359.0, 1.0), 2.0);
        Assert.AreEqual(NUtils.AngleDeltaDegrees(1.0, 3.0), 2.0);
        Assert.AreEqual(NUtils.AngleDeltaDegrees(357.0, 359.0), 2.0);
        Assert.AreEqual(NUtils.AngleDeltaDegrees(179.0, 181.0), 2.0);
        Assert.AreEqual(NUtils.AngleDeltaDegrees(181.0, 179.0), 358.0);
        Assert.AreEqual(NUtils.AngleDeltaDegrees(20.0, 0.0), 340.0);

    }
}
