using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sample game logic for orbit transfers and manual maneuvering of a spaceship in a scene with several 
/// target objects. 
/// 
/// This code uses a simple state machine concept and manages the UI elements that form the mini-game MoonXfer.
/// 
/// Transfers to the Moon rely on objects in the scene that detect when the spaceship enters the moon's sphere
/// of influence and when a specified (or closest) approach to the moon is acheived. To communicate these events
/// back this controller implements the callback interfaces IPatchedConicChange and IClosestApproach. [Events would
/// be an alternative to this, but in demo code I prefer to keep the control flow explicit via callbacks]
/// 
/// </summary>
public class OrbitMGController : MonoBehaviour, IPatchedConicChange, 
                            IClosestApproach, ITrajectoryListener { 

	//! Link to the GameObject holding the spaceship model (assumed parent is NBody)
	public SpaceshipController spaceshipCtrl;

    public NBody[] targets; 

	public NBody centralMass;

	//! Prefab for interects
	public GameObject interceptMarker;

	//! Prefab for match
	public GameObject rendezvousMarker;

	// UI Panels
	public GameObject objectivePanel;
    public SelectionPanel objectiveSelectionPanel;

    public GameObject courseChangePanel;

    public GameObject manualPanel;

    public GameObject maneuverPanel;

    public GameObject orbitXferPanel;
    public GameObject orbitSelectionPanel;

    public GameObject maneuverCircularizePanel;

    public Toggle trajectoryToggle;
    public Toggle orbitPredictionToggle; 

    // Orbit UI Factory
    public OrbitXferUIFactory orbitXferUIFactory;

    // text object on the panel
    public Text maneuverText;

	private const string SELECT = "Select a ship.";
	private const string SELECTED = "Ship:";
	private const string TARGET = "Select a target.";
	private const string TARGETED = "Target ";
	private const string CHOOSE = "Choose an intercept";
	private const string CONFIRM_INTERCEPT = "Add maneuver (Y/N)?";
	private const string MANEUVER = "Manuever Set. <SPACE> to run.";

	// UI State Machine:

	public enum State {
		SELECT_OBJECTIVE,
        COURSE_CHANGE,
		INTERCEPT_SELECTION,
		ORBIT_SELECTION,
        MANEUVER_CIRCULARIZE,  
		MANEUVER,
        MANUAL,
		RUNNING
	}

    //! Game Controller state
	private State state;

    //! target object for orbit transfer calculations
	private NBody target;

	private bool running = true;

	// List of intercepts used in INTERCEPT_SELECTION mode
	private List<TrajectoryData.Intercept> intercepts;

    private List<GameObject> orbitUIWidgets; 

	//! Optional element to mark trajectory intercepts of two spaceships
	private TrajectoryIntercepts trajIntercepts;

	private TransferCalc transferCalc;

    //! OrbitPredictors in the scene that are active when the scene starts
    private List<OrbitPredictor> orbitPredictors;
    private List<OrbitRenderer> orbitRenderers;
    private OrbitPredictor shipOrbitPredictor;

    private GameObject spaceshipGO;
    private OrbitUniversal spaceshipOrbit;

    LunarCourseCorrection.CorrectionData courseCorrectionData;

    private double predictedDistance = 0f;

    // typical moon approach time is about 2 days, guard as 5 days
    private const double TIME_TO_MOON_SEC = 5 * 24 * 3600;
    private double time_to_moon_phys; 

    // Use this for initialization (must Awake, since start of GameLoop will set states)
    void Awake () {
		state = State.SELECT_OBJECTIVE;
		intercepts = null;

        time_to_moon_phys = TIME_TO_MOON_SEC / GravityScaler.GetGameSecondPerPhysicsSecond();
	
		if (targets.Length == 0) {
			Debug.LogError("No targets configured");
		}
		// Player is spaceship 1, others are objectives
		
		// take first ship to tbe the player
		target = targets[0];


		// Need to configure objective chooser 
		SetObjectiveOptions(targets);

		SetState(state);

		// add a trajectory intercepts component (it need to handle markers so it has
		// a monobehaviour base class). 
		// The pair of spaceships to be checked will be selected dynamically
		trajIntercepts = gameObject.AddComponent<TrajectoryIntercepts>();
		trajIntercepts.interceptSymbol = interceptMarker;
		trajIntercepts.rendezvousSymbol = rendezvousMarker;

        spaceshipGO = spaceshipCtrl.transform.parent.gameObject;
        // optional 
        spaceshipOrbit = spaceshipGO.GetComponent<OrbitUniversal>();

        // only record the elements that are active at the start of the scene
        orbitPredictors = new List<OrbitPredictor>();
        foreach (OrbitPredictor op in  (OrbitPredictor[])Object.FindObjectsOfType(typeof(OrbitPredictor)) ) {
            if (op.gameObject.activeInHierarchy) {
                orbitPredictors.Add(op);
                if (op.transform.parent == spaceshipGO.transform) {
                    shipOrbitPredictor = op;
                }
            }
        }
        if (shipOrbitPredictor == null) {
            Debug.LogError("Did not find orbit predictor for ship");
        }

        orbitRenderers = new List<OrbitRenderer>();
        foreach (OrbitRenderer or in (OrbitRenderer[])Object.FindObjectsOfType(typeof(OrbitRenderer)) ) {
            if (or.gameObject.activeInHierarchy) {
                orbitRenderers.Add(or);
            }
        }
    }

    void Start() {
        DisplayManager.Instance().DisplayMessage("Begin by selecting an objective");

        orbitUIWidgets = new List<GameObject>();

        AddConsoleCommands();
    }

    private void ObjectiveSelected(int selection) {
        target = targets[selection];
        // enable maneuver selection panel
        SetState(State.COURSE_CHANGE);
    }

	private void SetObjectiveOptions(NBody[] targets) {
        int count = 0;
        objectiveSelectionPanel.Clear();
		foreach (NBody nbody in targets) 
		{
            int _count = count++;
            objectiveSelectionPanel.AddButton(nbody.gameObject.name, () => ObjectiveSelected(_count));
        }
    }

	private const float MouseSelectRadius = 20f;

	// Also allow ship selection with mouse click
	private int GetShipSelection() {
		// TODO: Extend to do touch
		if (Input.GetMouseButtonDown(0)) {
			Vector3 mousePos = Input.mousePosition;
			// see if close enough to a ship
			for( int i=0; i < targets.Length; i++) {
				Vector3 shipScreenPos = Camera.main.WorldToScreenPoint(targets[i].transform.position);
				Vector3 shipXYPos = new Vector3(shipScreenPos.x, shipScreenPos.y, 0);
				if (Vector3.Distance(mousePos, shipXYPos) < MouseSelectRadius) {
					return i;
				}
			}
		}
		return -1;
	}
    
    // Set all panels to inactive
	private void InactivatePanels() {
		objectivePanel.SetActive(false);
        courseChangePanel.SetActive(false);
        manualPanel.SetActive(false);
		orbitXferPanel.SetActive(false);
        orbitSelectionPanel.SetActive(false);
        maneuverCircularizePanel.SetActive(false);
		maneuverPanel.SetActive(false);
    }

    private void SetState(State newState) {
        if (state == newState) {
            return;
        }
        Debug.LogFormat("{0}:State change {1} => {2}", gameObject.name, state, newState);
        switch (newState) {
            case State.SELECT_OBJECTIVE:
                InactivatePanels();
                SetTrajectoryPrediction(false);
                // spaceshipCtrl.Cancel();
                spaceshipCtrl.ShowManueverAxes(false);
                objectivePanel.SetActive(true);
                GravityEngine.Instance().SetEvolve(true);
                break;

            case State.COURSE_CHANGE:
                InactivatePanels();
                courseChangePanel.SetActive(true);
                break;

            case State.INTERCEPT_SELECTION:
            case State.MANUAL:
                running = false;
                GravityEngine.Instance().SetEvolve(running);
                // trajectory prediction is "heavy", especially at high time zoom. Only enable it when it is
                // needed
                SetTrajectoryPrediction(trajectoryToggle.isOn);
                InactivatePanels();
                manualPanel.SetActive(true);
                spaceshipCtrl.ShowManueverAxes(true);
                break;

            case State.ORBIT_SELECTION:
                running = false;
                GravityEngine.Instance().SetEvolve(running);
                InactivatePanels();
                orbitXferPanel.SetActive(true);
                orbitSelectionPanel.SetActive(true);
                CalculateTransfers();
                spaceshipCtrl.ShowManueverAxes(false);
                break;

            case State.MANEUVER_CIRCULARIZE:
                // stay in orbit predictor mode (could do trajectory projection)
                // eventually give orbit choices here...
                running = false;
                GravityEngine.Instance().SetEvolve(running);
                InactivatePanels();
                maneuverCircularizePanel.SetActive(true);
                break;

            case State.MANEUVER:
                running = true;
                SetTrajectoryPrediction(false);
                GravityEngine.Instance().SetEvolve(running);
                trajIntercepts.ClearMarkers();
                InactivatePanels();
                maneuverPanel.SetActive(true);
                break;

            default:
                Debug.LogError("Internal error - unknown state");
                break;
        }
        state = newState;
	}

    /// <summary>
    /// Configure trajectory prediction. Since prediction is a computational burden (especially when timeZoom is high)
    /// only use this when the ship maneuver is being changed. 
    /// 
    /// Orbit renderering and prediction is replaced with trajectories of the same color, so need to flip between these
    /// sets of objects being enabled. 
    /// 
    /// </summary>
    /// <param name="enable"></param>
    private void SetTrajectoryPrediction(bool enable) {
        GravityEngine.Instance().SetTrajectoryPrediction(enable);
        foreach( OrbitPredictor op in orbitPredictors) {
            op.gameObject.SetActive(!enable);
        }
        foreach (OrbitRenderer or in orbitRenderers) {
            or.gameObject.SetActive(!enable);
        }
        if (!enable) {
            ClearUIWidgets();
            trajIntercepts.ClearMarkers();
        }
    }

    // "Note also that the Input flags are not reset until "Update()", 
    // so its suggested you make all the Input Calls in the Update Loop."
    // Do key processing in Update. Means some GE calls happen off stride in
    // FixedUpdate.
    void Update() {

		if (Input.GetKeyUp(KeyCode.Space)) {
			running = !running;
			GravityEngine.Instance().SetEvolve(running);
		}

 
        // RF: Have state inner class with update method to segregate?
        switch (state) 
		{
			case State.INTERCEPT_SELECTION:
                break;

            case State.MANUAL:
                 break;

            case State.MANEUVER_CIRCULARIZE:
                 break;

            case State.SELECT_OBJECTIVE:
				// check for mouse on a target object
				int selected = GetShipSelection();
				if (selected >= 0) {
					target = targets[selected];
				}
                 break;

			case State.MANEUVER:
				// Show current time and pending maneuvers
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				sb.Append(string.Format("World Time: {0:000.0} [x{1}]\n", 
                            GravityEngine.Instance().GetPhysicalTime(), 
                            GravityEngine.Instance().GetTimeZoom() ));
				sb.Append(string.Format("\nManeuvers Pending:\n"));
                string[] mstrings = spaceshipCtrl.ManeuverString();
                if (mstrings.Length > 0) {
                    foreach (string s in spaceshipCtrl.ManeuverString()) {
                        sb.Append(string.Format("{0}\n", s));
                    }
                    maneuverText.text = sb.ToString();
                } else {
                    SetState(State.SELECT_OBJECTIVE);
                }
				break;

			default:
				break;
		}

		
	}

    //-------------------------------------------------------------------------
    // Transfer/Maneuver calculations
    //-------------------------------------------------------------------------

    public void OrbitTransferSelected(OrbitTransfer transfer) {
        spaceshipCtrl.SetTransfer(transfer);
        ClearUIWidgets();
        SetState(State.MANEUVER);
    }

    private void ClearUIWidgets() {
        foreach (GameObject uiWidget in orbitUIWidgets) {
            Destroy(uiWidget);
        }
        orbitUIWidgets.Clear();
    }


    private void CalculateTransfers() {

		// Find xfer choices and present to user
		transferCalc = new TransferCalc(spaceshipCtrl.GetNBody(), target, centralMass);
        bool rendezvous = true; 
        List<OrbitTransfer> transfers =  transferCalc.FindTransfers(rendezvous);

        orbitUIWidgets.Clear();
        foreach (OrbitTransfer transfer in transfers) {
            GameObject uiWidget = orbitXferUIFactory.GetUIWidget(transfer, this);
            if (uiWidget != null) {
                uiWidget.transform.SetParent(orbitSelectionPanel.transform);
                orbitUIWidgets.Add(uiWidget);
            }

        }
	}

    public void InterceptSelected(TrajectoryData.Intercept intercept) {
        foreach (GameObject uiWidget in orbitUIWidgets) {
            Destroy(uiWidget);
        }
        orbitUIWidgets.Clear();
        spaceshipCtrl.SetManeuver(intercept);
        spaceshipCtrl.ShowManueverAxes(false);
        SetState(State.MANEUVER);
    }

    private void CheckIntercepts() {

        if (state == State.MANUAL || state == State.INTERCEPT_SELECTION) {
            // always start fresh
            foreach (GameObject uiWidget in orbitUIWidgets) {
                Destroy(uiWidget);
            }
            orbitUIWidgets.Clear();

            // check for intercepts
            // Create selection dialog for each one
            intercepts = MarkIntercepts(spaceshipCtrl, target);
            if (intercepts.Count > 0) {
                orbitSelectionPanel.SetActive(true);

                foreach (TrajectoryData.Intercept intercept in intercepts) {
                    GameObject uiWidget = orbitXferUIFactory.GetInterceptWidget(intercept, this);
                    if (uiWidget != null) {
                        uiWidget.transform.SetParent(orbitSelectionPanel.transform);
                        orbitUIWidgets.Add(uiWidget);
                    }
                }

            } else {
            	Debug.Log("No intercepts");
                InactivatePanels();
                manualPanel.SetActive(true);
            }
        }
	}

	/// <summary>
	/// Mark intercepts with the designated symbols and return a list of intercepts found
	/// for the predicted path of ship intersecting with the path of the target.
	/// </summary>
	///
	/// <param name="ship">The ship</param>
	/// <param name="target">The target</param>
	///
	/// <returns>The intercepts.</returns>
	private List<TrajectoryData.Intercept> MarkIntercepts(SpaceshipController shipCtrl, NBody target) {
        // delta distance is scale dependent. For now use an ugly *if*
        float deltaDistance = 1f;
        if (GravityEngine.Instance().units == GravityScaler.Units.ORBITAL) {
            deltaDistance = 20f;
        }
        const float deltaTime = 2f;
        const float rendezvousDT = 1f; 
		trajIntercepts.spaceship = shipCtrl.GetTrajectory();
        Trajectory trajectory = target.GetComponentInChildren<Trajectory>();
        if (trajectory == null) {
            Debug.LogError("Target requires a child with a trajectory component");
            return new List<TrajectoryData.Intercept>();
        }
        trajIntercepts.target = trajectory;
		trajIntercepts.ComputeAndMarkIntercepts(deltaDistance, deltaTime, rendezvousDT);
		intercepts = trajIntercepts.GetIntercepts();
		return intercepts;
	}

	//-------------------------------------------------------------------------
	// UI Callbacks
	// Buttons/Dropdowns boxes call back via these methods
	//-------------------------------------------------------------------------


	public void ManualMode() {
		SetState(State.INTERCEPT_SELECTION);
	}

    public void ManeuverMode() {
        SetState(State.MANEUVER_CIRCULARIZE);
    }

    public void OrbitSelectionMode() {
        SetState(State.ORBIT_SELECTION);
    }

    public void Top() {
        spaceshipCtrl.Cancel();
        SetState(State.SELECT_OBJECTIVE);
	}

    // Circularize button
    public void Circularize() {
        NBody centerNBody = shipOrbitPredictor.centerBody.GetComponent<NBody>();
        transferCalc = new TransferCalc(spaceshipCtrl.GetNBody(), null, centerNBody);
        spaceshipCtrl.SetTransfer(transferCalc.Circularize());
        SetState(State.MANEUVER);
    }

    /// <summary>
    /// Callback to apply the manual maneuver. 
    /// 
    /// Used in cases where a trajectory intercept or orbit transfer
    /// is not desired. 
    /// 
    /// </summary>
    public void ExecuteManeuver() {
        spaceshipCtrl.Execute();
        SetState(State.MANEUVER);
    }

    // Manual Mode: Set prediction

    public void ToggleTrajectoryPrediction(Toggle toggle) {
        orbitPredictionToggle.isOn = !toggle.isOn;
        SetTrajectoryPrediction(toggle.isOn);
    }

    public void ToggleOrbitPrediction(Toggle toggle) {
        trajectoryToggle.isOn = !toggle.isOn;
        SetTrajectoryPrediction(!toggle.isOn);
    }


    //-------------------------------------------------------------------------
    // Patched Conic Change
    //-------------------------------------------------------------------------

    // Debug routine to evaluate actual angle of arrival at SOI
    private void LogArrivalAngle() {
        Vector3 Rs = Vector3.Normalize(spaceshipGO.transform.position - target.transform.position);
        // cheat - know planet is at (0,0,0). If this changes, subtract from target
        Vector3 R = -1f*Vector3.Normalize(target.transform.position);
        float angle = Vector3.Angle(Rs, R);
        // check sign
        if (Vector3.Cross(Rs, R).z < 0f) {
            angle *= -1f;
        }
        // angle lambda1 defined from -X axis
        Debug.LogFormat("Actual lambda1={0:00.00} (deg) Rs={1} R={2}", angle, Rs, R);
    }

    /// <summary>
    /// Callback for the PatchedConicSOI script. 
    /// 
    /// When the ship NBody moves into or out of the spher of influence of the moon this callback will 
    /// be triggered. 
    /// 
    /// The OrbitPredictor center object will be changed to reflect the new infuencer. 
    /// 
    /// If the spaceship is in rails mode the center object for the OrbitUniversal will be updated.
    /// </summary>
    /// <param name="newObject"></param>
    /// <param name="oldObject"></param>
    public void OnNewInfluencer(NBody newObject, NBody oldObject) {
        Debug.LogFormat("Influence now={0} (was {1})", newObject.name, oldObject.name);
        DisplayManager.Instance().DisplayMessage(string.Format("Entered {0}'s Influence", newObject.name));
        shipOrbitPredictor.SetCenterObject(newObject.gameObject);
        spaceshipCtrl.orbitCenter = newObject.GetComponent<NBody>();
        LogArrivalAngle();

        if (spaceshipOrbit != null && spaceshipOrbit.IsOnRails()) {
            spaceshipOrbit.SetNewCenter(newObject);
        }
    }

    //-------------------------------------------------------------------------
    // Closest Approach
    //-------------------------------------------------------------------------

    public void OnClosestApproachTrigger(NBody body1, NBody body2, float distance) {

        Debug.LogFormat("Closest Approach d={0}", distance);
        GravityEngine.Instance().SetEvolve(false);
        GravityEngine.Instance().ClearManeuvers();
        SetState(State.MANUAL);
        DisplayManager.Instance().DisplayMessage(string.Format("Lunar distance tiggered, Maneuver for Orbit"));

    }

    //-------------------------------------------------------------------------
    // Spaceship has updated it's trajectory - check for intercepts
    //-------------------------------------------------------------------------
    public void TrajectoryUpdated(NBody nbody) {

        CheckIntercepts();
    }

    //============================================================================================
    // Console command Support
    //============================================================================================

    private NBody GetTargetByName(string name) {
        foreach (NBody nbody in targets) {
            if (nbody.gameObject.name == name)
                return nbody;
        }
        return null;
    }

    /// <summary>
    /// Initiate a transfer of the spaceship to the moon. The name of the targets[] entry that is the moon
    /// is provided as a string argument. 
    /// 
    /// Typically called by the GEConsole. 
    /// </summary>
    public string MoonTransfer(string moonName, float angle)  {
        // Find xfer choices and present to user
        OrbitData shipOrbit = new OrbitData();
        shipOrbit.SetOrbit(spaceshipCtrl.GetNBody(), centralMass);
        OrbitData targetOrbit = new OrbitData();
        targetOrbit.SetOrbit(GetTargetByName(moonName), centralMass);
        OrbitTransfer xfer = new PatchedConicXfer(shipOrbit, targetOrbit, angle);
        spaceshipCtrl.SetTransfer(xfer);
        SetState(State.MANEUVER);
        // display the maneuver in the console (will only be one for patched conic)
        return xfer.GetManeuvers()[0].LogString()+"\n";
    }

 
    /// <summary>
    /// Callback for MoonPreview when run in async mode. 
    /// </summary>
    /// <param name="lcc"></param>
    private void MoonPreviewCompleted(LunarCourseCorrection lcc) {
        // Update the GE Console with the result stored in calcData
        string s = string.Format("Closest approach of {0} at t={1}\n", 
                    courseCorrectionData.distance, courseCorrectionData.timeAtApproach);
        GEConsole.Instance().AddToHistory(s);
    }

    public string MoonPreview(string moonName, float lambda1, bool async) {
        // Step 1: Determine the patched conic xfer
        OrbitData shipOrbit = new OrbitData();
        NBody shipNbody = spaceshipCtrl.GetNBody();
        shipOrbit.SetOrbit( shipNbody, centralMass);
        OrbitData moonOrbit = new OrbitData();
        NBody moonNbody = GetTargetByName(moonName);
        moonOrbit.SetOrbit(moonNbody, centralMass);
        OrbitTransfer xfer = new PatchedConicXfer(shipOrbit, moonOrbit, lambda1);

        // Step 2: Make a copy of the universe state and evolve forward to find min distance to 
        // moon. 
        GravityEngine ge = GravityEngine.Instance();
        GravityState gs = ge.GetGravityStateCopy();
        // there is only one maneuver to add
        gs.maneuverMgr.Add(xfer.GetManeuvers()[0]);
        // run a simulation and find the closest approach (Expensive!)
        LunarCourseCorrection lcc = new LunarCourseCorrection(shipNbody, moonNbody);
        // want to be within 10% of Earth-Moon distance, before start checking
        courseCorrectionData = new LunarCourseCorrection.CorrectionData();
        courseCorrectionData.gravityState = gs;
        courseCorrectionData.approachDistance = 0.1f * moonOrbit.a; ;
        courseCorrectionData.correction = 0;
        courseCorrectionData.maxPhysTime = time_to_moon_phys;
        // Direct (unthreaded) calculation
        if (async) {
            lcc.ClosestApproachAsync(courseCorrectionData, MoonPreviewCompleted);
            return "Calculation started...\n";
        } else {
            predictedDistance = lcc.ClosestApproach(courseCorrectionData);
            return string.Format("Patched Conic with lambda={0} => approach={1}\n", lambda1, predictedDistance);
        }

    }

    public string ApproachPrediction(string moonName) {
        // @TODO: Too much C&P!
        // Step 1: Determine the patched conic xfer
        OrbitData shipOrbit = new OrbitData();
        NBody shipNbody = spaceshipCtrl.GetNBody();
        shipOrbit.SetOrbit(shipNbody, centralMass);
        OrbitData moonOrbit = new OrbitData();
        NBody moonNbody = GetTargetByName(moonName);
        moonOrbit.SetOrbit(moonNbody, centralMass);
        //Make a copy of the universe state and evolve forward to find min distance to 
        // moon. 
        GravityEngine ge = GravityEngine.Instance();
        GravityState gs = ge.GetGravityStateCopy();
        // run a simulation and find the closest approach (Expensive!)
        LunarCourseCorrection lcc = new LunarCourseCorrection(shipNbody, moonNbody);
        // want to be within 10% of Earth-Moon distance, before start checking
        courseCorrectionData = new LunarCourseCorrection.CorrectionData();
        courseCorrectionData.gravityState = gs;
        courseCorrectionData.approachDistance = 0.1f * moonOrbit.a; ;
        courseCorrectionData.correction = 0;
        courseCorrectionData.maxPhysTime = time_to_moon_phys;
        lcc.ClosestApproachAsync(courseCorrectionData, MoonPreviewCompleted);
        return "Calculation started...\n";
    }

    private LunarCourseCorrection lcc; 

    /// <summary>
    /// Determine the correction required to establish the desired distance to the moon (i.e. that which
    /// was predicted by the preview)
    /// </summary>
    /// <param name="moonName"></param>
    /// <returns></returns>
    public string LunarCourseCorrection(string moonName) {
        NBody moon = GetTargetByName(moonName);
        OrbitData moonOrbit = new OrbitData();
        NBody moonNbody = GetTargetByName(moonName);
        moonOrbit.SetOrbit(moonNbody, centralMass);
        GravityEngine.Instance().SetEvolve(false);
        lcc = new LunarCourseCorrection(spaceshipCtrl.GetNBody(), moon);
        float approachDistance = 0.1f * moonOrbit.a;
        double targetDistance = predictedDistance;
        double targetAccuracy = 0; // Does not matter for closest
        string result = lcc.CorrectionCalcAsync(targetDistance, targetAccuracy, approachDistance,
                    time_to_moon_phys, CorrectionCalcCompleted);
        // GravityEngine.Instance().SetEvolve(true);
        return result;
    }

    private void CorrectionCalcCompleted(LunarCourseCorrection lcc) {
        // Update the GE Console
        string s = LunarCorrectionApply();
        GEConsole.Instance().AddToHistory(s);
    }

    public string LunarCorrectionApply() {
        if (lcc != null) {
            Debug.Log("LCC is done.");
            // Get the final correction and apply it
            double[] v = new double[3];
            lcc.GetCorrectionVelocity(ref v);
            GravityEngine.Instance().SetVelocityDouble(spaceshipCtrl.GetNBody(), ref v);
            LunarCourseCorrection.CorrectionData cdata = lcc.correctionFinal;
            string s = string.Format("Correction applied. correction={0} giving distance={1} at {2}\n",
                cdata.correction, cdata.distance, cdata.timeAtApproach);
            Debug.Log(s);
            lcc = null;
            return s;
        } else {
            return "No calculation was scheduled.";
        }
        
    }

    //============================================================================================
    // Console commands: If there is a GEConsole in the scene, these commands will be available
    //============================================================================================

    private void AddConsoleCommands() {
        GEConsole.RegisterCommandIfConsole(new MoonXfer(this));
        GEConsole.RegisterCommandIfConsole(new LunarCorrectionStartCmd(this));
        GEConsole.RegisterCommandIfConsole(new PatchedConicPreview(this));
        GEConsole.RegisterCommandIfConsole(new PatchedConicPreviewAsync(this));
        GEConsole.RegisterCommandIfConsole(new ApproachPredictionCmd(this));
    }

    /// <summary>
    /// Perform a transfer to the Moon with a patched conic xfer
    /// </summary>
    public class MoonXfer : GEConsole.GEConsoleCommand
    {
        private OrbitMGController omgController; 

        public MoonXfer(OrbitMGController omgController) {
            names = new string[] { "moonxfer", "m" };
            help = "OrbitMGController: moonxfer <patch_conic_angle>\n" +
                   " transfer to the moon with patched conic angle (adds the required maneuver to spaceship)\n";
            this.omgController = omgController; 
        }

        override
        public string Run(string[] args) {
            if (args.Length != 2) {
                return "Require one argument (angle of SOI intersection). 75 is a good default";
            }
            float angle = float.Parse(args[1]);
            return omgController.MoonTransfer("Moon", angle);
        }
    }

    /// <summary>
    /// Perform a Lunar course correction. Requires a preceeding pcp angle has been run 
    /// to establish a target approach distance. 
    /// </summary>
    public class LunarCorrectionStartCmd : GEConsole.GEConsoleCommand
    {
        private OrbitMGController omgController;

        public LunarCorrectionStartCmd(OrbitMGController omgController) {
            names = new string[] { "lunar_correction", "c" };
            help = "OrbitMGController: calculate the course correction during lunar transfer.\n" + 
                   "  Must run pcp first to establish target distance.";
            this.omgController = omgController;
        }

        override
        public string Run(string[] args) {
            return omgController.LunarCourseCorrection("Moon");
        }
    }

    /// <summary>
    /// Preview a patched conic xfer by running a simulation and determing the closest approach. This
    /// run in-line and will cause a hit to the game frame rate. 
    /// </summary>
    public class PatchedConicPreview : GEConsole.GEConsoleCommand
    {
        private OrbitMGController omgController;

        public PatchedConicPreview(OrbitMGController omgController) {
            names = new string[] { "pcp" };
            help = "PatchedConicPreview (pcp): pcp <angle> :closest approach info for moon trajectory with angle\n" +
                "Runs synchornously and will cause a frame rate stall.";
            this.omgController = omgController;
        }

        override
        public string Run(string[] args) {
            if (args.Length != 2) {
                return "Require one argument (angle of SOI intersection)";
            }
            const bool async = false;
            return omgController.MoonPreview("Moon", float.Parse(args[1]), async );
        }
    }

   
    /// <summary>
    /// Preview a patched conic xfer by running a simulation and determing the closest approach. This
    /// run in-line and will cause a hit to the game frame rate. 
    /// </summary>
    public class PatchedConicPreviewAsync : GEConsole.GEConsoleCommand
    {
        private OrbitMGController omgController;

        public PatchedConicPreviewAsync(OrbitMGController omgController) {
            names = new string[] { "pcpa" };
            help = "PatchedConicPreview (pcpa): pcpa <angle> : closest approach info for moon trajectory with angle\n" +
                "Runs aynchronously. A callback will post the result to the console.";
            this.omgController = omgController;
        }

        override
        public string Run(string[] args) {
            if (args.Length != 2) {
                return "Require one argument (angle of SOI intersection)";
            }
            const bool async = true;
            return omgController.MoonPreview("Moon", float.Parse(args[1]), async);
        }
    }

    /// <summary>
    /// Preview a patched conic xfer by running a simulation and determing the closest approach. This
    /// run in-line and will cause a hit to the game frame rate. 
    /// </summary>
    public class ApproachPredictionCmd : GEConsole.GEConsoleCommand
    {
        private OrbitMGController omgController;

        public ApproachPredictionCmd(OrbitMGController omgController) {
            names = new string[] { "ap" };
            help = "Project the path forward and determine closest approach to Moon\n" +
                "Use only after a transfer has been initiated";
            this.omgController = omgController;
        }

        override
        public string Run(string[] args) {
            if (args.Length != 1) {
                return "No arguments required. ";
            }
            return omgController.ApproachPrediction("Moon");
        }
    }

}
