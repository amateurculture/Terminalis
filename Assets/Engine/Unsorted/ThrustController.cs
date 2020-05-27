using UnityEngine;
using UnityEngine.UI;

public class ThrustController : MonoBehaviour
{
    public Transform planet;
    public Text speed;

    int speedSetting;
    int previousSpeed;
    float actualSpeed;

    private void Start()
    {
        speed.text = (speedSetting <= 0) ? "Thrusters" : "Warp " + speedSetting + " (C x " + (speedSetting * speedSetting) + ")";
        previousSpeed = speedSetting;
        actualSpeed = speedSetting * speedSetting;
    }

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

        if (Time.frameCount % 10 == 0 && DH != 0)
        {
            speedSetting += Mathf.RoundToInt(DH);
            if (speedSetting != previousSpeed)
            {
                speed.text = (speedSetting <= 0) ? "Thrusters" : "Warp " + speedSetting + " (C x " + (speedSetting * speedSetting) + ")";
                previousSpeed = speedSetting;
                actualSpeed = speedSetting * speedSetting;
            }
        }

        if (speedSetting <= 0)
        {
            speedSetting = 0;
            // todo use this section for handling thrust vectoring instead of sublight/light engines
        }
        else
        {
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

                Vector3 temp = transform.position;

                // strafe up and down
                temp += transform.up * LSV * Time.deltaTime * .025f;

                // strafe horizontal
                temp += transform.right * LSH * Time.deltaTime * .025f;

                // move forward impulse engines (1x light)
                temp += transform.forward * RT * Time.deltaTime * .025f * actualSpeed;

                // move backward impulse engines (1x light)
                temp += transform.forward * -LT * Time.deltaTime * .025f * actualSpeed;

                // move forward or backward at 100x speed of light (~warp 4)
                //temp += transform.forward * DV * Time.deltaTime * .025f * speedSetting;

                transform.position = temp;

                /*
                if (Time.frameCount % 5 == 0)
                {
                    var dist = Vector3.SqrMagnitude(planet.position - temp);
                    if (dist > (.05f * .05f)) transform.position = temp;
                }
                */
            }
        }
    }
}
