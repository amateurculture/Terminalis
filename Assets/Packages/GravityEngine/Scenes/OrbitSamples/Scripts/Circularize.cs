using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller for demonstrating orbit circularization and returning an On-Rails object back to NBody 
/// evolution. 
/// 
/// Circularization will work for a ship that is under NBody control or one that is On-Rails (i.e. has a 
/// KeplerSequence component and an initial OrbitUniversal component). 
/// 
/// Press C to circularize the orbit of the ship
/// Press R to return an object to NBody control. 
/// 
/// See the sample scene Circularize.
/// </summary>
public class Circularize : MonoBehaviour {

    [SerializeField]
    private NBody  shipNbody = null;

    [SerializeField]
    private NBody centerNbody = null;

    //! Optional - ship can have a KeplerSequence and Xfer will be done with KS and not maneuvers
    private KeplerSequence keplerSeq;

    // Use this for initialization
    void Start () {
        keplerSeq = shipNbody.GetComponent<KeplerSequence>();
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyUp(KeyCode.C)) {
            OrbitData currentOrbit = new OrbitData();
            currentOrbit.SetOrbitForVelocity(shipNbody, centerNbody);
            // circularize the orbit
            OrbitTransfer t = new CircularizeXfer(currentOrbit);
            // If this is a ship with a Kepler sequence take the maneuvers and add them as KeplerSequence elements
            // This allows the transfer to be time-reversible if the whole scene is on rails. 
            if (keplerSeq != null) {
                keplerSeq.AddManeuvers(t.GetManeuvers());
            } else {
                // Nbody evolution (or plain onRails w/o KeplerSequence) use maneuvers
                GravityEngine.Instance().AddManeuver(t.GetManeuvers()[0]);
            }

        } else if (Input.GetKeyDown(KeyCode.R)) {
            // return orbit to GravityEngine control (go Off-Rails)
            if (keplerSeq != null) {
                keplerSeq.AppendReturnToGE(GravityEngine.Instance().GetPhysicalTime(), shipNbody);
            }
        } else if (Input.GetKeyDown(KeyCode.W)) {
            // return orbit to GravityEngine control (go Off-Rails)
            GravityEngine.Instance().ApplyImpulse(shipNbody, Vector3.up);
        }
    }
}
