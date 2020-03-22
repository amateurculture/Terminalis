using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Faster slower tester.
/// Test script to demonstrate the control of run-time speed via SetTimeZoom. 
///
/// </summary>
public class FasterSlowerTester : MonoBehaviour {

	private float timeZoom = 1.0f;

	void Update () {
		if (Input.GetKeyDown(KeyCode.F)) {
			timeZoom *= 2f;
			GravityEngine.Instance().SetTimeZoom(timeZoom);
		} else if (Input.GetKeyDown(KeyCode.S)) {
			timeZoom *= 0.5f;
			GravityEngine.Instance().SetTimeZoom(timeZoom);
		}
	}
}
