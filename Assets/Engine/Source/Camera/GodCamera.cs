using UnityEngine;

public class GodCamera : MonoBehaviour
{
    public GameObject hud;
    [Range(8, 256)] public float height;
    public Vector2 rotationClamp;

    Vector3 rot, pos, moveDirection;
    float leftStickHorizontal, leftStickVertical;
    float rightStickHorizontal, rightStickVertical;
    float zoomSpeed;
    float scrollInput;

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
        zoomSpeed = height;

        // Gamepad zoom
        height = Mathf.Clamp(height + ((Input.GetAxis("Fire1") - Input.GetAxis("Fire2") * zoomSpeed * Time.deltaTime)), 8, 256);

        // Key Input
        if (Input.GetKey(KeyCode.W)) leftStickVertical = 1;
        if (Input.GetKey(KeyCode.S)) leftStickVertical = -1;
        if (Input.GetKey(KeyCode.A)) leftStickHorizontal = -1;
        if (Input.GetKey(KeyCode.D)) leftStickHorizontal = 1;
        if (Input.GetKey(KeyCode.Q)) height -= zoomSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) height += zoomSpeed * Time.deltaTime;

        // Mouse scroll zoom
        scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput > 0f) height -= zoomSpeed * Time.deltaTime * scrollInput * 50f;
        else if (scrollInput < 0f) height -= zoomSpeed * Time.deltaTime * scrollInput * 50f;
        height = Mathf.Clamp(height, 8, 256);

        // Move and rotate camera
        rot = transform.eulerAngles;
        pos = transform.position;
        if (leftStickVertical != 0)
        {
            moveDirection = transform.forward;
            moveDirection.y = 0.0f;
            moveDirection = Vector3.Normalize(moveDirection);
            pos += moveDirection * Time.deltaTime * leftStickVertical * zoomSpeed;
            pos.y = height;
        }
        pos.y = height;
        transform.position = pos;

        if (leftStickHorizontal != 0) transform.position += transform.right * Time.deltaTime * (leftStickHorizontal * zoomSpeed);
        if (rightStickHorizontal != 0) rot.y += rightStickHorizontal * Time.deltaTime * 128f;
        if (rightStickVertical != 0) rot.x += rightStickVertical * Time.deltaTime * 128f;
        if (rot.x > rotationClamp.y) rot.x = rotationClamp.y;
        if (rot.x < rotationClamp.x) rot.x = rotationClamp.x;
        transform.eulerAngles = rot;
    }
}
