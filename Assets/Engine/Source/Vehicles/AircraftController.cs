using Opsive.UltimateCharacterController.Camera;
using Opsive.UltimateCharacterController.Character;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Aeroplane;
using UnityStandardAssets.Vehicles.Car;

/// <summary>
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

public class AircraftController : MonoBehaviour
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
    public CarController carController;
    public AeroplaneUserControl4Axis aircraftController;
    public AeroplaneAudio aircraftAudioController;

    NavigationStack navStack;
    GameObject player;
    OrbitCam orbitCam;
    CameraController playerCam;
    Camera cam;
    bool isInside;
    bool isAtDoor;
    WheelCollider[] m_Wheels;

    void Start()
    {
        navStack = FindObjectOfType<NavigationStack>();
        player = GameObject.FindGameObjectWithTag("Player");

        cam = Camera.main;
        orbitCam = cam.GetComponent<OrbitCam>();

        playerCam = cam.GetComponent<CameraController>();
        isInside = false;
        isAtDoor = false;
        headlights.SetActive(false);

        aircraftController.enabled = false;

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
    }

    private void TurnHeadlightsOff()
    {
        headlights.SetActive(false);
    }

    private void LateUpdate()
    {
        bool enterCarButtonPressed = Input.GetButtonDown("EnterCar") || Input.GetKeyDown(KeyCode.E);

        if (isInside)
        {
            // Handle hand brake indicator
            if (aircraftController.m_AirBrakes)
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

            // Handle horn
            bool hornButtonPressed = Input.GetButtonDown("Y");
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
                aircraftAudioController.EndAudio();

                if (dashboard) dashboard.gameObject.SetActive(false);

                orbitCam.enabled = false;

                m_Wheels = GetComponentsInChildren<WheelCollider>();
                for (int i = 0; i < m_Wheels.Length; ++i)
                {
                    var wheel = m_Wheels[i];
                    wheel.motorTorque = 0;
                }

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
                aircraftController.m_AirBrakes = true;
                aircraftController.enabled = false;
                player.GetComponent<UltimateCharacterLocomotionHandler>().enabled = true;
                cam.GetComponent<CameraControllerHandler>().enabled = true;

                navStack.ExitVehicle();
                //TurnHeadlightsOff();
            }
        }
        // Enter vehicle
        else if (isAtDoor && enterCarButtonPressed)
        {
            navStack.EnterVehicle();
            var euler = player.transform.rotation.eulerAngles;
            var rot = Quaternion.Euler(0, euler.y, 0);
            orbitCam.transform.rotation = rot;

            isAtDoor = false;
            playerCam.enabled = false;
            player.SetActive(false);
            orbitCam.focus = transform;
            orbitCam.enabled = true;
            isInside = true;
            aircraftController.enabled = true;
            orbitCam.distance = 20;

            aircraftAudioController.StartAudio();

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
            temp.a = (aircraftController.m_AirBrakes) ? 1f : .05f;

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
}
