using UnityEngine;

/*** 
 * Class Affect
 * 
 * Developer: Fiona Schultz
 * Last modified: Oct-12-2019
 * 
 * This class extends available button interactions. To use, place this on the object you wish your button to affect, then wire it up like you would any other interaction.
 *
 */ 

public class Affect : MonoBehaviour
{
    #region Transform Affectors

    public void ZeroPosition()
    {
        transform.position = Vector3.zero;
    }

    public void ZeroLocalPosition()
    {
        transform.localPosition = Vector3.zero;
    }

    public void X(float value)
    {
        Vector3 foo = transform.position;
        foo.x = value;
        transform.position = foo;
    }

    public void Y(float value)
    {
        Vector3 foo = transform.position;
        foo.y = value;
        transform.position = foo;
    }

    public void Z(float value)
    {
        Vector3 foo = transform.position;
        foo.z = value;
        transform.position = foo;
    }
    
    public void LocalX(float value)
    {
        Vector3 foo = transform.position;
        foo.x = value;
        transform.localPosition = foo;
    }

    public void LocalY(float value)
    {
        Vector3 foo = transform.position;
        foo.y = value;
        transform.localPosition = foo;
    }

    public void LocalZ(float value)
    {
        Vector3 foo = transform.position;
        foo.z = value;
        transform.localPosition = foo;
    }

    #endregion
}
