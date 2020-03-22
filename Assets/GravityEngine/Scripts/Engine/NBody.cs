using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// N body.
///
/// Specifies the information required for NBody physics evolution of the associated game object. 
///
/// </summary>
public class NBody : MonoBehaviour, IComparer<NBody> {

	//! mass of object (mass scale in GravityEngine will be applied to get value used in simulation)
	public float mass;	

	//! Velocity - initial velocity as set in the inspector. In the unit system selected in GE.
	public Vector3 vel; 

	//! Current physics velocity (in internal scaled units used in GE)
    // (This was a terrible idea and needs to get re-factored away)
	public Vector3 vel_phys; 

	/// <summary>
	/// The initial position/velocity
	/// This indicates the position in the active units used by the Gravity engine (m or AU). 
	/// If the units are DIMENSIONLESS, then this field is not active and the transform position
	/// is not affected by scaling. 
    /// 
	/// When m or AU are active, a change in the scale factor of the gravity engine in the editor will
	/// change all the associated transform positions and velocities but the initialPos/initialVel will not be changed. 
    /// 
    /// When the transform is changed in the inspector this value will be updated. 
	/// 
	/// Positions are affected by changes in the lengthScale
	/// Velocities are affected by changes in both the length scale and the timeScale. (See ApplyScale() )
	/// </summary>
	public Vector3 initialPos;

    // initial physical position (after scaling) or the position set by the orbit methods during init
    // public but not accessible in the inspector. Should not be set. 
    public Vector3 initialPhysPosition; 

	//! Automatically detect particle capture size from a child with a mesh
	public bool automaticParticleCapture = true;

	//! Particle capture radius. Particles closer than this will be inactivated.
	public double size = 0.1; 

	//! Opaque data maintained by the GravityEngine. Do not modify.
	public GravityEngine.EngineRef engineRef;

    //! Rotate the frame as the body moves. Used when objects are in orbit 
    public bool rotateFrame;

    //! Track the depth of the object in an orbit heirarchy. Used to add objects in order of increasing orbit depth
    // Aside: Kepler depth of fixed bodies is a similar concept, but for a different purpose, it controls evolution
    // ordering, not ordering of adding at the start.
    private int orbitDepth; 

	public void Awake() {
		// automatic size detection if there is an attached mesh filter
		if (automaticParticleCapture) {
			size = CalculateSize();
		}
		lastVelocity = Vector3.zero;
	}

	private Vector3 lastVelocity;

	public float CalculateSize() {
		foreach( Transform t in transform) {
			MeshFilter mf = t.gameObject.GetComponent<MeshFilter>();
			if (mf != null) {
				// cannot be sure it's a sphere, but assume it is
				return t.localScale.x/2f;
			}
		}
		// compound objects may not have a top level mesh
		return 1; 
	}

	/// <summary>
	/// Updates the velocity.
	/// The Gravity Engine does not copy back velocity updates during evolution. Calling this method causes
	/// an update to the scaled velocity. 
	/// </summary>
	//int logCount; 
	public void UpdateVelocity() {
		vel_phys = GravityEngine.instance.GetVelocity(this);
	}

	/// <summary>
	/// Update called from GE to set new position/velocity based on gravity evolution. 
	/// The NBody referance frame moves so that the local axis points along the path. 
	/// </summary>
	///
	/// <param name="position">The position</param>
	/// <param name="velocity">The velocity</param>
	public void GEUpdate(Vector3 position, Vector3 velocity, GravityEngine ge) {
		transform.position = ge.MapToScene( position);
        vel_phys = velocity;
        if (rotateFrame)
        {
            Quaternion q = new Quaternion();
            q.SetFromToRotation(lastVelocity, velocity);
            transform.rotation = transform.rotation * q;
        }
		lastVelocity = velocity;
	}

    /// <summary>
    /// Is the NBody activly under the control of a fixedOrbit element?
    /// </summary>
    /// <returns></returns>
    public bool IsFixedOrbit() {
        return 
            (engineRef.bodyType == GravityEngine.BodyType.FIXED) &&
            (engineRef.fixedBody != null) &&
            (engineRef.fixedBody.fixedOrbit != null);
    }

    /// <summary>
    /// Set transform position during scene editing if the object is being manipulated by orbit components
    /// </summary>
    /// <param name="ge"></param>
    public void EditorUpdate(GravityEngine ge) {
        transform.position = initialPhysPosition * ge.physToWorldFactor;
    }


    /// <summary>
    /// Used to determine the initial physics position of an NBody (initialPhysPosition). 
    /// 
    /// Used in two contexts:
    /// (1) When a body is added to GE (either at setup or when a body is dynamically added at run-time).
    /// (2) In the editor DrawGizmo calls when the orbit path is being show in a scene that is not running.
    ///     In the case of orbit gizmos the orbit parameters will MOVE the object to the correct position in the
    ///     orbit based on the position calculated here. 
    ///
    /// If the NBody game object has an INBodyInit component (e.g from an OrbitEllipse) then 
    /// this is used to determine it's position. There is a potential for recursion here, since that
    /// orbit ellipse may be on a NBody that is in turn positioned by an orbit ellipse e.g. moon/planet/sun. 
    ///
    /// If the NBody has an engine ref (i.e. GE has taken charge) then update with the position from GE.    
    /// /// </summary>
    /// <param name="ge"></param>
    public void InitPosition(GravityEngine ge) {
        // Not ideal that NBody knows so much about methods to run setup. Be better to delegate this eventually.

        // check for initial condition setup (e.g. initial conditions for elliptical orbit/binary pair)
        if (engineRef != null) {
            initialPhysPosition = ge.GetPhysicsPosition(this);
        } else {
            // If there is a Kepler sequence, use that instead of e.g. the first OrbitU
            INbodyInit initNbody; 
            KeplerSequence keplerSeq = GetComponent<KeplerSequence>();
            if (keplerSeq != null) {
                initNbody = (INbodyInit)keplerSeq;
            } else {
                initNbody = gameObject.GetComponent<INbodyInit>();
            }
            if (initNbody != null) {
                initNbody.InitNBody(ge.physToWorldFactor, ge.massScale);
            } else {
                // binary pair
                if (transform.parent != null) {
                    BinaryPair bp = transform.parent.GetComponent<BinaryPair>();
                    if (bp != null) {
                        bp.SetupBodies();
                        return;
                    }
                }
                if (ge.units != GravityScaler.Units.DIMENSIONLESS) { 
                    // value in scaled systems is taken from the NBody scaled position and not the transform
                    initialPhysPosition = initialPos;
                } else {
                    // value is taken from the transform object
                    initialPhysPosition = transform.position;
                }
            }
        }

    }

    /// <summary>
    /// Rescale with specified lengthScale. 
    /// 
    /// This is called:
    /// - from editor scripts when a scale update has occured in the GE inspector. 
    /// - from GravityScalar when a body is added to GE (before SetupGameObjectAndChildren)
    /// 
    /// Not typically called by user scripts.
    /// 
    /// </summary>
    /// <param name="lengthScale">Length scale.</param>
    /// <param name="velocityScale">Velocity scale (GE Units to Phys Units).</param>
    public void ApplyScale(float lengthScale, float velocityScale) {
		initialPhysPosition = lengthScale * initialPos;
		vel_phys = vel * velocityScale;
		#pragma warning disable 162		// disable unreachable code warning
			if (GravityEngine.DEBUG) {
				Debug.Log(string.Format("Nbody scale: {0} r=[{1} {2} {3}] v=[{4} {5} {6}] initial=[{7} {8} {9}]",
					gameObject.name, transform.position.x, transform.position.y, transform.position.z, 
					vel_phys.x, vel_phys.y, vel_phys.z, 
					initialPos.x, initialPos.y, initialPos.z));
			}
		#pragma warning restore 162		// enable unreachable code warning
	}

    /// <summary>
    /// Determine if and how many objects are orbital parents. 
    /// e.g. Sun = 0, planet=1, moon=2
    /// </summary>
    public void CalcOrbitDepth() {
        GameObject go = gameObject;
        do {
            OrbitEllipse ellipse = go.GetComponent<OrbitEllipse>();
            if (ellipse != null) {
                go = ellipse.centerObject;
                orbitDepth++;
                continue;
            }
            OrbitUniversal orbitU = go.GetComponent<OrbitUniversal>();
            if (orbitU != null) {
                go = orbitU.centerNbody.gameObject;
                orbitDepth++;
                continue;
            }
            OrbitHyper hyper = go.GetComponent<OrbitHyper>();
            if (hyper != null) {
                go = hyper.centerObject;
                orbitDepth++;
                continue;
            }
            if (go.transform.parent != null) {
                BinaryPair bp = go.transform.parent.gameObject.GetComponent<BinaryPair>();
                if (bp != null) {
                    go = bp.gameObject;
                    orbitDepth++;
                    continue;
                }
                // If parent has an NBody, then it's an orbital parent, since need to inherit its velocity
                NBody nbody_parent = go.transform.parent.gameObject.GetComponent<NBody>();
                if (nbody_parent != null) {
                    go = nbody_parent.gameObject;
                    orbitDepth++;
                    continue;
                }
            }
            go = null;
        } while (go != null);
    }

    public int GetOrbitDepth() {
        return orbitDepth;
    }

    /// <summary>
    /// Sort by increasing order of the order depth parameter
    /// </summary>
    /// <param name="_x"></param>
    /// <param name="_y"></param>
    /// <returns></returns>
    public int Compare(NBody x, NBody y) {
        if (x.orbitDepth < y.orbitDepth)
            return -1;
        if (x.orbitDepth > y.orbitDepth)
            return 1;
        return 0;
    }
}
