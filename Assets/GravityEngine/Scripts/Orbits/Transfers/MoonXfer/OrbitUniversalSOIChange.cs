using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Handle the change of centerbody when an object on rails enters/leaves a sphere of influence
/// (SOI). 
/// </summary>
[RequireComponent(typeof(OrbitUniversal))]
public class OrbitUniversalSOIChange : MonoBehaviour, IPatchedConicChange
{
    [Tooltip("Optional orbit predictor")]
    [SerializeField]
    private OrbitPredictor orbitPredictor = null;

    private OrbitUniversal orbitU;
    private KeplerSequence keplerSeq;

    public void Awake() {

        orbitU = GetComponent<OrbitUniversal>();
        keplerSeq = GetComponent<KeplerSequence>();
    }

    /// <summary>
    /// Do hand off from one center to a new center in the OrbitUniversal class. 
    /// </summary>
    /// <param name="newObject"></param>
    /// <param name="oldObject"></param>
    public void OnNewInfluencer(NBody newObject, NBody oldObject) {

        if (keplerSeq != null) {
            orbitU = keplerSeq.GetCurrentOrbit();
        }
        orbitU.SetNewCenter(newObject);
        if (orbitPredictor != null) {
            orbitPredictor.SetCenterObject(newObject.gameObject);
        }
    }


}
