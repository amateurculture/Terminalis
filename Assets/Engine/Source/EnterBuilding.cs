using Opsive.UltimateCharacterController.Camera;
using UnityEngine;

public class EnterBuilding : MonoBehaviour
{
    public Transform lightGroup;
    Camera cam;
    CameraController camController;

    private void Start()
    {
        cam = Camera.main;
        camController = cam.GetComponent<CameraController>();

        foreach(Transform light in lightGroup)
        {
            light.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            foreach(Transform light in lightGroup)
            {
                light.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            foreach(Transform light in lightGroup)
            {
                light.gameObject.SetActive(false);
            }
        }
    }
}
