using UnityEngine;

[System.Serializable]
public class S_Quaternion
{
    public float x;
    public float y;
    public float z;
    public float w;

    public S_Quaternion(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    public S_Quaternion(Quaternion rotation)
    {
        x = rotation.x;
        y = rotation.y;
        z = rotation.z;
        w = rotation.w;
    }
}
