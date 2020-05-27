using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script to flip Camera parent to one of the anchors based on Function keys
/// </summary>
public class CameraAnchor : MonoBehaviour {

    public GameObject[] anchors;

	
	// Update is called once per frame
	void Update () {
        for (int i=0; i < anchors.Length; i++) {
            if (Input.GetKeyDown(KeyCode.F1+i)) {
                transform.parent = anchors[i].transform;
                transform.position = anchors[i].transform.position;
                return;
            }
        }
	}
}
