using UnityEngine;

[System.Serializable]
public class S_Vector3
{
    public float x;
    public float y;
    public float z;

    public S_Vector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public S_Vector3(Vector3 position)
    {
        x = position.x;
        y = position.y;
        z = position.z;
    }
}
