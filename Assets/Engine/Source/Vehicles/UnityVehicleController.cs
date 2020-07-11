using Opsive.UltimateCharacterController.Camera;
using Opsive.UltimateCharacterController.Character;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Car;

public class UnityVehicleController : MonoBehaviour
{
    public AudioSource hornAudio;
    public Transform lookAtTarget;
    public Transform positionTarget;
    public Transform sideView;
    public GameObject exitPoint;
    public GameObject headlights;
    public GameObject dashboard;
    public Renderer carMaterial;
    public Image lowBeamsIndicator;
    public Image handBrakeIndicator;
    public int headLightIndex;
    public int tailLightIndex;
    
    [HideInInspector] public bool isInside;

    NavigationStack navStack;
    CarAudio engineAudio;
    CarController carController;
    CarUserControl carUserControl;
    CameraController playerCam;
    GameObject player;
    OrbitCam orbitCam;
    Camera cam;
    private WheelCollider[] m_Wheels;
    private Rigidbody rigid;
    bool isAtDoor;
   
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

        engineAudio = GetComponent<CarAudio>();
        carController = GetComponent<CarController>();
        carUserControl = GetComponent<CarUserControl>();
        rigid = GetComponent<Rigidbody>();

        rigid.isKinematic = false;
        engineAudio.enabled = false;
        carController.enabled = false;
        carUserControl.enabled = true;
        carUserControl.isDisabled = true;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
    }

    IEnumerator WheelHack()
    {
        yield return new WaitForSeconds(2);

        m_Wheels = GetComponentsInChildren<WheelCollider>();
        for (int i = 0; i < m_Wheels.Length; ++i)
        {
            var wheel = m_Wheels[i];
            wheel.motorTorque = 0;
        }
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
            // Handle hand brake indicator
            if (carUserControl.usingHandbrake)
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
            if (hornButtonPressed && hornAudio != null) hornAudio.Play();

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
                foreach (var sound in GetComponents<AudioSource>())
                    sound.enabled = false;

                if (dashboard) dashboard.gameObject.SetActive(false);

                //StartCoroutine(DisableWheels());

                m_Wheels = GetComponentsInChildren<WheelCollider>();
                for (int i = 0; i < m_Wheels.Length; ++i)
                {
                    var wheel = m_Wheels[i];
                    wheel.motorTorque = 0;
                }
                engineAudio.enabled = false;
                carController.enabled = false;
                carUserControl.enabled = false;
                orbitCam.enabled = false;

                // Fix to prevent exiting car underground
                var pos = new Vector3(transform.position.x - exitPoint.transform.position.x, transform.position.x - exitPoint.transform.position.y, transform.position.x - exitPoint.transform.position.z);
                //if (pos.y < 0) pos.y = Mathf.Abs(exitPoint.transform.position.y) + carMaterial.GetComponent<MeshFilter>().mesh.bounds.size.y;
                
                pos.y = pos.y < 0 ? 1f : pos.y;

                player.transform.position = exitPoint.transform.position;
                
                //var euler = cam.transform.rotation.eulerAngles;
                //var rot = Quaternion.Euler(0, euler.y, 0);
                //player.transform.rotation = rot;

                foreach (MonoBehaviour v in player.GetComponents<MonoBehaviour>())
                {
                    v.enabled = true;
                }
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

                Time.timeScale = 1;

                navStack.ExitVehicle();
            }
        }
        // Enter vehicle
        else if (isAtDoor && enterCarButtonPressed)
        {
            navStack.EnterVehicle();
            Debug.Log("Entering car");
            Cursor.lockState = CursorLockMode.Locked;

            Debug.Log("Turning on engine audio");
            foreach (var sound in GetComponents<AudioSource>()) 
                sound.enabled = true;

            isAtDoor = false;
            playerCam.enabled = false;
            
            if (player == null) player = GameObject.FindGameObjectWithTag("Player");

            Debug.Log("Found player: " + player.name + " -- Turning off all scripts");
            foreach (MonoBehaviour v in player.GetComponents<MonoBehaviour>())
            {
                Debug.Log(v.ToString());
                v.enabled = false;
            }

            player.SetActive(false);
            Debug.Log("Player turned off");

            orbitCam.focus = transform;
            orbitCam.distance = carMaterial.GetComponent<MeshFilter>().mesh.bounds.size.z * 2f; 
            orbitCam.focusRadius = .25f;
            orbitCam.focusCentering = 1f;
            orbitCam.rotationSpeed = 256f;
            orbitCam.alignDelay = 1f;
            orbitCam.fudge = 1f;
            orbitCam.enabled = true;

            Debug.Log("Turned on orbit camera");

            engineAudio.enabled = true;
            carController.enabled = true;
            carUserControl.enabled = true;
            carUserControl.isDisabled = false;
            isInside = true;

            if (dashboard) dashboard.gameObject.SetActive(true);

            lowBeamsIndicator.enabled = true;
            Color temp = lowBeamsIndicator.color;
            temp.a = headlights.activeSelf ? 1f : .05f;
            lowBeamsIndicator.color = temp;
            handBrakeIndicator.enabled = true;
            temp = handBrakeIndicator.color;
            temp.a = carUserControl.usingHandbrake ? 1f : .05f;
            handBrakeIndicator.color = temp;

            Debug.Log("Setup car properties and lights");

            if (cam == null) cam = Camera.main;

            Debug.Log("If camera main was null, find it again");

            cam.GetComponent<CameraControllerHandler>().enabled = false;

            Debug.Log("Turned on camera controller handler");

            rigid.isKinematic = true;
            rigid.isKinematic = false;
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

/*
IEnumerator DisableWheels()
{
    m_Wheels = GetComponentsInChildren<WheelCollider>();

    yield return new WaitForSeconds(3);

    for (int i = 0; i < m_Wheels.Length; ++i)
    {
        var wheel = m_Wheels[i];
        wheel.motorTorque = 0;
    }

    carController.enabled = false;
    carUserControl.enabled = false;
}
*/
