using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InstructionControl : MonoBehaviour {

    public GameObject panel;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyUp(KeyCode.Slash) || Input.GetKeyUp(KeyCode.Question)) {
            panel.SetActive(true);
        }
	}

    public void Close() {
        panel.SetActive(false);
    }
}

