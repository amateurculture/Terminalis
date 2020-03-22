using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GEMath  {

    /// <summary>
    /// Cotangent function. 
    /// 
    /// Adapted from Vallado's astmath CPP functions. 
    /// 
    /// </summary>
    public static double Cot(double x) {
        double temp = Mathd.Tan(x);
        if (Mathd.Abs(temp) < 1E-8) {
            return double.NaN;
        } else {
            return 1.0 / temp;
        }
    }

    public static double Acosh(double x) {
        return Mathd.Log(x + Mathd.Sqrt(x * x - 1));
    }

    public static double Asinh(double x) {
        return Mathd.Log(x + Mathd.Sqrt(x * x + 1));
    }

    public static double Sinh(double x) {
        return 0.5*(Mathd.Exp(x) - Mathd.Exp(-x));
    }

    public static double Cosh(double x) {
        return 0.5 * (Mathd.Exp(x) + Mathd.Exp(-x));
    }

    /// <summary>
    /// Return the rotation of the vector v by angleRad radians around the x axis.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="angleRad"></param>
    /// <returns></returns>
    public static Vector3d Rot1(Vector3d v, double angleRad) {
        double c = Mathd.Cos(angleRad);
        double s = Mathd.Sin(angleRad);
        return new Vector3d(v.x, c * v.y + s * v.z, c * v.z - s * v.y);
    }

    /// <summary>
    /// Return the rotation of the vector v by angleRad radians around the z axis.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="angleRad"></param>
    /// <returns></returns>
    public static Vector3d Rot3(Vector3d v, double angleRad) {
        double c = Mathd.Cos(angleRad);
        double s = Mathd.Sin(angleRad);
        return new Vector3d(c * v.x + s * v.y, c * v.y - s * v.x, v.z);
    }
}
