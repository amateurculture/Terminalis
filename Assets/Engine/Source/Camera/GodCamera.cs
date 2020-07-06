using Opsive.UltimateCharacterController.Camera;
using Opsive.UltimateCharacterController.Character;
using UnityEngine;

public class GodCamera : MonoBehaviour
{
    public GameObject hud;
    [Range(64, 256)] public float height = 128f;
    public Vector2 rotationClamp = new Vector2(35, 85);

    Camera cam;
    GameObject player;
    UltimateCharacterLocomotion loco;
    bool godCamEnabled;
    bool isOrbitCam;
    Vector3 rot, pos, moveDirection;
    float leftStickHorizontal, leftStickVertical;
    float rightStickHorizontal, rightStickVertical;
    float zoomSpeed;
    float scrollInput;

    void Start()
    {
        cam = GetComponent<Camera>();
        player = GameObject.FindGameObjectWithTag("Player");
        loco = player.GetComponent<UltimateCharacterLocomotion>();
        hud.SetActive(false);
    }

    void Update()
    {
        if (Input.GetButtonDown("Start"))
        {
            godCamEnabled = !godCamEnabled;

            if (godCamEnabled)
            {
                QualitySettings.SetQualityLevel(0);
                hud.SetActive(true);

                loco.enabled = false;
                foreach (MonoBehaviour v in player.GetComponents<MonoBehaviour>())
                {
                    v.enabled = false;
                }
                cam = GetComponent<Camera>();

                var orbit = cam.GetComponent<OrbitCam>().enabled;

                if (orbit)
                {
                    isOrbitCam = true;
                    cam.GetComponent<OrbitCam>().enabled = false;
                }
                else
                {
                    isOrbitCam = false;
                    cam.GetComponent<CameraController>().enabled = false;
                    cam.GetComponent<CameraControllerHandler>().enabled = false;
                }

                Vector3 t = cam.transform.position;
                t.y = height;
                t.x -= height / 2;
                transform.position = t;
                transform.eulerAngles = new Vector3(65, 90, 0);
            } 
            else
            {
                QualitySettings.SetQualityLevel(6);
                hud.SetActive(false);

                foreach (MonoBehaviour v in player.GetComponents<MonoBehaviour>())
                {
                    v.enabled = true;
                }

                if (isOrbitCam)
                    cam.GetComponent<OrbitCam>().enabled = true;
                else
                {
                    cam.GetComponent<CameraController>().enabled = true;
                    cam.GetComponent<CameraControllerHandler>().enabled = true;
                    loco.enabled = true;
                }
            }
        }

        if (godCamEnabled)
        {
            // Gamepad input
            leftStickHorizontal = Input.GetAxis("Horizontal");
            leftStickVertical = Input.GetAxis("Vertical");
            rightStickHorizontal = Input.GetAxis("Mouse X");
            rightStickVertical = Input.GetAxis("Mouse Y");
            zoomSpeed = height; // 256f * (height / 256f);
            
            // Gamepad zoom
            height = Mathf.Clamp(height + ((Input.GetAxis("Fire1") - Input.GetAxis("Fire2") * zoomSpeed * Time.deltaTime)), 64, 256);
            
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
            height = Mathf.Clamp(height, 64, 256);

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
}
