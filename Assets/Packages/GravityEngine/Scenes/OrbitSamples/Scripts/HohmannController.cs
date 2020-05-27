using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller for demonstrating Hohmann transfer. The inspector rendezvous field controls whether this is an 
/// immediate xfer or phased to allow a rendezvous. 
/// 
/// If a marker prefab is provided this object will be placed on the orbits to indicate the location of the manuevers.
/// The maneuver or Kepler sequence callback will remove the marker once the ship has reached it. 
/// 
/// Press H to perform the Hohmann xfer
/// /// </summary>
public class HohmannController : MonoBehaviour {

    [SerializeField]
    private NBody shipNbody = null;

    [SerializeField]
    private NBody targetNbody = null;

    [SerializeField]
    private NBody centerNbody = null;

    [SerializeField]
    private bool rendezvous = false;

    [SerializeField]
    private GameObject markerPrefab = null;

    [SerializeField]
    [Tooltip("Option UI text element to show time to next maneuver")]
    private Text timeToManeuverText = null;

    private List<GameObject> markers;

    //! Optional - ship can have a KeplerSequence and Xfer will be done with KS and not maneuvers
    private KeplerSequence keplerSeq;

    private OrbitTransfer orbitTransfer;

    // Use this for initialization
    void Start() {
        keplerSeq = shipNbody.GetComponent<KeplerSequence>();
        markers = new List<GameObject>();
    }

    /// <summary>
    /// Callback to remove markers when the maneuver is executed
    /// </summary>
    /// <param name="m"></param>
    private void RemoveMarker(Maneuver m) {
        if (markers.Count > 0) {
            GameObject marker = markers[0];
            Destroy(marker);
            markers.RemoveAt(0);
        }
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyUp(KeyCode.H)) {
            OrbitData shipOrbit = new OrbitData();
            shipOrbit.SetOrbitForVelocity(shipNbody, centerNbody);
            OrbitData targetOrbit = new OrbitData();
            targetOrbit.SetOrbitForVelocity(targetNbody, centerNbody);
            // determine the transfer
            orbitTransfer = new HohmannXfer(shipOrbit, targetOrbit, rendezvous);
            // If this is a ship with a Kepler sequence take the maneuvers and add them as KeplerSequence elements
            // This allows the transfer to be time-reversible if the whole scene is on rails. 
            if (keplerSeq != null) {
                keplerSeq.AddManeuvers(orbitTransfer.GetManeuvers());
            } else {
                // Nbody evolution (or plain onRails w/o KeplerSequence) use maneuvers
                foreach (Maneuver m in orbitTransfer.GetManeuvers()) {
                    GravityEngine.Instance().AddManeuver(m);
                }
            }
            // Maneuver markers
            if (markerPrefab != null) {
                foreach (Maneuver m in orbitTransfer.GetManeuvers()) {
                    // set maneuver position marker
                    GameObject marker = Instantiate(markerPrefab, centerNbody.gameObject.transform, true);
                    marker.transform.position = GravityEngine.Instance().MapToScene(m.physPosition.ToVector3());
                    markers.Add(marker);
                    m.onExecuted = RemoveMarker;
                }
            }

        }
        if (Input.GetKeyUp(KeyCode.C)) {
            // clear maneuvers
            GravityEngine.Instance().ClearManeuvers();
            // delete on rails maneuvers
            if (keplerSeq != null) {
                keplerSeq.RemoveManeuvers(orbitTransfer.GetManeuvers());
            }
            foreach (GameObject marker in markers) {
                Destroy(marker);
            }
            markers.Clear();
        }
        // optionally report time to next maneuver
        // for now just do first maneuver
        if ((timeToManeuverText != null) && (orbitTransfer != null)) {
            double time =  orbitTransfer.GetManeuvers()[0].worldTime - GravityEngine.Instance().GetPhysicalTime();
            timeToManeuverText.text = string.Format("Time to Next Maneuver = {0:0.00}", time);
        }
    
    }
}
