using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinusoidMove : MonoBehaviour {

    public float a;
    public float omega; 

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        Vector3 pos = transform.position;
        pos.x = a * Mathf.Sin(omega * Time.time);
        pos.y = a * Mathf.Cos(omega * Time.time);
        transform.position = pos;
	}
}
