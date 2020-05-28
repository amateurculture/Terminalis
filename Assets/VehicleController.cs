using Opsive.UltimateCharacterController.Camera;
using Opsive.UltimateCharacterController.Character;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VehicleController : MonoBehaviour
{
    GameObject player;
    public Transform lookAtTarget;
    public Transform positionTarget;
    public Transform sideView;
    public GameObject exitPoint;
    public Light headlightLeft;
    public Light headlightRight;
    public Renderer carMaterial;
    public AudioSource audioSource;

    public Image lowBeamsIndicator;
    public Image handBrakeIndicator;

    DriftCamera driftCamera;
    CameraController playerCam;
    WheelDrive1 wheelDrive;
    Camera cam;
    bool isInside;
    bool isAtDoor;

    private WheelCollider[] m_Wheels;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        wheelDrive = GetComponent<WheelDrive1>();
        wheelDrive.isDisabled = true;

        cam = Camera.main;
        driftCamera = cam.GetComponent<DriftCamera>();
        playerCam = cam.GetComponent<CameraController>();
        isInside = false;
        isAtDoor = false;
        headlightLeft.enabled = false;
        headlightRight.enabled = false;

        if (carMaterial != null) carMaterial.materials[0].DisableKeyword("_EMISSION");

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
        bool enterCarButtonPressed = Input.GetButtonDown("EnterCar");

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

            bool headlightButtonPressed = Input.GetButtonDown("Crouch");
            bool hornButtonPressed = Input.GetButtonDown("Toggle Perspective");

            if (hornButtonPressed)
            {
                audioSource.Play();
            }

            if (headlightButtonPressed)
            {
                if (headlightLeft.enabled == false)
                {
                    headlightLeft.enabled = true;
                    headlightRight.enabled = true;
                    if (carMaterial) carMaterial.materials[0].EnableKeyword("_EMISSION");
                }
                else
                {
                    headlightLeft.enabled = false;
                    headlightRight.enabled = false;
                    if (carMaterial) carMaterial.materials[0].DisableKeyword("_EMISSION");
                }

                lowBeamsIndicator.enabled = true;
                Color temp = lowBeamsIndicator.color;

                if (headlightLeft.enabled)
                    temp.a = 1f;
                else
                    temp.a = .05f;

                lowBeamsIndicator.color = temp;
            }

            if (enterCarButtonPressed)
            {
                StartCoroutine(DisableWheels());

                driftCamera.enabled = false;
                player.transform.position = exitPoint.transform.position;
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
            }
        }
        else if (isAtDoor && enterCarButtonPressed)
        {
            isAtDoor = false;
            playerCam.enabled = false;
            player.SetActive(false);
            driftCamera.sideView = sideView;
            driftCamera.positionTarget = positionTarget;
            driftCamera.lookAtTarget = lookAtTarget;
            driftCamera.enabled = true;
            wheelDrive.isDisabled = false;
            isInside = true;

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
        isAtDoor = true;
    }

    private void OnTriggerExit(Collider other)
    {
        isAtDoor = false;
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
