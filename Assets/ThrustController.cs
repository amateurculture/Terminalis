using UnityEngine;

public class ThrustController : MonoBehaviour
{
    void LateUpdate()
    {
        var LSH = Input.GetAxis("Horizontal"); // left stick horizontal
        var LSV = Input.GetAxis("Vertical"); // left stick vertical
        var RSH = Input.GetAxis("RightStickHorizontal"); // right stick horizontal
        var RSV = Input.GetAxis("RightStickVertical"); // right stick vertical
        var RTB = Input.GetButton("Equip Next Item"); // right trigger button
        var LTB = Input.GetButton("Equip Previous Item"); // left trigger button
        var RT = Input.GetAxis("Fire1"); // right trigger
        var LT = Input.GetAxis("Fire2"); // left trigger
        var DV = Input.GetAxis("DpadVertical");
        var DH = Input.GetAxis("DpadHorizontal");

        // Impulse Engines (Keyboard)
        if (Input.GetKey(KeyCode.RightBracket))
            transform.position += transform.forward * Time.deltaTime * .25f;
        else if (Input.GetKey(KeyCode.LeftBracket))
            transform.position += transform.forward * Time.deltaTime * -.25f;
        else if (Input.GetKey(KeyCode.RightArrow))
            transform.rotation *= Quaternion.AngleAxis(2, Vector3.up);
        else if (Input.GetKey(KeyCode.LeftArrow))
            transform.rotation *= Quaternion.AngleAxis(-2, Vector3.up);
        else if (Input.GetKey(KeyCode.UpArrow))
            transform.rotation *= Quaternion.AngleAxis(2, Vector3.right);
        else if (Input.GetKey(KeyCode.DownArrow))
            transform.rotation *= Quaternion.AngleAxis(-2, Vector3.right);
        else if (Input.GetKey(KeyCode.E))
            transform.rotation *= Quaternion.AngleAxis(-.5f, Vector3.forward);
        else if (Input.GetKey(KeyCode.Q))
            transform.rotation *= Quaternion.AngleAxis(.5f, Vector3.forward);

        // Impulse Engines (Controller)
        else if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
        {
            // rotate z axis
            transform.rotation *= Quaternion.AngleAxis((RTB == true) ? .5f : (LTB == true) ? -.5f : 0, Vector3.forward);

            // turn vertical (not strafe)
            transform.rotation *= Quaternion.AngleAxis(RSV * 2, Vector3.up);

            // turn horizontal (not strafe)
            transform.rotation *= Quaternion.AngleAxis(-RSH * 2, Vector3.right);

            // strafe up and down
            transform.position += transform.up * LSV * Time.deltaTime * .025f;

            // strafe horizontal
            transform.position += transform.right * LSH * Time.deltaTime * .025f;

            // move forward impulse engines (1x light)
            transform.position += transform.forward * RT * Time.deltaTime * .025f;

            // move backward impulse engines (1x light)
            transform.position += transform.forward * -LT * Time.deltaTime * .025f;

            // move forward or backward at 200x speed of light (warp 5)
            transform.position += transform.forward * DV * Time.deltaTime * .025f * 200f;  
        }
    }
}
