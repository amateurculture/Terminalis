using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Controller for the LambertXferDl and LambertXferSolar scenes. 
/// 
/// Spaceship with several potential destination orbits to transfer to. Destinations (targets)
/// are determined by finding all OrbitEllipses in the scene that are NOT the spaceship. 
/// 
/// The transfer makes use of the LambertUniversal class and creates a sequence of maneuvers to 
/// be executed by GE.
/// 
/// Inputs:
/// N key - select a target orbit for spaceship
/// A/D - control point on target ellipse to arrive at
/// W/S - control time of flight for the transfer 
/// F - flip the direction of transfer around the ellipse
/// X - execute the selected transfer
/// 
/// The controller is stateful and offers state dependent help in the statefulHelp component. 
/// 
/// </summary>
public class LambertDemoController : MonoBehaviour {

    public Material selectedMaterial;

    public NBody spaceshipNBody;
    public NBody starNBody;

    //! Prefab for symbol to be used for manuevering
    public GameObject maneuverSymbolPrefab;

    //! Text to show summary of maneuver (optional)
    public Text maneuverText;

    //! Text field used to display staeful help
    public Text statefulHelp;

    //! Text field used to display instructions
    public Text instructions; 

    public float scrollSpeed = 1f;

    public OrbitPredictor maneuverOrbitPredictor;
    public OrbitSegment maneuverSegment;

    //! Flag that designated which "way around" to go on the transfer ellipse. Toggled with F key
    private bool shortPath = true; 

    private int selectedEllipse = 0;

    private TargetEllipse[] targetEllipses;

    private LambertUniversal lambertU;
    private double transferTime;
    private double tflightFactor = 1;

    // optional - if there is ManeuverRenderer component on this Game Object then use it
    private ManeuverRenderer maneuverRenderer;

    private struct TargetEllipse
    {
        public OrbitEllipse ellipse;
        public LineRenderer lineRenderer;
        public Material originalMaterial;
        public GameObject maneuverSymbol;
        public OrbitData orbitData;
        public float manueverPhase;
    }

    /// <summary>
    /// Game state:
    /// SELECT_DEST: Use N key to toggle which ellipse is the destination orbit
    /// COMPUTE_MANEUVER: With a selected target use AD to designate position on target 
    ///                   ellipse. Use WS to control transfer time
    /// DOING_XFER: In flight to maneuver point on target ellipse. 
    /// </summary>
    private enum State { SELECT_DEST, COMPUTE_MANEUVER, DOING_XFER, STATE_COUNT};
    private State state = State.SELECT_DEST;

    private string[] stateHelpText;

    private GravityEngine ge;

    private OrbitData shipData;
    private OrbitData targetData;

    // Use this for initialization
    void Start () {

        ge = GravityEngine.Instance();
        // Scan ellipses in scene and gather into data structure
        OrbitEllipse[] ellipses = (OrbitEllipse[])Object.FindObjectsOfType(typeof(OrbitEllipse));
        targetEllipses = new TargetEllipse[ellipses.Length-1];
        int i = 0; 
        foreach (OrbitEllipse ellipse in ellipses) {
            // skip spaceship ellipse (not a target)
            if (ellipse.gameObject != spaceshipNBody.gameObject) {
                targetEllipses[i].ellipse = ellipse;
                targetEllipses[i].lineRenderer = ellipse.GetComponentInChildren<LineRenderer>();
                targetEllipses[i].originalMaterial = targetEllipses[i].lineRenderer.material;
                // create a maneuver symbol, set inactive for now. 
                targetEllipses[i].maneuverSymbol = Instantiate<GameObject>(maneuverSymbolPrefab);
                targetEllipses[i].maneuverSymbol.SetActive(false);
                // make it a child of controller (keep things tidy)
                targetEllipses[i].maneuverSymbol.transform.parent = transform;
                targetEllipses[i].manueverPhase = 0;
                i++;
            }
        }
        SelectEllipse(0);
        AddConsoleCommands();
        InitHelpText();
        instructions.gameObject.SetActive(false);

        // disable maneuver predictor until things settle (can get Invalid local AABB otherwise)
        maneuverOrbitPredictor.gameObject.SetActive(false);
        maneuverSegment.gameObject.SetActive(false);

        // is there a maneuver renderer?
        maneuverRenderer = GetComponent<ManeuverRenderer>();
    }

    private void InitHelpText() {
        stateHelpText = new string[(int) State.STATE_COUNT];
        stateHelpText[(int)State.SELECT_DEST] = "N to select target ellipse, M to choose maneuver";
        stateHelpText[(int)State.COMPUTE_MANEUVER] = "A/D to move target point around ellipse\n"
                                                    +"W/S to change transfer time\n" + 
                                                    "F to flip maneuver direction\n" +
                                                    "X to execute transfer";
        stateHelpText[(int)State.DOING_XFER] = "Transfer in Progress. No keys active.";

    }

    private void SelectEllipse(int index) {
        targetEllipses[selectedEllipse].lineRenderer.material = targetEllipses[selectedEllipse].originalMaterial;
        // use material to access first entry (cannot do a per index set - it is ignored)
        targetEllipses[index].lineRenderer.material = selectedMaterial;
        selectedEllipse = index;
    }

    private void UpdateManeuverUI() {
        if (maneuverText == null)
            return;
        List<Maneuver> maneuvers = lambertU.GetManeuvers();
        if (maneuvers.Count == 0)
            return;

        string s = string.Format("Burn1: dV=({0:G3}, {1:G3}, {2:G3}),   |dV|={3:G3}\n",
            maneuvers[0].velChange.x, maneuvers[0].velChange.y, maneuvers[0].velChange.z,
            maneuvers[0].velChange.magnitude);
        string s2 = string.Format("Burn2: dV=({0:G3}, {1:G3}, {2:G3}),   |dV|={3:G3}\n",
            maneuvers[1].velChange.x, maneuvers[1].velChange.y, maneuvers[1].velChange.z,
            maneuvers[1].velChange.magnitude);
        string s3 = string.Format("Transfer Time={0:G4}", transferTime);
        maneuverText.text = s + s2 + s3; 

        if (maneuverRenderer != null) {
            maneuverRenderer.ShowManeuvers(maneuvers);
        }

    }

    private void AdjustTimeOfFlight() {
        if (Input.GetKeyDown(KeyCode.S)) {
            tflightFactor = System.Math.Max(0.1, tflightFactor - 0.1);
            ComputeTransfer();
        } else if (Input.GetKeyDown(KeyCode.W)) {
            tflightFactor = System.Math.Min(1.5, tflightFactor + 0.1);
            ComputeTransfer();
        }
    }


    /// <summary>
    /// Use the A/D to position the maneuver symbol on the selected orbit. 
    /// 
    /// As move to a new maneuver destination the transfer time will reset to the 
    /// minimum energy value. At a given maneuver position, can use W/S to increase/decrease
    /// the transfer time. 
    /// </summary>
    private void UpdateManeuverSymbol(int index) {

        if (Input.GetKey(KeyCode.A)) {
            targetEllipses[index].manueverPhase += scrollSpeed;
            ComputeTransfer();
        } else if (Input.GetKey(KeyCode.D)) {
            targetEllipses[index].manueverPhase -= scrollSpeed;
            ComputeTransfer();
        }
        AdjustTimeOfFlight();

        UpdateManeuverUI();

        if (targetEllipses[index].manueverPhase > 360f) {
            targetEllipses[index].manueverPhase -= 360f; 
        } else if (targetEllipses[index].manueverPhase < 0f) {
            targetEllipses[index].manueverPhase += 360f;
        }
        Vector3 pos = targetEllipses[index].orbitData.GetPhysicsPositionforEllipse( targetEllipses[index].manueverPhase);
        pos = ge.MapToScene(pos);

        targetEllipses[index].maneuverSymbol.transform.position = pos;
    }


    private void ComputeTransfer() {
        shipData = new OrbitData();
        shipData.centralMass = starNBody;
        shipData.SetOrbitForVelocity(spaceshipNBody, starNBody);

        targetData = new OrbitData(targetEllipses[selectedEllipse].orbitData);
        targetData.phase = targetEllipses[selectedEllipse].manueverPhase;

        // compute the min energy path (this will be in the short path direction)
        lambertU = new LambertUniversal(shipData, targetData, shortPath);

        // apply any time of flight change
        transferTime = tflightFactor * lambertU.GetTMin();
        bool reverse = !shortPath;

        // recompute with updated time
        const bool df = false;
        const int nrev = 0;
        int error = lambertU.ComputeXfer(reverse, df, nrev, transferTime);
        if (error != 0) {
            Debug.LogWarning("Lambert failed to find solution.");
            maneuverSegment.gameObject.SetActive(false);
            return;
        }
        Vector3 vel = lambertU.GetTransferVelocity();
        maneuverOrbitPredictor.SetVelocity(vel);
        maneuverSegment.SetVelocity(vel);
        maneuverOrbitPredictor.gameObject.SetActive(true);
        maneuverSegment.gameObject.SetActive(true);
    }

    private void EnableManeuver(int selected) {
        // enable manuever icon on selected orbit at current position of body 
        targetEllipses[selectedEllipse].maneuverSymbol.SetActive(true);
        targetEllipses[selectedEllipse].orbitData = new OrbitData(targetEllipses[selectedEllipse].ellipse);
        targetEllipses[selectedEllipse].manueverPhase = targetEllipses[selectedEllipse].orbitData.phase;
        maneuverSegment.destination = targetEllipses[selectedEllipse].maneuverSymbol;
        ComputeTransfer();
    }

    /// <summary>
    /// When second maneuver is done, use this to blank the maneuver text
    /// </summary>
    /// <param name="m"></param>
    private void ManeuverDoneCallback(Maneuver m) {
        maneuverText.text = "";
        targetEllipses[selectedEllipse].maneuverSymbol.SetActive(false);
        state = State.SELECT_DEST;
    }

    private void SequenceDoneCallback(OrbitUniversal orbitu) {
        targetEllipses[selectedEllipse].maneuverSymbol.SetActive(false);
        state = State.SELECT_DEST;
    }

    /// <summary>
    /// The spaceship has a fixed orbit that is on rails (must be an OrbitUniversal).
    /// 
    /// To allow for setting of specific times and forward/backward motion implement the transfer as a
    /// KeplerSequence of three OrbitUniversals:
    /// - initial orbit when t less then now
    /// - transfer ellipse when t between now and (now + transfer_time)
    /// - destination orbit when t greater than (now + transfer_time)
    /// 
    /// </summary>
    private void TransferOnRails() {

        // initial orbit - needs to be OrbitUniversal (could morph an OrbitEllipse, but keep things simple)
        OrbitUniversal initialOrbit = spaceshipNBody.GetComponent<OrbitUniversal>();
        if (initialOrbit == null) {
            Debug.LogError("script assumes ship orbit will be OrbitUniversal");
            return;
        }

        KeplerSequence keplerSeq = spaceshipNBody.GetComponent<KeplerSequence>();
        if (keplerSeq == null) {
            Debug.LogError("Cannot do on-rails transfer. Ship does not have a KeplerSequence");
            return;
        }

        // Kepler sequence already has the initial orbit. 
        // transfer orbit
        Vector3d xfer_r0 = lambertU.GetTransferPositionDouble();
        Vector3d xfer_v0 = lambertU.GetTransferVelocityDouble();
        double xfer_t = ge.GetPhysicalTimeDouble();
        keplerSeq.AppendElementRVT(xfer_r0, xfer_v0, xfer_t, false, spaceshipNBody, starNBody, null);
        // destination orbit
        double dest_t = xfer_t + transferTime;
        Vector3d dest_v = new Vector3d(targetData.GetPhysicsVelocityForEllipse(targetData.phase));
        Vector3d dest_r = new Vector3d(targetData.GetPhysicsPositionforEllipse(targetData.phase));
        keplerSeq.AppendElementRVT(dest_r, dest_v, dest_t, false, spaceshipNBody, starNBody, SequenceDoneCallback);

    }

    private void ExecuteTransfer() {
        // should always be set
        if (lambertU == null) {
            Debug.LogError("Internal state bug - no Lambert xfer to execute");
            return;
        }

        // If ship is Kepler, then implement the transfer as a series of conics and not manuevers. 
        if (OrbitUtils.IsOnRails(spaceshipNBody)) {
            TransferOnRails();
        } else {

            List<Maneuver> mlist = lambertU.GetManeuvers();
            if (mlist.Count == 2) {
                // Add second maneuver to match target orbit
                // add callback to dismiss maneuver info once done
                mlist[1].onExecuted = ManeuverDoneCallback;
            } else {
                Debug.LogError("Expected two maneuvers");
            }
            bool useManeuvers = true;
            if (useManeuvers) {
                ge.AddManeuvers(mlist);
            } else {
                // to ensure no maneuver dt error on first burn, set velocity explicitly
                ge.SetVelocity(spaceshipNBody, lambertU.GetTransferVelocity());
                ge.AddManeuver(mlist[1]);
            }
        }
        // start evolution if paused. 
        if (!ge.GetEvolve()) {
            ge.SetEvolve(true);
        }
        maneuverOrbitPredictor.gameObject.SetActive(false);
        maneuverSegment.gameObject.SetActive(false);
        if (maneuverRenderer != null) {
            maneuverRenderer.Clear();
        }
    }

    // Update is called once per frame
    void Update() {

        statefulHelp.text = stateHelpText[(int)state];

        switch(state) {
            case State.SELECT_DEST:
                maneuverOrbitPredictor.gameObject.SetActive(false);
                maneuverSegment.gameObject.SetActive(false);
                if (Input.GetKeyDown(KeyCode.N)) {
                    // if maneuver selected, disable
                    targetEllipses[selectedEllipse].maneuverSymbol.SetActive(false);
                    // advance to next ellipse
                    int nextEllipse = selectedEllipse + 1;
                    if (nextEllipse >= targetEllipses.Length) {
                        nextEllipse = 0;
                    }
                    SelectEllipse(nextEllipse);
                }  else if (Input.GetKeyDown(KeyCode.M)) {
                    state = State.COMPUTE_MANEUVER;
                    EnableManeuver(selectedEllipse);
                    ge.SetEvolve(false);
                } else if (Input.GetKeyDown(KeyCode.Space)) {
                    // Pause/Resume GE evolution
                    bool evolve = GravityEngine.Instance().GetEvolve();
                    ge.SetEvolve(!evolve);
                }
                break;

            case State.COMPUTE_MANEUVER:
                if (Input.GetKeyDown(KeyCode.X)) {
                    ExecuteTransfer();
                    state = State.DOING_XFER;
                } else if (Input.GetKeyDown(KeyCode.N)) {
                    state = State.SELECT_DEST;
                    targetEllipses[selectedEllipse].maneuverSymbol.SetActive(false);
                } else if (Input.GetKeyDown(KeyCode.F)) {
                    // flip shortPath toggle
                    shortPath = !shortPath;
                    maneuverSegment.shortPath = shortPath;
                    ComputeTransfer();
                } else {
                    UpdateManeuverSymbol(selectedEllipse);
                }
                break;

            case State.DOING_XFER:
                // nothing. Manuever callback will set state back to SELECT_DEST
                if (Input.GetKeyDown(KeyCode.Space)) {
                    // Pause/Resume GE evolution
                    bool evolve = GravityEngine.Instance().GetEvolve();
                    ge.SetEvolve(!evolve);
                }
                break;

            default:
                Debug.LogError("Unsupported state :" + state);
                break;
        }

    }


    //============================================================================================
    // Console commands: If there is a GEConsole in the scene, these commands will be available
    //============================================================================================

    // In-class console method
    // Quick and Dirty implementation for testing LambertUniversal code. 

    private string LambertUniversal(float time) {
        OrbitData shipData = new OrbitData();
        shipData.SetOrbitForVelocity(spaceshipNBody, starNBody);

        OrbitData targetData = targetEllipses[selectedEllipse].orbitData;
        targetData.phase = targetEllipses[selectedEllipse].manueverPhase;

        // get time and try universal
        LambertUniversal lu = new LambertUniversal(shipData, targetData, true);
        const bool reverse = false;
        const bool df = false;
        const int nrev = 0;
        int error = lu.ComputeXfer(reverse, df, nrev, time);
        if (error != 0) {
            return string.Format("Error: LambertUniversal rc=" + error);
        }
        spaceshipNBody.vel_phys = lu.GetTransferVelocity();
        return string.Format("time={0}  univ={1}", time, lu.GetTransferVelocity());
    }

    private void AddConsoleCommands() {
        GEConsole.RegisterCommandIfConsole(new LambertUniversalCommand(this));
    }
    /// <summary>
    /// Dump GE state to console
    /// </summary>
    public class LambertUniversalCommand : GEConsole.GEConsoleCommand
    {
        private LambertDemoController controller;

        public LambertUniversalCommand(LambertDemoController controller) {
            names = new string[] { "lu" };
            help = "Lambert Universal : lu <t> : compute velocity for intercept time";
            this.controller = controller;
        }

        override
        public string Run(string[] args) {
            if (args.Length != 2) {
                return "Require one argument (time for trajectory e.g. lu 20)";
            }
            return controller.LambertUniversal(float.Parse(args[1]));
        }
    }
}
