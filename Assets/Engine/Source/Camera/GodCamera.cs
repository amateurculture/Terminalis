using UnityEngine;

/// <summary>
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

public class GodCamera : MonoBehaviour
{
    public GameObject hud;
    [Range(8, 256)] public float height;
    public Vector2 rotationClamp;

    Vector3 rot, pos, moveDirection;
    float leftStickHorizontal, leftStickVertical;
    float rightStickHorizontal, rightStickVertical;
    float mouseWheel;
    float zoomSpeed;
    float scrollSpeed;
    float scrollInput;
    float rotationRate;
    float scrollValue;

    private void Reset()
    {
        height = 128f;
        rotationClamp = new Vector2(25, 85);
    }

    void Update()
    {
        // Gamepad input
        leftStickHorizontal = Input.GetAxis("Horizontal");
        leftStickVertical = Input.GetAxis("Vertical");
        rightStickHorizontal = Input.GetAxis("Mouse X");
        rightStickVertical = Input.GetAxis("Mouse Y");
        scrollInput = Input.GetAxis("Fire1") - Input.GetAxis("Fire2");

        // Mouse Input
        mouseWheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(mouseWheel) > 0) scrollInput = mouseWheel;

        // Key Input
        if (Input.GetKey(KeyCode.W)) leftStickVertical = 1;
        if (Input.GetKey(KeyCode.S)) leftStickVertical = -1;
        if (Input.GetKey(KeyCode.A)) leftStickHorizontal = -1;
        if (Input.GetKey(KeyCode.D)) leftStickHorizontal = 1;
        if (Input.GetKey(KeyCode.Q)) scrollInput = -1;
        if (Input.GetKey(KeyCode.E)) scrollInput = 1;

        // Pre-Calculate Values
        zoomSpeed = scrollSpeed = rotationRate = 0;

        // Move camera
        if (Mathf.Abs(leftStickHorizontal) > 0 || Mathf.Abs(leftStickVertical) > 0) {
            zoomSpeed = height * Time.deltaTime;
            pos = transform.position;

            if (leftStickVertical != 0)
            {
                moveDirection = transform.forward;
                moveDirection.y = 0.0f;
                moveDirection = Vector3.Normalize(moveDirection);
                pos += moveDirection * leftStickVertical * zoomSpeed;
                transform.position = pos;
            }

            if (leftStickHorizontal != 0) 
                transform.position += transform.right * leftStickHorizontal * zoomSpeed;
        }

        // Zoom Camera
        if (scrollInput != 0)
        {
            pos = transform.position;
            scrollSpeed = height * Time.deltaTime;
            scrollValue = scrollSpeed * scrollInput;
            height = Mathf.Clamp(height + scrollValue, 8, 256);
            pos.y = height;
            transform.position = pos;
        }

        // Rotate Camera
        if (rightStickHorizontal != 0 || rightStickVertical != 0)
        {
            rotationRate = Time.deltaTime * 128f;
            rot = transform.eulerAngles;
            if (rightStickHorizontal != 0) rot.y += rightStickHorizontal * rotationRate;
            if (rightStickVertical != 0) rot.x += rightStickVertical * rotationRate;
            if (rot.x > rotationClamp.y) rot.x = rotationClamp.y;
            if (rot.x < rotationClamp.x) rot.x = rotationClamp.x;
            transform.eulerAngles = rot;
        }
    }
}
