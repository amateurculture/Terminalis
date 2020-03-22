using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSelectFKey : MonoBehaviour {

    [Tooltip("Cameras (GameObjects) to be selected (slot 0 =F1, slot 1=F2 etc.)\nScene starts with 0 selected. ")]
    public GameObject[] cameras;

    private GameObject selectedCamera; 

    // Use this for initialization
    void Start () {
        foreach(GameObject go in cameras) {
            go.SetActive(false);
        }
        selectedCamera = cameras[0];
        selectedCamera.SetActive(true);
	}
	
	// Update is called once per frame
	void Update () {
		for (int i=0; i < cameras.Length; i++) {
            if (Input.GetKeyDown(KeyCode.F1 + i)) {
                selectedCamera.SetActive(false);
                selectedCamera = cameras[i];
                selectedCamera.SetActive(true);
                break;
            }
        }
	}
}
