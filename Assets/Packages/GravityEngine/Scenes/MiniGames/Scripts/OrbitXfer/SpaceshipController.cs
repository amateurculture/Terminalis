using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manage the maneuver UI on screen elements for adjusting velocity and illustrating the
/// velocity change vector. 
/// 
/// This widget interacts with the mouse and determines if mouse actions are being used to adjust
/// the velocity vector of the ship as displayed in the scene. 
/// 
/// A mouse down on an axis label (or the velocity "bar" for the axis) allows further mouse moves (while pressed)
/// to adjust that axis based on the projection of the mouse distance from this initial mouse down location. 
/// 
/// 
/// </summary>
public class SpaceshipController : MonoBehaviour {

    public enum Frame
    {
        WORLD,      // use Unity's x,y,z axes
        BODY,       // use the axes of the spaceship models orientation
        ORBIT       // use axes wrt to the orbit around a body
    };

    //! The frame along which the velocity change handles are to be shown 
    public Frame velocityFrame;

    //! the center of the orbit (only required for ORBIT frame)
    public NBody orbitCenter;

    public GameObject trajectoryListener; 
    private ITrajectoryListener trajectoryListenerIF;

    //! Mouse control absolute velocity vs change in velocity
    public bool mouseVelocityIncrease = false;

     //! Prefab for the axis ends (must have a collider for raycast to work)
    public GameObject axisEndPrefab;
    private Vector3 DEFAULT_DIRECTION = Vector3.forward;

    //! Offset in world space for axis prefabs
    public float axisOffset = 3f;
    //! Material applied to axis end prefabs for each axis
    public Material axis1Material;
    public Material axis2Material;
    public Material axis3Material;
    // re-orginaztion of axis materials into easy indexed version
    private Material[] axisEndMaterials;

    public float uiToPhysVelocityScale = 0.1f; 

    // (Put all this in an editor foldout)
    //! show velocity and dynamic adjustments in scene
    public bool showVelocityVector = true;
    //! dV velocity lengths in Unity co-ordinates will be the physics velocity times this factor
    public float velocityScalePhysToScreen = 1f; 
    public Material velocityLineMaterial;
    public float velocityLineWidth = 1.0f;

    // Editor variables for foldout memory
    public bool editorShowAxis;
    public bool editorShowVelocity;

    private GameObject[] axisEndObjects;
    private int axisSelected = 0; 
    // one of 6 axis ends
    private int axisEndSelected = 0;
    // location of initial left-click for changing a velocity
    private Vector3 clickStartPosition; 
    //! parent game object must have an NBody component
    private NBody spaceShipNbody;

    // velocity adjust is time-based (moving along axis away from initial click controls the speed of increase/decrease)
    // If within a fixed screen percentage of origin then conside this "neutral" and no velocity adjust happens
    private float DV_THRESHOLD = 0.05f * Screen.width;

    // twitch factor to scale how dynamic mouse velocity changes are
    private float MOUSE_DYNAMIC_TWITCH = 0.01f;

    //! The velocity change being created by this widget
    private Vector3 shipManeuverAdjust;
    private Vector3 shipManeuverVelocityNet;

    private const int NUM_AXES = 3;
    private Vector3[] axes = new Vector3[3];

    // objects to hold lines
    private LineRenderer velocityLine;
    private LineRenderer axisLine; 

    private bool last_running;

    private const float MAX_MANEUVER_VELOCITY = 100f;

    // Keep a copy of maneuvers for display purpose. Need to register callback to 
    // be notified when are cleaned up. 
    private List<Maneuver> maneuverList;
    private Trajectory trajectory;

    private double[] shipVelocity; 

    // time of last impulse update
    private float lastImpulseUpdate;  

    // UI state tracking
    private enum UIState
    {
        NONE,   // at startup
        IDLE, 
        AXIS_DISPLAYED,
        AXIS_SELECTED
    };

    private UIState uiState;

    //! trajectory has changed, used in checking for intercepts in manual maneuver mode
    private bool lastTrajectoryUpToDate = false;


    // Use this for initialization
    void Start() {
        spaceShipNbody = transform.parent.GetComponent<NBody>();
        if (spaceShipNbody == null) {
            Debug.Log("Configuration error. Parent of SpaceshipRV must have NBody");
        }

        if (axisEndPrefab == null) {
            Debug.LogError("axisEndPrefan not set");
        } else if (axisEndPrefab.GetComponent<Collider>() == null) {
            Debug.LogError("Collider missing on axisEndPrefan - needed for raycast to work.");
        }

        // Optional trajectory may be used by GameController code (see OrbitMGController)
        trajectory = spaceShipNbody.GetComponentInChildren<Trajectory>();

        maneuverList = new List<Maneuver>();
 
        axisEndObjects = new GameObject[2*NUM_AXES];
        uiState = UIState.NONE;
        SetState(UIState.IDLE);

        shipVelocity = new double[3];

        if (trajectoryListener != null) {
            trajectoryListenerIF = trajectoryListener.GetComponent<ITrajectoryListener>();
            if (trajectoryListenerIF == null) {
                Debug.LogError("Object " + trajectoryListener.name + " is missing ITrajectoryListener");
            }
        }

        axisEndMaterials = new Material[] {axis1Material, axis1Material,
                                    axis2Material, axis2Material,
                                    axis3Material, axis3Material};

    }

    public NBody GetNBody() {
        return spaceShipNbody;
    }

    /// <summary>
    /// Based on the selected co-ordinate frame, assigne the vectors for the principle axes. 
    /// </summary>
    private void SetAxes() {
    // create unit length axis vectors
    switch(velocityFrame) {
        case Frame.BODY:
            axes[0] = new Vector3(1, 0, 0);
            axes[1] = new Vector3(0, 1, 0);
            axes[2] = new Vector3(0, 0, 1);
            Quaternion rotation = transform.rotation;
            axes[0] = rotation * axes[0];
            axes[1] = rotation * axes[1];
            axes[2] = rotation * axes[2];
            break;
        case Frame.ORBIT:
            axes[0] = Vector3.Normalize(spaceShipNbody.transform.position - orbitCenter.transform.position);
            // axes[2] is normal to the orbit plane
            axes[2] = Vector3.Normalize(Vector3.Cross(axes[0], Vector3.Normalize(spaceShipNbody.vel_phys)));
            axes[1] = Vector3.Normalize(Vector3.Cross(axes[0], axes[2])); 
            break;
        case Frame.WORLD:
        default:
            axes[0] = new Vector3(1, 0, 0);
            axes[1] = new Vector3(0, 1, 0);
            axes[2] = new Vector3(0, 0, 1);
            break;
    }
}

    /// <summary>
    /// Using the specified co-ordinate systems, create axis end objects. 
    /// 
    /// This implementation assumes scene is paused and can do SetAxes once as they are created. 
    /// 
    /// Need to leave as world level objects - since otherwise they can pick up rotation from NBody or model.
    /// 
    /// </summary>
    private void ShowAxisEndPoints() {

        // create if necessary
        if (axisEndObjects[0] == null) {
            for (int i = 0; i < 2*NUM_AXES; i++) {
                axisEndObjects[i] = Instantiate<GameObject>(axisEndPrefab);
                Renderer renderer = axisEndObjects[i].GetComponent<Renderer>();
                renderer.material = axisEndMaterials[i];
            }
        }

        // set positions
        Vector3 shipPos = spaceShipNbody.transform.position;
        SetAxes();

        for (int i = 0; i < NUM_AXES; i++) {
            // pos direction
            axisEndObjects[2*i].transform.position = shipPos + axisOffset * axes[i];
            axisEndObjects[2 * i].transform.rotation = Quaternion.FromToRotation(DEFAULT_DIRECTION, axes[i]);
            // neg direction
            axisEndObjects[2 * i + 1].transform.position = shipPos - axisOffset * axes[i];
            axisEndObjects[2 * i + 1].transform.rotation = Quaternion.FromToRotation(DEFAULT_DIRECTION, -axes[i]);
        }

        for (int i = 0; i < 2 * NUM_AXES; i++) {
            axisEndObjects[i].SetActive(true);
        }

    }

    /// <summary>
    /// Leave selected axis endpoint active in some cases. (-1 for hide all)
    /// </summary>
    /// <param name="exceptIndex"></param>
    private void HideAxisEndPoints(int exceptIndex) {
        for (int i=0; i < axisEndObjects.Length; i++) {
            if (i != exceptIndex) {
                axisEndObjects[i].SetActive(false);
            }
        }
    }

    private void DeleteAxisEndPoints() {
        for (int i = 0; i < axisEndObjects.Length; i++) {
            if (axisEndObjects[i] != null) {
                Destroy(axisEndObjects[i]);
                axisEndObjects[i] = null;
            }
        }
    }

    /// <summary>
    /// Lines are used to represent the axis being adjusted and the net delta velocity
    /// if show for this feature is enabled. 
    /// 
    /// </summary>
    private void InitLines() {
        if (showVelocityVector && (velocityLine == null)) {
            velocityLine = new GameObject("velocityLine").AddComponent<LineRenderer>();
            velocityLine.material = velocityLineMaterial;
            velocityLine.positionCount = 2;
            velocityLine.startWidth = velocityLineWidth;
            velocityLine.endWidth = velocityLineWidth;
            velocityLine.useWorldSpace = true;

            axisLine = new GameObject("axisLine").AddComponent<LineRenderer>();
            axisLine.material = axisEndMaterials[axisEndSelected];
            axisLine.positionCount = 2;
            axisLine.startWidth = velocityLineWidth;
            axisLine.endWidth = velocityLineWidth;
            axisLine.useWorldSpace = true;
        }
    }

    private void DestroyLines() {
        if (showVelocityVector) {
            if (velocityLine != null) {
                Destroy(velocityLine.gameObject);
            }
            if (axisLine != null) {
                Destroy(axisLine.gameObject);
            }
        }
    }

    private void SetState(UIState newState) {

        // Debug.LogFormat("{0}:State change {1} => {2}", gameObject.name, uiState, newState);
        // code on state exit
        switch (uiState) {
            case UIState.AXIS_DISPLAYED:
                shipManeuverVelocityNet += shipManeuverAdjust;
                shipManeuverAdjust = Vector3.zero;
                break;

            case UIState.AXIS_SELECTED:
                break;

            case UIState.IDLE:
                // if GE not stopped, stop it
                if (GravityEngine.Instance().GetEvolve()) {
                    GravityEngine.Instance().SetEvolve(false);
                }
                GravityEngine.Instance().GetVelocityDouble(spaceShipNbody, ref shipVelocity);
                break;

            case UIState.NONE:
                break;

            default:
                Debug.LogError("Unsupported state: " + newState);
                break;

        }
        // enter new state
        uiState = newState;
        switch (newState) {
            case UIState.AXIS_DISPLAYED:
                lastImpulseUpdate = 0f;
                UpdateScreenLines();    // hide axis line
                ShowAxisEndPoints();
                InitLines();
                break;

            case UIState.AXIS_SELECTED:
                HideAxisEndPoints(axisEndSelected); 
                break;

            case UIState.IDLE:
                shipManeuverAdjust = Vector3.zero;
                shipManeuverVelocityNet = Vector3.zero;
                DeleteAxisEndPoints();
                DestroyLines();
                break;

            default:
                Debug.LogError("Unsupported state: " + newState);
                break;
        }
    }

    public void ShowManueverAxes(bool show) {
        if (show) {
            SetState(UIState.AXIS_DISPLAYED);
        } else {
            SetState(UIState.IDLE);
        }
    }

    void Update() {

        if (Input.GetKeyUp(KeyCode.Space)) {
            // apply impulse, start evolution and return to IDLE
            // (not ideal that we're doing state-ish things here...)
            GravityEngine.Instance().ApplyImpulse(spaceShipNbody, uiToPhysVelocityScale * shipManeuverAdjust);
            GravityEngine.Instance().SetEvolve(true);
            SetState(UIState.IDLE);
        } else if (Input.GetKeyUp(KeyCode.M)) {
            ShowManueverAxes(true);
        }

        switch (uiState) {
            case UIState.AXIS_DISPLAYED:
                // if there is no axis selected and mouse down, then check for Raycast
                if (Input.GetMouseButtonDown(0)) {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit)) {
                        for (int i = 0; i < axisEndObjects.Length; i++) {
                            if (hit.transform == axisEndObjects[i].transform) {
                                axisEndSelected = i;
                                axisSelected = i / 2;
                                clickStartPosition = Input.mousePosition;
                                SetState(UIState.AXIS_SELECTED);
                                break;
                            }
                        }
                    }
                }
                break;

            case UIState.AXIS_SELECTED:
                // while the mouse button is held down, adjust the ship velocity. The trajectory or OrbitPrediction will 
                // be updated as this is done. 
                if (Input.GetMouseButton(0)) {
                    UpdateVelocityUI(Input.mousePosition);  // updates shipManeuverVelocity
                    // apply impulse will update trajectories
                    bool updateImpulse = true;
                    // continually applying impulse does not give GE enough time to draw it. When in trajectory mode need
                    // to pace out the apply impulse commands
                    if (GravityEngine.Instance().trajectoryPrediction) {
                        if (Time.time - lastImpulseUpdate < 0.2f) {
                            updateImpulse = false;
                        }
                    }
                    if (updateImpulse) {
                        Vector3 impulse = uiToPhysVelocityScale * (shipManeuverAdjust + shipManeuverVelocityNet);
                        GravityEngine.instance.SetVelocityDouble(spaceShipNbody, ref shipVelocity);
                        GravityEngine.instance.ApplyImpulse(spaceShipNbody, impulse);
                        // need to update NBody when paused so OP will update
                        spaceShipNbody.UpdateVelocity();
                        lastTrajectoryUpToDate = false;
                    }
                } else {
                    // button was released
                    SetState(UIState.AXIS_DISPLAYED);
                }
                break;

            default:
                break;
        }

        // if trajectory prediction is enabled, need to notify listeners once up to date
        if (GravityEngine.Instance().trajectoryPrediction && (trajectoryListenerIF != null)) {
            if (!lastTrajectoryUpToDate && GravityEngine.Instance().TrajectoryUpToDate()) {
                trajectoryListenerIF.TrajectoryUpdated(spaceShipNbody);
            }
            lastTrajectoryUpToDate = GravityEngine.Instance().TrajectoryUpToDate();
        }

    }

    /// <summary>
    /// Use line renderers to indicate the 3D velocity 
    /// </summary>
    private void UpdateScreenLines() {
        if (showVelocityVector && (velocityLine != null)) {
            Vector3[] positions = new Vector3[2];
            positions[0] = spaceShipNbody.transform.position;
            positions[1] = positions[0] + velocityScalePhysToScreen * (shipManeuverAdjust + shipManeuverVelocityNet);
            velocityLine.SetPositions(positions);

            if (uiState == UIState.AXIS_SELECTED) {
                axisLine.gameObject.SetActive(true);
                // show the projection in the selected axis
                positions[1] = positions[0] + velocityScalePhysToScreen *
                     Vector3.Project((shipManeuverAdjust+shipManeuverVelocityNet), axes[axisSelected]);
                axisLine.SetPositions(positions);
            } else {
                axisLine.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Map the mouse position into a velocity vector for the selected axis. The mouse is 2D and we have a 3D velocity
    /// axis. 
    /// - take origin and endpoint of axis in 3D space and map to screen space (using MainCamera)
    /// - find vector from mouseStart to current position and project this onto screen space axes
    /// 
    /// The shipManeuverVelocity is a GE physics space value and the adjustment rate and display scaling will
    /// be game-dependent. Tuning values come from the inspector. 
    /// 
    /// Velocity adjust can optionally be time-dependent (the longer the mouse is held to one side the more the velocity grows). 
    /// To avoid run-away behavior and allow fine/coarse tuning the mouse offset is scaled non-linearly using Log. 
    /// </summary>
    /// <param name="mousePos"></param>
    /// 

    private void UpdateVelocityUI(Vector3 mousePos) {
        // map axis to screen space
        Vector3 origin = Camera.main.WorldToScreenPoint(spaceShipNbody.transform.position);
        Vector3 axisEnd = Camera.main.WorldToScreenPoint(axisEndObjects[axisEndSelected].transform.position);
        Vector3 screenAxis = Vector3.Normalize(axisEnd - origin);

        Vector3 mouseVec = mousePos - clickStartPosition;
 
        Vector3 deltaV = Vector3.Project(mouseVec, screenAxis);
        float dVsign = Mathf.Sign(Vector3.Dot(mouseVec, screenAxis));
        float dVmag = Vector3.Magnitude(deltaV);

        float axisSign = 1f; 
        if (axisEndSelected % 2 == 1) {
            axisSign = -1f;
        }

        // Future: UI could show axis end object slide as axis is adjusted
 
        if (dVmag > DV_THRESHOLD) {
            // non-linear scaling of dVmag
            float velScale = Mathf.Log(dVmag / DV_THRESHOLD);
            if (mouseVelocityIncrease) {
                shipManeuverAdjust += dVsign * velScale * axisSign * axes[axisSelected] * MOUSE_DYNAMIC_TWITCH;
            } else {
                shipManeuverAdjust = dVsign * velScale * axisSign * axes[axisSelected];
            }
            UpdateScreenLines();
        }
        // Update any Text UI elements in the scene
    }

    //*** Maneuver list functionality - used when orbit maneuvers are scheduled by a game controller for this ship

    protected void ManeuverExecutedCallback(Maneuver m) {
        maneuverList.Remove(m);
    }

    /// <summary>
    /// Add the manuever specified by the intercept to add a course correction
    /// at the intercept time to have the spaceship match the target course. 
    /// 
    /// Maneuvers are passed on to the GravityEngine to be executed on the correct
    /// integrator timeslice. The SpaceshipRV maintains a copy so they can be displayed/tracked. 
    /// </summary>
    /// <param name="intercept">Intercept.</param>
    public void SetManeuver(TrajectoryData.Intercept intercept) {
        Maneuver m = new Maneuver(spaceShipNbody, intercept);
        m.onExecuted = ManeuverExecutedCallback;
        GravityEngine.Instance().AddManeuver(m);
        maneuverList.Add(m);
    }

    /// <summary>
    /// Sets a series of maneuvers required to execute an orbital transfer. 
    /// 
    /// Maneuvers are passed on to the GravityEngine to be executed on the correct
    /// integrator timeslice. 
    /// </summary>
    ///
    /// <param name="transfer">The transfer</param>
    public void SetTransfer(OrbitTransfer transfer) {
        foreach (Maneuver m in transfer.GetManeuvers()) {
            m.onExecuted = ManeuverExecutedCallback;
            GravityEngine.Instance().AddManeuver(m);
            maneuverList.Add(m);
        }
    }

    public string[] ManeuverString() {
        string[] str = new string[maneuverList.Count];
        int i = 0;
        foreach (Maneuver m in maneuverList) {
            str[i++] = string.Format("T={0:F1} dV={1:F2}", m.worldTime, m.dV);
        }
        return str;
    }

    public void LogManuevers() {
        foreach (string s in ManeuverString()) {
            Debug.Log(s);
        }
    }


    public Trajectory GetTrajectory() {
        return trajectory;
    }

    /// <summary>
    /// Apply the currently displayed velocity change. 
    /// </summary>
    public void Execute() {
        Vector3 impulse = uiToPhysVelocityScale * (shipManeuverAdjust+shipManeuverVelocityNet);
        GravityEngine.instance.SetVelocityDouble(spaceShipNbody, ref shipVelocity);
        GravityEngine.instance.ApplyImpulse(spaceShipNbody, impulse);
        SetState(UIState.IDLE);
    }

    public void Cancel() {
        if (uiState != UIState.IDLE) {
            GravityEngine.instance.SetVelocityDouble(spaceShipNbody, ref shipVelocity);
            spaceShipNbody.UpdateVelocity();
        }
        SetState(UIState.IDLE);
    }
}
