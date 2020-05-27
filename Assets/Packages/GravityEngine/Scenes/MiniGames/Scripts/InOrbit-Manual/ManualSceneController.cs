using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implement a UI to allow a player to set a point on the existing orbit of the spaceship and specify a
/// maneuver at that future point. The controller has the following modes:
///     IDLE: Evolve the ship (allow spacebar to pause/resume)
///     SET_POINT: Using the mouse or user pulldown, set the position of the point at which to 
///                specify a maneuver
///     SET_MANEUVER: Using the mouse, drag to set the ship velocity at the specified point. 
///         - press X to accept the manuever
///     EVOLVE_TO_MANEUVER: Leave the orbit point in place (without velocity controls) until the maneuver is
///         executed. Once executed return to IDLE
/// 
/// UI:
///     M: Set manuever
///         - causes IDLE -> SET_MANUEVER
///        
/// </summary>
public class ManualSceneController : MonoBehaviour
{
    [SerializeField]
    private NBody spaceship = null;

    [SerializeField]
    //! Object in scene to be used as the ship at the orbit point. Needs to have a ManualShipControl and OrbitPredictor. 
    //! (Best if this is inactive to avoid race condition with GE detecting NBody objects)
    private GameObject shipAtOrbitPoint = null;

    private OrbitPoint orbitPoint;

    private ManualShipControl shipControl; 

    private enum State { IDLE, SET_MANUEVER, EVOLVE_TO_MANEUVER};
    private State state = State.IDLE;

    private GravityEngine ge;

    private NBody shipNbody; 
    private Vector3 lastShipPos; 

    // Start is called before the first frame update
    void Start()
    {
        shipControl = shipAtOrbitPoint.GetComponent<ManualShipControl>();
        if (shipControl == null) {
            Debug.LogError("Misconfigured. The shipAtOrbitPoint needs to have a ManualShipControl component.");
        }

        orbitPoint = shipAtOrbitPoint.GetComponent<OrbitPoint>();
        if (orbitPoint == null) {
            Debug.LogError("Misconfigured. The shipAtOrbitPoint needs to have a OrbitPoint component.");
        }

        ge = GravityEngine.Instance();
        SetState(state);
    }

    private void SetState(State newState) {
        Debug.LogFormat("State transition from {0} => {1}", state, newState);
        switch(newState)
        {
            case State.IDLE:
                shipAtOrbitPoint.SetActive(false);
                shipControl.SetActive(false);
                break;

            case State.SET_MANUEVER:
                ge.AddBody(shipAtOrbitPoint);
                shipAtOrbitPoint.SetActive(true);
                shipControl.SetActive(true);
                shipNbody = shipAtOrbitPoint.GetComponent<NBody>();
                lastShipPos = ge.GetPhysicsPosition(shipNbody);
                shipControl.ShipMoved();
                break;

            case State.EVOLVE_TO_MANEUVER:
                shipControl.SetActive(false);
                ge.RemoveBody(shipAtOrbitPoint);
                ge.SetEvolve(true);
                break;

            default:
                break;
        }
        state = newState;
    }

    private void ManeuverExecuted(Maneuver m) {
        shipAtOrbitPoint.SetActive(false);
        SetState(State.IDLE);
    }

    // Update is called once per frame
    void Update()
    {     
        switch(state) {
            case State.IDLE:
                if (Input.GetKeyUp(KeyCode.M)) {
                    // enter manuever mode
                    SetState(State.SET_MANUEVER);
                }
                break;

            case State.SET_MANUEVER:
                if (Input.GetKeyUp(KeyCode.M)) {
                    // exit maneuver mode back to idle
                    SetState(State.IDLE);
                } else if(Input.GetKeyUp(KeyCode.X)) {
                    // Create a manuever at the orbit point and enter EVOLVE_TO_MANEUVER
                    // (when maneuver completes callback will move state back to idle)
                    Maneuver maneuver = shipControl.CreateManeuver(spaceship, orbitPoint.GetOrbit());
                    maneuver.onExecuted = ManeuverExecuted;
                    ge.AddManeuver(maneuver);
                    SetState(State.EVOLVE_TO_MANEUVER);
                    break;
                } else if (Input.GetKeyUp(KeyCode.A)) {
                    orbitPoint.SetPointType(OrbitPoint.PointType.APOAPSIS);
                } else if (Input.GetKeyUp(KeyCode.P)) {
                    orbitPoint.SetPointType(OrbitPoint.PointType.PERIAPSIS);
                } else if (Input.GetKeyUp(KeyCode.U)) {
                    orbitPoint.SetPointType(OrbitPoint.PointType.PHASE_FROM_MOUSE);
                }

                // if the orbit point has moved, get ManualShipControl to move widgets etc. 
                Vector3 shipPos = ge.GetPhysicsPosition(shipNbody);
                if ((shipPos-lastShipPos).magnitude > 1E-2) {
                    shipControl.ShipMoved();
                }
                lastShipPos = shipPos;
                // If user is dragging a velocity etc, then it gets priority with the mouse input, otherwise
                // interpret the click as a request to move to orbit point. 
                if (!shipControl.HandleMouseInput())
                    orbitPoint.HandleMouseInput();
                break;

            default:
                break;
        }
    }
}
