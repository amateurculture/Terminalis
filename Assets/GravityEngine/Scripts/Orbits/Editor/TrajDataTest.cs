using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using NUnit.Framework;

public class TrajDataTest {

	private static bool FloatEqual(float a, float b) {
		return (Mathf.Abs(a-b) < 1E-3); 
	}

	private static bool FloatEqual(float a, float b, double error) {
		return (Mathf.Abs(a-b) < error); 
	}


	[Test]
    public void Basic()
    {
    	TrajectoryData tdata = new TrajectoryData();
    	for (float x=0; x< 10f; x++) {
    		Vector3 r = new Vector3(x, 0, 0);
    		tdata.AddPoint(r, Vector3.zero, x);
    	}
    	// check each entry is less than next

    	for (int i=0; i < tdata.Count()-1; i++) {
    		TrajectoryData.Tpoint tp1 = tdata.GetByIndex(i);
			TrajectoryData.Tpoint tp2 = tdata.GetByIndex(i+1);
			Assert.IsTrue( tp1.r.x < tp2.r.x );
    	}
    }

	[Test]
    public void LineIntersection2DXY()
    {
		Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
    	float slope1 = 2f;
    	float slope2 = 0.5f;
    	TrajectoryData line1 = new TrajectoryData();
		TrajectoryData line2 = new TrajectoryData();
    	for (float x=-20f; x < 20f; x++) {
    		Vector3 r = new Vector3(x, slope1*x , 0);
    		line1.AddPoint(r, Vector3.zero, x);
			r = new Vector3(x, slope2*x , 0);
    		line2.AddPoint(r, Vector3.zero, x);
    	}
    	// debug
//    	Debug.Log("points=" + line1.Count());
//    	for (int i=0; i < line1.Count(); i++) {
//    		TrajectoryData.Tpoint tp = line1.GetByIndex(i);
//    		Debug.Log(string.Format("{0} r={1}", i, tp.r));
//    	}
    	// check the lines intersect at the origin
    	List<TrajectoryData.Intercept> intercepts = new List<TrajectoryData.Intercept>();
    	intercepts = line1.GetIntercepts(line2, 0.1f, 1f);
    	Assert.IsTrue( intercepts.Count == 1);
     }

	[Test]
    public void LineIntersection2DZY()
    {
		Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
    	float slope1 = 2f;
    	float slope2 = 0.5f;
    	TrajectoryData line1 = new TrajectoryData();
		TrajectoryData line2 = new TrajectoryData();
    	for (float x=-20f; x < 20f; x++) {
    		Vector3 r = new Vector3(0, slope1*x , x);
    		line1.AddPoint(r, Vector3.zero, x);
			r = new Vector3(0, slope2*x , x);
    		line2.AddPoint(r, Vector3.zero, x);
    	}
    	// check the lines intersect at the origin
    	List<TrajectoryData.Intercept> intercepts = new List<TrajectoryData.Intercept>();
    	intercepts = line1.GetIntercepts(line2, 0.1f, 1f);
    	Assert.IsTrue( intercepts.Count == 1);
     }

	[Test]
    public void CirclesXY()
    {
    	// Coarse theta stepping, so only two matches
		Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
    	float dtheta = 0.05f;
    	float radius = 10f;
    	float offset = radius/3f;
    	TrajectoryData c1 = new TrajectoryData();
		TrajectoryData c2 = new TrajectoryData();
		Debug.Log(string.Format("r={0} offset={1} dtheta={2}", radius, offset, dtheta));
    	for (float theta=0f; theta < 2f*Mathf.PI; theta += dtheta) {
    		Vector3 r = new Vector3(radius*Mathf.Cos(theta) + offset, radius*Mathf.Sin(theta), 0);
    		c1.AddPoint(r, Vector3.zero, theta);
			r = new Vector3(radius*Mathf.Cos(theta) - offset, radius*Mathf.Sin(theta), 0);
    		c2.AddPoint(r, Vector3.zero, theta);
    	}
    	// check the lines intersect at the origin
    	List<TrajectoryData.Intercept> intercepts = new List<TrajectoryData.Intercept>();
    	intercepts = c1.GetIntercepts(c2, 0.3f, 1f);
		Debug.Log("Num intercepts=" + intercepts.Count);
		foreach (TrajectoryData.Intercept intercept in intercepts) {
			Debug.Log(string.Format("r_i={0} r_j={1}", intercept.tp1.r, intercept.tp2.r));
		}
    	Assert.IsTrue( intercepts.Count == 2);
     }

	[Test]
    public void CirclesYZ()
    {
    	// Coarse theta stepping, so only two matches
		Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
    	float dtheta = 0.05f;
    	float radius = 10f;
    	float offset = radius/3f;
    	TrajectoryData c1 = new TrajectoryData();
		TrajectoryData c2 = new TrajectoryData();
		Debug.Log(string.Format("r={0} offset={1} dtheta={2}", radius, offset, dtheta));
    	for (float theta=0f; theta < 2f*Mathf.PI; theta += dtheta) {
    		Vector3 r = new Vector3(0, radius*Mathf.Cos(theta) + offset, radius*Mathf.Sin(theta));
    		c1.AddPoint(r, Vector3.zero, theta);
			r = new Vector3(0, radius*Mathf.Cos(theta) - offset, radius*Mathf.Sin(theta));
    		c2.AddPoint(r, Vector3.zero, theta);
    	}
    	// check the lines intersect at the origin
    	List<TrajectoryData.Intercept> intercepts = new List<TrajectoryData.Intercept>();
    	intercepts = c1.GetIntercepts(c2, 0.3f, 1f);
		Debug.Log("Num intercepts=" + intercepts.Count);
		foreach (TrajectoryData.Intercept intercept in intercepts) {
			Debug.Log(string.Format("r_i={0} r_j={1}", intercept.tp1.r, intercept.tp2.r));
		}
    	Assert.IsTrue( intercepts.Count == 2);
     }

	[Test]
    public void CirclesXYManyIntercepts()
    {
    	// Fine grained theta. intersection yields many points, bust should be filtered down
    	// to the two best
		Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
    	float dtheta = 0.01f;
    	float radius = 10f;
    	float offset = radius/3f;
    	TrajectoryData c1 = new TrajectoryData();
		TrajectoryData c2 = new TrajectoryData();
		Debug.Log(string.Format("r={0} offset={1} dtheta={2}", radius, offset, dtheta));
    	for (float theta=0f; theta < 2f*Mathf.PI; theta += dtheta) {
    		Vector3 r = new Vector3(radius*Mathf.Cos(theta) + offset, radius*Mathf.Sin(theta), 0);
    		c1.AddPoint(r, Vector3.zero, theta);
			r = new Vector3(radius*Mathf.Cos(theta) - offset, radius*Mathf.Sin(theta), 0);
    		c2.AddPoint(r, Vector3.zero, theta);
    	}
    	// check the lines intersect at the origin
    	List<TrajectoryData.Intercept> intercepts = new List<TrajectoryData.Intercept>();
    	intercepts = c1.GetIntercepts(c2, 0.2f, 1f);
		Debug.Log("Num intercepts=" + intercepts.Count);
		foreach (TrajectoryData.Intercept intercept in intercepts) {
			Debug.Log(string.Format("r_i={0} r_j={1}", intercept.tp1.r, intercept.tp2.r));
		}
    	Assert.IsTrue( intercepts.Count == 2);
     }

	[Test]
    public void CirclesYZManyIntercepts()
    {
    	// Fine grained theta. intersection yields many points, bust should be filtered down
    	// to the two best
		Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
    	float dtheta = 0.01f;
    	float radius = 10f;
    	float offset = radius/3f;
    	TrajectoryData c1 = new TrajectoryData();
		TrajectoryData c2 = new TrajectoryData();
		Debug.Log(string.Format("r={0} offset={1} dtheta={2}", radius, offset, dtheta));
    	for (float theta=0f; theta < 2f*Mathf.PI; theta += dtheta) {
    		Vector3 r = new Vector3(0, radius*Mathf.Cos(theta) + offset, radius*Mathf.Sin(theta));
    		c1.AddPoint(r, Vector3.zero, theta);
			r = new Vector3(0, radius*Mathf.Cos(theta) - offset, radius*Mathf.Sin(theta));
    		c2.AddPoint(r, Vector3.zero, theta);
    	}
    	// check the lines intersect at the origin
    	List<TrajectoryData.Intercept> intercepts = new List<TrajectoryData.Intercept>();
    	intercepts = c1.GetIntercepts(c2, 0.2f, 1f);
		Debug.Log("Num intercepts=" + intercepts.Count);
		foreach (TrajectoryData.Intercept intercept in intercepts) {
			Debug.Log(string.Format("r_i={0} r_j={1}", intercept.tp1.r, intercept.tp2.r));
		}
    	Assert.IsTrue( intercepts.Count == 2);
     }

}
