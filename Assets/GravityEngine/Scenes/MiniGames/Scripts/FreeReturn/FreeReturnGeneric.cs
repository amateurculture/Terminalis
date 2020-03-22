using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Second generation Free return computation allowing the moon to be in a more general orbit (inclined, eccentric). 
/// Ship is assumed to be in a circular orbit around the planet. 
/// 
/// The controller creates a virtual "ghost" moon and advances it according to the requested transfer time. 
/// (The transfer time is expressed as a fraction of the Hohmann transfer time assuming both orbits are circular)
/// 
/// Use of OrbitUniversal for the ship and moon is assumed. 
/// 
/// General idea:
/// 1) Move the ghost moon to the position at SOI arrival
/// 2) Place ghost soiEnter ship at the correct position around the ghost moon
/// 3) Compute a Lambert xfer from antipode of ghost moon orbit to soiEnter. 
/// 3) Determine the position for ghost soiExit ship by mirroring on hyperbola around ghost moon
/// 4) Set velocity on soiExit ghost to show return orbit
/// 
/// User-Interface:
/// Keys:   K/L: adjust SOI inclination
///         S/W: adjust time of flight
///         X: Execute transfer to moon
///         SPACE: pause
///         C: circularize orbit around moon (After execute, once in moons SOI)
///         R: raise orbit around moon. (After in moon SOI and circularize)
///         
/// Mouse: left click near the Moon at arrival will set the angle of arrival on the SOI circle. 
/// 
/// </summary>
public class FreeReturnGeneric : MonoBehaviour
{
    [Header("Transfer Parameters")]
    [Tooltip("Change in SOI position per key press")]
    [SerializeField]
    private float dAngleDegrees = 0.1f;

    [SerializeField]
    [Tooltip("Angle of SOI arrival (with respect to Earth/Moon line)")]
    private float soiAngleDeg = 0.6f * 180f;

    [SerializeField]
    [Tooltip("Angle of TLI departure (with respect to Earth/Moon line")]
    private float shipTLIAngleDeg = 10f;

    [SerializeField]
    [Tooltip("Inclination of hyperbola orbit around moon.")]
    private float soiInclination = 0.0f;

    [SerializeField]
    [Tooltip("Transfer time expressed as fraction of Hohmann xfer time.")]
    private double tflightFactor = 1.0;

    [Header("Object Handles")]
    [Tooltip("Spaceship to be sent to the moon")]
    [SerializeField]
    private NBody spaceship = null;

    [SerializeField]
    private NBody planet = null;

    [SerializeField]
    private NBody moonBody = null;

    //! prefab for the ghost ships created at SOI entry and exit. Need to have an NBody and OrbitUniversal attached. 
    [SerializeField]
    [Tooltip("Prefab for ghost ship with NBody, OrbitUniversal and OrbitPredictor")]
    private GameObject shipSOIPrefab = null;

    [Header("Orbit Path Materials")]
    [SerializeField]
    private Material toMoonMaterial = null;
    [SerializeField]
    private Material aroundMoonMaterial = null;
    [SerializeField]
    private Material fromMoonMaterial = null;

    //! Text to show summary of maneuver (optional)
    [Header("UI Components (optional)")]

    //! Text field used to display instructions
    [SerializeField]
    private Text instructions = null;

    [SerializeField]
    private Text freeReturnInfo = null;

    [SerializeField]
    private float lineWidth = 0.1f;

    private OrbitUniversal shipOrbit;
    private OrbitPredictor shipOrbitPredictor;

    //! Flag that designated which "way around" to go on the transfer ellipse. Toggled with F key
    private bool shortPath = true;

    private LambertBattin lambertB;

    private double timeHohmann; 

    private float soiRadius;
    private double shipRadius;
    private double moonRadius;

    // use two ghost moon, one for SOI entry and one for SOI exit. 
    private const int MOON_SOI_ENTER = 0;
    private const int MOON_SOI_EXIT = 1;
    private NBody[] ghostMoon;
    private OrbitUniversal[] ghostMoonOrbit;

    // Use a series of ghost ships for varios points of interest in the segements of the free return 
    // trajectory
    private const int NUM_GHOST_SHIPS = 5;
    private const int TLI = 0; 
    private const int TO_MOON = 1;
    private const int ENTER_SOI = 2;
    private const int SOI_HYPER = 3;
    private const int EXIT_SOI = 4;

    private NBody[] ghostShip;
    private OrbitUniversal[] ghostShipOrbit;
    private OrbitPredictor[] ghostShipOrbitPredictor;

    private OrbitPredictor ghostMoonSoiEnterOrbitPredictor; 

    private Vector3d startPoint;

    private GravityEngine ge;

    private const float tFlightAdjust = 0.001f;

    private bool running;

    private double t_soiExit;


    // Use this for initialization
    void Start() {

        ge = GravityEngine.Instance();

        // mass scaling will cancel in this ratio
        soiRadius = OrbitUtils.SoiRadius(planet, moonBody);

        // TODO: allow moon to be OrbitUniversal as well.
        OrbitUniversal moonOrbit = moonBody.gameObject.GetComponent<OrbitUniversal>();
        if (moonOrbit == null) {
            Debug.LogError("Moon is required to have OrbitUniversal");
        }
        moonRadius = moonOrbit.GetMajorAxis();

        shipOrbit = spaceship.GetComponent<OrbitUniversal>();
        if (shipOrbit == null) {
            Debug.LogError("Require that the ship have an OrbitU");
        }
        if (shipOrbit.evolveMode != OrbitUniversal.EvolveMode.KEPLERS_EQN) {
            Debug.LogError("Controller requires ship on-rails but spaceship is off-rails");
        }
               
        // assuming circular orbit for ship
        shipRadius = shipOrbit.GetApogee();

        shipOrbitPredictor = spaceship.GetComponentInChildren<OrbitPredictor>();

        ge.AddGEStartCallback(GEStarted); 
    }

    private void GEStarted() {
        AddGhostBodies();
        ComputeBaseTransferTime();
        Debug.LogFormat("Moon period={0:0.0}", ghostMoonOrbit[MOON_SOI_ENTER].GetPeriod());
    }

    private void AddGhostBodies() {

        // Create a ghost moon and put soiEnter/Exit ships into orbit around it. Add all to GE
        // (ghost moon does not have an OrbitPredictor)
        ghostMoon = new NBody[2];
        ghostMoonOrbit = new OrbitUniversal[2];
        for (int i = 0; i < 2; i++) {
            GameObject ghostMoonGO = Instantiate(moonBody.gameObject);
            ghostMoon[i] = ghostMoonGO.GetComponent<NBody>();
            ghostMoonOrbit[i] = ghostMoonGO.GetComponent<OrbitUniversal>();
        }
        ghostMoon[MOON_SOI_ENTER].gameObject.name = "GhostMoon (SOI enter)";
        ghostMoon[MOON_SOI_EXIT].gameObject.name = "GhostMoon (SOI exit)";
        ghostMoonSoiEnterOrbitPredictor = ghostMoon[MOON_SOI_ENTER].GetComponentInChildren<OrbitPredictor>();

        ghostMoon[MOON_SOI_ENTER].GetComponentInChildren<LineRenderer>().material = toMoonMaterial;
        ghostMoon[MOON_SOI_EXIT].GetComponentInChildren<LineRenderer>().material = fromMoonMaterial;

        ghostShip = new NBody[NUM_GHOST_SHIPS];
        ghostShipOrbit = new OrbitUniversal[NUM_GHOST_SHIPS];
        ghostShipOrbitPredictor = new OrbitPredictor[NUM_GHOST_SHIPS];
        GameObject ghostShipGO;
        for (int i = 0; i < NUM_GHOST_SHIPS; i++) {
            ghostShipGO = Instantiate(shipSOIPrefab);
            ghostShip[i] = ghostShipGO.GetComponent<NBody>();
            ghostShipOrbit[i] = ghostShipGO.GetComponent<OrbitUniversal>();
            ghostShipOrbit[i].p_inspector = soiRadius;
            ghostShipOrbit[i].centerNbody = planet;
            ghostShipOrbitPredictor[i] = ghostShipGO.GetComponentInChildren<OrbitPredictor>();
            ghostShipOrbitPredictor[i].body = ghostShipGO;
            ghostShipOrbitPredictor[i].centerBody = planet.gameObject;
            LineRenderer lineR = ghostShipOrbitPredictor[i].GetComponent<LineRenderer>();
            lineR.startWidth = lineWidth;
            lineR.endWidth = lineWidth;
            ghostShipGO.transform.SetParent(planet.gameObject.transform);
        }

        // check prefab has orbitU in Kepler mode
        if (ghostShipOrbit[0].evolveMode != OrbitUniversal.EvolveMode.KEPLERS_EQN) {
            Debug.LogError("ShipSoi prefab must have an on-rails OrbitU");
            return;
        }

        ghostShip[TLI].gameObject.name = "Ghost TLI";
        ghostShipOrbit[TLI].p_inspector = shipRadius;
        ghostShipOrbitPredictor[TLI].GetComponent<LineRenderer>().material = toMoonMaterial;

        // customize ghost ships as necessary
        // ENTER_SOI
        ghostShip[TO_MOON].gameObject.name = "Ghost To Moon";
        ghostShipOrbit[TO_MOON].p_inspector = shipRadius;
        ghostShipOrbitPredictor[TO_MOON].GetComponent<LineRenderer>().material = toMoonMaterial;

        // SOI Enter
        ghostShip[ENTER_SOI].gameObject.name = "Ghost SOI Enter";
        ghostShip[ENTER_SOI].gameObject.transform.SetParent(ghostMoon[MOON_SOI_ENTER].gameObject.transform);
        Destroy(ghostShipOrbitPredictor[ENTER_SOI]);
        // Use OrbitPredictor to show SOI at entry
        //ghostShipOrbitPredictor[ENTER_SOI].centerBody = ghostMoon[MOON_SOI_ENTER].gameObject;
        //ghostShipOrbitPredictor[ENTER_SOI].GetComponent<LineRenderer>().material = aroundMoonMaterial;
        ghostShipOrbit[ENTER_SOI].centerNbody = ghostMoon[MOON_SOI_ENTER];

        // HYPER_SOI
        ghostShip[SOI_HYPER].gameObject.name = "Ghost SOI Hyper";
        ghostShip[SOI_HYPER].gameObject.transform.SetParent(ghostMoon[MOON_SOI_ENTER].gameObject.transform);
        ghostShipOrbitPredictor[SOI_HYPER].centerBody = ghostMoon[MOON_SOI_ENTER].gameObject;
        ghostShipOrbitPredictor[SOI_HYPER].hyperDisplayRadius = soiRadius;
        ghostShipOrbit[SOI_HYPER].centerNbody = ghostMoon[MOON_SOI_ENTER];
        ghostShipOrbitPredictor[SOI_HYPER].GetComponent<LineRenderer>().material = aroundMoonMaterial;

        // EXIT_SOI
        ghostShip[EXIT_SOI].gameObject.name = "Ghost SOI Exit";
        ghostShipOrbitPredictor[EXIT_SOI].GetComponent<LineRenderer>().material = fromMoonMaterial;

        // Tell GE about everything
        ge.AddBody(ghostMoon[MOON_SOI_ENTER].gameObject); // also adds ENTER_SOI
        ge.AddBody(ghostShip[TLI].gameObject);
        ge.AddBody(ghostShip[TO_MOON].gameObject);
        ge.AddBody(ghostShip[EXIT_SOI].gameObject);
        ge.AddBody(ghostMoon[MOON_SOI_EXIT].gameObject); 

        // set TLI ship to inactive and control pos/vel in ComputeTransfer
        ge.InactivateBody(ghostShip[TO_MOON].gameObject);
    }

    private void RemoveGhostBodies() {
        foreach ( NBody nbody in ghostShip) {
            ge.RemoveBody(nbody.gameObject);
            Destroy(nbody.gameObject);
        }
        foreach (NBody nbody in ghostMoon) {
            ge.RemoveBody(nbody.gameObject);
            Destroy(nbody.gameObject);
        }
    }

    /// <summary>
    /// Simple calculation of time base for the transfer assuming both are in circular orbit
    /// </summary>
    private void ComputeBaseTransferTime() {
        double xferOrbit = 0.5 * (shipRadius + moonRadius)/ge.GetPhysicalScale();
        double planetMass = ge.GetMass(planet);
        timeHohmann = Mathd.PI * Mathd.Sqrt(xferOrbit * xferOrbit * xferOrbit / planetMass);
    }

    /// <summary>
    /// Use WS to change the transfer time. Transfer time is implemented as a factor (0.1 .. 1.5)
    /// applied to the Hohmann transfer time using the assumption the moon orbit is circular.
    /// </summary>
    /// <returns></returns>
    private bool AdjustTimeOfFlight() {
        bool keyPressed = false;
        if (Input.GetKey(KeyCode.S)) {
            tflightFactor = System.Math.Max(0.1, tflightFactor - tFlightAdjust);
            keyPressed = true;
        } else if (Input.GetKey(KeyCode.W)) {
            tflightFactor = System.Math.Min(1.5, tflightFactor + tFlightAdjust);
            keyPressed = true;
        }
        return keyPressed;
    }

    /// <summary>
    /// Use AD/KL to position the arrival symbol on the SOI. 
    ///  
    /// </summary>
    private bool UpdateManeuverSymbols() {
        bool keyPressed = false;
        // ship
        if (Input.GetKey(KeyCode.K)) {
            soiInclination = (float)NUtils.DegreesPM180(soiInclination + dAngleDegrees );
            keyPressed = true;
        } else if (Input.GetKey(KeyCode.L)) {
            soiInclination = (float)NUtils.DegreesPM180(soiInclination - dAngleDegrees);
            keyPressed = true;
        } else if (Input.GetKey(KeyCode.A)) {
            soiAngleDeg = (float)NUtils.DegreesMod360(soiAngleDeg + dAngleDegrees);
            keyPressed = true;
        } else if (Input.GetKey(KeyCode.D)) {
            soiAngleDeg = (float)NUtils.DegreesMod360(soiAngleDeg - dAngleDegrees);
            keyPressed = true;
        }

        // update degree values so can see result in inspector
        return keyPressed;
    }

    /// <summary>
    /// Computes the transfer and updates all the ghost bodies.
    /// </summary>
    /// <returns></returns>
    private void ComputeTransfer() {

        double timeNow = ge.GetPhysicalTimeDouble();

        // First using the transfer time, move the ghost Moon to position at SOI arrival. 
        // Call evolve via LockAtTime on the ghostMoon to move it. Set position based on this.
        double t_flight = tflightFactor * timeHohmann;
        double timeatSoi = timeNow + t_flight;
        ghostMoonOrbit[MOON_SOI_ENTER].LockAtTime(timeatSoi);
        // Determine the moon phase angle
        double moonPhase = ghostMoonSoiEnterOrbitPredictor.GetOrbitUniversal().phase;

        // Place the TLI ship at the user-requested angle wrt planet-moon line
        // Put ghost ship in same orbit geometry as the moon, assuming it is circular. Then 
        // can use same phase value. 
        // (Ship needs to reach this departure point, it may not even be on the ship orbit
        //  in general). 
        ghostShipOrbit[TLI].phase = shipTLIAngleDeg + (moonPhase + 180f);
        ghostShipOrbit[TLI].inclination = ghostMoonOrbit[MOON_SOI_ENTER].inclination;
        ghostShipOrbit[TLI].omega_lc = ghostMoonOrbit[MOON_SOI_ENTER].omega_lc;
        ghostShipOrbit[TLI].omega_uc = ghostMoonOrbit[MOON_SOI_ENTER].omega_uc;
        ghostShipOrbit[TLI].p_inspector = shipOrbit.p;
        ghostShipOrbit[TLI].Init();
        ghostShipOrbit[TLI].LockAtTime(0);

        // Place the SOI enter ship at the user-requested angle in an SOI orbit. Lock at time 0 so the phase
        // is held per the user input. 
        ghostShipOrbit[ENTER_SOI].phase = soiAngleDeg + moonPhase;
        ghostShipOrbit[ENTER_SOI].inclination = soiInclination + shipOrbit.inclination;
        ghostShipOrbit[ENTER_SOI].omega_lc = ghostMoonOrbit[MOON_SOI_ENTER].omega_lc;
        ghostShipOrbit[ENTER_SOI].omega_uc = ghostMoonOrbit[MOON_SOI_ENTER].omega_uc;
        ghostShipOrbit[ENTER_SOI].Init();
        ghostShipOrbit[ENTER_SOI].LockAtTime(0);

        // Find the line to the ENTER_SOI position. Ship departs from that line continued through planet
        // at the shipRadius distance (assumes circular ship orbit)
        // TODO: Handle planet not at (0,0,0)
        Vector3d soiEntryPos = ge.GetPositionDoubleV3(ghostShip[ENTER_SOI]);
        Vector3d departurePoint = ge.GetPositionDoubleV3(ghostShip[TLI]);

        // Use Lambert to find the departure velocity to get from departure to soiEntry
        // Since we need 180 degrees from departure to arrival, use LambertBattin
        lambertB = new LambertBattin(ghostShip[TO_MOON], planet, departurePoint, soiEntryPos, shipOrbit.GetAxis());

        // apply any time of flight change
        bool reverse = !shortPath;

        const bool df = false;
        const int nrev = 0;
        int error = lambertB.ComputeXfer(reverse, df, nrev, t_flight);
        if (error != 0) {
            Debug.LogWarning("Lambert failed to find solution. error=" + error);
            return;
        }
        // Check Lambert is going in the correct direction
        //Vector3 shipOrbitAxis = Vector3.Cross(ge.GetPhysicsPosition(spaceship), ge.GetVelocity(spaceship) ).normalized;
        //Vector3 tliOrbitAxis = Vector3.Cross(departurePoint.ToVector3(), lambertB.GetTransferVelocity().ToVector3()).normalized;
        Vector3 shipOrbitAxis = Vector3.Cross(ge.GetVelocity(spaceship), ge.GetPhysicsPosition(spaceship)).normalized;
        Vector3 tliOrbitAxis = Vector3.Cross( lambertB.GetTransferVelocity().ToVector3(), departurePoint.ToVector3()).normalized;
        if (Vector3.Dot(shipOrbitAxis, tliOrbitAxis) < 0) {
            error = lambertB.ComputeXfer(!reverse, df, nrev, t_flight);
            if (error != 0) {
                Debug.LogWarning("Lambert failed to find solution for reverse path. error=" + error);
                return;
            }
        }
        //Debug.LogFormat("tli_vel={0}", lambertB.GetTransferVelocity());

        ghostShipOrbit[TO_MOON].InitFromRVT(departurePoint, lambertB.GetTransferVelocity(), timeNow, planet, false);

        // Set velocity for orbit around moon. Will be updated every frame
        ghostShipOrbit[SOI_HYPER].InitFromRVT(soiEntryPos, lambertB.GetFinalVelocity(), timeNow, ghostMoon[MOON_SOI_ENTER], false);

        // Find the exit point of the hyperbola in the SOI
        OrbitUtils.OrbitElements oe = OrbitUtils.RVtoCOE(soiEntryPos, lambertB.GetFinalVelocity(), ghostMoon[MOON_SOI_ENTER], false);
        Vector3d soiExitR = new Vector3d();
        Vector3d soiExitV = new Vector3d();
        OrbitUtils.COEtoRVMirror(oe, ghostMoon[MOON_SOI_ENTER], ref soiExitR, ref soiExitV, false);

        // Find time to go around the moon. TOF requires relative positions!!
        Vector3d ghostSoiEnterPos = ge.GetPositionDoubleV3(ghostMoon[MOON_SOI_ENTER]);
        Vector3d soiEnterRelative = soiEntryPos - ghostSoiEnterPos;
        Vector3d soiExitRelative = soiExitR - ghostSoiEnterPos;
        Vector3d soiExitVelRelative = soiExitV - ge.GetVelocityDoubleV3(ghostMoon[MOON_SOI_ENTER]);
        double hyperTOF = ghostShipOrbit[SOI_HYPER].TimeOfFlight(soiEnterRelative, soiExitRelative);

        // Position the ghost moon for SOI exit (timeAtSoi includes timeNow)
        t_soiExit = timeatSoi + hyperTOF;
        ghostMoonOrbit[MOON_SOI_EXIT].LockAtTime(t_soiExit);

        // Set position and vel for exit ship, so exit orbit predictor can run. 
        Vector3d ghostMoonSoiExitPos = ge.GetPositionDoubleV3(ghostMoon[MOON_SOI_EXIT]);
        Vector3d ghostMoonSoiExitVel = ge.GetVelocityDoubleV3(ghostMoon[MOON_SOI_EXIT]);

        ghostShipOrbit[EXIT_SOI].InitFromRVT(soiExitRelative + ghostMoonSoiExitPos, 
                                             soiExitVelRelative + ghostMoonSoiExitVel, 
                                             timeNow, planet, false);
    }

    /// <summary>
    /// Set up a KeplerSeqeunce to do the three phases of the transfer as Kepler mode conics.
    /// 
    /// Add all the ghost orbits with the required times
    /// </summary>
    /// <param name="transferTime"></param>
    private void TransferOnRails() {
        // the ship needs to have a KeplerSequence
        KeplerSequence kseq = spaceship.GetComponent<KeplerSequence>();
        if (kseq == null) {
            Debug.LogError("Could not find a KeplerSequence on " + spaceship.name);
            return;
        }
        // Ellipse 1: shipPos/shipvel already phased by the caller.
        double t_start = ge.GetPhysicalTime();
        double t_toSoi = timeHohmann * tflightFactor;
        KeplerSequence.ElementStarted noCallback = null;
        Vector3d r0 = new Vector3d();
        Vector3d v0 = new Vector3d();
        double time0 = 0;
        ghostShipOrbit[TO_MOON].GetRVT(ref r0, ref v0, ref time0);
        kseq.AppendElementRVT(r0, v0, t_start, true, spaceship, planet, noCallback);

        // Hyperbola: start at t + transferTime
        // Need to add wrt to ghostMoon (relative=true), then for actual Kepler motion want it around moon
        ghostShipOrbit[SOI_HYPER].GetRVT(ref r0, ref v0, ref time0);
        OrbitUniversal hyperObit = kseq.AppendElementRVT(r0, v0, t_start + t_toSoi, true, spaceship, ghostMoon[MOON_SOI_ENTER], EnterMoonSoi);
        hyperObit.centerNbody = moonBody;

        // Ellipse 2: 
        ghostShipOrbit[EXIT_SOI].GetRVT(ref r0, ref v0, ref time0);
        kseq.AppendElementRVT(r0, v0, t_soiExit, true, spaceship, planet, ExitMoonSoi);
    }

    /// <summary>
    /// Callback for when ship enters the moons SOI. Change the center object of the moon orbit predictor
    /// to be the moon. 
    /// </summary>
    /// <param name="orbitU"></param>
    public void EnterMoonSoi(OrbitUniversal orbitU) {
        shipOrbitPredictor.SetCenterObject(moonBody.gameObject);
        shipOrbitPredictor.hyperDisplayRadius = soiRadius;
    }

    /// <summary>
    /// Callback for when the ship leaves the moon SOI. Change the the ship orbit predictor center object
    /// back to the planet. 
    /// </summary>
    /// <param name="orbitU"></param>
    public void ExitMoonSoi(OrbitUniversal orbitU) {
        shipOrbitPredictor.SetCenterObject(planet.gameObject);
    }

    private void ExecuteTransfer() {

        TransferOnRails();
        // remove placeholder ships/orbit visualizers
        RemoveGhostBodies();
    }

    /// <summary>
    /// Circularize around Moon
    /// - currently only onRails is implemented
    /// </summary>
    private void CircularizeAroundMoon() {
        // check ship is on segment where it near Moon
        KeplerSequence keplerSeq = spaceship.GetComponent<KeplerSequence>();
        OrbitUniversal orbitU = keplerSeq.GetCurrentOrbit();
        if (orbitU.centerNbody == moonBody) {
            // in orbit around the moon - do circularization
            OrbitData orbitData = new OrbitData(orbitU);
            OrbitTransfer circularizeXfer = new CircularizeXfer(orbitData);
            keplerSeq.RemoveFutureSegments();
            keplerSeq.AddManeuvers(circularizeXfer.GetManeuvers());
        }
    }

    /// <summary>
    /// Raise a circular orbit by the specified percent
    /// - only on-rail is implemented
    /// </summary>
    /// <param name="percentRaise"></param>
    private void NewCircularOrbit(float percentRaise) {
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
    }

    /// <summary>
    /// Use left click position as a means of determining the desired Soi angle if there is a click within a reasonable
    /// distance of the SOI circle on the screen.
    /// 
    /// </summary>
    private bool HandleMouseSOIInput() {
        if (Input.GetMouseButton(0)) {
            Vector3 mousePos = Input.mousePosition;
            Vector3 moonScreenPos = Camera.main.WorldToScreenPoint(ghostMoon[MOON_SOI_ENTER].gameObject.transform.position);
            moonScreenPos.z = 0;
            Vector3 shipScreenPos = Camera.main.WorldToScreenPoint(ghostShip[ENTER_SOI].gameObject.transform.position);
            shipScreenPos.z = 0;
            float radius = Vector3.Distance(moonScreenPos, shipScreenPos);
            if (Vector3.Distance(mousePos, moonScreenPos) < 2.0f * radius) {
                // close enough to use for SOI angle. Take angle from mouse click to 
                Vector3 angleLine = (mousePos - moonScreenPos).normalized;
                soiAngleDeg = Mathf.Atan2(angleLine.y, angleLine.x) * Mathf.Rad2Deg;
                return true;
            }
        }
        return false;
    }

    // Update is called once per frame
    void Update() {

        if (!running) {
            // Getting user input for FR
            AdjustTimeOfFlight();
            UpdateManeuverSymbols();
            HandleMouseSOIInput();
            ComputeTransfer();
            if (freeReturnInfo != null) {
                freeReturnInfo.text = string.Format("Perilune = {0:0.0}\nReturn Perigee={1:0.0}\nTime to SOI = {2:0.0}\n{3}",
                    ghostShipOrbit[SOI_HYPER].GetPerigee(),
                    ghostShipOrbit[EXIT_SOI].GetPerigee(),
                    timeHohmann * tflightFactor,
                    GravityScaler.GetWorldTimeFormatted(timeHohmann * tflightFactor, ge.units));
            }
        } else {
            // RUNNING
            if (Input.GetKeyUp(KeyCode.C)) {
                // Circularize (if in the vicinity of the moon)
                CircularizeAroundMoon();
            } else if (Input.GetKeyUp(KeyCode.R)) {
                // Raise circular orbit but Hohmann Xfer
                NewCircularOrbit(1.3f);
            }
            return;
        }


        if (Input.GetKeyUp(KeyCode.X)) {
            // execute the transfer
            ExecuteTransfer();
            if (instructions != null)
                instructions.gameObject.SetActive(false);
            running = true;
            ge.SetEvolve(true);
        } else if (Input.GetKeyUp(KeyCode.Space)) {
            ge.SetEvolve(!ge.GetEvolve());
        } 

    }

    // IPatchedConicChange: Only needed in the GE case (when on-rails add a callback to the hyper segement to 
    // accomplish this)
    // Update the center of the orbitPredictor
    public void OnNewInfluencer(NBody newObject, NBody oldObject) {
        // toMoonOrbit.SetCenterObject(newObject.gameObject);
    }
}
