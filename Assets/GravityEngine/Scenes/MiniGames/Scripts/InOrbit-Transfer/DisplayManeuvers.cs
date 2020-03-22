using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Display a sequence of maneuvers by activating a ghost Nbody at each maneuver point and configuring 
/// their OrbitPredictors to show the path that results for the maneuver. 
/// 
/// This component works in conjunction with scene controller logic that determines a series of maneuvers. 
/// 
/// </summary>
public class DisplayManeuvers : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Ship to display sequence of maneuvers for")]
    private NBody shipNbody = null;

    [SerializeField]
    [Tooltip("Array of existing game objects to mark maneuver points. Will be inactivated at start.")]
    private GameObject[] markers = null;

    // parallel arrays to markers to hold items we need
    private NBody[] nbodies = null;
    private bool[] addedToGE = null; 
    private OrbitPredictor[] orbitPredictors = null;

    private OrbitPredictor shipOrbitPredictor = null; 

    private GravityEngine ge = null; 

    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();
        nbodies = new NBody[markers.Length];
        addedToGE = new bool[markers.Length];
        orbitPredictors = new OrbitPredictor[markers.Length];
        for (int i=0; i < markers.Length; i++) { 
            markers[i].SetActive(false);
            nbodies[i] = markers[i].GetComponent<NBody>(); 
            if (nbodies[i] == null) {
                Debug.LogError("Cannot find NBody on " + markers[i].name);
                return;
            }
            orbitPredictors[i] = markers[i].GetComponentInChildren<OrbitPredictor>();
            if (orbitPredictors[i] == null) {
                Debug.LogError("Cannot find orbitPredictor on " + markers[i].name);
                return;
            }
            if (!orbitPredictors[i].velocityFromScript) {
                Debug.LogWarning("Changing OrbitPredictor to take velocity from script: " + markers[i].name);
                orbitPredictors[i].velocityFromScript = true;
            }

        }
        shipOrbitPredictor = shipNbody.GetComponentInChildren<OrbitPredictor>();
        if (shipOrbitPredictor == null) {
            Debug.LogError("Ship nbody is required to have an shipOrbitPredictor attached.");
        }
    }

    /// <summary>
    /// Display the sequence of maneuvers using the markers defined in the inspector. 
    /// 
    /// Note that the number of maneuvers in a transfer can change from call to call. 
    /// </summary>
    /// <param name="maneuvers"></param>
    public void Display(List<Maneuver> maneuvers) {
        if (maneuvers.Count > markers.Length) {
            Debug.LogError("Not enough markers provided for " + maneuvers.Count + " maneuvers");
            return;
        }
        OrbitPredictor lastOrbit = shipOrbitPredictor;
        for (int i=0; i < maneuvers.Count; i++) {
            // enable marker at correct location
            Vector3 pos = maneuvers[i].physPosition.ToVector3();
            // markers[i].transform.position = ge.MapPhyPosToWorld( pos);
            if (!markers[i].activeInHierarchy) {
                markers[i].SetActive(true);
            }
            nbodies[i].initialPhysPosition = pos;
            // add to GE??
            if (!addedToGE[i]) {
                ge.AddBody(markers[i]);
                addedToGE[i] = true;
            }
            ge.SetPositionDoubleV3(nbodies[i], new Vector3d(pos));

            // determine the post-maneuver velocity so OP will show post maneuver path
            // - what we have is the preceeding orbit (potentially from OP) and the maneuver position
            // require the velocity of the orbit at the given point. 
            Vector3 shipVel = lastOrbit.GetOrbitUniversal().VelocityForPosition(pos);
            switch (maneuvers[i].mtype) {
                case Maneuver.Mtype.scalar:
                    shipVel = shipVel + maneuvers[i].dV * shipVel.normalized;
                    break;

                case Maneuver.Mtype.setv:
                    shipVel = maneuvers[i].velChange;
                    break;

                case Maneuver.Mtype.vector:
                    shipVel += maneuvers[i].velChange;
                    break;

                default:
                    Debug.LogError("Unsupported type " + maneuvers[i].mtype);
                    return;
            }
            orbitPredictors[i].SetVelocity(shipVel);
            lastOrbit = orbitPredictors[i];
        }
    }

    public void Stop() {
        for (int i=0; i < markers.Length; i++) {
            if (addedToGE[i]) {
                ge.RemoveBody(markers[i]);
                addedToGE[i] = false;
                if (markers[i].activeInHierarchy) {
                    markers[i].SetActive(false);
                }
            }
        }
    }

    public void LockPosition() {
        for (int i = 0; i < markers.Length; i++) {
            if (addedToGE[i]) {
                ge.InactivateBody(markers[i]);
            }
        }
    }


    public GameObject[] GetMarkers() {
        return markers;
    }
}
