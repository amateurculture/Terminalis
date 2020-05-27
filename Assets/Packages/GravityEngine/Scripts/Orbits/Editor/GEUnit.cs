using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GEUnit  {

    public const float FLOAT_ERR = 1E-5f;

    public static bool FloatEqual(float a, float b) {
        return FloatEqual(a, b, FLOAT_ERR);
    }

    public static bool FloatEqual(float a, float b, double error) {
        return (Mathf.Abs(a - b) < error);
    }

    public static bool DoubleEqual(double a, double b, double error) {
        return (Mathd.Abs(a - b) < error);
    }

    public static bool Vec3dEqual(Vector3d a, Vector3d b, double error) {
        return DoubleEqual(a.x, b.x, error) &&
                DoubleEqual(a.y, b.y, error) && 
                DoubleEqual(a.z, b.z, error);
    }
}
