using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// OrbitPoint places an object at a specified location on the orbit. The location
/// can be specified in various ways according to the PointType enum. 
/// 
/// The game object to which this component is attached will be placed at the 
/// specified location. 
/// 
/// If the script is attached to an NBody element it will set the position and velocity of the NBody
/// element based on the values at the specified point of the orbit. This requires that the NBody 
/// attached has been added to GE. 
/// 
/// </summary>
public class OrbitPoint : MonoBehaviour
{
    [SerializeField]
    private bool mouseControl = true; 

    [SerializeField]
    private OrbitPredictor orbitPredictor = null;

    public enum PointType  {APOAPSIS,
                        PERIAPSIS,
                        ALTITUDE_1ST,
                        ALTITUDE_2ND,
                        ASCENDING_NODE,
                        DESCENDING_NODE,
                        FIXED_TIME,
                        PHASE,
                        PHASE_FROM_MOUSE };

    [SerializeField]
    private PointType pointType = PointType.APOAPSIS;

    [SerializeField]
    //! data field to be used with fixed time, altitude or phase point types
    private double pointData  = 0.0;

    [SerializeField]
    private NBody timeRefBody = null; 

    [SerializeField]
    //! Camera reference is only required if using PHASE_FROM_MOUSE
    private Camera sceneCamera = null;

    //! NBody the OrbitPoint script is attached to
    private NBody nbody = null;

    private OrbitUniversal orbitU;
    private GravityEngine ge;

    private float lastPhase = float.NaN; // ensure update first time through
    private float mousePhase = 0;

    private Vector3 relativeMousePos;

    // Start is called before the first frame update
    void Awake()
    {
        nbody = GetComponent<NBody>();
        ge = GravityEngine.Instance();
    }

    public void SetPointType(PointType ptype) {
        pointType = ptype;
    }

    public Vector3d GetPhysicsPosition3d() {
        Vector3d pos;
        if (nbody != null) {
            pos = ge.GetPositionDoubleV3(nbody);
        } else {
            pos = new Vector3d(ge.UnmapFromScene(transform.position));
        }
        return pos;
    }

    public Vector3d GetPhysicsVelocity3d() {
        return new Vector3d();
    }

    public OrbitUniversal GetOrbit() {
        if (orbitU == null)
            orbitU = orbitPredictor.GetOrbitUniversal();

        return orbitU;
    }

    public float GetPhase() {
        return lastPhase;
    }

    public NBody GetNBody() {
        return nbody;
    }

    /// <summary>
    /// Get the time required to move to the current orbit point position in GE internal time. 
    /// </summary>
    /// <param name="fromNbody"></param>
    /// <returns></returns>
    public double TimeToOrbitPoint(NBody fromNbody) {
        Vector3d shipPos = ge.GetPositionDoubleV3(nbody);
        GetOrbit();
        return orbitU.TimeOfFlight(ge.GetPositionDoubleV3(fromNbody), shipPos);
    }

    /// <summary>
    /// Set the orbit point position based on the position of the provided NBody evolved into the
    /// future by the value of time. 
    /// 
    /// Only valid when mode is FIXED_TIME.  
    /// </summary>
    /// <param name="time"></param>
    public void SetTime(NBody refBody, double time) {
        if (pointType != PointType.FIXED_TIME) {
            Debug.LogError("Require FIXED_TIME type have " + pointType + " on " + gameObject.name);
            return;
        }
        timeRefBody = refBody;
        pointData = time; 
    }

    public float MouseDistanceToShip() { 
        Vector3 mousePos = Input.mousePosition;
        Vector3 shipPos = sceneCamera.WorldToScreenPoint(transform.position);
        Vector3 relativePos = mousePos - shipPos;
        return relativePos.magnitude;
    }

    /// <summary>
    /// Handle mouse input and return true if this resulted in a change of position
    /// </summary>
    /// <returns></returns>
    public bool HandleMouseInput() {
        if (pointType == PointType.PHASE_FROM_MOUSE) {
            // Use the angle from the mouse click to the center of the orbit predictor to determine
            // a phase to place the element. This phase is with respect to the periapsis point, so 
            // need to project that on screen as well.
            if (Input.GetMouseButton(0)) {
                Vector3 mousePos = Input.mousePosition;
                Vector3 originPos = orbitU.centerNbody.transform.position;
                Vector3 origin = sceneCamera.WorldToScreenPoint(originPos);
                relativeMousePos = (mousePos - origin).normalized;
                // peri: Need to map peri position onto the screen via world position. 
                // (peri position includes center body offset)
                Vector3 periPos = ge.MapPhyPosToWorld( orbitU.PositionForPhase(0f));
                Vector3 periLine = (sceneCamera.WorldToScreenPoint(periPos) - origin).normalized;
                float periPhase = Mathf.Atan2(periLine.y, periLine.x) * Mathf.Rad2Deg;
                mousePhase = Mathf.Atan2(relativeMousePos.y, relativeMousePos.x) * Mathf.Rad2Deg - periPhase;
            } else {
                mousePhase = lastPhase;
                // special case after OnEnable(). Awkward.
                if (float.IsNaN(lastPhase))
                    mousePhase = 0; 
            }
        }
        return (mousePhase != lastPhase);
    }

    /// <summary>
    /// Compute the mouse radial distance from the center normalized to the position of the orbit point 
    /// with respect to the center. 
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetRelativeMouseRadius() {
        Vector3 posOnScreen = sceneCamera.WorldToScreenPoint(GetPhysicsPosition3d().ToVector3());
        Vector3 origin = sceneCamera.WorldToScreenPoint(orbitU.centerNbody.transform.position);
        return relativeMousePos.magnitude/(posOnScreen-origin).magnitude;
    }

    void OnEnable() {
        // ensure position/vel will be re-set when we get re-enabled
        lastPhase = float.NaN;
    }

    /// <summary>
    /// Set the flag that enables mouse checking in the Update loop. In some cases this may be delegated to a
    /// controller class e.g. TransferSceneContoller in TransferWithOrbitPoint scene. 
    /// </summary>
    /// <param name="control"></param>
    public void SetMouseControl(bool control) {
        mouseControl = control;
    }

    /// <summary>
    /// Set the initial phase for the PHASE_FROM_MOUSE mode. Typically set to a few degrees ahead
    /// of the ship position so that it is visually distinct from the ship. 
    /// </summary>
    /// <param name="phase"></param>
    public void SetMousePhase(float phase) {
        mousePhase = phase;
    }

    void Update() {
        DoUpdate();
    }

    // Update is called once per frame
    public void DoUpdate()
    {
        if (!ge.IsSetup())
            return;
        
        // Awkward. OrbitPredictor gets orbitU in start(). Start ordering would be annoying...
        if (orbitU == null)
            orbitU = orbitPredictor.GetOrbitUniversal();

        if (mouseControl) {
            HandleMouseInput();
        }

        float phase = 0;
        switch (pointType) {
            case PointType.APOAPSIS:
                // only defined for an ellipse
                if (orbitU.eccentricity < 1.0) {
                    phase = 180f;
                } else {
                    Debug.LogWarning("Cannot determine apoapsis unless orbit is ellipse ecc=" + orbitU.eccentricity);
                    return;
                }
                break;

            case PointType.PERIAPSIS:
                phase = 0f;
                break;

            case PointType.ALTITUDE_1ST:
                phase = orbitU.GetPhaseDegForRadius(pointData);
                break;

            case PointType.ALTITUDE_2ND:
                phase = -orbitU.GetPhaseDegForRadius(pointData);
                break;

            case PointType.FIXED_TIME:
                if (timeRefBody != null) {
                    // Don't assume the NBody has an orbit, build a new OrbitData from internal details 
                    // Greedy implementation - could move some to SetTime
                    // TODO: Check if things have changed before doing all this every frame
                    OrbitUniversal targetOrbit = timeRefBody.gameObject.AddComponent<OrbitUniversal>();
                    targetOrbit.InitFromActiveNBody(timeRefBody, orbitU.centerNbody, OrbitUniversal.EvolveMode.KEPLERS_EQN);
                    targetOrbit.LockAtTime(GravityEngine.Instance().GetPhysicalTimeDouble() + pointData);
                    Vector3d pos = new Vector3d();
                    Vector3d vel = new Vector3d();
                    double time0 = 0;
                    targetOrbit.GetRVTLastEvolve(ref pos, ref vel, ref time0);
                    if (nbody != null) {
                        ge.SetPositionDoubleV3(nbody, pos);
                        ge.SetVelocityDoubleV3(nbody, vel);
                    } else {
                        transform.position = ge.MapPhyPosToWorld(pos.ToVector3());
                    }
                    MonoBehaviour.Destroy(targetOrbit);
                }
                return;

            case PointType.ASCENDING_NODE:
                phase = (float)(0f - orbitU.omega_lc);
                break;

            case PointType.DESCENDING_NODE:
                phase = (float)(180f - orbitU.omega_lc);
                break;

            case PointType.PHASE:
                phase = (float) pointData;
                break;

            case PointType.PHASE_FROM_MOUSE:
                phase = mousePhase;
                break;

            default:
                break;
        }
        // Only update velocity on a phase change (may be a ManualController adjusting velocity at this location, so 
        // do not want to do this all the time)
        if (phase != lastPhase) {
            Vector3 phyPosition = orbitU.PositionForPhase(phase);
            if (nbody != null) {
                if (nbody.engineRef == null) {
                    Debug.LogWarning("Skip update, not added to engine yet " + nbody.gameObject.name);
                    return;
                }
                ge.SetPositionDoubleV3(nbody, new Vector3d(phyPosition));
                // there may be an OP on this spaceship, so set velocity as well. 
                ge.SetVelocity(nbody, orbitU.VelocityForPhase(phase));
            } else {
                // Need to adapt the phyPosition to a scene position based on GE scale etc. 
                transform.position = ge.MapPhyPosToWorld(phyPosition);
            }
        }
        lastPhase = phase;
    }
}
