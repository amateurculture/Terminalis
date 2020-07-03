using Opsive.UltimateCharacterController.Camera;
using Opsive.UltimateCharacterController.Character;
using UnityEngine;

public class GodCamera : MonoBehaviour
{
    public GameObject hud;
    [Range(64, 256)] public float height = 128f;

    Camera cam;
    GameObject player;
    UltimateCharacterLocomotion loco;
    bool godCamEnabled;
    bool isOrbitCam;

    public Vector2 rotationClamp = new Vector2(35, 85); 

    void Start()
    {
        cam = GetComponent<Camera>();
        player = GameObject.FindGameObjectWithTag("Player");
        loco = player.GetComponent<UltimateCharacterLocomotion>();
        hud.SetActive(false);
    }

    void LateUpdate()
    {
        if (Input.GetButtonDown("Back"))
        {
            godCamEnabled = !godCamEnabled;

            if (godCamEnabled)
            {
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

                Vector3 t = player.transform.position;
                t.y = height;
                t.x -= height / 2;
                transform.position = t;
                transform.eulerAngles = new Vector3(65, 90, 0);
            } 
            else
            {
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
            float hinput = Input.GetAxis("Horizontal");
            float vinput = Input.GetAxis("Vertical");
            float hinput1 = Input.GetAxis("Mouse X");
            float vinput1 = Input.GetAxis("Mouse Y");
            float zoomSpeed = 256f * (height / 256f);
            
            // Gamepad zoom
            height = Mathf.Clamp(height + ((Input.GetAxis("Fire2") - Input.GetAxis("Fire1") * zoomSpeed * Time.deltaTime)), 64, 256);
            
            // Key Input
            if (Input.GetKey(KeyCode.W)) vinput = 1;
            else if (Input.GetKey(KeyCode.S)) vinput = -1;
            else if (Input.GetKey(KeyCode.A)) hinput = -1;
            else if (Input.GetKey(KeyCode.D)) hinput = 1;
            else if (Input.GetKey(KeyCode.Q)) height -= zoomSpeed * Time.deltaTime;
            else if (Input.GetKey(KeyCode.E)) height += zoomSpeed * Time.deltaTime;

            // Mouse scroll zoom
            var d = Input.GetAxis("Mouse ScrollWheel");
            if (d > 0f) height -= zoomSpeed * Time.deltaTime * d * 50f;
            else if (d < 0f) height -= zoomSpeed * Time.deltaTime * d * 50f;
            height = Mathf.Clamp(height, 64, 256);

            // Move camera
            var rot = transform.eulerAngles;
            var pos = transform.position;
            if (vinput != 0)
            {
                var moveDirection = transform.forward;
                moveDirection.y = 0.0f;
                moveDirection = Vector3.Normalize(moveDirection);
                pos += moveDirection * Time.deltaTime * (vinput * zoomSpeed);
                pos.y = height;
            }
            pos.y = height;
            transform.position = pos;

            if (hinput != 0) transform.position += transform.right * Time.deltaTime * (hinput * zoomSpeed);
            if (hinput1 != 0) rot.y += (hinput1);
            if (vinput1 != 0) rot.x += vinput1;
            if (rot.x > rotationClamp.y) rot.x = rotationClamp.y;
            if (rot.x < rotationClamp.x) rot.x = rotationClamp.x;
            transform.eulerAngles = rot;
        }
    }
}
