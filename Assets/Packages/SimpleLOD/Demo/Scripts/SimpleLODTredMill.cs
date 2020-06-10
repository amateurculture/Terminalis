using UnityEngine;
using System.Collections;

public class SimpleLODTredMill : MonoBehaviour {

	private Vector3 startPosition;

	// Use this for initialization
	void Start () {
		startPosition = transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		if(transform.position.z < Camera.main.transform.position.z - 10f) {
			transform.position = startPosition;  // start again
		}
	}
}
