using UnityEngine;
using System.Collections;
using System;

public class NUtils  {

	private const int NDIM = 3; 

	/// <summary>
	/// Calculate the energy of the system
	/// Used internally by GravityEngine. Use GravityEngine.GetEnergy() from developer scripts. 
	/// </summary>
	/// <returns>The energy.</returns>
	/// <param name="numBodies">Number bodies.</param>
	/// <param name="m">M.</param>
	/// <param name="r">The red component.</param>
	/// <param name="v">V.</param>
	public static double GetEnergy(int numBodies, ref double[] m, ref double[,] r, ref double[,] v) {
		// If any bodies have gone from active -> inactive this won't be meaningful
		double epot = 0.0; 
		double ekin = 0.0; 
		double[] rji = new double[NDIM]; 
		double r2; 
		for (int i=0; i < numBodies; i++) {
			for (int j=i+1; j < numBodies; j++) {
				for (int k=0; k < NDIM; k++) {
					rji[k] = r[j,k] - r[i,k];
				}
				r2 = 0; 
				for (int k=0; k < NDIM; k++) {
					r2 += rji[k] * rji[k]; 
				}
				epot -= m[i] * m[j]/System.Math.Sqrt(r2);
			}
			for (int k=0; k < NDIM; k++) {
				ekin += 0.5 * m[i] * v[i,k]*v[i,k];
			}
		}	
		return ekin + epot; 	
	}

	public static float GaussianValue(float mean, float stdDev) {
		// Box-Mueller from StackOverflow
		//Random rand = new Random(); //reuse this if you are generating many
		float u1 = UnityEngine.Random.value; //these are uniform(0,1) random doubles
		float u2 = UnityEngine.Random.value;
		float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) *
             Mathf.Sin(2.0f * Mathf.PI * u2); //random normal(0,1)
		float randNormal =
             mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)
        return randNormal;
	}

	/// <summary>
	/// Degress modules 360f (angle in 0..360)
	/// </summary>
	/// <returns>The mod360.</returns>
	/// <param name="angle">Angle.</param>
	public static float DegreesMod360(float angle) {
        float newAngle = angle % 360.0f;
        if (newAngle < 0) {
            newAngle += 360.0f;
        }
        return newAngle;
	}

    /// <summary>
    /// Normalize degrees to +/- 180. Typically used for orbital inclination.
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static float DegreesPM180(float angle) {
        float newAngle = angle;
        if (angle > 180.0f) {
            newAngle = angle - 360.0f;
        } else if (angle < -180.0f) {
            newAngle = angle + 360.0f;
        }
        return newAngle;
    }

    /// <summary>
    /// Ensure angle in radians is in (0, 2 Pi)
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static double AngleMod2Pi(double angle) {
        double modAngle = angle % (2.0 * Mathd.PI);
        if (angle < 0) {
            modAngle += 2*System.Math.PI;
        } 
        return modAngle;
    } 

    /// <summary>
    /// Check if the vector has any NaN components
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
	public static bool VectorNaN(Vector3 v) {
		return System.Single.IsNaN(v.x) ||
			System.Single.IsNaN(v.y) ||
			System.Single.IsNaN(v.z);
	}

    // Determine angle in radians between the vectors and express in (0, 2 Pi) 
    internal static float AngleFullCircleRadians(Vector3 from, Vector3 to, Vector3 normal) {
        float angle = Vector3.Angle(from, to) * Mathf.Deg2Rad;
        // check cross product wrt to normal
        Vector3 fromXto = Vector3.Cross(from, to).normalized;
        float crossCheck = Vector3.Dot(fromXto, normal.normalized);
        if (crossCheck < 0) {
            angle = 2f * Mathf.PI - angle;
        }
        return angle;
    }

    /// <summary>
    /// Find counter-clockwise angle between from and to. 
    /// e.g. 359 to 1 = 2 
    ///      1 to 359 = 358
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public static double AngleDeltaDegrees(double from, double to) {
        if (from > to) {
            return (Mathd.Abs(360-from) + to);
        } else {
            return to - from; 
        }
    }

    public static double AngleDeltaRadians(double from, double to) {
        if (from > to) {
            return (Mathd.Abs(2.0*Mathd.PI - from) + to);
        } else {
            return to - from;
        }
    }

    public static bool FloatEqualMod360(float a_, float b_, double error) {
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
}
