using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class to manage spaceship encounters with RigidBody physics. The design assumes
/// the ships are in close proximity and have relative velocities that are "reasonable".
/// This allows docking, bouncing, collisions to make use of Unity colliders in the standard
/// way.
/// 
/// The concept is to take e.g. a pair of ships in similar orbits and:
/// - create an NBody object that represents the motion of the center of mass of the two ships
///   (using the RigidBody masses, since their NBody masses will be 0). 
/// - set the RigidBody position and velocity with respect to the CM NBody
/// - add the CM NBody and inactivate the spaceship NBodies
/// - make the spaceships children of the CM NBody, so they track it's position and move
///   relative to it using Unity RigidBody phyics
///   
/// The RigidBody component is expected to be on a child object of the NBody (to allow independent scaling
/// etc. of the object). The RigidBodies should have "Use Gravity" disabled and "Is Kinematic" enabled. (It will
/// be disabled when Activate() is called.)
/// 
/// Each ship involved in docking is required to have a DockingPort component. This is attached as a child 
/// component of the ship model. e.g.
///       ShipNBody 
///            + ShipModel
///                 + DockingPort
/// 
/// This is not a perfect solution, since there would be slight radius dependent velocities
/// in orbit, but for small encounter distances it is a good approximation. 
/// 
/// Future: Extend to allow docking of e.g. two ships with a station at the same time. 
/// 
/// </summary>
public class DockingGroup : MonoBehaviour {

    [SerializeField]
    [Tooltip("Docking mode radius (scene units)")]
    private float dockingModeRadius = 1;

    [SerializeField]
    [Tooltip("Docking ship 1")]
    private NBody ship1 = null;

    [SerializeField]
    [Tooltip("Docking ship 2")]
    private NBody ship2 = null;

    //! optional ship orbit predictors
    private OrbitPredictor ship1predictor;
    private OrbitPredictor ship2predictor;

    private float timeLastModeChange;

    private const float MIN_MODE_TIME = 0.2f;

    // Set of objects in docking group
    private NBody[] nbodies;

    // Preserve parents so on deactivate can restore in heirarchy
    private Transform[] priorParents;
    private Rigidbody[] rigidBodies;
    private DockingPort[] dockingPorts;

    private GameObject cmObject;
    private NBody cmNbody;

    //! Nbodies MAY contain a ReactionControlSystem
    private ReactionControlSystem[] rcs;

    /// <summary>
    /// State of controller:
    /// FREE: Ships are under GE control. Rigidbody dynamics are off. 
    /// DOCKING: Ships are using Rigidbody for motion with repect to a single NBody
    ///          representing the center ofmass of the pair. 
    /// DOCKED: The ships are attached and using the CM object to represent their evoltion. 
    ///         Maneuvers apply to the combined objects. 
    /// </summary>
    private enum State { DOCKING, DOCKED, FREE };
    private State state = State.FREE;

    private void Start() {

        // (optional)
        ship1predictor = ship1.GetComponentInChildren<OrbitPredictor>();
        ship2predictor = ship2.GetComponentInChildren<OrbitPredictor>();

        nbodies = new NBody[] { ship1, ship2 };

        // optional to have RCS
        rcs = new ReactionControlSystem[nbodies.Length];
        for(int i=0; i < nbodies.Length; i++) {
            rcs[i] = nbodies[i].GetComponentInChildren<ReactionControlSystem>();
            if (rcs[i] != null) {
                rcs[i].SetNBody(nbodies[i]);
            } 
        }

        // required to have docking ports
        dockingPorts = new DockingPort[nbodies.Length];
        dockingPorts[0] = ship1.GetComponentInChildren<DockingPort>();
        dockingPorts[1] = ship2.GetComponentInChildren<DockingPort>();

    }

    private void SetOrbitPredictors(bool value) {
        if (ship1predictor != null)
            ship1predictor.enabled = value;
        if (ship2predictor != null)
            ship2predictor.enabled = value;
    }

    public Vector3 SeparationDistance() {
        return dockingPorts[0].SeparationDistance(dockingPorts[1]);
    }

    // Update is called once per frame
    void Update() {
        float d = Vector3.Magnitude(ship1.transform.position - ship2.transform.position);
        bool inRange = (d < dockingModeRadius);
        State lastState = state;

        switch (state) {
            case State.FREE:
                if (inRange) {
                    Activate();
                    SetOrbitPredictors(false);
                    state = State.DOCKING;
                }
                break;

            case State.DOCKING:
                if (!inRange) {
                    Deactivate();
                    SetOrbitPredictors(true);
                    state = State.FREE;
                } else {
                    // Check to see ships have docked
                    if (dockingPorts[0].Capture( dockingPorts[1])) {
                        state = State.DOCKED;
                        // RCS: Now want to affect both rigid bodies....Hmmmm
                        // Null out local velocities
                        foreach(Rigidbody rb in rigidBodies) {
                            rb.velocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                        }
                        
                    }
                }
                break;

            case State.DOCKED:
                break;

            default:
                Debug.LogError("Coding error: unknown state " + state);
                break;
        }
        if (state != lastState) {
            Debug.Log("State changed to " + state);
        }
    }

    /// <summary>
    /// Take the Nbody objects in the nbodies list set them inactive and make them children of a new
    /// NBody object moving as the CM of the nbodies. This allows RigidBody mechanics during close
    /// encounters. 
    /// </summary>
	private void Activate() {

        if (nbodies.Length < 2) {
            Debug.LogError("Need two or more nbodies");
            return;
        }

        GravityEngine ge = GravityEngine.Instance();

        // Step 1: calculate CM position and velocity
        Vector3d cmPos = new Vector3d(0, 0, 0);
        Vector3d cmVel = new Vector3d(0, 0, 0);

        float mass = 0f;
        rigidBodies = new Rigidbody[nbodies.Length];

        // RigidBody is assumed to be attached to one of the children (to keep model scale independent)
        int i = 0;
        foreach (NBody nbody in nbodies) {
            rigidBodies[i] = nbody.GetComponentInChildren<Rigidbody>();
            //rigidBodies[i] = nbody.GetComponent<Rigidbody>();
            if (rigidBodies[i] == null) {
                Debug.LogError("Abort - No rigidbody detected on " + nbody.gameObject.name);
                return;
            }
            mass += rigidBodies[i].mass;
            cmPos += rigidBodies[i].mass * ge.GetPositionDoubleV3(nbody);
            cmVel += rigidBodies[i].mass * ge.GetVelocityDoubleV3(nbody);
            i++;
        }
        cmPos /=  mass;
        cmVel /=  mass;
        Debug.LogFormat("CM p={0} v={1} mass={2}", cmPos.ToVector3(), cmVel.ToVector3(), mass);

        // Step2: Inactivate the NBodies and make children of a new NBody object
        priorParents = new Transform[nbodies.Length];
        cmObject = new GameObject("DockingGroupCM");
        cmNbody = cmObject.AddComponent<NBody>();

        // Set cm pos/vel
        // NBody InitPosition will use transform or initialPos base on units. Set both. 
        cmNbody.initialPos = cmPos.ToVector3() * ge.physToWorldFactor;
        cmNbody.transform.position = cmNbody.initialPos;
        ge.AddBody(cmObject);
        ge.SetVelocity(cmNbody, cmVel.ToVector3());
        Debug.LogFormat("set pos={0} actual={1}", cmPos.ToVector3(), ge.GetPhysicsPosition(cmNbody));
        i = 0;
        foreach (NBody nbody in nbodies) {
            Vector3d pos = ge.GetPositionDoubleV3(nbody);
            Vector3d vel = ge.GetVelocityDoubleV3(nbody);
            priorParents[i] = nbody.gameObject.transform.parent;
            ge.InactivateBody(nbody.gameObject);
            nbody.gameObject.transform.parent = cmObject.transform;
            // position wrt to CM. Need to convert to Unity scene units from GE Internal
            pos = (pos - cmPos) * ge.physToWorldFactor;
            vel = GravityScaler.ScaleVelPhysToScene(vel - cmVel);
            nbody.transform.localPosition = pos.ToVector3();
            rigidBodies[i].velocity = vel.ToVector3();
            // rigidBodies[i].isKinematic = false;
            i++;
            Debug.LogFormat("body {0} p={1} v={2}", nbody.gameObject.name, pos.ToVector3(), vel.ToVector3());
        }
        // activate any RCS elements
        foreach(ReactionControlSystem r in rcs) {
            if (r != null) {
                r.SetRigidBodyEnabled(true);
            }
        }
    }

    private void Deactivate() {

        // Get CM object pos & vel from GE
        GravityEngine ge = GravityEngine.Instance();
        Vector3d cmPos = ge.GetPositionDoubleV3(cmNbody);
        Vector3d cmVel = ge.GetVelocityDoubleV3(cmNbody);

        int i = 0;
        foreach (NBody nbody in nbodies) {
            Rigidbody rb = nbody.GetComponentInChildren<Rigidbody>();
            if (rb == null) {
                Debug.LogWarning("could not find rigidbody for  " + nbody.gameObject.name);
                continue;
            }
            // rb.isKinematic = true;
            // set position and velocity
            Vector3d nbodyVel = new Vector3d( GravityScaler.ScaleVelSceneToPhys(rb.velocity));
            ge.SetVelocityDoubleV3(nbody, cmVel + nbodyVel);
            Vector3d nbodyPos = new Vector3d(GravityScaler.ScalePositionSceneToPhys(nbody.transform.localPosition));
            ge.SetPositionDoubleV3(nbody, cmPos + nbodyPos);
            ge.ActivateBody(nbody.gameObject);
            // restore parent
            nbody.transform.parent = priorParents[i];
            i++;
        }
        ge.RemoveBody(cmObject);
        Destroy(cmObject);
        // de-activate any RCS elements
        foreach (ReactionControlSystem r in rcs) {
            if (r != null) {
                r.SetRigidBodyEnabled(false);
            }
        }
    }

    /// <summary>
    /// Are the pair of ships docked?
    /// </summary>
    /// <returns></returns>
    public bool IsDocked() {
        return state == State.DOCKED;
    }

}
