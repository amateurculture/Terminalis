using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Test script for circular transfer
/// 
/// Press T to intiate the transfer test code. 
/// </summary>
public class HohmannGeneralTester : MonoBehaviour {

    [SerializeField]
    private bool rendezvous = false;

    [SerializeField]
    //! Initial xfer immediatly, do not wait for a key press
    private bool xferAtStart = true;

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

    private GravityEngine ge; 

	// Use this for initialization
	void Awake () {
        ge = GravityEngine.Instance();
	}

    // Update is called once per frame
    void Update() {
        if (!ge.IsSetup())
            return;

        if (xferRequested)
            return;

        if (Input.GetKeyUp(KeyCode.T) || xferAtStart) {
            OrbitData od1 = new OrbitData();
            od1.SetOrbitForVelocity(orbit1, centerBody);
            OrbitData od2 = new OrbitData();
            od2.SetOrbitForVelocity(orbit2, centerBody);

            OrbitTransfer transfer = new HohmannGeneral(od1, od2, rendezvous);

            List<Maneuver> maneuvers = transfer.GetManeuvers();
            for (int i = 0; i < maneuvers.Count; i++) {
                commonPointMarker[i].transform.position = 
                    ge.MapToScene( maneuvers[i].physPosition.ToVector3());
            }

            ge.AddManeuvers(transfer.GetManeuvers());
            xferRequested = true;
        }
    }
}
