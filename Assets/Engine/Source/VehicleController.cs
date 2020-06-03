using Opsive.UltimateCharacterController.Camera;
using Opsive.UltimateCharacterController.Character;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Car;

public class VehicleController : MonoBehaviour
{
    public Transform lookAtTarget;
    public Transform positionTarget;
    public Transform sideView;
    public GameObject exitPoint;
    public GameObject headlights;
    public GameObject dashboard;
    public AudioSource audioSource;
    public Image lowBeamsIndicator;
    public Image handBrakeIndicator;
    public Renderer carMaterial;
    public int headLightIndex;
    public int tailLightIndex;

    private WheelCollider[] m_Wheels;

    GameObject player;
    OrbitCam orbitCam;
    CameraController playerCam;
    WheelDrive1 wheelDrive;
    Camera cam;
    bool isInside;
    bool isAtDoor;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        wheelDrive = GetComponent<WheelDrive1>();
        wheelDrive.isDisabled = true;

        cam = Camera.main;
        orbitCam = cam.GetComponent<OrbitCam>();

        playerCam = cam.GetComponent<CameraController>();
        isInside = false;
        isAtDoor = false;
        headlights.SetActive(false);

        if (carMaterial)
        {
            carMaterial.materials[tailLightIndex].SetColor("_EmissionColor", Color.white);
            carMaterial.materials[headLightIndex].SetColor("_EmissionColor", Color.white);
            carMaterial.materials[headLightIndex].DisableKeyword("_EMISSION");
            carMaterial.materials[tailLightIndex].DisableKeyword("_EMISSION");
            if (headLightIndex != tailLightIndex) carMaterial.materials[tailLightIndex].SetColor("_EmissionColor", new Color(.1f, 0, 0, 1));
        }

        if (dashboard) dashboard.gameObject.SetActive(false);

        Color temp = lowBeamsIndicator.color;
        temp.a = 5;
        lowBeamsIndicator.color = temp;
        lowBeamsIndicator.enabled = false;

        temp = handBrakeIndicator.color;
        temp.a = 5;
        handBrakeIndicator.color = temp;
        handBrakeIndicator.enabled = false;
    }

    private void TurnHeadlightsOn()
    {
        headlights.SetActive(true);
        if (carMaterial)
            carMaterial.materials[headLightIndex].EnableKeyword("_EMISSION");
    }

    private void TurnHeadlightsOff()
    {
        headlights.SetActive(false);
        if (carMaterial)
            carMaterial.materials[headLightIndex].DisableKeyword("_EMISSION");
    }

    private void LateUpdate()
    {
        bool enterCarButtonPressed = Input.GetButtonDown("EnterCar") || Input.GetKeyDown(KeyCode.E);

        if (isInside)
        {
            // Enable wheel drive
            wheelDrive.isDisabled = false;

            // Handle hand brake indicator
            if (wheelDrive.handbrakeEnabled)
            {
                Color temp = handBrakeIndicator.color;
                temp.a = 1f;
                handBrakeIndicator.color = temp;
            }
            else
            {
                Color temp = handBrakeIndicator.color;
                temp.a = .05f;
                handBrakeIndicator.color = temp;
            }

            // Handle brakelights
            if (headLightIndex != tailLightIndex)
            {
                if (Input.GetAxis("Fire2") > .1f)
                    carMaterial.materials[tailLightIndex].SetColor("_EmissionColor", Color.red);
                else
                    carMaterial.materials[tailLightIndex].SetColor("_EmissionColor", new Color(.1f, 0, 0, 1));

                carMaterial.materials[tailLightIndex].EnableKeyword("_EMISSION");
            }

            if (headlights.activeSelf)
                carMaterial.materials[headLightIndex].EnableKeyword("_EMISSION");

            // Handle horn
            bool hornButtonPressed = Input.GetButtonDown("Toggle Perspective");
            if (hornButtonPressed) audioSource.Play();

            // Handle headlights
            bool headlightButtonPressed = Input.GetButtonDown("Crouch");
            if (headlightButtonPressed)
            {
                if (headlights.activeSelf == false) 
                    TurnHeadlightsOn();
                else TurnHeadlightsOff();

                lowBeamsIndicator.enabled = true;
                Color temp = lowBeamsIndicator.color;

                if (headlights.activeSelf)
                    temp.a = 1f;
                else
                    temp.a = .05f;

                lowBeamsIndicator.color = temp;
            }

            // Exit vehicle
            if (enterCarButtonPressed)
            {
                if (dashboard) dashboard.gameObject.SetActive(false);

                //StartCoroutine(DisableWheels());

                m_Wheels = GetComponentsInChildren<WheelCollider>();
                for (int i = 0; i < m_Wheels.Length; ++i)
                {
                    var wheel = m_Wheels[i];
                    wheel.motorTorque = 0;
                }
                wheelDrive.isDisabled = true;

                orbitCam.enabled = false;

                // Fix to prevent exiting car underground
                var pos = new Vector3(exitPoint.transform.position.x, exitPoint.transform.position.y - 1.6f, exitPoint.transform.position.z);
                if (pos.y < 0) pos.y = Mathf.Abs(exitPoint.transform.position.y) + carMaterial.GetComponent<MeshFilter>().mesh.bounds.size.y;

                player.transform.position = pos;

                var euler = transform.rotation.eulerAngles;
                var rot = Quaternion.Euler(0, euler.y, 0);

                player.transform.rotation = rot;
                player.SetActive(true);
                playerCam.enabled = true;
                isInside = false;
                lowBeamsIndicator.enabled = false;
                handBrakeIndicator.enabled = false;

                player.GetComponent<UltimateCharacterLocomotionHandler>().enabled = true;
                cam.GetComponent<CameraControllerHandler>().enabled = true;

                //TurnHeadlightsOff();

                if (headLightIndex != tailLightIndex)
                    carMaterial.materials[tailLightIndex].DisableKeyword("_EMISSION");
            }
        }
        // Enter vehicle
        else if (isAtDoor && enterCarButtonPressed)
        {
            isAtDoor = false;
            playerCam.enabled = false;
            player.SetActive(false);

            orbitCam.focus = transform;
            orbitCam.distance = 4;
            orbitCam.enabled = true;

            wheelDrive.isDisabled = false;
            isInside = true;

            if (dashboard) dashboard.gameObject.SetActive(true);

            lowBeamsIndicator.enabled = true;
            Color temp = lowBeamsIndicator.color;

            if (headlights.activeSelf)
                temp.a = 1f;
            else
                temp.a = .05f;

            lowBeamsIndicator.color = temp;
            handBrakeIndicator.enabled = true;
            temp = handBrakeIndicator.color;

            if (wheelDrive.handbrakeEnabled)
                temp.a = 1f;
            else
                temp.a = .05f;

            handBrakeIndicator.color = temp;

            player.GetComponent<UltimateCharacterLocomotionHandler>().enabled = false;
            cam.GetComponent<CameraControllerHandler>().enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player") isAtDoor = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player") isAtDoor = false;
    }

    IEnumerator DisableWheels()
    {
        m_Wheels = GetComponentsInChildren<WheelCollider>();

        yield return new WaitForSeconds(3);

        for (int i = 0; i < m_Wheels.Length; ++i)
        {
            var wheel = m_Wheels[i];
            wheel.motorTorque = 0;
        }

        wheelDrive.isDisabled = true;
    }
}
