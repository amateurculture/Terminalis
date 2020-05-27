using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GettingStartedShip : MonoBehaviour {

    public float thrust = 1.0f;

    private NBody ship;

	// Use this for initialization
	void Start () {
        ship = GetComponent<NBody>();
        if (ship == null) {
            Debug.LogError(gameObject.name + " does not have an Nbody component");
        }
	}
	
	// Update is called once per frame
	void Update () {

        if (Input.GetKeyUp(KeyCode.W)) {
            GravityEngine.Instance().ApplyImpulse(ship, thrust * Vector3.up);
        } 
	}
}
