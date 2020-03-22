using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Provide an interactive three-axis velocity change under mouse control for a spaceship. 
/// 
/// This controller can handle user input directly (based on the flags keyboardControl and mouseControl) to 
/// operate in a stand alone mode, or can have it's user input pre screened and called explicitly. 
/// See @ManualSceneController for an example of operation in this mode. 
/// 
/// The following states are implemented:
///    IDLE: 
///      - Not operating
///    AXIS_DISPLAYED: 
///      - Show the axisEndPrefabs at each end of three co-ordinate axes
///      - when HandleMouseInput is called raycast to see if there is a click on an axis end to select an axis
///    AXIS_SELECTED:
///      - project the mouse drag onto the selected axis and adjust the ship velocity based on a velocity change on 
///        this axis
///        
/// As the user changes each axis the cumulative velocity change is stored in shipManeuverVelocityNet. 
/// 
/// During a mouse drag the component of the change is stored in velocityChange. On the release of the mouse button this
/// is added to shipManeuverVelocityNet and velocityChange is set to zero.
/// 
/// In order for the net velocity change to be visible in an OrbitPredictor the class auto-detects an OrbitPredictor in the
/// children and sets the velocity explicitly if one is present.
/// 
/// A change to the ship velocity in GE is performed when the X key is pressed. Alternately an external controller may
/// use the @CreateManuver() method for a maneuver at some future point.
///    
/// </summary>
public class ManualShipControl : MonoBehaviour {

    public enum Frame
    {
        WORLD,      // use Unity's x,y,z axes
        BODY,       // use the axes of the spaceship models orientation
        ORBIT       // use axes wrt to the orbit around a body
    };

    //! The frame along which the velocity change handles are to be shown 
    [SerializeField]
    [Tooltip("Frame to align the velocity control axes to")]
    private Frame velocityFrame = Frame.ORBIT;

    [SerializeField]
    private bool keyboardControl = true;

    [SerializeField]
    private bool mouseControl = true;

    //! the center of the orbit (only required for ORBIT frame)
    [SerializeField]
    private NBody orbitCenter = null;

    [Header("Axis Elements")]
    //! Prefab for the axis ends (must have a collider for raycast to work)
    [SerializeField]
    private GameObject axisEndPrefab = null;

    private Vector3 DEFAULT_DIRECTION = Vector3.forward;

    //! Offset in world space for axis prefabs
    [SerializeField]
    private float axisOffset = 3f;

    [SerializeField]
    private float axisEndScale = 1f;

    //! Material applied to axis end prefabs for each axis
    [SerializeField]
    private Material axis1Material = null;
    [SerializeField]
    private Material axis2Material = null;
    [SerializeField]
    private Material axis3Material = null;

    // re-orginaztion of axis materials into easy indexed version
    private Material[] axisEndMaterials;

    [Header("Velocity Elements")]
    //! scale factor used in sensitivity of mouse scale to make velocity changes
    [SerializeField]
    public float velocitySensitivity = 5.0f;

    // (Put all this in an editor foldout)
    //! show velocity and dynamic adjustments in scene
    [SerializeField]
    public bool showVelocity = true;

    //! dV velocity lengths in Unity co-ordinates will be the physics velocity times this factor
    [SerializeField]
    public float velocityZoom = 1f;

    [SerializeField]
    public Material velocityMaterial = null;

    [SerializeField]
    public float velocityWidth = 1.0f;

    private GameObject[] axisEndObjects;
    private Vector3[] axisEndPoints;

    private GameObject vectorEnd; 

    private OrbitPredictor orbitPredictor;
    //! preserve the state of velocity from script (will be over-riden here and then returned to original state)
    private bool orbitVelFromScript; 

    // one of 6 axis ends
    private int axisEndSelected = 0;
    // location of initial left-click for changing a velocity
    private Vector3 clickStartPosition; 
    //! parent game object must have an NBody component
    private NBody shipNbody;

    //! The velocity change being created by this widget
    private Vector3 velocityChange;

    private Vector3 shipManeuverVelocityNet;

    private const int NUM_AXES = 3;
    private Vector3[] axes = new Vector3[3];

    // objects to hold lines
    private LineRenderer velocityLine;

    private bool last_running;

    private const float MAX_MANEUVER_VELOCITY = 100f;

    private Vector3d shipVelocity;

    //! Max velocity change expected from user input (for objects in orbit 1.5*v_circ at current position)
    private float v_scale; 

    //! Use main camera for now, but keep seperate to allow changes later
    private Camera sceneCamera;

    private GravityEngine ge; 
 
    // UI state tracking
    private enum UIState
    {
        NONE,   // at startup
        IDLE, 
        AXIS_DISPLAYED,
        AXIS_SELECTED
    };

    private UIState uiState = UIState.NONE;


    // Use this for initialization
    void Awake() {
        ge = GravityEngine.Instance(); 

        shipNbody = GetComponent<NBody>();
        if (shipNbody == null) {
            Debug.Log("Configuration error. Parent of SpaceshipRV must have NBody");
        }

        if (axisEndPrefab == null) {
            Debug.LogError("axisEndPrefan not set");
        } else if (axisEndPrefab.GetComponent<Collider>() == null) {
            Debug.LogError("Collider missing on axisEndPrefan - needed for raycast to work.");
        }

        sceneCamera = Camera.main;
 
        axisEndObjects = new GameObject[2*NUM_AXES];
        axisEndPoints = new Vector3[2 * NUM_AXES];
        uiState = UIState.NONE;
        SetState(UIState.IDLE);

        axisEndMaterials = new Material[] {axis1Material, axis1Material,
                                    axis2Material, axis2Material,
                                    axis3Material, axis3Material};

        orbitPredictor = GetComponentInChildren<OrbitPredictor>();
        if (orbitPredictor != null) {
            if (!orbitPredictor.velocityFromScript) {
                Debug.LogWarning("Require OrbitPredictor to enable velocityFromScript -> fixing");
                orbitVelFromScript = orbitPredictor.velocityFromScript;
            }
        }
    }

    public NBody GetNBody() {
        return shipNbody;
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
                axes[0] = Vector3.Normalize(shipNbody.transform.position - orbitCenter.transform.position);
                // axes[2] is normal to the orbit plane
                axes[2] = Vector3.Normalize(Vector3.Cross(axes[0], Vector3.Normalize(shipNbody.vel)));
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
    /// This implementation assumes scene is paused.
    /// 
    /// Need to leave as world level objects - since otherwise they can pick up rotation from NBody or model.
    /// 
    /// </summary>
    private void ShowAxisEndPoints() {

        // create if necessary
        if (axisEndObjects[0] == null) {
            for (int i = 0; i < 2*NUM_AXES; i++) {
                axisEndObjects[i] = Instantiate<GameObject>(axisEndPrefab);
                axisEndObjects[i].transform.SetParent(this.transform);
                axisEndObjects[i].name = "AxisEnd" + i;
                Renderer renderer = axisEndObjects[i].GetComponent<Renderer>();
                renderer.material = axisEndMaterials[i];
                axisEndObjects[i].transform.localScale = axisEndObjects[i].transform.localScale * axisEndScale;
            }
        }

        SetAxisTransforms();

        for (int i = 0; i < 2 * NUM_AXES; i++) {
            axisEndObjects[i].SetActive(true);
        }

    }

    private void SetAxisTransforms() {
        Vector3 shipPos = shipNbody.transform.position;
        SetAxes();
        for (int i = 0; i < NUM_AXES; i++) {
            // pos direction
            axisEndObjects[2 * i].transform.position = shipPos + axisOffset * axes[i];
            axisEndObjects[2 * i].transform.rotation = Quaternion.FromToRotation(DEFAULT_DIRECTION, axes[i]);
            axisEndPoints[2 * i] = shipPos + axes[i];
            // neg direction
            axisEndObjects[2 * i + 1].transform.position = shipPos - axisOffset * axes[i];
            axisEndObjects[2 * i + 1].transform.rotation = Quaternion.FromToRotation(DEFAULT_DIRECTION, -axes[i]);
            axisEndPoints[2 * i + 1] = shipPos - axes[i];
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
        if (showVelocity && (velocityLine == null)) {
            velocityLine = new GameObject("velocityLine").AddComponent<LineRenderer>();
            velocityLine.material = velocityMaterial;
            velocityLine.positionCount = 2;
            velocityLine.startWidth = velocityWidth;
            velocityLine.endWidth = velocityWidth;
            velocityLine.useWorldSpace = true;
            velocityLine.transform.SetParent(this.transform);
            // Create and hide the vector end cone
            vectorEnd = Instantiate<GameObject>(axisEndPrefab);
            vectorEnd.transform.localScale *= axisEndScale; 
            Renderer vrenderer = vectorEnd.GetComponent<Renderer>();
            vrenderer.material = velocityMaterial;
        }
    }

    private void DestroyLines() {
        if (showVelocity) {
            if (velocityLine != null) {
                Destroy(velocityLine.gameObject);
            }
            Destroy(vectorEnd);
        }
    }

    /// <summary>
    /// Set a scale based on the difference between a circular orbit at the current postion and V_escape if there
    /// is a center object. (v_escape = Sqrt(2) * v_circ. 
    /// 
    /// Want to allow some excess velocity beyond escape (v_infinity^2 = V^2 - v_escape^2), so do up to 1.5* V_circ. 
    /// </summary>
    private void ComputeVelocityScale() {
        if (orbitCenter != null) {
            Vector3d centerPos = ge.GetPositionDoubleV3(orbitCenter);
            Vector3d shipPos = ge.GetPositionDoubleV3(shipNbody);
            double r = (shipPos - centerPos).magnitude;
            double M = ge.GetMass(orbitCenter);
            double v_circular = Mathd.Sqrt(M / r);
            v_scale =  1.5f * (float) v_circular;
        } else {
            v_scale = ge.GetVelocity(shipNbody).magnitude;
            // don't expect this to come up much, but need a non-zero scale
            if (v_scale < 1) {
                v_scale = 1;
            }
        }
    }

    /// <summary>
    /// Create a maneuver from nbody. Maneuver creation requires an OrbitUniversal and both the fromNbody
    /// and the current ship must be on the provided orbit. (This is used to determine time of flight to the
    /// maneuver point from the current position)
    /// 
    /// </summary>
    /// <param name="nbody"></param>
    /// <returns></returns>
    public Maneuver CreateManeuver(NBody fromNbody, OrbitUniversal orbitU) {
        Vector3d shipPos = ge.GetPositionDoubleV3(shipNbody);
        double tof = orbitU.TimeOfFlight( ge.GetPositionDoubleV3(fromNbody), shipPos);

        // create a maneuver that add dV to existing velocity
        Maneuver maneuver = new Maneuver();
        maneuver.mtype = Maneuver.Mtype.vector;
        maneuver.worldTime = (float)(ge.GetGETime() + tof);
        maneuver.velChange = shipManeuverVelocityNet + velocityChange;
        maneuver.nbody = fromNbody;
        return maneuver;
    }

    private void SetState(UIState newState) {

        Debug.LogFormat("{0}:State change {1} => {2}", gameObject.name, uiState, newState);
        // Exit actions of current state
        switch (uiState) {
            case UIState.AXIS_DISPLAYED:
                shipVelocity = ge.GetVelocityDoubleV3(shipNbody);
                shipManeuverVelocityNet += velocityChange;
                velocityChange = Vector3.zero;
                break;

            case UIState.AXIS_SELECTED:
                break;

            case UIState.IDLE:
                // if GE not stopped, stop it
                if (ge.GetEvolve()) {
                    ge.SetEvolve(false);
                }
                shipVelocity = ge.GetVelocityDoubleV3(shipNbody);
                break;

            case UIState.NONE:
                break;

            default:
                Debug.LogError("Unsupported state: " + newState);
                break;

        }
        // Entry actions of the new state
        uiState = newState;
        switch (newState) {
            case UIState.AXIS_DISPLAYED:
                UpdateScreenLines();    // hide axis line
                ShowAxisEndPoints();
                ComputeVelocityScale();
                InitLines();
                if (orbitPredictor != null)
                    orbitPredictor.velocityFromScript = true;
                UpdateOrbit();
                break;

            case UIState.AXIS_SELECTED:
                HideAxisEndPoints(axisEndSelected); 
                break;

            case UIState.IDLE:
                velocityChange = Vector3.zero;
                shipManeuverVelocityNet = Vector3.zero;
                DeleteAxisEndPoints();
                DestroyLines();
                if (orbitPredictor != null)
                    orbitPredictor.velocityFromScript = orbitVelFromScript;
                ge.SetEvolve(true);
                break;

            default:
                Debug.LogError("Unsupported state: " + newState);
                break;
        }
    }

    public void SetActive(bool active) {
        if (active) {
            shipManeuverVelocityNet = Vector3.zero;
            velocityChange = Vector3.zero;
            SetState(UIState.AXIS_DISPLAYED);
        } else if (uiState == UIState.NONE) {
            Awake();
        } else if (uiState != UIState.IDLE) {
            SetState(UIState.IDLE);
        }
    }

    /// <summary>
    /// API for scene controller to indicate ship has moved. 
    /// </summary>
    public void ShipMoved() {
        switch(uiState) {
            case UIState.IDLE:
                return;

            case UIState.AXIS_SELECTED:
                SetState(UIState.AXIS_DISPLAYED);
                return;

            default:
                break;
        }
        // Move all the axisEndPoints
        SetAxisTransforms();
        shipVelocity = ge.GetVelocityDoubleV3(shipNbody);
        shipManeuverVelocityNet = Vector3.zero;
        UpdateScreenLines();
        UpdateOrbit();
    }

    public void ShowManueverAxes(bool show) {
        if (show) {
            SetState(UIState.AXIS_DISPLAYED);
        } else {
            SetState(UIState.IDLE);
        }
    }

    private void UpdateOrbit() {
        Vector3 impulse = (velocityChange + shipManeuverVelocityNet);
        if (orbitPredictor != null) {
            orbitPredictor.SetVelocity(shipVelocity.ToVector3() + impulse);
        }
    }

    /// <summary>
    /// Handle key events:
    /// M - show maneuver axes and pause the GE evolution
    /// X - execute the maneuver created by dragging and creating a new orbit
    /// </summary>
    public void HandleKeyInput() {
        if (Input.GetKeyUp(KeyCode.M)) {
            ShowManueverAxes(uiState == UIState.IDLE);
        } else if (Input.GetKeyUp(KeyCode.X)) {
            Execute();
        }
    }

    public bool HandleMouseInput() {
        bool handled = false;
        switch (uiState) {
            case UIState.AXIS_DISPLAYED:
                // if there is no axis selected and mouse down, then check for Raycast
                if (Input.GetMouseButtonDown(0)) {
                    Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit)) {
                        for (int i = 0; i < axisEndObjects.Length; i++) {
                            if (hit.transform == axisEndObjects[i].transform) {
                                axisEndSelected = i;
                                clickStartPosition = Input.mousePosition;
                                SetState(UIState.AXIS_SELECTED);
                                handled = true;
                                Debug.Log("Selected axis " + i);
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
                    UpdateVelocityUI(Input.mousePosition);  // updates velocityChange
                    UpdateOrbit();
                    handled = true;
                } else {
                    // button was released
                    SetState(UIState.AXIS_DISPLAYED);
                }
                break;

            default:
                break;
        }
        return handled;
    }

    void Update() {

        if (keyboardControl)
            HandleKeyInput();

        if (mouseControl)
            HandleMouseInput();

    }

    /// <summary>
    /// Map the mouse position into a velocity vector for the selected axis. The mouse is 2D and we have a 3D velocity
    /// axis. 
    /// - take origin and endpoint of axis in 3D space and map to screen space (using the active camera)
    /// - find vector from mouseStart to current position and project this onto screen space axes
    /// 
    /// Scaling:
    /// - maximum distance user can drag is assumed to be 1/3 of screen height
    /// - want varying sensitivity to allow small adjustments and large => use 
    /// 
    /// Velocity adjust can optionally be time-dependent (the longer the mouse is held to one side the more the velocity grows). 
    /// To avoid run-away behavior and allow fine/coarse tuning the mouse offset is scaled non-linearly using Log. 
    /// </summary>
    /// <param name="mousePos"></param>
    /// 
    private void UpdateVelocityUI(Vector3 mousePos) {
        // map axis to screen space
        Vector3 origin = sceneCamera.WorldToScreenPoint(shipNbody.transform.position);
        Vector3 axisEnd = sceneCamera.WorldToScreenPoint(axisEndPoints[axisEndSelected]);
        Vector3 screenAxis = Vector3.Normalize(axisEnd - origin);

        Vector3 mouseVec = mousePos - clickStartPosition;
        // Debug.LogFormat("mouseVec={0} screenAxis={1}", mouseVec, screenAxis);
        float mouseScale = velocitySensitivity * Screen.height;
 
        Vector3 deltaV = Vector3.Project(mouseVec, screenAxis);
        float dVsign = Mathf.Sign(Vector3.Dot(mouseVec, screenAxis));
        // mouse scaled will typically vary from 0..1.5. 
        float dVMouseScaled = Mathf.Exp(deltaV.magnitude/mouseScale)-1.0f;

        Vector3 axis3d = axisEndPoints[axisEndSelected]  - ge.MapToScene( ge.GetPhysicsPosition(shipNbody));
        velocityChange = dVsign * v_scale * dVMouseScaled * axis3d.normalized;
        //Debug.LogFormat("dVsign={0} v_scale={1} dvm={2} vChange={3} smvn={4}", 
        //        dVsign, v_scale, dVMouseScaled, velocityChange, shipManeuverVelocityNet);
        UpdateScreenLines();
    }

    /// <summary>
    /// Use line renderers to indicate the 3D velocity 
    /// </summary>
    private void UpdateScreenLines() {
        if (showVelocity && (velocityLine != null)) {
            Vector3[] positions = new Vector3[2];
            positions[0] = shipNbody.transform.position;
            positions[1] = positions[0] + velocityZoom * (velocityChange + shipManeuverVelocityNet);
            velocityLine.SetPositions(positions);
            // Set cone on the end
            vectorEnd.transform.position = positions[1];
            vectorEnd.transform.rotation = Quaternion.FromToRotation(DEFAULT_DIRECTION, positions[1] - positions[0]);
        }
    }

    /// <summary>
    /// Apply the currently displayed velocity change. 
    /// </summary>
    public void Execute() {
        Vector3 impulse =  velocityChange+shipManeuverVelocityNet;
        ge.ApplyImpulse(shipNbody, impulse);
        SetState(UIState.IDLE);
    }

}
