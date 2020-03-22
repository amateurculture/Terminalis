using UnityEngine;
using System.Collections;

/// <summary>
/// Custom force.
/// Sample code to show how to make a custom force. To use this
/// set the GE force delegate to custom and attach this script
/// to the object holding the GravityEngine
/// </summary>
public class CustomForce : MonoBehaviour, IForceDelegate  {

	public float a = 2.0f;
	public float b = 1.0f;

	/// <summary>
	/// acceleration = a * ln(b * r)
	/// </summary>
	/// <returns>The accel.</returns>
	/// <param name="r_sep">R sep. The distance between the bodies</param>
    /// <param name="i">index of one pair in force (i < j)</param>
    /// <param name="j">index of second pair in force</param>
	public double CalcPseudoForce(double r_sep, int i, int j) {

		return a*System.Math.Log(b*r_sep);
	}

    public double CalcPseudoForceMassless(double r_sep, int i, int j) {

        return a * System.Math.Log(b * r_sep);
    }

    public double CalcPseudoForceDot(double r_sep, int i, int j) {
		return a * b/r_sep;
	}

}
