using System.Collections.Generic;
using UnityEngine;

/*! \mainpage Gravity Engine Unity Asset
 *
 *  To use the gravity engine in a Unity scene there must be an object with a GravityEngine. GravityEngine will
 *  compute and move objects that have NBody components or particle systems that have GravityParticles components.
 *
 *  GravityEngine is commonly used in a mode that auto-detects all bodies in a scene. This default mode can be
 *  turned off and objects can be added as part of an explicit list or via API calls to AddBody(). 
 *
 * Tutorial and demo videos can be found on You Tube on the <a href="https://www.youtube.com/channel/UCxH9ldb8ULCO_B7_hZwIPvw">NBodyPhysics channel</a>
 * 
 * On-line documentation: <a href="http://nbodyphysics.com/blog/gravity-engine-doc">Gravity Engine Documentation</a>
 *
 * Support: nbodyphysics@gmail.com
 */


/// <summary>
/// GravityEngine (NBE)
/// Primary controller of Nbody physics evolution with selectable integration algorithms. Singleton. 
/// 
/// Bodies
/// The positions and masses of the N bodes are initialized here and passed by reference to the integrator. 
/// This allows high precision evolution of the bodies and a simpler integration scheme for particle systems
/// moving in the fields of the N bodies. 
///
/// Particles
/// NBE creates a ParticleEvolver and evolves particle systems once per fixed update based on new positions
/// of the N bodies. Particles are massless and so do not interact with each other (too computationally expensive).
///
/// Body initialization:
/// TODO
/// </summary>
public class GravityEngine : MonoBehaviour {

	public static string TAG = "GravityEngine";
	//! Singleton instance handle. Initialized during Awake().
	public static GravityEngine instance; 

	//! global flag for debug logging
	public const bool DEBUG = false;

    //! Enable single step mode (code only available from GEConsole)
    private bool singleStep = false;
    private bool stepHasRun = false; 

    // Map To Scene
    // Commonly, GE is used as a world controller and there is simple mapping from the physics space to the scene. 
    // In this case leave MapToScene false to avoid extra calculations on each update. 
    //
    // In applications where GE is supplying information in a scene element (e.g. floating above the navigation console on the
    // bridge of a starship) then it is useful to be able to re-locate, rotate and scale the body positions and to do this dynamically. 
    // In this case enable Map To Scene. (Note model scales will not be adjusted - that's up to game logic outside of GE)
    //
    //! Flag to apply GE transform to scale, rotate and re-position all objects under it's control
    public bool mapToScene = false; 
	                                       
	/// <summary>
	/// Integrator choices:
	/// LEAPFROG - a fixed timestep, with good energy conservation 
	/// HERMITE - an adaptive timestep algorithm with excellent energy conservation
	/// AZTRIPLE - For 3 bodies ONLY. Works in regularized co-ordinates that allow close encounters.
	/// ANY_FORCE_FL - Leapfrog with a force delegate
	/// <summary>
	public enum Algorithm { LEAPFROG, HERMITE8, AZTRIPLE};
	private static string[] algorithmName = new string[]{"Leapfrog", "Hermite (adaptive)", "TRIPLE (Regularized Burlisch-Stoer)"};

	/// <summary>
	/// The force used when one of the ANY_FORCE integrators is selected. 
	/// Any force use requires that the scale be set to DIMENSIONLESS. 
	/// </summary>
	public ForceChooser.Forces force; 

	//! Algorithm for numerical integration of massive bodies
	public Algorithm algorithm; 
	//! Use masslessEngine to reduce computations when many massless bodies.
	public bool optimizeMassless = true; 
	//! Automatically detect all objects with an NBody component and add to the engine.
	public bool detectNbodies = true; 

	//! Enable trajectory prediction - TrajectoryTrails attached to NBody objects will be updated
	public bool trajectoryPrediction = false; 

	//! time to evolve forward for trajectory prediction
	public float trajectoryTime = 15f;

 	//! Used by Trajectory when text labels for time are enabled
	public GameObject trajectoryCanvas; 

	//! Optional parent object to assign trajectory markers to (so they do not clutter up the root object space)
	public GameObject markerParent;

    //! Multiplier for trajectory recompute simulations per frame. Low number spread update over more frames and
    //! have less impact on run-time performance and the cost of longer times to see new trajectory
    public float trajectoryComputeFactor = 4f;

	/// <summary>
	/// physToWorldFactor: factor to allow distance measurements in NBE to be on a different scale than in the Unity world view. 
	/// This is useful when taking initial conditions from literature (e.g. the three body solutions) in which 
	/// the data provided are normalized to [-1..1]. Having all world objects in [-1..1] becomes awkward. Setting
	/// this scale allows the Unity positions to be expanded to a more convenient range.
	///
	/// If this is used for objects that are created in Unity world space it will change the distance scale used
	/// by the physics engine and consequently the time evolution will also change. Moving objects closer (physToWorldFactor > 1)
	/// will result in stronger gravity and faster interactions. 
	/// </summary>
	public float physToWorldFactor = 1.0f;

	public GravityScaler.Units units;

	/// <summary>
	/// The length scale.
	/// Scale from NBody initial pos to Unity position
	/// Expressed in Unity units per scale unit (pos = scale_pos * lengthScale).
	/// Changing this value will result in changes in the positions of all NBody objects in 
	/// the scene. It is intended for use by the Editor scripts during script setup and not
	/// for run-time changes.
	/// </summary>
	[SerializeField]
	private float _lengthScale = 1f; 
	//! Orbital scale in e.g. Unity unity per km
	public float lengthScale {
		get { return _lengthScale; }
		set { UpdateLengthScale(value);}
	}

    /// <summary>
    /// Mass scale applied to all NBody objects handled by the Gravity Engine. Increasing mass scale 
    /// makes everything move faster for the same CPU cost.
    /// 
    /// In dimensionless units the massScale can be set directly in the inspector. In other units, the
    /// mass scale is determined by the choice of length and time scale ans computed by UpdateTimeScale
    /// in the GravityScalar class.
    /// </summary>
    public float massScale = 1.0f;

	/// <summary>
	/// The time scale.
	/// Time scale is used for overall scale at setup and is used via dimension analysis to set an overall
	/// mass scale to produce the required evolution. 
	///
	/// To change evolution speed at run time use SetTimeZoom(). This affectes the amount of physics calculations
	/// performed per frame. 
	///
	/// </summary>
	[SerializeField]
	private float _timeScale = 1.0f; 
	//! Orbital scale in Unity unity per AU
	public float timeScale {
		get { return _timeScale; }
		set { UpdateTimeScale(value);}
	}

	private float timeZoom = 1f; 
	private bool timeZoomChangePending; 
	private float newTimeZoom; 

	//! Array of game objects to be controlled by the engine at start (used when detectNbodies=false). During evolution use AddBody().
	public GameObject[] bodies; 

	//! Begin gravitational evolution when the scene starts.
	public bool evolveAtStart = true;  
	private bool evolve;

	//! State of inspector Advanced foldout
	public bool editorShowAdvanced; 
	//! State of inspector Scale foldout
	public bool editorShowScale; 
	//! State of inspector Center of Mass foldout
	public bool editorCMfoldout; 
	//! Track state of foldout in editor
	public bool editorShowTrajectory; 

	//--Integrator stuff--	
	// private INBodyIntegrator integrator;

	//! Number of physics steps per frame for massive body evolution. For LEAPFROG will directly map to CPU
	//! use and accuracy. In the case of HERMITE, number of iterations per frame will vary depending on
	//! speeds of bodies. 
	public int stepsPerFrame = 8;
	//! Number of steps per frame for particle evolution. All particle evolution is via LEAPFROG, independent of the
	//! choice of algorithm for massive body evolution.
	public int particleStepsPerFrame = 2;

	// Sub-divide frame time into 8 steps for Leapfrog integration
    // @TODO: readonly
	public double engineDt = 1.0/(60.0 * 8);

	// run particles at a larger timestep to save CPU
	private double particle_dt;  
		
	// mass/position information for massive bodies
	// For performance reasons (http://jacksondunstan.com/articles/3058) need to manage arrays and
	// grow them dynamically when required. Adding to the fun, the integrator delegates have the same
	// issue and must stay aligned with the arrays here.
	//
	// Typically over-allocate the initial body count
	private int arraySize; 
	private const int GROW_SIZE = 500;

	//! current state of massive bodies
	private GravityState worldState;

    //! last world dT time
    private double lastWorldDt; 

    //! Bit flag used in integrator code
    public const byte INACTIVE = 1;     // mass should be skipped in integration
                                        //! Bit flag used in integrator code
    public const byte FIXED_MOTION = 1 << 1;    // integrator should not update position/velocity but
                                                // will use mass to affect other object
    private const byte NOT_FIXED_MOTION = 0xFD; 

    public const byte TRAJ_DATA = 1 << 2;       // Track trajectory data for the object                                        

    public const byte INACTIVE_OR_FIXED = GravityEngine.FIXED_MOTION | GravityEngine.INACTIVE;

    //! future state of the world (if trajectory prediction is enabled)
    private GravityState trajectoryState; 
	
	//! Objects to show future trajectories (if trajectory prediction is enabled)
	private Trajectory[] trajectories; 

	private List<GameObject> addedByScript; 

	// Gravitational Body tracking
	private NBody[] gameNBodies; 
	
	// less than this consider a body massless
	private const double MASSLESS_LIMIT = 1E-6; 

	private bool isSetup = false; // flag to trigger setup on first evolution

    //! List to hold bodies to go off rails at end of update
    private List<NBody> offRailsDefered; 

	// Force delegate
	private IForceDelegate forceDelegate;

	// ---------inner classes-----------
	public class FixedBody {
        public NBody nbody;
		public IFixedOrbit fixedOrbit;
        // if it's OrbitUniversal/KeplerSequence keep a reference handy
        public OrbitUniversal orbitU;
        public KeplerSequence keplerSeq;
        // must evolve Kepler objects in order parent, child, gradchild etc. so that each update builds
        // on the updated parental positions. Record depth and insert accordingly. 
        public int kepler_depth; 
		
		public FixedBody(NBody nbody, IFixedOrbit fixedOrbit) {
			this.nbody = nbody; 
			this.fixedOrbit = fixedOrbit;
            orbitU = nbody.GetComponent<OrbitUniversal>();
            keplerSeq = nbody.GetComponent<KeplerSequence>();
		}
	}

	//! NBody type - used in integrator code. 
	public enum BodyType { MASSIVE, MASSLESS, FIXED };

	// Held by a NBody object. Hold reference to internal reference details. 
	public class EngineRef {
		public BodyType bodyType;
        public FixedBody fixedBody;
		public int index; 

        public EngineRef() {

        }

        public EngineRef(BodyType bodyType, int index) {
            this.bodyType = bodyType;
            this.index = index;
        }
	}

    public delegate void GEStart(); 

    private List<GEStart> geStartList;

	// ---------main class-------------

	/// <summary>
	/// Static accessor that finds Instance. Useful for Editor scripts.
	/// </summary>
	public static GravityEngine Instance()
 	{
     	if (instance == null)
         	instance = (GravityEngine)FindObjectOfType(typeof(GravityEngine));
     	return instance;
    }

    void Awake() {
        if (instance == null) {
            instance = this;
        } else if (this != instance) {
            Debug.LogWarning("More than one GravityEngine in Scene");
        }
        DoAwake(); 
    }

    /// <summary>
    /// Unit test call in to wake up GE (since Awake is not directly callable due to protection level)
    /// </summary>
    public void UnitTestAwake() {
        DoAwake();
    }
		
	private void DoAwake () {

		ConfigureDT();
        geStartList = new List<GEStart>();

        if (massScale == 0) {
			Debug.LogError("Cannot evolve with massScale = 0"); 
			return;
		}
		if (physToWorldFactor == 0) {
			Debug.LogError("Cannot evolve with physToWorldFactor = 0"); 
			return;
		}
		if (timeScale == 0) {
			Debug.LogError("Cannot evolve with timeScale = 0"); 
			return;
		}
		// force computation of massScale
		UpdateTimeScale(_timeScale);

		addedByScript = new List<GameObject>();
        offRailsDefered = new List<NBody>();

        // defensive init for early access to physical time at start (will get replaced)
        worldState = new GravityState(arraySize);
        SetAlgorithm(algorithm);
    }

    void Start() {
		// setup if evolve is set
		if (evolveAtStart) {
			// evolve on start requires something to evolve!
			//if (bodies.Length == 0 && !detectNbodies) {
			//	Debug.LogError("No bodies attached to Engine. Do not start until bodies present");
			//	evolve = false;
			//	return;
			//}
			SetEvolve(true);
		}
        AddConsoleCommands();
	}

	/// <summary>
	/// Control evolution of masses and particles in the gravity engine. 
	/// </summary>
	/// <param name="evolve">If set to <c>true</c> evolve.</param>
	public void SetEvolve(bool evolve) {

        if (!this.evolve && evolve && isSetup) {
            // any objects added when we were paused need to be added now
            foreach(GameObject go in addedByScript) {
                SetupGameObjectAndChildren(go);
            }
            addedByScript.Clear();
        }
		this.evolve = evolve;
        
	}

	public bool GetEvolve() {
		return evolve;
	}
	
	/// <summary>
	/// Sets the integration algorithm used for massive bodies. 
	///
	/// The integration algorithm cannot be changed while the engine is running. 
	/// </summary>
	/// <param name="algorithm">Algorithm.</param>
	public void SetAlgorithm(Algorithm algorithm) {
		if (evolve) {
			Debug.LogError("Cannot change algorithm while evolving");
			return;
		}
        forceDelegate = ForceChooser.InstantiateForce(force, this.gameObject);
        worldState.SetAlgorithmAndForce(algorithm, forceDelegate);

	}

    public IForceDelegate GetForceDelegate() {
        return forceDelegate;
    }

	/// <summary>
	/// Gets the name of the algorithm as a string.
	/// </summary>
	/// <returns>The algorithm name.</returns>
	/// <param name="algorithm">Algorithm.</param>
	public static string GetAlgorithmName(Algorithm algorithm) {
		return algorithmName[(int) algorithm];
	}

	/// <summary>
	/// Gets the particle time step size.
	/// </summary>
	/// <returns>The particle dt.</returns>
	public double GetParticleDt() {
		return particle_dt;
	}

	/// <summary>
	/// Reset the bodies/particle systems known to the Gravity Engine.
	/// </summary>
	public void Clear() {

		#pragma warning disable 162		// disable unreachable code warning
		if (DEBUG) {
			Debug.Log("Clearing " + worldState.numBodies + " bodies");
		}
        #pragma warning restore 162
        for (int i=0; i < worldState.numBodies; i++) {
			RemoveBody(gameNBodies[i].gameObject);
            gameNBodies[i] = null;
		}
        // be paranoid - clear all engine refs in scene
        NBody[] nbodies = (NBody[])Object.FindObjectsOfType(typeof(NBody));
        foreach( NBody n in nbodies) {
            n.engineRef = null;
        }
        worldState.Clear();
		isSetup = false;
		#pragma warning disable 162		// disable unreachable code warning
		if (DEBUG) {
			Debug.Log("All bodies cleared.");
		}
		#pragma warning restore 162
	}

	public void Setup() {
        worldState.numBodies = 0;
		GravityScaler.UpdateTimeScale(units, _timeScale, _lengthScale);
		GravityScaler.ScaleScene(units,  _lengthScale);

        offRailsDefered = new List<NBody>();

        if (detectNbodies) {
			SetupAutoDetect();
		} else {
			SetupExplicit();
		}
		worldState.ResetPhysicalTime();
        worldState.PreEvolve(this);

        // update positions on screen
        UpdateGameObjects();

        ClearAllTrails();

        if (trajectoryPrediction) {
            ResetTrajectoryPrediction();
        }

        isSetup = true; // needs to be here so setup calls know we're ready
        // Run any registered startup code
        foreach( GEStart geStart in geStartList) {
            geStart();
        }
        geStartList.Clear();

#pragma warning disable 162        // disable unreachable code warning
        if (DEBUG) {
			int numMassless = 0; 
			if (worldState.masslessEngine != null)
				numMassless = worldState.masslessEngine.NumBodies();
			Debug.Log(string.Format("GravityEngine started with {0} massive, {1} massless, {2} particle systems. {3} fixed",
                            worldState.numBodies, numMassless, worldState.gravityParticles.Count, 
                            worldState.fixedBodies.Count));
			LogDump();
		}
		#pragma warning restore 162
	}


    /// <summary>
    /// Scripts may wish to do some setup in the Start() method but GE is not yet running at scene start. 
    /// This is a method to register code to run once GE setup has been completed. 
    /// 
    /// If GE is already running, just go ahead and do the callback now. 
    /// </summary>
    /// <param name="callback"></param>
    public void AddGEStartCallback(GEStart callback) {
        if (isSetup) {
            callback();
        } else {
            geStartList.Add(callback);
        }
    }

    private void ClearAllTrails() {
        NBody[] nbodies = (NBody[])Object.FindObjectsOfType(typeof(NBody));
        foreach (NBody nb in nbodies) {
            TrailRenderer[] trails = nb.gameObject.GetComponentsInChildren<TrailRenderer>();
            foreach (TrailRenderer trail in trails) {
                trail.Clear();
            }
        }
    }

    /// <summary>
    /// Setup only the bodies (and their children) that have been explicitly added to the NBody engine
    /// via the bodies list. There can be bodies already added programatically via AddBody() as well, these
    /// are on the addedByScript list.
    /// </summary>
    private void SetupExplicit() {
		int maxBodies = 0; 
		// two passes - first get a count
		if (bodies != null) {
			foreach (GameObject body in bodies) {
				maxBodies += body.GetComponentsInChildren<NBody>().Length;
			}
		}
		if (addedByScript.Count > 0) {
			foreach (GameObject body in addedByScript) {
				maxBodies += body.GetComponentsInChildren<NBody>().Length;
			}
		}
		InitArrays(maxBodies+GROW_SIZE);
		// Now do setup on each body
		if (bodies != null) {
			foreach (GameObject body in bodies) {
				SetupGameObjectAndChildren(body);
			}
		}
		if (addedByScript.Count > 0) {
			foreach (GameObject body in addedByScript) {
				SetupGameObjectAndChildren(body);
			}
			addedByScript.Clear();
		}
	}

	/// <summary>
	/// Find all active NBody objects in the scene and add them to the engine. 
	/// </summary>
	private void SetupAutoDetect() {

        if (addedByScript.Count > 0)
            Debug.LogWarning("Using auto-detect but some bodies added by script before starting. ");

        // Need to deterimine maxBodies from body lists
        // GetComponentsInChildren also returns components in parent object!
        int maxBodies = 0; 
        // objects added in the inspector
        if (bodies != null) {
               foreach (GameObject body in bodies) {
                       maxBodies += body.GetComponentsInChildren<NBody>().Length;
               }
        }
                       
		NBody[] nbodies = (NBody[]) Object.FindObjectsOfType(typeof(NBody));
		// allocate physics arrays (will over-allocate by number of massless bodies if optimizing massless)
		// add some buffer to allow for dynamic additions
		maxBodies += nbodies.Length;
        InitArrays(maxBodies+GROW_SIZE);

        // add in order of orbit depth to ensure e.g. moons can get positions and velocities from planets, 
        // planets from stars etc.
        foreach (NBody nbody in nbodies) {
            nbody.CalcOrbitDepth();
        }
        System.Array.Sort(nbodies, 0, nbodies.Length, nbodies[0]);
        foreach (NBody nbody in nbodies) {
			SetupOneGameObject(nbody.gameObject, nbody);
		}
	}

	private void InitArrays(int size) {
		// Typically over-allocate to allow for dynamic additions EXCEPT for the AZT integrator which can only
		// handle three bodies
		arraySize = size;
		worldState.InitArrays(arraySize);
		trajectories = new Trajectory[arraySize];
        gameNBodies = new NBody[arraySize];
		// integrator will allocate internal data and set dt
		worldState.integrator.Setup(arraySize, engineDt);
	}

	// Grow arrays to hold new massive bodies and trigger the same operation in the 
	// integrator to maintain array alignment. 
	//
	// Not ideal - but scientific computing is array based and direct arrays have the best performance. 
	//
	private bool GrowArrays(int growBy) {

        worldState.GrowArrays(growBy);  // Also grows integrator arrays

		Trajectory[] traj_copy = new Trajectory[arraySize];
		NBody[] gameNBodies_copy = new NBody[arraySize];

		for (int i=0; i < arraySize; i++) {
			traj_copy[i] = trajectories[i];
            gameNBodies_copy[i] = gameNBodies[i];
		}

		trajectories = new Trajectory[arraySize+growBy];
        gameNBodies = new NBody[arraySize+growBy];

		for (int i=0; i < arraySize; i++) {
			trajectories[i] = traj_copy[i];
            gameNBodies[i] = gameNBodies_copy[i];
		}
		arraySize += growBy;
		#pragma warning disable 162		// disable unreachable code warning
		if (DEBUG)
			Debug.Log("GrowArrays by " + growBy);
		#pragma warning restore 162		
		return true;
	}

	
	
	private int logCounter; 

	public bool IsSetup() {
		return isSetup;
	}

    //******************************************
    // Map position to scene position based on the GE transform
    // This allows the location, orientation and visual scale of the entire
    // system to be adjusted by the GE transform. 
    //
    // Anything that is placed in the GE scene from physics calculations (e.g. orbit paths etc.)
    // needs to go through this method. 
    //******************************************

    public Vector3 MapToScene(Vector3 pos) {
        if (!mapToScene)
            return pos;

        // someone will think of a weird reason to scale x/y/z differently
        Vector3 scaledPos = new Vector3(transform.localScale.x * pos.x,
                                    transform.localScale.y * pos.y,
                                    transform.localScale.z * pos.z); 
        return transform.rotation * scaledPos + transform.position;
    }

    public Vector3 UnmapFromScene(Vector3 pos) {
        if (!mapToScene)
            return pos;

        // someone will think of a weird reason to scale x/y/z differently
        Vector3 scaledPos = new Vector3( pos.x/ transform.localScale.x ,
                                    pos.y/transform.localScale.y,
                                    pos.z/transform.localScale.z);
        return Quaternion.Inverse(transform.rotation) * scaledPos - transform.position;

    }

    /// <summary>
    /// Return a clone of the current world state. 
    /// 
    /// This can then be independently evolved as part of e.g. course correction determination
    /// 
    /// </summary>
    /// <returns></returns>
    public GravityState GetGravityStateCopy() {
        return new GravityState(worldState);
    }


    /*******************************************
	* Trajectory Prediction
	* TP prediction is based on maintaining a parallel integrator and 
	* masslessEngine and running them ahead in time. 
	* Trajectory objects attached are given the updated position information 
	* so the future path can be displayed. 
	*
	* If the inputs to the system change (velocity change, body added) then the 
	* system needs to be reset and run forward from the current state again. 
	/*******************************************/

    private bool trajectoryRestart; 

	public void TrajectoryRestart() {
        if (trajectoryPrediction) {
            trajectoryRestart = true;
        }
	}

	/// <summary>
	/// Sets the trajectory prediction state (enable/disable). 
	/// On enable, will activate the Trajectory elements and re-run the trajectory prediction code.
	/// On disable will de-activate all trajectory elements. 
	/// </summary>
	/// <param name="newState">If set to <c>true</c> new state.</param>
	public void SetTrajectoryPrediction(bool newState) {
		if (newState != trajectoryPrediction) {
			if (newState) {
				// do a restart sync-ed with FixedUpdate
				trajectoryRestart = true;
				// set all trajectories active
				for (int i=0; i < worldState.numBodies; i++) {
					if ((trajectories[i] != null) && (worldState.info[i] & INACTIVE) == 0) {
						trajectories[i].gameObject.SetActive(true);
					}
				}
				if (worldState.masslessEngine != null) {
                    worldState.masslessEngine.TrajectoryEnable(true);
				}
                trajectoryPrediction = true;
            } else {
				// hide all trajectories and remove all time/text markers
				for (int i=0; i < worldState.numBodies; i++) {
					if ((trajectories[i] != null) && (worldState.info[i] & INACTIVE) == 0) {
						trajectories[i].Cleanup();
						trajectories[i].gameObject.SetActive(false);
					}
				}
				if (worldState.masslessEngine != null) {
                    worldState.masslessEngine.TrajectoryEnable(false);
				}
                trajectoryPrediction = false;
                trajectoryRestart = false;
            }
        }
	}

	private void ResetTrajectoryPrediction() {

		// trajectory state starts as a clone of current world state
		trajectoryState = new GravityState(worldState);
        trajectoryState.hasTrajectories = true;

        // @Awkward: hold trajectories in GS?
        for (int i = 0; i < worldState.numBodies; i++) {
            if ((trajectories[i] != null) && (worldState.info[i] & INACTIVE) == 0) {
                trajectories[i].Init((float) worldState.time);
            }
        }

		trajectoryRestart = false;
#pragma warning disable 162     // disable unreachable code warning
        if (GravityEngine.DEBUG)
            Debug.Log("Reset trajectory prediction");
#pragma warning restore 162
    }

    /// <summary>
    /// If any NBodies have Trajectory components then update them with new projected position/times
    /// 
    /// Internal use only (called from GravityState during evolution when trajectories are present)
    /// </summary>
    public void UpdateTrajectories() {
		for (int i=0; i < worldState.numBodies; i++) {
			if ((trajectories[i] != null) && (worldState.info[i] & INACTIVE) == 0) {
				Vector3 position = new Vector3((float)trajectoryState.r[i,0], (float)trajectoryState.r[i,1], (float)trajectoryState.r[i,2]); 
				position = physToWorldFactor * position;
				trajectories[i].AddPoint(position, (float)trajectoryState.time, (float) worldState.time);
				// update trajectory data if enabled (used for intercept detection)
				if (trajectories[i].recordData) {
                    // Want scaled velocity
                    Vector3 velocity = Vector3.zero;
                    if ((worldState.info[i] & FIXED_MOTION) != 0) {
                        NBody nbody = gameNBodies[i];
                        velocity = nbody.engineRef.fixedBody.fixedOrbit.GetVelocity();
                    } else {
                        velocity = worldState.integrator.GetVelocityForIndex(i);
                    }
                    trajectories[i].AddData(position, velocity, (float)trajectoryState.time);
				}
			}
		}
		// massless bodies update their own trajectories (a bit klunky)
		if (trajectoryState.masslessEngine != null) {
            trajectoryState.masslessEngine.UpdateTrajectories(physToWorldFactor, (float)trajectoryState.time, (float) worldState.time );
		}

	}

 
    private bool trajectoryUpToDate = false;

    /// <summary>
    /// Evolves the trajectory to the specified time or advances the trajectory by the fraction constrained
    /// by the trajectoryComputeFactor. This limits the number of trajectory integrations in a given fixed update
    /// to reduce the frame rate impact of frequent trajectory updates. 
    /// </summary>
    private void EvolveTrajectory(double gameDt) {

        // determine delta time to evolve and invoke common Evolve routine
        double timeInterval = (worldState.time + trajectoryTime) - trajectoryState.time;
        // if we're catching up, then reduce amount of work we do on this step
        trajectoryUpToDate = true;
        if (timeInterval > gameDt) {
            timeInterval = Mathd.Min(trajectoryComputeFactor * gameDt, trajectoryTime);
            trajectoryUpToDate = false;
        }
        trajectoryState.Evolve(this, timeInterval);
    }

    public bool TrajectoryUpToDate() {
        return trajectoryUpToDate;
    }

    /// <summary>
    /// Utility function used by GEConsole to advance the scene by one GE FixedUpdate. 
    /// If single step mode is not active, this call will activate it.
    /// </summary>
    public void EvolveOneFixedUpdate() {
        singleStep = true;
        stepHasRun = false; 
    }

    /// <summary>
    /// Main physics evolution entry point. 
    /// </summary>
    /// 


    void FixedUpdate () {

        float startTime;
        double startWorldTime;
#pragma warning disable 162       // disable unreachable code warning
        if (DEBUG) {
            startTime = Time.realtimeSinceStartup;
            startWorldTime = worldState.time;
        }
#pragma warning restore 162       // disable unreachable code warning
        if (evolve) {
            if (!isSetup) {
                Setup();
                isSetup = true;
            }

            // support for GEconsole single step mode
            if (singleStep && stepHasRun) {
                return;
            }
            stepHasRun = true;

            if (trajectoryPrediction && trajectoryRestart) {
                ResetTrajectoryPrediction();
            }

            // Note: In fixed update this is the physics deltaTime (not frame rate)
            // "When called from inside MonoBehaviour's FixedUpdate, returns the fixed framerate delta time."
            double gameDt = Time.deltaTime * timeZoom;
            EvolveByTimestep(gameDt);
            lastWorldDt = gameDt;

        } else if (isSetup && trajectoryPrediction) {
            if (trajectoryRestart) {
                ResetTrajectoryPrediction();
            }
            // run trajectory evolution always (if setup). May be paused and looking at vel. changes
            EvolveTrajectory(Time.fixedDeltaTime * timeZoom);
        }

#pragma warning disable 162       // disable unreachable code warning
        if (DEBUG) {
            float runtime = Time.realtimeSinceStartup - startTime;
            if (runtime > Time.fixedDeltaTime) {
                Debug.LogWarningFormat("GE physics overrun. Used {0} (delta time={1}) from worldTime={2} => {3}", 
                    runtime, Time.fixedDeltaTime, startWorldTime, worldState.time);
            }
        }
#pragma warning restore 162       // re-enable unreachable code warning

        if (offRailsDefered.Count > 0) {
            foreach (NBody nbody in offRailsDefered) {
                // a massless fixed body ended up on the massive list. Leave it there. 
                worldState.RemoveFixedBody(nbody);
                nbody.engineRef.fixedBody = null;
                nbody.engineRef.bodyType = BodyType.MASSIVE;
            }
            offRailsDefered.Clear();
            worldState.UpdateOnRails();
        }
    }

    /// <summary>
    /// Evolve the physics to the specified time. 
    /// </summary>
    /// <param name="timestep"></param>
    private void EvolveByTimestep(double timestep) {

        // if evolution triggers a maneuver, then will need to restart trajectory prediction with new velocities etc.
        trajectoryRestart = worldState.Evolve(this, timestep);

        // no point in evolving if we're restarting trajectories
        if (trajectoryPrediction && !trajectoryRestart) {
            EvolveTrajectory(timestep);
        }

        // if there is a timescale change pending, apply it
        if (timeZoomChangePending) {
            timeZoom = newTimeZoom;
            timeZoomChangePending = false;
        }
 
        // update positions on screen
        UpdateGameObjects();

    }

    /// <summary>
    /// Gets the physical scale.
    /// </summary>
    /// <returns>The physical scale.</returns>
    public float GetPhysicalScale() {
		return physToWorldFactor;
	}

	/// <summary>
	/// Return the length scale that maps lengths in the specified unit system to 
	/// Unity game length units (e.g. Unity units per meter, Unity units per AU)
	/// </summary>
	/// <returns>The length scale.</returns>
	public float GetLengthScale() {
		return lengthScale;
	}

	/// <summary>
	/// Changes the timescale during runtime
	/// </summary>
	/// <param name="value">Value.</param>
	public void SetTimeZoom(float value) {
		newTimeZoom = value;
		timeZoomChangePending = true;
	}

    /// <summary>
    /// Fast forward by the specified time. 
    /// 
    /// This can involve *significant* computation and is very 
    /// likely to cause a frame rate stall! 
    /// 
    /// Will eventually be moved into the job system to remove this impact.
    /// </summary>
    /// <param name="time"></param>
    public void FastForward(float time) {
        EvolveByTimestep(worldState.time + time);
    }

    /// <summary>
    /// Set the current physical time and update all objects to be at this time. 
    /// 
    /// ONLY VALID if all objects are "On-Rails" i.e. in Kepler mode. (GravityState will issue
    /// a warning and leave time unchanged if this is not true).
    /// 
    /// Depending on the history of events it can be possible to set to an earlier time. This
    /// depends on whether any Kepler objects have done ApplyImpulse or SOI changes that have 
    /// updated their time0 reference. If this *has* happened, then unspecified Kepler evolution will
    /// result. User code must limit earliest time based on knowledge of when impules were applied.
    /// 
    /// </summary>
    public void SetPhysicalTime(double newTime) {
        worldState.SetTime(newTime);
        if (trajectoryState != null) {
            TrajectoryRestart();
        }
        UpdateGameObjects();
    }

    /// <summary>
    /// Get the current time zoom factor (run-time scaling of physics time execution). 
    /// 
    /// Note that the baseline timescale is set by timeScale (based on units selected) when 
    /// the Gravity Engine initializes. 
    /// 
    /// </summary>
    /// <returns></returns>
    public float GetTimeZoom() {
        if (timeZoomChangePending)
            return newTimeZoom;
        return timeZoom;
    }

	/// <summary>
	/// Gets the physical time. Physical time may differ from game time by the timescale factor.
	/// </summary>
	/// <returns>The physical time.</returns>
	public float GetPhysicalTime() {
        // DrawGizmos can use this before GE has inited
        if (worldState == null)
            return 0;
        return (float)worldState.GetPhysicsTime();
	}

    /// <summary>
    /// Gets the physical time. Physical time may differ from game time by the timescale factor.
    /// </summary>
    /// <returns>The physical time.</returns>
    public double GetPhysicalTimeDouble() {
        if (worldState == null)
            return 0;
        return worldState.GetPhysicsTime();
    }

    public double GetTimeWorldSeconds() {
        if (worldState == null)
            return 0;
        return GravityScaler.GetWorldTimeSeconds(worldState.GetPhysicsTime());
    }

    private void SetupGameObjectAndChildren(GameObject gameObject) {

		NBody[] nbodies = gameObject.GetComponentsInChildren<NBody>();
        // Add in order of orbitDepth
        foreach (NBody nbody in nbodies) {
            nbody.CalcOrbitDepth();
        }
        System.Array.Sort(nbodies, 0, nbodies.Length, nbodies[0]);

        foreach (NBody nbody in nbodies) {
			SetupOneGameObject(nbody.transform.gameObject, nbody);
		}
	}

    // Adds one game object 
    // - massless bodies go to the masslessEngine (if "optimize massless" is turned on)
    // - fixed bodies:
    //     * massive: 
    //          add to the integrator and r[] (so their mass can effect others)
    //          add to the fixedBodies list
    //     * massless: also added to integrator & fixedBodies list [integrator not necessary - need to fix]
    //
	private void SetupOneGameObject(GameObject go, NBody nbody) {

		bool fixedObject = false;
        // If there is a KeplerSequence - it wins (It wraps the OrbitUniversal that sits beside it)
        IFixedOrbit fixedOrbit = null;
        fixedOrbit = (IFixedOrbit) go.GetComponent<KeplerSequence>();
        if (fixedOrbit == null) {
            fixedOrbit = go.GetComponent<IFixedOrbit>();
        } 

        // grow arrays if needed
        if (worldState.numBodies + 1 > arraySize) {
            if (!GrowArrays(GROW_SIZE)) {
                // Grow arrays has logged the error
                return;
            }
        }

        // apply e.g. orbit positioning if required (heirarchically) and update initialPhyPos
        nbody.InitPosition(this);

        // this engine ref is only used for massive bodies (MBE will create one of it's own) ICK!
        EngineRef engineRef = new EngineRef();

        // FixedObject and Ellipses in Kepler mode are fixed, their mass affects others but they move
        // (or not) under control of the IFixedOrbit component.
        if (fixedOrbit != null && fixedOrbit.IsOnRails()) {
			fixedObject = true;
            // Fixed objects are ALSO added to the list of massive objects below so that their gravity can affect others
            worldState.info[worldState.numBodies] = FIXED_MOTION;
            FixedBody fixedBody = new FixedBody(nbody, fixedOrbit);

            fixedBody.kepler_depth = OrbitUtils.CalcKeplerDepth(fixedOrbit);
            worldState.AddFixedBody(fixedBody);
            // was if (evolve) - seemed unnecessary. @TODO - delete comment if regression passes
			fixedOrbit.PreEvolve(physToWorldFactor, massScale);
            engineRef.fixedBody = fixedBody;
            engineRef.bodyType = BodyType.FIXED;
		} else {
            worldState.info[worldState.numBodies] = 0;
		} 


		if (nbody.mass == 0 && optimizeMassless && !fixedObject) {
            // use the massless engine and its built-in Leapfrog integrator
            // MBE will create an EngineRef
            worldState.AddMasslessBody(go, physToWorldFactor, engineDt);
            // update the NBody transform so e.g. trail renderers can be enabled immediatly
            nbody.GEUpdate(nbody.initialPhysPosition, GetVelocity(nbody), this);
            #pragma warning disable 162     // disable unreachable code warning
            if (DEBUG) {
				Vector3 pos = nbody.transform.position;
				Debug.Log(string.Format("GE add massless: {0} world r=[{1} {2} {3}] v=[{4} {5} {6}]",
					go.name, pos.x, pos.y, pos.z, 
					nbody.vel_phys.x, nbody.vel_phys.y, nbody.vel_phys.z));
			}
            #pragma warning restore 162        // enable unreachable code warning

        } else {
            // Use the standard (massive) engine and the configured integrator

            // traj will be attached to a child of NBody object - record them all
            // Note: if dynamically enable/disable trajectory prediction with scripting all will be affected. 
            // Could choose to bias this to initially active ones in this loop if that was preferred.
            for (int childNum=0; childNum < nbody.transform.childCount; childNum++) {
				Trajectory trajectory = nbody.transform.GetChild(childNum).GetComponent<Trajectory>(); 
				if (trajectory != null) {
					trajectories[worldState.numBodies] = trajectory;
					break;
				}
			}

            engineRef.index = worldState.numBodies;
            gameNBodies[engineRef.index] = nbody;
            if (!fixedObject) {
                engineRef.bodyType = BodyType.MASSIVE;
            }
            nbody.engineRef = engineRef;
            // divide by the physics to world scale factor - required for threebody solutions
            Vector3 physicsPosition = nbody.initialPhysPosition / physToWorldFactor;
            worldState.AddNBody(nbody, physicsPosition, nbody.vel_phys, massScale);
            // update the NBody transform so e.g. trail renderers can be enabled immediatly
            nbody.GEUpdate(nbody.initialPhysPosition, nbody.vel_phys, this);
			#pragma warning disable 162		// disable unreachable code warning
			if (DEBUG) {
				int i = worldState.numBodies - 1;
				Debug.Log(string.Format("GE add massive: {0} as {1} r=[{2} {3} {4}] v=[{5} {6} {7}] index={8} info={9} mass={10}",
					go.name, i, worldState.r[i,0], worldState.r[i,1], worldState.r[i,2], 
					nbody.vel_phys.x, nbody.vel_phys.y, nbody.vel_phys.z, nbody.engineRef.index,
                    worldState.info[i], worldState.m[i]));
			}
			#pragma warning restore 162		// enable unreachable code warning
		}
        
	}
		
	/// <summary>
	/// Adds the game object and it's children to GravityEngine. The engine will then handle position updates for the 
	/// body based on the gravitational force of all other bodies controlled by the engine. 
	///
	/// If the GravityEngine is set to auto-detect bodies, all game objects present in the scene with a NBody
	/// component will be added once the GravityEngine is set to evolve. If auto-detect is not enabled bodies are
	/// added by calling this method. 
	///
	/// A gameObject added to the engine must have an NBody script attached. The NBody script specifies the
	/// mass and initial velocity of the object. 
	///
	/// The add method will traverse the children of the added gameObject and add any that have NBody components.  
	///
	/// Optionally, a body may also have a fixed motion script (e.g. FixedEllipticalOrbit) or a script that
	/// set the initial position and velocity based on orbit parameters (e.g. EllipticalStart)
	/// </summary>
	/// <param name="go">Game object.</param>
	public void AddBody(GameObject go) {

		if (isSetup) {
			NBody nbody = go.GetComponent<NBody>(); 
			if (nbody == null) {
				Debug.LogError("No NBody found on " + go.name);
				return;
			}
			GravityScaler.ScaleNBody(nbody, units, lengthScale);
			SetupGameObjectAndChildren(go);
		} else {
			addedByScript.Add(go);
		}
		if (trajectoryPrediction) {
			trajectoryRestart = true;
		}

	}

    /// <summary>
    /// Remove game object from GE.
    /// 
    /// Note: In a large-N simulation the shuffle down may cause a real-time hit. In those cases, 
    /// marking the body inactive with InactivateGameObject will exclude it from physics calculations
    /// without the shuffle overhead.
    /// </summary>
    /// <param name="toRemove">Game object to remove (must have a NBody component)</param>
    /// 
    public void RemoveBody(GameObject toRemove) {

		NBody nbody = toRemove.GetComponent<NBody>();
        if (nbody == null ) {
			Debug.LogWarning("object to remove has no NBody " + toRemove.name);
			return;
		}
        if ( nbody.engineRef == null) {
            Debug.LogWarning("object to remove has not been added: " + toRemove.name);
            return;
        }
        if ((worldState.info[nbody.engineRef.index] & FIXED_MOTION) != 0) {
            worldState.RemoveFixedBody(nbody);
		}
		if ((nbody.engineRef.bodyType == BodyType.MASSLESS) && optimizeMassless) {
            worldState.RemoveMasslessBody(toRemove);
        } else {
            // shuffle down the gameObjects array
            for (int j = nbody.engineRef.index; j < (worldState.numBodies - 1); j++) {
                gameNBodies[j] = gameNBodies[j + 1];
                NBody nextNBody = gameNBodies[j];
                nextNBody.engineRef.index = j;
            }
            gameNBodies[worldState.numBodies - 1] = null;
            worldState.RemoveNBody(nbody);
        }
        worldState.UpdateOnRails();
        if (trajectoryPrediction) {
			trajectoryRestart = true;
		}
        nbody.engineRef = null;
	}

	/// <summary>
	/// Inactivates the body in the GravityEngine. 
	///
	/// Mark the object as inactive. It will not affect other bodies/particles in the simulation. 
	///
	/// This can be a better choice than removing since a removal may impact real-time performance. 
	///
	/// This does not affect the activity state of the GameObject, only it's involvement in the GravityEngine
	/// 
	/// </summary>
	/// <returns>The game object.</returns>
	/// <param name="toInactivate">Game object to inactivate.</param>
	public void InactivateBody(GameObject toInactivate) {
		NBody nbody = toInactivate.GetComponent<NBody>(); 
		if (nbody == null) {
			Debug.LogWarning("Not an NBody - cannot remove"); 
			return;
		}
		if (nbody.engineRef.bodyType == BodyType.MASSLESS ) {
            worldState.masslessEngine.InactivateBody(nbody);
            if (trajectoryState != null) {
                trajectoryState.masslessEngine.InactivateBody(nbody);
            }
		} else {
			int i = nbody.engineRef.index;
            worldState.info[i] |= INACTIVE;
            if (trajectoryState != null) {
                trajectoryState.info[i] |= INACTIVE;
            }
        }
#pragma warning disable 162        // disable unreachable code warning
        if (DEBUG)
            Debug.Log("Inactivate body " + toInactivate.name);
#pragma warning restore 162       // enable unreachable code warning
        // if massless, no change on trajectories
        if (trajectoryPrediction && !(nbody.engineRef.bodyType == BodyType.MASSLESS)) {
			trajectoryRestart = true;
		}
	}

	/// <summary>
	/// Re-activates an inactive body.
	/// </summary>
	/// <param name="toInactivate">To inactivate.</param>
	/// Code from John Burns
	public void ActivateBody(GameObject activate) {
		NBody nbody = activate.GetComponent<NBody>(); 
		if (nbody == null) {
			Debug.LogWarning("Not an NBody - cannot remove"); 
			return;
		}
		if (nbody.engineRef.bodyType == BodyType.MASSLESS ) {
            worldState.masslessEngine.ActivateBody(nbody);
            if (trajectoryState != null) {
                trajectoryState.masslessEngine.ActivateBody(nbody);
            }
        } else {
			int i = nbody.engineRef.index;
            worldState.info[i] &= unchecked((byte)~INACTIVE);
            if (trajectoryState != null) {
                trajectoryState.info[i] &= unchecked((byte)~INACTIVE);
            }
#pragma warning disable 162        // disable unreachable code warning
            if (DEBUG)
				Debug.Log("Activate body " + activate.name);
			#pragma warning restore 162		// enable unreachable code warning
		}
		if (trajectoryPrediction) {
			trajectoryRestart = true;
		}
	}

    /// <summary>
    /// Recompute and update the Kepler depth of the specified NBody. 
    /// 
    /// Used when a Kepler sequence changes OrbitU segements. 
    /// </summary>
    public void UpdateKeplerDepth(NBody nbody, OrbitUniversal orbitU) {
        worldState.UpdateKeplerDepth(nbody, orbitU);
    }

    /// <summary>
    /// Change a body from On-rails to off. This may be called from a KeplerSeqeunce evolved and thus
    /// within the MoveFixedBodies code in GravityState. As a result it cannot make changes to the
    /// fixed bodies list at this time. 
    /// 
    /// (When a KeplerSequence goes off rails it finds the r,v from the previous segment. Hence the need for r,v.)
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="pos"></param>
    /// <param name="vel"></param>
    public void BodyOffRails(NBody nbody, Vector3d pos, Vector3d vel ) {
        if (nbody.engineRef.bodyType != BodyType.FIXED)
            return;
        // flip type immediatly to avoid a KS update in UpdateGameObjects() and set position in
        // integrator. 
        nbody.engineRef.bodyType = BodyType.MASSIVE;
        worldState.info[nbody.engineRef.index] &= NOT_FIXED_MOTION;
        SetPositionDoubleV3(nbody, pos);
        SetVelocityDoubleV3(nbody, vel);
        offRailsDefered.Add(nbody);
    }

    /// <summary>
    /// Take a body that is doing Nbody evolution and put it on rails around the centerNBody. 
    /// 
    /// If there is no KeplerSequence/OrbitUniversal on the NBody, one will be created. 
    /// 
    /// Any existing KeplerSequence on the body will be reset (since cannot time reverse rails to 
    /// earlier segements since nbody evolution phase was off-rails). 
    /// 
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="centerNBody"></param>
    /// <returns></returns>
    public KeplerSequence BodyOnRails(NBody nbody, NBody centerNBody) {
        if (nbody.engineRef.bodyType == BodyType.FIXED)
            return nbody.gameObject.GetComponent<KeplerSequence>(); 
        // Setup on-rails with the r,v and center object we have
        const bool relativePosFalse = false;

        // get the position and velocity and remove from NBody
        Vector3d pos = GetPositionDoubleV3(nbody);
        Vector3d vel = GetVelocityDoubleV3(nbody);
        RemoveBody(nbody.gameObject); // this will update GravityState isOnRails()

        // Require a KeplerSequence
        KeplerSequence ks = null;
        ks = nbody.gameObject.GetComponent<KeplerSequence>();
        if (ks == null) {
            // KeplerSequence is inited via InitNBody during add. Since KeplerSequence requires an OrbitUniversal
            // it will create a base orbitU as it is added. 
            ks = nbody.gameObject.AddComponent<KeplerSequence>();
            OrbitUniversal orbitU = nbody.gameObject.GetComponent<OrbitUniversal>();
            orbitU.InitFromRVT(pos, vel, GetPhysicalTimeDouble(), centerNBody, relativePosFalse);
            orbitU.evolveMode = OrbitUniversal.EvolveMode.KEPLERS_EQN;
        } else {
            // Will not be able to time reverse to a time before this, so clear out any orbitU elements
            // (this will leave the first segement in place)
            ks.Reset();
            ks.AppendElementRVT(pos, vel, GetPhysicalTimeDouble(), relativePosFalse, nbody, centerNBody, null);
        }

        AddBody(nbody.gameObject);
        return ks;
    }

    /// <summary>
    /// Updates the position and velocity of an existing body in the engine to new 
    /// GE values (e.g. teleport of the object)
    /// </summary>
    /// <param name="nbody">Nbody.</param>
    /// <param name="pos">position (internal units)</param>
    /// <param name="vel">velocity (internal units)</param>
    public void UpdatePositionAndVelocity(NBody nbody, Vector3 pos, Vector3 vel) {

		if (nbody == null) {
			Debug.LogError("object to update has no NBody: ");
			return;
		}
		int i = nbody.engineRef.index;
        if (nbody.engineRef.bodyType == BodyType.MASSIVE) {
            // GE holds pos/vel in array
            worldState.r[i, 0] = pos.x;
            worldState.r[i, 1] = pos.y;
            worldState.r[i, 2] = pos.z;
            worldState.integrator.SetVelocityForIndex(i, vel);
        } else if (nbody.engineRef.bodyType == BodyType.FIXED) {
            nbody.engineRef.fixedBody.fixedOrbit.UpdatePositionAndVelocity(pos, vel);
            double[] new_r = new double[] { 0, 0, 0 };
            nbody.engineRef.fixedBody.fixedOrbit.Evolve(worldState.time, ref new_r);
            worldState.r[i, 0] = new_r[0];
            worldState.r[i, 1] = new_r[1];
            worldState.r[i, 2] = new_r[2];
        } else {
            worldState.masslessEngine.SetPositionAtIndex(i, pos, physToWorldFactor);
            worldState.masslessEngine.SetVelocityAtIndex(i, vel);
		}
		if (trajectoryPrediction) {
			trajectoryRestart = true;
		}
        nbody.UpdateVelocity();
        // If not evolving force game objects to new position(s)
        // @TODO Ideally would only update the one object
        if (!evolve) {
            UpdateGameObjects();
        }
	}

    /// <summary>
    /// Changes the length scale of all NBody objects in the scene due to a change in the inspector.
    /// Find all NBody containing objects.
    /// - independent objects are rescaled
    /// - orbit based objects have their primary dimension adjusted (e.g. for ellipse, a)
    ///   (these objects are scalable and are asked to rescale themselves)
    ///
    /// Length scale is Nbody units/Unity Length e.g. km/Unity Length
    /// Not intended for run-time use.
    /// </summary>
    private void UpdateLengthScale(float newScale) {
        if (newScale != _lengthScale) {
            _lengthScale = newScale;
            GravityScaler.UpdateTimeScale(units, _timeScale, _lengthScale);
            GravityScaler.ScaleScene(units, _lengthScale);
        }
	}

	/// <summary>
	/// Updates the time scale.
	/// Prior to scene starting GE adjusts the time scale by setting DT for the numerical integrators.
	///
	/// During evolution DT cannot be changed on the fly for the Leapfrog integrators without violating
	/// energy conservation - so changes are made in the number of integration performed. This imposes a
	/// practical limit on how much "speed up" can occur - since too much time evolution will lower the
	/// frame rate.
	/// </summary>
	/// <param name="value">Value.</param>

	private void UpdateTimeScale(float value) {

		if (!evolve) {
			_timeScale = value;
			GravityScaler.UpdateTimeScale(units, _timeScale, _lengthScale);
			// need to do something with timeZoom here...
		} 
	}

	// TODO: Need to get conversion from phys to world
	public float GetVelocityScale() {
		return GravityScaler.GetVelocityScale();
	}


	// Intially thought in terms of changing DT based on units - but decided it's better to rescale
	// distances and masses to adapt to a universal timescale. 
	private void ConfigureDT() {
		
		double time_g1 = 1f;
		double stepsPerSec = 60.0 * (double) stepsPerFrame;
		double particleStepsPerSec = 60.0 * (double) particleStepsPerFrame;
		engineDt = time_g1/stepsPerSec; 
		particle_dt = time_g1/particleStepsPerSec; 

	}

    /// <summary>
    /// Update physics based on collisionType between body1 and body2. 
    ///
    /// In all cases except bounce, the handling is a "hit and stick" and body2 is assumed to be
    /// removed. It's momtm is not updated. body1 velocity is adjusted based on conservation of momtm.
    ///
    /// </summary>
    /// <param name="body1">Body1.</param>
    /// <param name="body2">Body2.</param>
    /// <param name="collisionType">Collision type.</param>
    public void Collision(GameObject body1, GameObject body2, NBodyCollision.CollisionType collisionType, float bounce) {
        NBody nbody1 = body1.GetComponent<NBody>();
        NBody nbody2 = body2.GetComponent<NBody>();
        Collision(nbody1, nbody2, collisionType, bounce);
    }

    public void Collision(NBody nbody1, NBody nbody2, NBodyCollision.CollisionType collisionType, float bounce) {

        int index1 = nbody1.engineRef.index;
		int index2 = nbody2.engineRef.index;
		if (index1 < 0 || index2 < 0)
			return; 

		// if either is massless, no momtm to exchange
		if (nbody1.mass == 0 || nbody2.mass == 0) {
			if (collisionType == NBodyCollision.CollisionType.BOUNCE) {
				// reverse the velocities
				if (nbody1.mass == 0) {
					if (nbody1.engineRef.bodyType == BodyType.MASSLESS) {
						Vector3 vel1_ml = worldState.masslessEngine.GetVelocity(nbody1);
                        worldState.masslessEngine.SetVelocity(nbody1, -1f*vel1_ml);
					} else {
						Vector3 vel1_m = worldState.integrator.GetVelocityForIndex(index1);
                        worldState.integrator.SetVelocityForIndex(index1, -1f*vel1_m);
					}
				}
				if (nbody2.mass == 0) {
					if (nbody2.engineRef.bodyType == BodyType.MASSLESS) {
						Vector3 vel2_ml = worldState.masslessEngine.GetVelocity(nbody2);
                        worldState.masslessEngine.SetVelocity(nbody1, vel2_ml);
					} else {
						Vector3 vel2_m = worldState.integrator.GetVelocityForIndex(index2);
                        worldState.integrator.SetVelocityForIndex(index1, -1f*vel2_m);
					}
				}
			}
			return;
		}
		// velocity information is in the integrators. 
		// 
		Vector3 vel1 = worldState.integrator.GetVelocityForIndex(index1);
		Vector3 vel2 = worldState.integrator.GetVelocityForIndex(index2);
		// work in CM frame of B1 and B2
		float m_total = (float)(nbody1.mass + nbody2.mass);
		Vector3 cm_vel = (((float) nbody1.mass)*vel1 + ((float)nbody2.mass)*vel2)/m_total;
		// Determine new velocities in CM frame
		Vector3 vel1_cm = vel1 - cm_vel;
		Vector3 vel2_cm = vel2 - cm_vel;
		if (collisionType == NBodyCollision.CollisionType.ABSORB_IMMEDIATE ||
			collisionType == NBodyCollision.CollisionType.EXPLODE) {
			// hit and stick 
			Vector3 v_final_cm = (((float) nbody1.mass)*vel1_cm + ((float)nbody2.mass)*vel2_cm)/m_total;
			// Translate back to world frame
			vel1 = v_final_cm + cm_vel;
			// update mass of body1 to include body2
			nbody1.mass = m_total;
            // Update velocities in integrator
            worldState.integrator.SetVelocityForIndex(index1, vel1);
		} else if (collisionType == NBodyCollision.CollisionType.BOUNCE) {
            // reverse CM velocities and flip back to world velocities
            worldState.integrator.SetVelocityForIndex(index1, cm_vel - bounce * vel1_cm);
            worldState.integrator.SetVelocityForIndex(index2, cm_vel - bounce * vel2_cm);
		}
	}
	
	// Update the game objects positions from the values held by the GravityEngine based on physics evolution
	// These positions are globally scaled by physicalScale to allow the physics to act on a
	// suitable scale where required. 	
	//
	private void UpdateGameObjects() {
		for (int i=0; i < worldState.numBodies; i++) {
            // fixed objects update their own transforms as they evolve
            NBody nbody = gameNBodies[i];
            if ((worldState.info[i] & INACTIVE) == 0) {
                if (nbody.engineRef.bodyType != BodyType.MASSLESS) {
                    Vector3 position = new Vector3((float)worldState.r[i, 0], (float)worldState.r[i, 1], (float)worldState.r[i, 2]);
                    if (NUtils.VectorNaN(position)) {
                        InactivateBody(gameNBodies[i].gameObject);
                        Debug.LogWarning("Position NaN - inactivated " + gameNBodies[i].name);
                    } else {
                        position = physToWorldFactor * position;
                        nbody.GEUpdate(position, GetVelocity(nbody), this);
                    }
                } 
            }
		}
        // particles
        foreach (GravityParticles nbp in worldState.gravityParticles) {
            nbp.UpdateParticles(physToWorldFactor, this);
        }
        // massless bodies
        if (worldState.masslessEngine != null) {
            worldState.masslessEngine.UpdateBodies(physToWorldFactor, this);
        }

    }

    /// <summary>
    /// Convert a GE internal physical position to a transform position in the scene. 
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public Vector3 MapPhyPosToWorld(Vector3 pos) {
        return MapToScene(pos * physToWorldFactor);
    }

    /// <summary>
    /// Move all GE controlled objects position by the Vector3 move.
    /// 
    /// This moves the internal physics positions. The idea here is to allow an object that is important in the
    /// scene (e.g. a spaceship) to always be with a few thousand unity units of the origin, so that it's position
    /// maps to the scene without precision errors that could arise if it were at e.g. millions of units in position. 
    /// 
    /// This call will update massive, massless, fixed and particles under the control of GE. If trajectory prediciton is
    /// enabled trajectory values will be updated and this may be CPU-expensive. 
    /// 
    /// OrbitPredictors will recalculate positions on the next frame.
    /// 
    /// This is typically triggered by an external agent monitoring the physical position. Note that other elements in the
    /// scene (e.g. cameras) may need to be adjusted. Elements with internal position state (e.g. trail renderers) must be 
    /// adjusted by external code. 
    /// </summary>
    /// <param name="move">Amount to move in physics space (internal GE positions, not necessarily scene positions depending on units and scale choice.</param>

    public void MoveAll(NBody nbody) {
        Vector3d moveBy = -1.0 * GetPositionDoubleV3(nbody);
        MoveAll(moveBy);
    }

    public void MoveAll(Vector3 move) {
        MoveAll(new Vector3d(move));
    }

    public void MoveAll(Vector3d move) {
        double[] moveBy = new double[] { move.x, move.y, move.z };

        // @TODO: Move into GravityState
        for (int n = 0; n < worldState.numBodies; n++) {
            worldState.r[n, 0] += moveBy[0];
            worldState.r[n, 1] += moveBy[1];
            worldState.r[n, 2] += moveBy[2];
        }
        if (worldState.masslessEngine != null) {
            worldState.masslessEngine.MoveBodies(ref moveBy);
        }
        // Particles
        foreach (GravityParticles nbp in worldState.gravityParticles) {
            nbp.MoveAll(ref moveBy);
        }
        // Trajectories
        if (trajectoryState != null) {
            // move massless trajectories
            if (trajectoryState.masslessEngine != null) {
                trajectoryState.masslessEngine.MoveBodies(ref moveBy);
                trajectoryState.masslessEngine.MoveTrajectories(move);
            }
            for (int i = 0; i < worldState.numBodies; i++) {
                // move massive trajectories if present
                if ((trajectories[i] != null) && (worldState.info[i] & INACTIVE) == 0) {
                    trajectories[i].MoveAll(move);
                }
                // update trajectory state info (so continue projecting from shifted location)
                trajectoryState.r[i, 0] += moveBy[0];
                trajectoryState.r[i, 1] += moveBy[1];
                trajectoryState.r[i, 2] += moveBy[2];
            }
        }
        // Kepler objects cache their position, tell them about move
        foreach (FixedBody fixedBody in worldState.fixedBodies) {
            // TODO: Pass as a vector3d
            fixedBody.fixedOrbit.Move(move.ToVector3());
        }

        // update positions on screen
        UpdateGameObjects();

    }

    // TODO: Try and clean up this API bloat without breaking backwards compatibility...

    /// <summary>
    /// Gets the velocity of the body in "Physics Space" using a GameObject. 
    /// May be different from Unity co-ordinates if physToWorldFactor is not 1. 
    /// This is the velocity in Unity units. For dimensionful velocity use @GetScaledVelocity
    /// </summary>
    /// <returns>The velocity.</returns>
    /// <param name="body">Body</param>
    public Vector3 GetVelocity(GameObject body) {
        NBody nbody = body.GetComponent<NBody>();
        if (nbody == null) {
            Debug.LogError("No NBody found on " + body.name + " cannot get velocity");
            return Vector3.zero;
        }
        return GetVelocity(nbody);
    }

    /// <summary>
    /// Gets the velocity of the body in "Physics Space" using an NBody reference. 
    /// May be different from Unity co-ordinates if physToWorldFactor is not 1 or MapToScene is enabled.
    /// 
    /// This is the velocity in Unity units. For dimensionful velocity use @GetScaledVelocity
    /// </summary>
    /// <returns>The velocity.</returns>
    /// <param name="body">Body</param>
    public Vector3 GetVelocity(NBody nbody) {
        double[] v = new double[3];
        worldState.GetVelocityDouble(nbody, ref v);
        return new Vector3((float)v[0], (float)v[1], (float)v[2]);
    }

    /// <summary>
    /// Set the velocity of an Nbody. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="velocity">Physics velocity (GE internal value)</param>
	public void SetVelocity(NBody nbody, Vector3 velocity) {
		if (nbody.engineRef.bodyType == BodyType.MASSLESS) {
            worldState.masslessEngine.SetVelocityAtIndex(nbody.engineRef.index, velocity);
		} else {
            worldState.integrator.SetVelocityForIndex(nbody.engineRef.index, velocity);
		}
	}

     /// <summary>
    /// Double precision access to GE internal velocity (in physics units)
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="vel"></param>
    public void GetVelocityDouble(NBody nbody, ref double[] vel) {
        worldState.GetVelocityDouble(nbody, ref vel);
    }

    /// <summary>
    /// Get double precision velocity in internal physics units. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <returns></returns>
    public Vector3d GetVelocityDoubleV3(NBody nbody) {
        double[] v = { 0, 0, 0 };
        worldState.GetVelocityDouble(nbody, ref v);
        return new Vector3d(ref v);
    }

    /// <summary>
    /// Double precision setter for internal velocity in GE. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="velocity"></param>
    public void SetVelocityDouble(NBody nbody, ref double[] velocity) {
        worldState.SetVelocityDouble(nbody, ref velocity);
    }

    public void SetVelocityDoubleV3(NBody nbody, Vector3d vel) {
        double[] velDouble = vel.ToDoubleArray();
        worldState.SetVelocityDouble(nbody, ref velDouble);
    }

    /// <summary>
    /// Double precision access to GE internal position (in physics units)
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="vel"></param>
    public void GetPositionDouble(NBody nbody, ref double[] pos) {
        if (nbody.IsFixedOrbit()) {
            // If Kepler evolution 
            Vector3 r = nbody.engineRef.fixedBody.fixedOrbit.GetPosition();
            pos[0] = r.x;
            pos[1] = r.y;
            pos[2] = r.z;

        } else if (nbody.engineRef.bodyType == BodyType.MASSLESS) {
            worldState.masslessEngine.GetPositionDouble(nbody, ref pos);
        } else {
            pos[0] = worldState.r[nbody.engineRef.index, 0];
            pos[1] = worldState.r[nbody.engineRef.index, 1];
            pos[2] = worldState.r[nbody.engineRef.index, 2];
        }
    }

    /// <summary>
    /// Double precision internal position in physics units.  
    /// </summary>
    /// <param name="nbody"></param>
    /// <returns></returns>
    public Vector3d GetPositionDoubleV3(NBody nbody) {
        double[] p = { 0, 0, 0 };
        GetPositionDouble(nbody, ref p);
        return new Vector3d(ref p);
    }

    public void SetPositionDoubleV3(NBody nbody, Vector3d pos) {
        double[] dpos = pos.ToDoubleArray();
        worldState.SetPositionDouble(nbody, ref dpos);
        if (!evolve) {
            UpdateGameObjects();
        }
    }

    /// <summary>
    /// Gets the position and velocity in double precision in scaled units.
    /// (Note that the scale factors used to create these are floats - 
    /// </summary>
    /// <param name="nbody">Nbody.</param>
    /// <param name="p">P.</param>
    /// <param name="v">V.</param>
    public void GetPositionVelocityScaled(NBody nbody, ref double[] p, ref double[] v ) {
		if (nbody.engineRef.bodyType == BodyType.MASSLESS && worldState.masslessEngine != null) {
            worldState.masslessEngine.GetPositionVelocityScaled(nbody.engineRef.index, ref p, ref v);
		} else {
			p[0] = worldState.r[nbody.engineRef.index,0];
			p[1] = worldState.r[nbody.engineRef.index,1];
			p[2] = worldState.r[nbody.engineRef.index,2];
            worldState.integrator.GetVelocityDoubleForIndex(nbody.engineRef.index, ref v);
		}
		p[0] = p[0]*lengthScale;
		p[1] = p[1]*lengthScale;
		p[2] = p[2]*lengthScale;
		double vscale = (double) GravityScaler.GetVelocityScale();
		v[0] = v[0]/vscale;
		v[1] = v[1]/vscale;
		v[2] = v[2]/vscale;

	}

    /// <summary>
    /// Return the internal physics engine mass
    /// </summary>
    /// <param name="nbody"></param>
    /// <returns></returns>
    public double GetMass(NBody nbody) {
        if (nbody.engineRef == null) {
            return nbody.mass * massScale;
        }
        return worldState.GetMass(nbody);
    }


	/// <summary>
	/// Gets the velocity of the body in selected unit system.
	/// e.g. for SOLAR get value in km/sec.
    /// 
    /// NBody objects have their position and velocity updated each frame and getting the info from them 
    /// is the normal approach. This routine is used internally to get an updated value during the Evolve()
    /// process (e.g. when a Kepler object wants a update of it's center body mid-integration)
	/// </summary>
	/// <returns>The velocity.</returns>
	/// <param name="body">Body</param>
	public Vector3 GetScaledVelocity(GameObject body) {
		NBody nbody = body.GetComponent<NBody>();
		if (nbody == null) {
			Debug.LogError("No NBody found on " + body.name + " cannot get velocity"); 
			return Vector3.zero;
		}
        
		return GetScaledVelocity(nbody);
	}

    public Vector3 GetScaledVelocity(NBody nbody) {
        Vector3 velocity = Vector3.zero;
        if (nbody.IsFixedOrbit()) {
            // If Kepler evolution 
            return nbody.engineRef.fixedBody.fixedOrbit.GetVelocity();
        } else if (nbody.engineRef.bodyType == BodyType.MASSLESS) {
            velocity = worldState.masslessEngine.GetVelocity(nbody);
        } else {
            velocity = worldState.integrator.GetVelocityForIndex(nbody.engineRef.index);
        }
        velocity = velocity / GravityScaler.GetVelocityScale();
        return velocity;
    }

    /// <summary>
    /// Gets the position of the body in selected unit system.
    /// e.g. for SOLAR get value in km/sec.
    /// 
    /// NBody objects have their position and velocity updated each frame and getting the info from them 
    /// is the normal approach. This routine is used internally to get an updated value during the Evolve()
    /// process (e.g. when a Kepler object wants a update of it's center body mid-integration)
    /// </summary>
    /// <returns>The position in world space.</returns>
    /// <param name="body">Body</param>
    public Vector3 GetScenePosition(NBody body) {

        return GetPhysicsPosition(body)/lengthScale;
    }

    /// <summary>
    /// Gets the position of the body in internal physics units. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <returns></returns>
    public Vector3 GetPhysicsPosition(NBody nbody) {

        return worldState.GetPhysicsPosition(nbody);
    }

    /// <summary>
    /// Get the mass value used internally in GE. This mass value is scaled by the units and timescale factors
    /// as an Nbody is added to GE. 
    /// </summary>
    /// <param name="nBody"></param>
    /// <returns></returns>
    public float GetPhysicsMass(NBody nBody) {
        float mass = 0.0f;
        if  ((nBody.engineRef.bodyType == BodyType.MASSIVE) || 
             (nBody.engineRef.bodyType == BodyType.FIXED)) {
            mass = (float) worldState.m[nBody.engineRef.index];
        }
        return mass;
    }

    /// <summary>
    /// Gets the acceleration of the body in "Physics Space". 
    /// May be different from world co-ordinates if physToWorldFactor is not 1. 
    /// </summary>
    /// <returns>The acceleration.</returns>
    /// <param name="body">Body.</param>
    public Vector3 GetAcceleration(GameObject body) {
		NBody nbody = body.GetComponent<NBody>();
		if (nbody == null) {
			Debug.LogError("No NBody found on " + body.name + " cannot get velocity"); 
			return Vector3.zero;
		}
		if (optimizeMassless && nbody.mass < MASSLESS_LIMIT) {
			return worldState.masslessEngine.GetAcceleration(body);
		}
        // Fixed body will return zero - fix
		return worldState.integrator.GetAccelerationForIndex(nbody.engineRef.index);
	}

    public Vector3 GetAccelerationScaled(GameObject body)
    {
        Vector3 a = GetAcceleration(body);
        return GravityScaler.ScaleAcceleration( a, lengthScale, timeScale);
    }

    /// <summary>
    /// Applies an impulse to an evolving body. The impulse is a change in momentum. The resulting
    /// velocity change will be impulse/mass. In the case of a massless body the velocity will be
    /// changed by the impulse value directly. 
    /// </summary>
    /// <param name="nbody">Nbody.</param>
    /// <param name="impulse">Impulse.</param>
    public void ApplyImpulse(NBody nbody, Vector3 impulse) {
		ApplyImpulseInternal(nbody, impulse, true);
	}

	/// <summary>
	/// Determine the velocity that will result if the impulse is applied BUT
	/// do not apply the impulse. This is typically used to preview an orbit
    /// change for an nbody that has an OrbitPredictor attached. 
	/// </summary>
	/// <returns>The for impulse.</returns>
	/// <param name="nbody">Nbody.</param>
	/// <param name="impulse">Impulse.</param>
	public Vector3 VelocityForImpulse(NBody nbody, Vector3 impulse) {
		return ApplyImpulseInternal(nbody, impulse, false);
	}

	private Vector3 ApplyImpulseInternal(NBody nbody, Vector3 impulse, bool apply) {
		// apply an impulse to the indicated NBody
		// impulse = step change in the momentum (p) of a body
		// delta v = delta p/m
		// If the spaceship is massless, then treat impulse as a change in velocity
		Vector3 velocity = Vector3.zero;
        if (nbody.engineRef.fixedBody != null) {
            // Can only apply impulse to OrbitU (not OrbitEllipse or OrbitHyper)
            OrbitUniversal orbitU = null;
            if (nbody.engineRef.fixedBody.keplerSeq != null) {
                orbitU = nbody.engineRef.fixedBody.keplerSeq.GetCurrentOrbit();
            } else if (nbody.engineRef.fixedBody.orbitU != null) {
                orbitU = nbody.engineRef.fixedBody.orbitU;
            }
            if (orbitU != null) {
                // OrbitU will do the velocity addition internally. 
                if (nbody.mass > 1E-6) {
                    impulse = impulse / (nbody.mass * massScale);
                }
                if (apply) {
                    orbitU.ApplyImpulse(impulse);
                }
                velocity = impulse + nbody.engineRef.fixedBody.orbitU.GetVelocity();
            }
        } else {
            switch (nbody.engineRef.bodyType) {
                case BodyType.MASSIVE:
                    velocity = worldState.integrator.GetVelocityForIndex(nbody.engineRef.index);
                    if (nbody.mass < 1E-6) {
                        velocity += impulse;
                    } else {
                        velocity += impulse / (nbody.mass * massScale);
                    }
                    if (apply) {
                        worldState.integrator.SetVelocityForIndex(nbody.engineRef.index, velocity);
                    }
                    break;

                case BodyType.MASSLESS:
                    velocity = worldState.masslessEngine.GetVelocity(nbody);
                    velocity += impulse;
                    if (apply) {
                        worldState.masslessEngine.SetVelocity(nbody, velocity);
                    }
                    break;

                case BodyType.FIXED:
                    // not yet supported
                    break;
            }
        }
		// will need to re-calc trajectories
		if (trajectoryPrediction) {
             trajectoryRestart = true;
        }
		return velocity;
	}

	/// <summary>
	/// Update the mass of a NBody in the integration while evolving.
	/// </summary>
	/// <param name="nbody">Nbody.</param>
	public void UpdateMass(NBody nbody) {
		// Can only do the update if the body had a mass (otherwise would need to shuffle from 
		// massless engine to mass-based engine - not currently supported)
		if (nbody.engineRef.bodyType == BodyType.MASSIVE) {
			worldState.m[nbody.engineRef.index] = nbody.mass * massScale;
		} else {
			Debug.LogWarning("Cannot set mass on a massless body");
		}
		if (trajectoryPrediction) {
			trajectoryRestart = true;
		}
	}

    /// <summary>
    /// Update the particle capture size of a NBody in the integration while evolving.
    /// Use the size from the provided NBody
    /// </summary>
    /// <param name="nbody">Nbody.</param>
    public void UpdateSize(NBody nbody) {
        // Only massive objects use their particle capture size
        if (nbody.engineRef.bodyType == BodyType.MASSIVE) {
            worldState.size2[nbody.engineRef.index] = nbody.size * nbody.size;
        } 
    }

    /// <summary>
    /// Gets the world center of mass in world space co-ordinates.
    /// </summary>
    /// <returns>The world center of mass.</returns>
    public Vector3 GetWorldCenterOfMass() {
		// called by editor prior to setup - need to find all the NBodies
		NBody[] nbodies = (NBody[]) Object.FindObjectsOfType(typeof(NBody));

		Vector3 cmVector = Vector3.zero;
		float mTotal = 0.0f; 
		foreach( NBody nbody in nbodies) {
			cmVector += ((float) nbody.mass) * nbody.transform.position;
			mTotal += ((float) nbody.mass);
		}
		return cmVector/mTotal;		
	}

	/// <summary>
	/// Gets the world center of mass velocity.
	/// </summary>
	/// <returns>The world center of mass velocity.</returns>
	public Vector3 GetWorldCenterOfMassVelocity() {
		// called by editor prior to setup - need to find all the NBodies
		NBody[] nbodies = (NBody[]) Object.FindObjectsOfType(typeof(NBody));

		Vector3 cmVector = Vector3.zero;
		float mTotal = 0.0f; 
		foreach( NBody nbody in nbodies) {
			cmVector += ((float) nbody.mass) * nbody.vel;
			mTotal += ((float) nbody.mass);
		}
		return cmVector/mTotal;		
	}

    /// <summary>
    /// Return the world time in the selected units as a string
    /// </summary>
    /// <returns></returns>
    public string GetScaledTimeFormatted() {
       
        return GravityScaler.GetWorldTimeFormatted(worldState.time, units); 
    }

    /// <summary>
    /// Return the world time in internal GE units
    /// </summary>
    /// <returns></returns>
    [System.Obsolete("Use the better named GetGETime")]
    public double GetWorldTime() {

        return worldState.time;
    }

    /// <summary>
    /// Return the world time in internal GE units
    /// </summary>
    /// <returns></returns>
    public double GetGETime() {
        return worldState.time;
    }

    /// <summary>
    /// Return the world state
    /// </summary>
    /// <returns></returns>
    public GravityState GetWorldState() {

        return worldState;
    }

    /// <summary>
    /// Get the internal INFO bits for display in the console. Not typically used by game code.
    /// </summary>
    /// <param name="nBody"></param>
    /// <returns></returns>
    public byte GetInfo(NBody nbody) {
        return worldState.GetInfo(nbody);
    }

    /// <summary>
    /// Get the timestep size from the last frame in scaled time units. 
    /// </summary>
    /// <returns></returns>
    public double GetLastScaledDt() {
        return lastWorldDt * GravityScaler.GetGameSecondPerPhysicsSecond();
    }

    /// <summary>
    /// Gets the initial energy.
    /// </summary>
    /// <returns>The initial energy.</returns>
        public float GetInitialEnergy() {
		return worldState.integrator.GetInitialEnergy(worldState);
	}

	/// <summary>
	/// Gets the current energy.
	/// </summary>
	/// <returns>The energy.</returns>
	public float GetEnergy() {
	    if (isSetup)
		return worldState.integrator.GetEnergy(worldState);
            else
                return 0f;
	}

    
    /// <summary>
    /// Register a particle system (with GravityParticles component) to be evolved via the GravityEngine.
    /// </summary>
    /// <param name="nbp">Nbp.</param>
    public void RegisterParticles(GravityParticles nbp) {
		worldState.gravityParticles.Add(nbp);
    }

    /// <summary>
    /// Remove a particle system from the Gravity Engine. 
    /// </summary>
    /// <param name="particles">Particles.</param>
    public void DeregisterParticles(GravityParticles particles) {
		worldState.gravityParticles.Remove(particles);
    }

    //-------------------------------------------------------------------------------
    // Maneuver wrappers

    public void AddManeuvers(List<Maneuver> mlist) {
        worldState.maneuverMgr.Add(mlist);
    }

    public void AddManeuver(Maneuver m) {
		worldState.maneuverMgr.Add(m);
	}

	public void RemoveManeuver(Maneuver m) {
        worldState.maneuverMgr.Remove(m);
	}

	public List<Maneuver> GetManeuvers(NBody nbody) {
		return worldState.maneuverMgr.GetManeuvers(nbody);
	}

    public void ClearManeuvers() {
        worldState.maneuverMgr.Clear();
    }

	//-------------------------------------------------------------------------------

    public void LogDump() {
        DumpAll(worldState);
    }

    public string DumpWorldState() {
        return DumpAll(worldState);
    }

	public string DumpAll(GravityState gs) {
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		sb.Append(string.Format("massScale={0} timeScale={1} lengthScale={2}\n", massScale, timeScale, lengthScale));
        sb.Append(string.Format("   time={0} physToWorldFactor={1} onRails={2} gameSecPerPhySec={3} engineDt={4}\n", 
                    gs.time, physToWorldFactor, worldState.IsOnRails(), GravityScaler.game_sec_per_phys_sec, engineDt));
        // Reaching into GravityState directly here is not great. Need to RF but have GetVelocity and gameObjects
        // to deal with
        sb.Append(worldState.DumpAll(gameNBodies, this));
		return sb.ToString();
	}

    //============================================================================================
    // Console commands: If there is a GEConsole in the scene, these commands will be availbale
    //============================================================================================

    private void AddConsoleCommands() {
        GEConsole.RegisterCommandIfConsole(new ClearCommand());
        GEConsole.RegisterCommandIfConsole(new DumpCommand());
        GEConsole.RegisterCommandIfConsole(new FastForwardCommand());
        GEConsole.RegisterCommandIfConsole(new GoCommand());
        GEConsole.RegisterCommandIfConsole(new InfoCommand());
        GEConsole.RegisterCommandIfConsole(new PauseCommand());
        GEConsole.RegisterCommandIfConsole(new SetTimeCommand());
        GEConsole.RegisterCommandIfConsole(new SingleStepCommand());
        GEConsole.RegisterCommandIfConsole(new TimeZoomCommand());

    }

    /// <summary>
    /// Dump GE state to console
    /// </summary>
    public class DumpCommand : GEConsole.GEConsoleCommand
    {
        public DumpCommand() {
            names = new string[] { "dump", "d" };
            help = "dump all bodies and their (r,v) info";
        }

        override
        public string Run(string[] args) {
            return GravityEngine.Instance().DumpWorldState();
        }
    }

    /// <summary>
    /// Show all possible position/velocity representations of an Nbody object.
    /// </summary>
    public class InfoCommand : GEConsole.GEConsoleCommand
    {
        public InfoCommand() {
            names = new string[] { "info", "i" };
            help = "dump assorted info about a game object by name ";
        }

        override
        public string Run(string[] args) {
            if (args.Length != 2) {
                return "info requires one argument (name of gameobject)";
            }
            GameObject go = GameObject.Find(args[1]);
            if (go == null) {
                return string.Format("Cannot find game object {0} in scene.", args[1]);
            }
            NBody nbody = go.GetComponent<NBody>();
            if (nbody == null) {
                return string.Format("Game object {0} does not have an NBody component.", args[1]);
            }
            GravityEngine ge = GravityEngine.Instance();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            // flags
            sb.Append(string.Format("Info for {0}\n", args[1]));
            // check flags
            sb.Append("    Info: ");
            if ((ge.GetInfo(nbody) & INACTIVE) > 0 ) {
                sb.Append("INACTIVE ");
            }
            if ((ge.GetInfo(nbody) & FIXED_MOTION) > 0) {
                sb.Append("FIXED_MOTION ");
            }
            sb.Append("\n");

            sb.Append(string.Format("   Scene:\n"));
            sb.Append(string.Format("     transform={0} nbody.mass={1} orbitDepth={2}\n", 
                nbody.transform.position, nbody.mass, nbody.GetOrbitDepth() ));
            // engine
            sb.Append(string.Format("   Engine:\n"));
            Vector3 pos = ge.GetPhysicsPosition(nbody);
            sb.Append(string.Format("     r={0}  r_mag={1} m\n", pos, pos.magnitude));
            Vector3 vel = ge.GetVelocity(nbody);
            sb.Append(string.Format("     v={0}  v_mag={1}\n", vel, vel.magnitude));
            sb.Append(string.Format("    engine mass={0}\n", ge.GetPhysicsMass(nbody)));
            // scaled
            GravityScaler.Units units = GravityEngine.Instance().units;
            sb.Append(string.Format("   Scaled: units={0}\n", units));
            pos = ge.GetPhysicsPosition(nbody);
            sb.Append(string.Format("     r={0} {1} r_mag={2} {1}\n",
                           pos, GravityScaler.LengthUnits(units), pos.magnitude));
            vel = ge.GetScaledVelocity(nbody);
            sb.Append(string.Format("     v={0} {1} v_mag={2} {1}\n", 
                            vel, GravityScaler.VelocityUnits(units), vel.magnitude));
            // SI
            sb.Append(string.Format("   SI:\n"));
            float conversion = (float) GravityScaler.PositionScaletoSIUnits();
            pos = conversion * ge.GetPhysicsPosition(nbody);
            sb.Append(string.Format("     r={0} m r_mag={1} m\n", pos, pos.magnitude ));
            conversion = (float)GravityScaler.VelocityScaletoSIUnits();
            vel = conversion * ge.GetVelocity(nbody);
            sb.Append(string.Format("     v={0} m/s  v_mag={1} m/s\n", vel, vel.magnitude));

            // Maneuvers
            List<Maneuver> maneuvers = ge.GetManeuvers(nbody);
            sb.Append(string.Format("   Maneuvers:\n"));
            if (maneuvers.Count > 0) {
                foreach (Maneuver m in maneuvers) {
                    sb.Append(string.Format("    t={0} type={1} dv={2} v={3}", m.worldTime, m.mtype, m.dV, m.velChange));
                }
            } else {
                sb.Append("    none\n");
            }
            return sb.ToString();
        }
    }
    /// <summary>
    /// Dump GE state to console
    /// </summary>
    public class PauseCommand : GEConsole.GEConsoleCommand
    {
        public PauseCommand() {
            names = new string[] { "pause", "p" };
            help = "pause GravityEngine";
        }

        override
        public string Run(string[] args) {
            GravityEngine.Instance().SetEvolve(false);
            return "paused\n";
        }
    }

    /// <summary>
    /// Dump GE state to console
    /// </summary>
    public class GoCommand : GEConsole.GEConsoleCommand
    {
        public GoCommand() {
            names = new string[] { "go" };
            help = "resume evolution";
        }

        override
        public string Run(string[] args) {
            GravityEngine.Instance().SetEvolve(true);
            return "running...\n";
        }
    }

    /// <summary>
    /// Dump GE state to console
    /// </summary>
    public class ClearCommand : GEConsole.GEConsoleCommand
    {
        public ClearCommand() {
            names = new string[] { "clear" };
            help = "clear all bodies in the engine";
        }

        override
        public string Run(string[] args) {
            GravityEngine.Instance().Clear();
            return "Cleared\n";
        }
    }

    /// <summary>
    /// Dump GE state to console
    /// </summary>
    public class TimeZoomCommand : GEConsole.GEConsoleCommand
    {
        public TimeZoomCommand() {
            names = new string[] { "timezoom", "z" };
            help = "set time zoom (run time): timezoom <number>";
        }

        override
        public string Run(string[] args) {
            if (args.Length != 2) {
                return "timezoom requires one argument";
            }
            float value = float.Parse(args[1]);
            GravityEngine.Instance().SetTimeZoom(value);
            return string.Format("Timezoom set to: {0}\n", value);
        }
    }

    /// <summary>
    /// Fast forward by a specified amount. Runs a full Nbody sim to get to that time.
    /// </summary>
    public class FastForwardCommand : GEConsole.GEConsoleCommand
    {
        public FastForwardCommand() {
            names = new string[] { "fastfwd", "ff" };
            help = "fast forward by the specified amount: fastfwd <time>";
        }

        override
        public string Run(string[] args) {
            if (args.Length != 2) {
                return "fast forward requires one argument";
            }
            float value = float.Parse(args[1]);
            GravityEngine.Instance().FastForward(value);
            return string.Format("Fast forward to: {0}\n", GravityEngine.Instance().GetScaledTimeFormatted());
        }
    }

    public class SetTimeCommand : GEConsole.GEConsoleCommand
    {
        public SetTimeCommand() {
            names = new string[] { "settime", "st" };
            help = "set time to the specified amount: settime <time>. Only available if all objects on rails";
        }

        override
        public string Run(string[] args) {
            if (args.Length != 2) {
                return "set time requires one argument";
            }
            if (!GravityEngine.Instance().GetWorldState().IsOnRails()) {
                return "Not all objects are on rails. Cannot set the time.\n";
            }
            float value = float.Parse(args[1]);
            GravityEngine.Instance().SetPhysicalTime(value);
            return string.Format("Set time to: {0}\n", GravityEngine.Instance().GetScaledTimeFormatted());
        }
    }

    /// <summary>
    /// Dump GE state to console
    /// </summary>
    public class SingleStepCommand : GEConsole.GEConsoleCommand
    {
        public SingleStepCommand() {
            names = new string[] { "step", "s" };
            help = "evolve one fixed update cycle";
        }

        override
        public string Run(string[] args) {
            GravityEngine.Instance().EvolveOneFixedUpdate();
            GravityEngine.Instance().SetEvolve(true);
            return "step\n";
        }
    }

}
