using Opsive.UltimateCharacterController.Camera;
using Opsive.UltimateCharacterController.Character;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VehicleController : MonoBehaviour
{
    public Transform lookAtTarget;
    public Transform positionTarget;
    public Transform sideView;
    public GameObject exitPoint;
    public Light headlightLeft;
    public Light headlightRight;
    public GameObject speedometer;
    public AudioSource audioSource;
    public Image lowBeamsIndicator;
    public Image handBrakeIndicator;
    public Renderer carMaterial;
    public int headLightIndex;
    public int tailLightIndex;
    
    private WheelCollider[] m_Wheels;

    GameObject player;
    //DriftCamera driftCamera;
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
        //driftCamera = cam.GetComponent<DriftCamera>();
        orbitCam = cam.GetComponent<OrbitCam>();

        playerCam = cam.GetComponent<CameraController>();
        isInside = false;
        isAtDoor = false;
        headlightLeft.enabled = false;
        headlightRight.enabled = false;

        if (carMaterial)
        {
            carMaterial.materials[tailLightIndex].SetColor("_EmissionColor", Color.white);
            carMaterial.materials[headLightIndex].SetColor("_EmissionColor", Color.white);
            carMaterial.materials[headLightIndex].DisableKeyword("_EMISSION");
            carMaterial.materials[tailLightIndex].DisableKeyword("_EMISSION");
            if (headLightIndex != tailLightIndex) carMaterial.materials[tailLightIndex].SetColor("_EmissionColor", new Color(.1f, 0, 0, 1));
        }

        if (speedometer) speedometer.gameObject.SetActive(false);

        Color temp = lowBeamsIndicator.color;
        temp.a = 5;
        lowBeamsIndicator.color = temp;
        lowBeamsIndicator.enabled = false;

        temp = handBrakeIndicator.color;
        temp.a = 5;
        handBrakeIndicator.color = temp;
        handBrakeIndicator.enabled = false;
    }

    private void LateUpdate()
    {
        bool enterCarButtonPressed = Input.GetButtonDown("EnterCar") || Input.GetKeyDown(KeyCode.E);

        if (isInside)
        {
            wheelDrive.isDisabled = false;

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

            // Handle Brakelights

            if (headLightIndex != tailLightIndex)
            {
                if (Input.GetAxis("Fire2") > .1f)
                    carMaterial.materials[tailLightIndex].SetColor("_EmissionColor", Color.red);
                else
                    carMaterial.materials[tailLightIndex].SetColor("_EmissionColor", new Color(.1f, 0, 0, 1));

                carMaterial.materials[tailLightIndex].EnableKeyword("_EMISSION");
            }

            if (headlightLeft.enabled)
                carMaterial.materials[headLightIndex].EnableKeyword("_EMISSION");

            // Handle Horn
            bool hornButtonPressed = Input.GetButtonDown("Toggle Perspective");
            if (hornButtonPressed) audioSource.Play();

            // Handle Headlights
            bool headlightButtonPressed = Input.GetButtonDown("Crouch");
            if (headlightButtonPressed)
            {
                if (headlightLeft.enabled == false)
                {
                    headlightLeft.enabled = true;
                    headlightRight.enabled = true;

                    if (carMaterial)
                    {
                        carMaterial.materials[headLightIndex].EnableKeyword("_EMISSION");
                        //carMaterial.materials[tailLightIndex].EnableKeyword("_EMISSION");
                    }
                }
                else
                {
                    headlightLeft.enabled = false;
                    headlightRight.enabled = false;

                    if (carMaterial)
                    {
                        carMaterial.materials[headLightIndex].DisableKeyword("_EMISSION");
                        //carMaterial.materials[tailLightIndex].DisableKeyword("_EMISSION");
                    }
                }

                lowBeamsIndicator.enabled = true;
                Color temp = lowBeamsIndicator.color;

                if (headlightLeft.enabled)
                    temp.a = 1f;
                else
                    temp.a = .05f;

                lowBeamsIndicator.color = temp;
            }

            // Exit vehicle
            if (enterCarButtonPressed)
            {
                if (speedometer) speedometer.gameObject.SetActive(false);

                StartCoroutine(DisableWheels());

                //driftCamera.enabled = false;
                orbitCam.enabled = false;

                player.transform.position = new Vector3(exitPoint.transform.position.x, 3, exitPoint.transform.position.z);

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


            //driftCamera.sideView = sideView;
            //driftCamera.positionTarget = positionTarget;
            //driftCamera.lookAtTarget = lookAtTarget;
            //driftCamera.enabled = true;
            orbitCam.focus = transform;
            orbitCam.enabled = true;

            wheelDrive.isDisabled = false;
            isInside = true;

            if (speedometer) speedometer.gameObject.SetActive(true);

            lowBeamsIndicator.enabled = true;
            Color temp = lowBeamsIndicator.color;

            if (headlightLeft.enabled)
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
