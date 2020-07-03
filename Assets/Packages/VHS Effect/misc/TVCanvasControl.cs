using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TVCanvasControl : MonoBehaviour {
public TV_Effect _tv;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void Resolution(float f){
		_tv._resolution = f;
	}
}
