using Opsive.UltimateCharacterController.Camera;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerZoom : MonoBehaviour
{
    float zoom;
    CameraController cameraController;
    Vector3 tempAnchor;

    private void Start()
    {
        cameraController = GetComponent<CameraController>();
        tempAnchor = cameraController.AnchorOffset;
        zoom = -1.6f;
        tempAnchor.z = zoom;
        cameraController.AnchorOffset = tempAnchor;
    }

    void Update()
    {
        if (Input.GetButtonDown("Toggle Perspective"))
        {
            tempAnchor = cameraController.AnchorOffset;

            if (zoom == -1.6f)
            {
                tempAnchor.z = -14;
                zoom = -14;
            } 
            else
            {
                tempAnchor.z = -1.6f;
                zoom = -1.6f;
            }
            cameraController.AnchorOffset = tempAnchor;
        }
    }
}
