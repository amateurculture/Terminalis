using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple utility to modify model scale based on the length scale provided in GE (when units other than 
/// DIMENSIONLESS are used). 
/// 
/// Handy for testing code to ensure length scale is working without having to re-scale all the models in a scene
/// manually as the scale is adjusted. 
/// 
/// </summary>
public class GlobalScaler : MonoBehaviour {

	// Use this for initialization
	void Start () {
        GravityEngine ge = GravityEngine.Instance();
        if (ge.units != GravityScaler.Units.DIMENSIONLESS) {
            transform.localScale *= ge.GetLengthScale();
        }
		
	}
	

}
