using UnityEngine;
using System.Collections;

public class SimpleLODClickObject : MonoBehaviour {

	public Light emphasisLight;
	public bool sendNullInstead = false;

	void Start() {
		if(emphasisLight != null) emphasisLight.enabled = false;
	}
	void OnMouseEnter() {
		if(emphasisLight != null) emphasisLight.enabled = true;
	}
	void OnMouseExit() {
		if(emphasisLight != null) emphasisLight.enabled = false;
	}
	void OnMouseUpAsButton() {
		if(sendNullInstead) Camera.main.gameObject.GetComponent<SimpleLODDemoCamera>().SetClickedObject(null);
		else Camera.main.gameObject.GetComponent<SimpleLODDemoCamera>().SetClickedObject(gameObject);
	}
}
