using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implement a UI to allow a player to set a point on the existing orbit of the spaceship and specify a
/// transfer at that future point. The point at which the tranfer is to start is managed by an OrbitPoint
/// component attached to the shipAtOrbitPoint. 
/// 
/// The type of transfer is specified by the TransferShip component attached to the shipAtOrbitPoint. In the case
/// of Hohmann and Lambert transfers the starting point will be the designated orbit point. If a Hohmann Rendezvous
/// is selected the start point will be changed to reflect the position required for correct staging. 
/// 
/// The display of the sequence of maneuvers is updated as the ship orbit point is updated. This is delegated to the
/// DisplayManeuvers component. 
/// 
/// Once active (M pressed) the shipAtOrbitPoint OrbitPoint becomes active and mouse clicks can be used to adjust 
/// the location of the transfer point. If a Lambert transfer is selected the OrbitPoint on the destination is ALSO
/// active and this controller needs to determine which orbitPoint will handle mouse events. 
/// 
/// If a LAMBERT transfer has been selected then the radial movement of the mouse wrt center body will adjust the 
/// transfer time of the orbit in a non-linear fashion. 
/// 
/// The controller has the following modes:
///     IDLE: Evolve the ship (allow spacebar to pause/resume)
///     SET_POINT: Using the mouse or user pulldown, set the position of the point at which to 
///                specify a maneuver
///     SET_HOHMANN: Using the mouse, drag to set the ship velocity at the specified point. 
///         - press X to accept the manuever
///     SET_LAMBERT: Mouse may be controlling start or end point. State needs to determine which orbitPoint
///         should be 
///     EVOLVE_TO_MANEUVER: Leave the orbit point in place (without velocity controls) until the maneuver is
///         executed. Once executed return to IDLE
/// 
/// UI:
///     M: Set manuever
///         - causes IDLE -> SET_MANUEVER
///         
///     X: Execute maneuver
///         - add the maneuvers to GE and add a callback to set state to IDLE once complete
///         - SET_MANUEVER -> EVOLVE_TO_MANEUVER
///         
///    W/S: Adjust time of flight factor for Lambert transfers. W to decrease, S to increase
///         
///     mouse:
///         - Hohmann mode the mouse is used only to set the phase of the starting OrbitPoint (if the OrbitPoint is
///           in PHASE_FROM_MOUSE mode)
///         - Lambert Point/Lambert Orbit transfers can adjust both the source and destination OrbitPoints. The selection is made
///           based on the distance to the orbit points when the button is pressed down. The mouse position
///           is used to set the phase of the selected point
///         
/// In SET_HOHMANN
///         
/// Time of arrival is the time of the last maneuver in the sequence. 
/// 
/// Note: SpaceshipWithOrbitPoint and LambertDestination point MUST be inactive at the start, otherwise
///       GE will report an error. (Scene will continue to run properly)
///       
/// In Lambert modes: 
///        
/// </summary>
[RequireComponent(typeof(DisplayManeuvers))]
public class TransferSceneController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Ship that will be perfoming an orbit transfer")]
    private NBody spaceship = null;

    [SerializeField]
    [Tooltip("Target object")]
    private NBody target = null; 

    [SerializeField]
    [Tooltip("Ghost ship used during transfer display. Needs TransferShip and OrbitPredictor attached")]
    //! Object in scene to be used as the ship at the orbit point. Needs to have a TransferShip and OrbitPredictor. 
    //! (Best if this is inactive to avoid race condition with GE detecting NBody objects)
    private GameObject shipAtOrbitPoint = null;

    [SerializeField]
    [Tooltip("(optional) Destination if Lambert point transfer is selected. (OrbitPoint without NBody)")]
    private OrbitPoint destinationOrbitPoint = null;


    //! Point at which the transfer will start (may be over-ruled if a rendezvous transfer is selected).
    private OrbitPoint shipOrbitPoint;

    //! (optional) Destination NBody for the case where the transfer is a rendezvous. 
    private TransferShip transferShip; 

    private enum State { IDLE, SET_HOHMANN, SET_LAMBERT_RI, SET_LAMBERT_PO, EVOLVE_TO_MANEUVER};
    private State state = State.IDLE;

    private OrbitPoint activeOrbitPoint; 

    private GravityEngine ge;

    private DisplayManeuvers displayManeuvers;

    //! factor to adjust min energy Lambert transfer time by. Used when transfer ship is in LAMBERT_ORBIT mode. 
    private float xferTimeFactor = 1.0f;

    const float TIME_FACTOR_DELTA = 0.05f;

    // Start is called before the first frame update
    void Start()
    {
        transferShip = shipAtOrbitPoint.GetComponent<TransferShip>();
        if (transferShip == null) {
            Debug.LogError("Misconfigured. The shipAtOrbitPoint needs to have a TransferShip component.");
        }

        shipOrbitPoint = shipAtOrbitPoint.GetComponent<OrbitPoint>();
        if (shipOrbitPoint == null) {
            Debug.LogError("Misconfigured. The shipAtOrbitPoint needs to have a OrbitPoint component.");
        }

        displayManeuvers = GetComponent<DisplayManeuvers>();

        // ensure OrbitPoint mouse input is controlled from here
        shipOrbitPoint.SetMouseControl(false);
        if (destinationOrbitPoint != null) {
            destinationOrbitPoint.SetMouseControl(false);
            destinationOrbitPoint.gameObject.SetActive(false);
        }

        ge = GravityEngine.Instance();
        activeOrbitPoint = shipOrbitPoint;
        SetState(state);
    }

    private void ActivateGhostShip() {
        ge.AddBody(shipAtOrbitPoint);
        // set orbit point to lead ship position 
        OrbitUniversal shipOrbit = spaceship.GetComponent<OrbitUniversal>();
        shipOrbitPoint.SetMousePhase(30f + (float)shipOrbit.GetCurrentPhase());

        shipAtOrbitPoint.SetActive(true);
        shipOrbitPoint.DoUpdate();

        if (destinationOrbitPoint != null) {
            destinationOrbitPoint.gameObject.SetActive(true);
            NBody destNBody = destinationOrbitPoint.GetNBody();
            if (destNBody != null) {
                ge.AddBody(destNBody.gameObject);
                if (state != State.SET_LAMBERT_PO) {
                    // update the ghost target based on how far the spaceshipAtOrbitPoint has moved. 
                    // (this is needed so have updated position for intercept/rendezvous modes)
                    double timeToPoint = shipOrbitPoint.TimeToOrbitPoint(spaceship);
                    destinationOrbitPoint.SetTime(target, timeToPoint);
                }
            }
            destinationOrbitPoint.DoUpdate();
        }
        ge.SetEvolve(false);
    }

    private void DeactivateGhostShip() {
        if (destinationOrbitPoint != null) {
            destinationOrbitPoint.gameObject.SetActive(false);
            NBody destNBody = destinationOrbitPoint.GetNBody();
            if (destNBody != null) {
                ge.RemoveBody(destNBody.gameObject);
            }
        }
        ge.RemoveBody(shipAtOrbitPoint);
        shipAtOrbitPoint.SetActive(false);
    }

    private void SetState(State newState) {
        Debug.LogFormat("State transition from {0} => {1}", state, newState);
        state = newState;
        switch (newState)
        {
            case State.IDLE:
                shipAtOrbitPoint.SetActive(false);
                displayManeuvers.Stop();
                if (destinationOrbitPoint != null) {
                    destinationOrbitPoint.gameObject.SetActive(false);
                }
                break;

            case State.SET_HOHMANN:
                destinationOrbitPoint.SetPointType(OrbitPoint.PointType.FIXED_TIME);
                ActivateGhostShip();
                updateTransfer = true;
                break;

            case State.SET_LAMBERT_RI:
                destinationOrbitPoint.SetPointType(OrbitPoint.PointType.FIXED_TIME);
                ActivateGhostShip();
                updateTransfer = true;
                break;

            case State.SET_LAMBERT_PO:
                destinationOrbitPoint.SetPointType(OrbitPoint.PointType.PHASE_FROM_MOUSE);
                ActivateGhostShip();
                updateTransfer = true;
                break;

            case State.EVOLVE_TO_MANEUVER:
                DeactivateGhostShip();
                ge.SetEvolve(true);
                break;

            default:
                break;
        }
    }

    private void ManeuverExecuted(Maneuver m) {
        SetState(State.IDLE);
    }

    private bool HandleKeyboardInput(OrbitPoint orbitPoint) {
        if (Input.GetKeyUp(KeyCode.M)) {
            if (state == State.IDLE) {
                switch (transferShip.GetTransferType()) {
                    case TransferShip.Transfer.HOHMANN:
                    case TransferShip.Transfer.HOHMANN_RDVS:
                        SetState(State.SET_HOHMANN);
                        break;

                    case TransferShip.Transfer.LAMBERT_POINT:
                    case TransferShip.Transfer.LAMBERT_ORBIT:
                        SetState(State.SET_LAMBERT_PO);
                        break;

                    case TransferShip.Transfer.LAMBERT_RDVS:
                    case TransferShip.Transfer.LAMBERT_INTERCEPT:
                        SetState(State.SET_LAMBERT_RI);
                        break;
                }
            } else {
                SetState(State.IDLE);
            }
            return true; 
        } else if (Input.GetKeyUp(KeyCode.X)) {
            if ((state == State.IDLE) || (state == State.EVOLVE_TO_MANEUVER)) {
                return false;
            }
            // Create a manuever at the orbit point and enter EVOLVE_TO_MANEUVER
            // (when maneuver completes callback will move state back to idle)
            List<Maneuver> maneuvers = transferShip.GetTransfer().GetManeuvers();
            // Need to adjust maneuvers:
            // - apply to spaceship (not the ghost at the orbit point)
            // - maneuver time needs to begin when ship reached point where ghost ship is
            double timeToPoint = shipOrbitPoint.TimeToOrbitPoint(spaceship);
            foreach (Maneuver m in maneuvers) {
                m.nbody = spaceship;
                m.worldTime += (float)timeToPoint;
            }
            // callback when last maneuver is executed
            transferShip.DoTransfer(ManeuverExecuted);
            SetState(State.EVOLVE_TO_MANEUVER);
            displayManeuvers.LockPosition();
            return true;
        } else if (Input.GetKeyUp(KeyCode.A)) {
            orbitPoint.SetPointType(OrbitPoint.PointType.APOAPSIS);
        } else if (Input.GetKeyUp(KeyCode.P)) {
            orbitPoint.SetPointType(OrbitPoint.PointType.PERIAPSIS);
        } else if (Input.GetKeyUp(KeyCode.U)) {
            orbitPoint.SetPointType(OrbitPoint.PointType.PHASE_FROM_MOUSE);
        }
        return false; 
    }

    private bool CheckLambertTimeAdjust() {
        bool adjust = false; 
        // Code to see if transfer time should be adjusted
        if (Input.GetKeyDown(KeyCode.W)) {
            xferTimeFactor -= TIME_FACTOR_DELTA;
            adjust = true; 
        } else if (Input.GetKeyDown(KeyCode.S)) {
            xferTimeFactor += TIME_FACTOR_DELTA;
            adjust = true; 
        }
        transferShip.SetTransferTimeFactor(xferTimeFactor);
        return adjust; 
    }

    // Update is called once per frame
    // Needs to be LateUpdate so that OrbitPoint is guaranteed to run before this and set the position and
    // velocity of the ghost ship. 

    // Make this stateful to allow it to be set when we first enter a transfer state
    bool updateTransfer = false;

    void LateUpdate()
    {
        if (HandleKeyboardInput(shipOrbitPoint))
            return; // let ge update (this assumes game frame rate > fixed update rate). Icky.


        switch (state) {
            case State.IDLE:
                return;

            case State.SET_HOHMANN:
                updateTransfer |= shipOrbitPoint.HandleMouseInput();
                break;

            case State.SET_LAMBERT_PO:
                if (Input.GetMouseButtonDown(0)) {
                    // user has pressed the mouse button in this frame, need to determine which OrbitPoint it is 
                    // closer to and send events there until it is re-pressed. 
                    float startDistance = shipOrbitPoint.MouseDistanceToShip();
                    float destDistance = destinationOrbitPoint.MouseDistanceToShip();
                    if (startDistance < destDistance) {
                        activeOrbitPoint = shipOrbitPoint;
                    } else {
                        activeOrbitPoint = destinationOrbitPoint;
                    }
                } else if(Input.GetMouseButton(0)) {
                    updateTransfer |= activeOrbitPoint.HandleMouseInput();
                }
                if (destinationOrbitPoint != null) {
                    if (transferShip.GetTransferType() == TransferShip.Transfer.LAMBERT_POINT) {
                        transferShip.SetTargetPoint(destinationOrbitPoint.GetPhysicsPosition3d());
                    } else {
                        transferShip.SetTargetPhase(destinationOrbitPoint.GetPhase());
                    }
                }
                updateTransfer |= CheckLambertTimeAdjust();
                break;

            case State.SET_LAMBERT_RI:
                updateTransfer |= shipOrbitPoint.HandleMouseInput();
                updateTransfer |= CheckLambertTimeAdjust();
                break;

            default:
                break;
        }
        if (updateTransfer) {
            if (activeOrbitPoint == shipOrbitPoint) {
                if (state != State.SET_LAMBERT_PO) {
                    // update the ghost target based on how far the spaceshipAtOrbitPoint has moved. 
                    // (this is needed so have updated position for intercept/rendezvous modes)
                    double timeToPoint = shipOrbitPoint.TimeToOrbitPoint(spaceship);
                    destinationOrbitPoint.SetTime(target, timeToPoint);
                }
            }
            transferShip.ShipMoved();
            if (transferShip.GetTransfer() != null)
                displayManeuvers.Display(transferShip.GetTransfer().GetManeuvers());
        }
        updateTransfer = false; 
    }
}
