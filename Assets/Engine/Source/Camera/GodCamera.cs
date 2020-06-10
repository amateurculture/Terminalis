using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GodCamera : MonoBehaviour
{
    Camera cam;

    void Start()
    {
        QualitySettings.shadowDistance = 256;
        cam = GetComponent<Camera>();
    }

    private void OnDisable()
    {
        QualitySettings.shadowDistance = 64;
    }

    void Update()
    {
        var pos = transform.position;

        if (Input.GetKeyDown(KeyCode.W))
        {
            pos.z += (10 * (cam.orthographicSize/25));
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            pos.z -= (10 * (cam.orthographicSize / 25));
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            pos.x -= (10 * (cam.orthographicSize / 25));
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            pos.x += (10 * (cam.orthographicSize / 25));
        }
        else if (Input.GetKeyDown(KeyCode.Equals)) 
        {
            cam.orthographicSize -= 25;
        } 
        else if (Input.GetKeyDown(KeyCode.Minus))
        {
            cam.orthographicSize += 25;
        }

        if (cam.orthographicSize < 25) 
        {
            cam.orthographicSize = 25;
        }
        else if (cam.orthographicSize > 250)
        {
            cam.orthographicSize = 250;
        }

        transform.position = pos;
    }
}
