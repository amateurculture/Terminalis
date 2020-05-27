using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Demonstrate the operation of the LambertUniversal constructor that allows an intercept or rendezvous with 
/// a target NBody in a given orbit. 
/// 
/// Keys allow the target point to be moved. 
/// 
/// T - compute the transfer to the target point with transfer time for min energy route
/// ,/. - decrease/increase transfer time from the min energy value
/// 
/// </summary>
public class LambertRendezvousController : MonoBehaviour {

    [SerializeField]
    private bool rendezvous = false; 

    [SerializeField]
    private  NBody spaceship = null;

    [SerializeField]
    private NBody targetBody = null;

    [SerializeField]
    private NBody centerBody = null; 

    public OrbitPredictor maneuverOrbitPredictor;
    public OrbitSegment maneuverSegment;
    public float targetMoveScale = 1.0f;

    public Text dvText;

    private OrbitData shipOrbit;

    private LambertUniversal lambertU;

    private bool shortPath = true;

    //! use a factor and apply to time of min energy flight
    private double tflightFactor = .75;

    // optional - if there is ManeuverRenderer component on this Game Object then use it
    private ManeuverRenderer maneuverRenderer;

    private bool maneuverDone = false; 

	// Use this for initialization
	void Start () {

        // disable maneuver predictor until things settle (can get Invalid local AABB otherwise)
        maneuverOrbitPredictor.gameObject.SetActive(false);
        maneuverSegment.gameObject.SetActive(false);

        // is there a maneuver renderer?
        maneuverRenderer = GetComponent<ManeuverRenderer>();

        GravityEngine.Instance().AddGEStartCallback(OnGEStart);
    }

    private void OnGEStart() {
        maneuverOrbitPredictor.gameObject.SetActive(true);
        maneuverSegment.gameObject.SetActive(true);
    }


    private void AdjustTimeOfFlight() {
        if (Input.GetKeyDown(KeyCode.Z)) {
            tflightFactor = System.Math.Max(0.1, tflightFactor - 0.1);
        } else if (Input.GetKeyDown(KeyCode.X)) {
            tflightFactor = System.Math.Min(1.5, tflightFactor + 0.1);
        }
    }

    private void ComputeTransfer() {

        OrbitData fromOrbit = new OrbitData();
        fromOrbit.SetOrbitForVelocity(spaceship, centerBody);
        OrbitData toOrbit = new OrbitData();
        toOrbit.SetOrbitForVelocity(targetBody, centerBody);

        // compute the min energy path (this will be in the short path direction)
        lambertU = new LambertUniversal(fromOrbit, toOrbit, shortPath);

        // apply any time of flight change
        double t_flight = tflightFactor * lambertU.GetTMin();
        bool reverse = !shortPath;

        const bool df = false;
        const int nrev = 0;
        int error = lambertU.ComputeXferWithPhasing(reverse, df, nrev, t_flight, rendezvous);
        if (error != 0) {
            Debug.LogWarning("Lambert failed to find solution.");
            maneuverSegment.gameObject.SetActive(false);
            return;
        }

        maneuverOrbitPredictor.SetVelocity(lambertU.GetTransferVelocity());
        maneuverSegment.gameObject.SetActive(true);
        maneuverSegment.SetDestination(lambertU.GetR2().ToVector3());
        maneuverSegment.SetVelocity(lambertU.GetTransferVelocity());
    }

    // Update is called once per frame
    void Update () {

        if (!GravityEngine.Instance().IsSetup())
            return;

        if (Input.GetKeyDown(KeyCode.Space)) {
            // toggle evolution
            GravityEngine.Instance().SetEvolve(!GravityEngine.Instance().GetEvolve());
        } else if (Input.GetKeyDown(KeyCode.M)) {
            // perform the maneuver
            // clobber the existing ship velocity and do the adjustment directly
            GravityEngine.Instance().AddManeuvers(lambertU.GetManeuvers());
            maneuverOrbitPredictor.gameObject.SetActive(false);
            maneuverSegment.gameObject.SetActive(false);
            maneuverRenderer.Clear();
            maneuverDone = true;
        } else if (Input.GetKeyDown(KeyCode.F)) {
            // flip shortPath toggle
            shortPath = !shortPath;
            maneuverSegment.shortPath = shortPath;
        }

        if (!maneuverDone) {
            AdjustTimeOfFlight();
            // Recompute every frame, since in general the ship is moving
            ComputeTransfer();

            if ((maneuverRenderer != null) && (lambertU != null)) {
                maneuverRenderer.ShowManeuvers(lambertU.GetManeuvers());
            }
        }
    }



}
