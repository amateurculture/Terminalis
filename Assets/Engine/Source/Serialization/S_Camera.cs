using UnityEngine;
using System;

[Serializable]
public class S_Camera
{
    public string name;
    public float _distance;
    public S_Vector3 position;
    public S_Vector3 eulerRotation;
    public S_Vector3 _position;
    public S_Vector3 _rotation;

    /*
    public S_Camera(BzFreeLookCam cam)
    {
        position = new S_Vector3(cam.transform.position);
        eulerRotation = new S_Vector3(cam._cameraPivot.eulerAngles);

        eulerRotation.y = (eulerRotation.y) % 180;
        if (eulerRotation.y < 0)
            eulerRotation.y += 180;
        
        _distance = cam._distance;
    }

    public void Deserialize(BzFreeLookCam bz)
    {
        bz.enabled = false;

        bz.transform.position = new Vector3(position.x, position.y, position.z);

        var pivot = bz.transform.Find("Pivot");
        pivot.Rotate(new Vector3(eulerRotation.x, eulerRotation.y, eulerRotation.z));

        bz._distance = _distance;

        //bz.enabled = true;
    }
    */
}
