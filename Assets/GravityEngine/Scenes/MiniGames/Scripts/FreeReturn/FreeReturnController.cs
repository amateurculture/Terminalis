using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Lunar Transfer Calculator
/// This code is only intended for looking at the trajecotry path for different starting and target
/// phases from a circular start orbit to a point on the moon sphere of influence (SOI). It displays the
/// orbit in the equitorial plane but will handle inclination IF the ship and moon orbit have the same
/// inclination.
/// 
/// Keys AS control the phase of the starting point (spaceship). 
/// Keys KL control the phase of the destination point on the SOI. 
/// Keys WS increase/decreae the transfer time. 
/// Key E resets the transfer time to the minimum energy transfer for the selected points. 
/// 
/// The point to point Lambert transfer is calculated when there is a keypress.
/// 
/// Code is quick and dirty and transform position based so will not scale. 
/// - assumes planet is at (0,0,0)
/// 
/// </summary>
public class FreeReturnController : MonoBehaviour, IPatchedConicChange
{

    [Header("Transfer Parameters")]
    [Tooltip("Change in SOI position per key press")]
    [SerializeField]
    private float dAngleDegrees = 0.1f;

    // depart from optimal orbit phase
    private float shipAngleDeg = 180f;
    private float shipAngle;

    [SerializeField]
    private float soiAngleDeg = 0.6f * 180f;
    private float soiAngle;

    [SerializeField]
    [Tooltip("Transfer time expressed as fraction of Hohmann xfer time.")]
    private double tflightFactor = 1.0;

    [SerializeField]
    [Tooltip("Perform free return trajectory as KeplerSequence on-rails")]
    private bool onRails = false;

    [Header("Object Handles")]
    [Tooltip("Spaceship to be sent to the moon")]
    [SerializeField]
    private NBody spaceship = null;

    [SerializeField]
    private NBody planet = null;

    [SerializeField]
    private NBody moonBody = null;

    //! ship that shows target position at SOI entry. Position adjusted by KL keys on SOI. 
    [SerializeField]
    private NBody shipEnterSOI = null;

    //! ship that shows resulting exit position at SOI, determined from SOI position by hyperbola around moon
    [SerializeField]
    private NBody shipExitSOI = null;

    [SerializeField]
    private OrbitPredictor toMoonOrbit = null;

    [SerializeField]
    private OrbitSegment toMoonSegment = null;

    [SerializeField]
    private OrbitSegment aroundMoonSegment = null;

    //! Text to show summary of maneuver (optional)
    [Header("UI Components (optional)")]

    //! Text field used to display instructions
    [SerializeField]
    private Text instructions = null;

    //! Flag that designated which "way around" to go on the transfer ellipse. Toggled with F key
    private bool shortPath = true;

    private LambertUniversal lambertU;

    private float soiRadius;
    private float shipRadius;
    private float moonRadius;

    /// <summary>
    /// Game state:
    /// SELECT_DEST: Use N key to toggle which ellipse is the destination orbit
    /// COMPUTE_MANEUVER: With a selected target use AD to designate position on target 
    ///                   ellipse. Use WS to control transfer time
    /// DOING_XFER: In flight to maneuver point on target ellipse. 
    /// </summary>


    private Vector3d targetPoint;
    private Vector3d startPoint;

    private GravityEngine ge;

    private const float tFlightAdjust = 0.005f;

    private bool running;

    // Use this for initialization
    void Start() {

        ge = GravityEngine.Instance();

        shipAngle = shipAngleDeg * Mathf.Deg2Rad;
        soiAngle = soiAngleDeg * Mathf.Deg2Rad;

        // disable maneuver predictor until things settle (can get Invalid local AABB otherwise)
        SetOrbitDisplays(false);

        // mass scaling will cancel in this ratio
        soiRadius = OrbitUtils.SoiRadius(planet, moonBody);
        toMoonOrbit.hyperDisplayRadius = soiRadius;

        // TODO: allow moon to be OrbitUniversal as well.
        OrbitEllipse moonEllipse = moonBody.gameObject.GetComponent<OrbitEllipse>();
        moonRadius = moonEllipse.a_scaled;
        targetPoint = new Vector3d(moonRadius, soiRadius, 0);

        float inclination = 0; 
        OrbitEllipse shipEllipse = spaceship.gameObject.GetComponent<OrbitEllipse>();
        if (shipEllipse != null) {
            shipRadius = shipEllipse.a_scaled;
            inclination = shipEllipse.inclination;
        } else {
            OrbitUniversal orbitU = spaceship.GetComponent<OrbitUniversal>();
            if (orbitU != null) {
                // assuming circular orbit
                shipRadius = (float)orbitU.GetApogee();
                inclination = (float) orbitU.inclination;
            }
        }

        // check moon and ship orbit are co-planar
        if (Mathf.Abs(inclination - moonEllipse.inclination) > 1E-3) {
            Debug.LogWarning("Ship inclination and moon inclination are not equal.");
        }

        startPoint = new Vector3d(0, -shipRadius, 0);
        UpdateSoiPosition();
        UpdateStartPosition();

    }

    private void SetOrbitDisplays(bool state) {
        toMoonOrbit.gameObject.SetActive(state);
        toMoonSegment.gameObject.SetActive(state);
        aroundMoonSegment.gameObject.SetActive(state);
    }

    private bool AdjustTimeOfFlight() {
        bool keyPressed = false;
        if (Input.GetKeyDown(KeyCode.S)) {
            tflightFactor = System.Math.Max(0.1, tflightFactor - tFlightAdjust);
            keyPressed = true;
        } else if (Input.GetKeyDown(KeyCode.W)) {
            tflightFactor = System.Math.Min(1.5, tflightFactor + tFlightAdjust);
            keyPressed = true;
        }
        return keyPressed;
    }

    private void UpdateSoiPosition() {
        targetPoint = new Vector3d(soiRadius * Mathf.Cos(soiAngle) + moonRadius,
                                         soiRadius * Mathf.Sin(soiAngle),
                                         0);
        targetPoint = GravityScaler.ScaleVector3dPhyToScene(targetPoint);
    }

    private void UpdateStartPosition() {
        startPoint = new Vector3d(shipRadius * Mathf.Cos(shipAngle),
                                      shipRadius * Mathf.Sin(shipAngle),
                                      0);
        startPoint = GravityScaler.ScaleVector3dPhyToScene(startPoint);
    }

    /// <summary>
    /// Use the A/D to position the maneuver symbol on the start orbit. 
    /// Use KL to position the symbol on the SOI. 
    /// 
    /// As move to a new maneuver destination the transfer time will reset to the 
    /// minimum energy value. At a given maneuver position, can use W/S to increase/decrease
    /// the transfer time. 
    /// </summary>
    private bool UpdateManeuverSymbols() {
        bool keyPressed = false;
        // ship
        if (Input.GetKeyDown(KeyCode.K)) {
            soiAngle = (float)NUtils.AngleMod2Pi(soiAngle + dAngleDegrees * Mathf.Deg2Rad);
            UpdateSoiPosition();
            keyPressed = true;
        } else if (Input.GetKeyDown(KeyCode.L)) {
            soiAngle = (float)NUtils.AngleMod2Pi(soiAngle - dAngleDegrees * Mathf.Deg2Rad);
            UpdateSoiPosition();
            keyPressed = true;
        }
        // update degree values so can see result in inspector
        shipAngleDeg = shipAngle * Mathf.Rad2Deg;
        soiAngleDeg = soiAngle * Mathf.Rad2Deg;
        return keyPressed;
    }

    /// <summary>
    /// Computes the transfer with the moon on the +X axis without accounting for the moon motion during
    /// transit. (That is accounted for in the ExecuteTransfer routine). 
    /// 
    /// This allows a co-rotating visualization of the orbit. 
    /// </summary>
    /// <returns></returns>
    private Vector3 ComputeTransfer() {
        OrbitData shipOrbit = new OrbitData();
        shipOrbit.SetOrbitForVelocity(spaceship, planet);

        // compute the min energy path (this will be in the short path direction)
        lambertU = new LambertUniversal(shipOrbit, startPoint, targetPoint, shortPath);

        // apply any time of flight change
        double t_flight = tflightFactor * lambertU.GetTMin();
        bool reverse = !shortPath;

        const bool df = false;
        const int nrev = 0;
        int error = lambertU.ComputeXfer(reverse, df, nrev, t_flight);
        if (error != 0) {
            Debug.LogWarning("Lambert failed to find solution.");
            aroundMoonSegment.gameObject.SetActive(false);
            return Vector3.zero;
        }
        // Check Lambert is going in the correct direction
        Vector3 shipOrbitAxis = Vector3.Cross(ge.GetVelocity(spaceship), ge.GetPhysicsPosition(spaceship)).normalized;
        Vector3 tliOrbitAxis = Vector3.Cross(lambertU.GetTransferVelocity(), startPoint.ToVector3());
        if (Vector3.Dot(shipOrbitAxis, tliOrbitAxis) < 0) {
            error = lambertU.ComputeXfer(!reverse, df, nrev, t_flight);
            if (error != 0) {
                Debug.LogWarning("Lambert failed to find solution for reverse path. error=" + error);
                return Vector3.zero;
            }
        }

        Vector3 tliVelocity = lambertU.GetTransferVelocity();
        toMoonOrbit.SetVelocity(tliVelocity);
        toMoonSegment.SetVelocity(tliVelocity);
        aroundMoonSegment.gameObject.SetActive(true);

        // Set velocity for orbit around moon
        Vector3 soiEnterVel = lambertU.GetFinalVelocity();
        aroundMoonSegment.SetVelocity(soiEnterVel);

        // update shipEnterSOI object
        ge.UpdatePositionAndVelocity(shipEnterSOI, targetPoint.ToVector3(), soiEnterVel);

        // Find the orbit around the moon. By using the mirror position we're assuming it's
        // a hyperbola (since there is no course correction at SOI this is true). 
        // (Moon is in correct position for these calcs so can use world positions, relativePos=false)
        Vector3d soiEnterV = new Vector3d(lambertU.GetFinalVelocity());
        OrbitUtils.OrbitElements oe = OrbitUtils.RVtoCOE(targetPoint, soiEnterV, moonBody, false);
        Vector3d soiExitR = new Vector3d();
        Vector3d soiExitV = new Vector3d();
        OrbitUtils.COEtoRVMirror(oe, moonBody, ref soiExitR, ref soiExitV, false);
        // Set position and vel for exit ship, so exit orbit predictor can run. Moon offset/vel already added.
        ge.SetPositionDoubleV3(shipExitSOI, soiExitR);
        ge.SetVelocityDoubleV3(shipExitSOI, soiExitV);
        aroundMoonSegment.UpdateOrbit();
        return tliVelocity;
    }

    /// <summary>
    /// Set up a KeplerSeqeunce to do the three phases of the transfer as Kepler mode conics.
    /// 
    /// Leave the existing ship orbit as the first 
    /// </summary>
    /// <param name="transferTime"></param>
    private void TransferOnRails(double transferTime, Vector3 shipPos, Vector3 shipVel, float moonOmega) {
        // the ship needs to have a KeplerSequence
        KeplerSequence kseq = spaceship.GetComponent<KeplerSequence>();
        if (kseq == null) {
            Debug.LogError("Could not find a KeplerSequence on " + spaceship.name);
            return;
        }
        float moonPhaseDeg = moonOmega * (float)transferTime * Mathf.Rad2Deg;
        Quaternion moonPhaseRot = Quaternion.AngleAxis(moonPhaseDeg, Vector3.forward);

        // Ellipse 1: shipPos/shipvel already phased by the caller.
        double t = ge.GetPhysicalTime();
        KeplerSequence.ElementStarted noCallback = null;
        kseq.AppendElementRVT(new Vector3d(shipPos), new Vector3d(shipVel), t, false, spaceship, planet, noCallback);

        // Hyperbola: start at t + transferTime
        // have targetPoint and final velocity from LambertTransfer. Need to make these wrt moon at this time
        // targetPoint is w.r.t current moon position, but need to rotate around SOI by amount moon will shift
        // as ship transits to moon
        Vector3 targetPos = targetPoint.ToVector3();
        Vector3 moonPosAtSoi = moonPhaseRot * ge.GetPhysicsPosition(moonBody);
        Vector3 moonVelAtSoi = moonPhaseRot * ge.GetVelocity(moonBody);
        // get the relative positions (i.e. as if moon at the origin with v=0)
        Vector3 adjustedTarget = moonPhaseRot * targetPos - moonPosAtSoi;
        Vector3 adjustedVel = moonPhaseRot * lambertU.GetFinalVelocity() - moonVelAtSoi;

        // Create moon hyperbola at the moon position after flight to moon. This means the init cannot make reference
        // to the CURRENT moon position. 
        Vector3d soiEnterR = new Vector3d(adjustedTarget);
        Vector3d soiEnterV = new Vector3d(adjustedVel);
        OrbitUniversal hyperOrbit = kseq.AppendElementRVT(soiEnterR, 
                                            soiEnterV, 
                                            t + transferTime, 
                                            true,
                                            spaceship, 
                                            moonBody,
                                            EnterMoonSoi);

        // Find the hyperbola exit SOI position/vel 
        OrbitUtils.OrbitElements oe = OrbitUtils.RVtoCOE(soiEnterR, soiEnterV, moonBody, true);
        Vector3d soiExitR = new Vector3d();
        Vector3d soiExitV = new Vector3d();
        Debug.Log("oe=" + oe);
        // Gives position and velocity in relative position 
        OrbitUtils.COEtoRVMirror(oe, moonBody, ref soiExitR, ref soiExitV, true);

        // Determine hyperbola transit time to the soiExit position
        double hyperTOF = hyperOrbit.TimeOfFlight(soiEnterR, soiExitR);
        //Debug.LogFormat("Hyper TOF={0} r0={1} r1={2} p={3}", hyperTOF, adjustedTarget, soiExitR, 
        //    hyperOrbit.p);

        // Ellipse 2:
        // Adjust phase to allow for moon travel during hyperbola transit
        // Need to set position and vel relative to the planet using position relative to moon at 0
        moonPhaseDeg = moonOmega *  (float) hyperTOF * Mathf.Rad2Deg;
        Quaternion moonHyperRot = Quaternion.AngleAxis(moonPhaseDeg, Vector3.forward);
        Vector3 moonAtExit = moonHyperRot * moonPosAtSoi;
        Vector3 moonVelAtExit = moonHyperRot * moonVelAtSoi;
        Vector3 soiExitwrtPlanet = soiExitR.ToVector3()  + moonAtExit;
        // soiexitV is relative to moon at (0,0,0) BUT frame of hyperbola does not rotate
        Vector3 soiExitVelwrtPlanet = moonHyperRot  * soiExitV.ToVector3() + moonVelAtExit;
        Debug.LogFormat("Ellipse2: soiExitV={0} moonV={1} net={2}", soiExitV, moonVelAtExit, soiExitVelwrtPlanet);

        kseq.AppendElementRVT(new Vector3d(soiExitwrtPlanet),
                                new Vector3d(soiExitVelwrtPlanet),
                                t + transferTime + hyperTOF,
                                true,
                                spaceship,
                                planet,
                                ExitMoonSoi);
        running = true;
    }

    /// <summary>
    /// Callback for when ship enters the moons SOI. Change the center object of the moon orbit predictor
    /// to be the moon. 
    /// </summary>
    /// <param name="orbitU"></param>
    public void EnterMoonSoi(OrbitUniversal orbitU) {
        Debug.Log("Enter SOI");
        toMoonOrbit.SetCenterObject(moonBody.gameObject);
    }

    /// <summary>
    /// Callback for when the ship leaves the moon SOI. Change the the ship orbit predictor center object
    /// back to the planet. 
    /// </summary>
    /// <param name="orbitU"></param>
    public void ExitMoonSoi(OrbitUniversal orbitU) {
        Debug.Log("Exit SOI");
        toMoonOrbit.SetCenterObject(planet.gameObject);
    }

    private void ExecuteTransfer() {

        // Need to account for phasing, rotate ship forward to correct launch point and 
        // rotate TLI vector. (This assumes circular orbit in the XY plane with the planet at the origin!)
        // Should set as a maneuver, but just jump there for demonstration code
        double transferTime = tflightFactor * lambertU.GetTMin();
       
        float moonOmega = (float) System.Math.Sqrt( ge.GetMass(planet)) / Mathf.Sqrt(moonRadius * moonRadius * moonRadius);
        float shipThetaDeg = (float)(transferTime * moonOmega) * Mathf.Rad2Deg;
        Debug.LogFormat("t={0} theta={1} deg. omega={2} rad", transferTime, shipThetaDeg, moonOmega);
        // Inclination support. Recompute the transfer using inclination
        // @TODO: HACK XY only
        Vector3 shipPos = ge.GetPhysicsPosition(spaceship);
        Vector3 shipPosPhased = Quaternion.AngleAxis(shipThetaDeg, Vector3.forward) * shipPos;
        Vector3 shipVelPhased = Quaternion.AngleAxis(shipThetaDeg, Vector3.forward) * lambertU.GetTransferVelocity();
        if (onRails) {
            TransferOnRails(transferTime, shipPosPhased, shipVelPhased, moonOmega);
        } else {
            Debug.LogFormat("tli NBody mode r={0} v={1}", shipPosPhased, shipVelPhased);
            ge.UpdatePositionAndVelocity(spaceship, shipPosPhased, shipVelPhased);
        }
 
        // remove placeholder ships/orbit visualizers
        ge.RemoveBody(shipExitSOI.gameObject);
        shipExitSOI.gameObject.SetActive(false);
        ge.RemoveBody(shipEnterSOI.gameObject);
        shipEnterSOI.gameObject.SetActive(false);
        SetOrbitDisplays(false);
        
        ge.SetEvolve(true);
        running = true;

    }

    /// <summary>
    /// Circularize around Moon
    /// - currently only onRails is implemented
    /// </summary>
    private void CircularizeAroundMoon() {
        // check ship is on segment where it near Moon
        if (onRails) {
            KeplerSequence keplerSeq = spaceship.GetComponent<KeplerSequence>();
            OrbitUniversal orbitU = keplerSeq.GetCurrentOrbit();
            if (orbitU.centerNbody == moonBody) {
                // in orbit around the moon - do circularization
                OrbitData orbitData = new OrbitData(orbitU);
                OrbitTransfer circularizeXfer = new CircularizeXfer(orbitData);
                keplerSeq.RemoveFutureSegments();
                keplerSeq.AddManeuvers(circularizeXfer.GetManeuvers());
            }
        } else {
            // assume we're in orbit around the moon
            OrbitData orbitData = new OrbitData();
            orbitData.SetOrbitForVelocity(spaceship, moonBody);
            OrbitTransfer circularizeXfer = new CircularizeXfer(orbitData);
            ge.AddManeuvers(circularizeXfer.GetManeuvers());
        }
    }

    /// <summary>
    /// Raise a circular orbit by the specified percent
    /// - only on-rail is implemented
    /// </summary>
    /// <param name="percentRaise"></param>
    private void NewCircularOrbit(float percentRaise) {
        if (onRails) {
            KeplerSequence keplerSeq = spaceship.GetComponent<KeplerSequence>();
            OrbitUniversal orbitU = keplerSeq.GetCurrentOrbit();
            // check orbit is circular
            if (orbitU.eccentricity < 1E-2) {
                // circular, ok to proceed
                OrbitData fromOrbit = new OrbitData(orbitU);
                OrbitData toOrbit = new OrbitData(fromOrbit);
                toOrbit.a = percentRaise * fromOrbit.a;
                const bool rendezvous = false;
                OrbitTransfer hohmannXfer = new HohmannXfer(fromOrbit, toOrbit, rendezvous);
                keplerSeq.RemoveFutureSegments();
                keplerSeq.AddManeuvers(hohmannXfer.GetManeuvers());
            }
        } else {
                // assume we're in orbit around the moon
                OrbitData orbitData = new OrbitData();
                orbitData.SetOrbitForVelocity(spaceship, moonBody);
                OrbitData toOrbit = new OrbitData(orbitData);
                toOrbit.a = percentRaise * orbitData.a;
                const bool rendezvous = false;
                OrbitTransfer hohmannXfer = new HohmannXfer(orbitData, toOrbit, rendezvous);
                ge.AddManeuvers(hohmannXfer.GetManeuvers());
        }
    }

    /// <summary>
    /// Use left click position as a means of determining the desired Soi angle if there is a click within a reasonable
    /// distance of the SOI circle on the screen.
    /// </summary>
    private bool HandleMouseSOIInput() {
        if (Input.GetMouseButton(0)) {
            Vector3 mousePos = Input.mousePosition;
            Vector3 moonScreenPos = Camera.main.WorldToScreenPoint(moonBody.gameObject.transform.position);
            moonScreenPos.z = 0;
            Vector3 shipScreenPos = Camera.main.WorldToScreenPoint(shipEnterSOI.gameObject.transform.position);
            shipScreenPos.z = 0; 
            float radius = Vector3.Distance(moonScreenPos, shipScreenPos);
            if ( Vector3.Distance(mousePos, moonScreenPos) < 2.0f * radius) {
                // close enough to use for SOI angle. Take angle from mouse click to 
                Vector3 angleLine = (mousePos - moonScreenPos).normalized;
                soiAngle = Mathf.Atan2(angleLine.y, angleLine.x); // radians!
                UpdateSoiPosition();
                return true;
            }
        }
        return false;
    }

    // Update is called once per frame
    void Update() {

        // different key codes when running...
        if (running) {
            if (Input.GetKeyUp(KeyCode.C)) {
                // Circularize (if in the vicinity of the moon)
                CircularizeAroundMoon();
            } else if (Input.GetKeyUp(KeyCode.R)) {
                // Raise circular orbit but Hohmann Xfer
                NewCircularOrbit(1.3f); 
            }
            return;
        }

        bool doUpdate = UpdateManeuverSymbols();

        // Stop GE once it has started
        if (ge.GetEvolve() && !running) {
            ge.UpdatePositionAndVelocity(shipEnterSOI, targetPoint.ToVector3(), Vector3.zero);
            ge.SetEvolve(false);
            doUpdate = true;            
        }

        doUpdate |= AdjustTimeOfFlight();

        if (Input.GetKeyUp(KeyCode.X)) {
            // execute the transfer
            ExecuteTransfer();
            if (instructions != null)
                instructions.gameObject.SetActive(false);
            // HACK
            toMoonOrbit.gameObject.SetActive(true);
            toMoonOrbit.velocityFromScript = false;
        } else if (Input.GetKeyUp(KeyCode.Space)) {
            ge.SetEvolve(!ge.GetEvolve());
        } else {
            doUpdate |= HandleMouseSOIInput();
        }

        if (doUpdate) {
            // Orbit predictor gets an explicit velocity, so no need to set here
            Vector3 tliVelocity = ComputeTransfer(); // false - ignore inclination for scene display
            SetOrbitDisplays(true);
            // move ship to position, with correct velocity
            ge.UpdatePositionAndVelocity(spaceship, startPoint.ToVector3(), tliVelocity);
        }
    }

    // IPatchedConicChange: Only needed in the GE case (when on-rails add a callback to the hyper segement to 
    // accomplish this)
    // Update the center of the orbitPredictor
    public void OnNewInfluencer(NBody newObject, NBody oldObject) {
        toMoonOrbit.SetCenterObject(newObject.gameObject);
    }
}
