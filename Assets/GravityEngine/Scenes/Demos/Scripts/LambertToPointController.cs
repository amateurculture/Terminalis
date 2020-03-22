using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Demonstrate the operation of the LambertUniversal constructor that allows the target to be shown as 
/// as a point. Compute the path to the target
/// 
/// Keys allow the target point to be moved. 
/// 
/// L - compute a Lambert trajectory to the target point with transfer time for min energy route
/// ,/. - decrease/increase transfer time from the min energy value
/// 
/// </summary>
public class LambertToPointController : MonoBehaviour {

    public NBody spaceship;
    public OrbitPredictor maneuverOrbitPredictor;
    public OrbitSegment maneuverSegment;
    public NBody centralMass;
    public GameObject targetPoint;
    public float targetMoveScale = 1.0f;

    public Text dvText;

    private OrbitData shipOrbit;

    private LambertUniversal lambertU;

    private bool shortPath = true;

    //! use a factor and apply to time of min energy flight
    private double tflightFactor = 1;

    // optional - if there is ManeuverRenderer component on this Game Object then use it
    private ManeuverRenderer maneuverRenderer;


    private class KeyForMove
    {
        public KeyCode code;
        public Vector3 direction;

        public KeyForMove(KeyCode code, Vector3 v) {
            this.code = code;
            direction = v;
        }
    }

    private KeyForMove[] keyCodes;

	// Use this for initialization
	void Start () {
        keyCodes = new KeyForMove[]{ 
           new KeyForMove(KeyCode.A, new Vector3(-1,0,0)),
           new KeyForMove(KeyCode.D, new Vector3(1, 0, 0)),
           new KeyForMove(KeyCode.W, new Vector3(0, 1, 0)),
           new KeyForMove(KeyCode.S, new Vector3(0, -1, 0)),
           new KeyForMove(KeyCode.Q, new Vector3(0, 0, 1)),
           new KeyForMove(KeyCode.E, new Vector3(0, 0, -1)),
        };

        // disable maneuver predictor until things settle (can get Invalid local AABB otherwise)
        maneuverOrbitPredictor.gameObject.SetActive(false);
        maneuverSegment.gameObject.SetActive(false);

        // is there a maneuver renderer?
        maneuverRenderer = GetComponent<ManeuverRenderer>();
    }


    private void MoveTarget() {
        foreach( KeyForMove key in keyCodes) {
            if (Input.GetKeyDown(key.code)) {
                targetPoint.transform.position += key.direction * targetMoveScale;
                return;
            }
        }
    }

    private void AdjustTimeOfFlight() {
        if (Input.GetKeyDown(KeyCode.Z)) {
            tflightFactor = System.Math.Max(0.1, tflightFactor - 0.1);
        } else if (Input.GetKeyDown(KeyCode.X)) {
            tflightFactor = System.Math.Min(1.5, tflightFactor + 0.1);
        }
    }

    private void ComputeTransfer() {
        Vector3d r_from = GravityEngine.Instance().GetPositionDoubleV3(spaceship);
        // need to set point wrt to GE origin for Lambert calculation
        Vector3d r_to = new Vector3d(GravityEngine.Instance().UnmapFromScene( targetPoint.transform.position));
        OrbitData shipOrbit = new OrbitData();
        shipOrbit.SetOrbitForVelocity(spaceship, centralMass);

        // compute the min energy path (this will be in the short path direction)
        lambertU = new LambertUniversal(shipOrbit, r_from, r_to, shortPath);

        // apply any time of flight change
        double t_flight = tflightFactor * lambertU.GetTMin();
        bool reverse = !shortPath;

        const bool df = false;
        const int nrev = 0;
        int error = lambertU.ComputeXfer(reverse, df, nrev, t_flight);
        if (error != 0) {
            Debug.LogWarning("Lambert failed to find solution.");
            maneuverSegment.gameObject.SetActive(false);
            return;
        }
        Vector3 dv = lambertU.GetTransferVelocity() - GravityEngine.Instance().GetVelocity(spaceship);
        dvText.text = string.Format("dV = {0:00.00}    Time={1:00.00}", dv.magnitude, t_flight);
        maneuverOrbitPredictor.SetVelocity( lambertU.GetTransferVelocity());
        maneuverSegment.gameObject.SetActive(true);
        maneuverSegment.SetDestination(r_to.ToVector3());
        maneuverSegment.SetVelocity(lambertU.GetTransferVelocity());
    }

    // Update is called once per frame
    void Update () {

        if (!GravityEngine.Instance().IsSetup())
            return;

        // Awkward - if allow maneuverOrbit active at the start get a pair of Invalid AABB exceptions
        // then it settles down. Do this as a work around - but look for a better fix!
        if (!maneuverOrbitPredictor.gameObject.activeInHierarchy) {
            maneuverOrbitPredictor.gameObject.SetActive(true);
            maneuverSegment.gameObject.SetActive(true);
        }

        MoveTarget();

        if (Input.GetKeyDown(KeyCode.Space)) {
            // toggle evolution
            GravityEngine.Instance().SetEvolve(!GravityEngine.Instance().GetEvolve());
        } else if (Input.GetKeyDown(KeyCode.M)) {
            // perform the maneuver
            // clobber the existing ship velocity and do the adjustment directly
            GravityEngine.Instance().SetVelocity(spaceship, lambertU.GetTransferVelocity());
        } else if (Input.GetKeyDown(KeyCode.F)) {
            // flip shortPath toggle
            shortPath = !shortPath;
            maneuverSegment.shortPath = shortPath;
        }

        AdjustTimeOfFlight();
        // Recompute every frame, since in general the ship is moving
        ComputeTransfer();

        if ((maneuverRenderer != null) && (lambertU != null)) {
            maneuverRenderer.ShowManeuvers(lambertU.GetManeuvers());
        }
    }



}
