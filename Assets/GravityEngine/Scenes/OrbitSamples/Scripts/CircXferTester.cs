using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Test script for circular transfer
/// 
/// Press T to intiate the transfer test code. 
/// </summary>
public class CircXferTester : MonoBehaviour {

    [SerializeField]
    private NBody orbit1 = null;

    [SerializeField]
    private NBody orbit2 = null;

    [SerializeField]
    private NBody centerBody = null;

    [SerializeField]
    //! Array of objects already in scene to use as markers (keeps things simple)
    private GameObject[] commonPointMarker = null;

    private bool xferRequested = false; 

	// Use this for initialization
	void Start () {
		
	}

    // Update is called once per frame
    void Update() {
        if (xferRequested)
            return;

        if (Input.GetKeyUp(KeyCode.T)) {
            OrbitData od1 = new OrbitData();
            od1.SetOrbitForVelocity(orbit1, centerBody);
            OrbitData od2 = new OrbitData();
            od2.SetOrbitForVelocity(orbit2, centerBody);

            OrbitTransfer transfer = new CircularInclinationAndAN(od1, od2);

            // Update for as many markers as provided
            List<Maneuver> maneuvers = transfer.GetManeuvers();
            for (int i = 0; i < commonPointMarker.Length; i++) {
                commonPointMarker[i].transform.position = 
                    GravityEngine.Instance().MapToScene( maneuvers[i].physPosition.ToVector3());
            }

            GravityEngine.instance.AddManeuvers(transfer.GetManeuvers());
            xferRequested = true;
        }
    }
}
