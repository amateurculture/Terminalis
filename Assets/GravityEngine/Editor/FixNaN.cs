using UnityEngine;
using System.Collections;

public class FixNaN  {

	public static Vector3 FixIfNaN(Vector3 v)
    {
        if (float.IsNaN(v.x))
        {
            v.x = 0;
        }
        if (float.IsNaN(v.y))
        {
            v.y = 0;
        }
        if (float.IsNaN(v.z))
        {
            v.z = 0;
        }
        return v;
    }

	public static float FixIfNaN(float x)
    {
        if (float.IsNaN(x))
        {
            x = 0;
        }
        return x;
    }
}
