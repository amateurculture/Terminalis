using UnityEngine;
using System.Collections;

/// <summary>
/// Orbit predictor.
/// An in-scene object that will determine the future orbit based on the current velocity. 
/// Depending on the velocity the orbit may be an ellipse or a hyperbola. This class
/// use an OrbitUniversal, since it can handle both cases. 
///
/// Orbit prediction is based on the two-body problem and is with respect to one other
/// body (presumably the dominant source of gravity for the affected object). The OrbitPredictor will
/// create an OrbitUniversal and update the classical orbital elements (COE) based on the current
/// position and velocity on each Update() cycle.
/// 
/// The orbital elements can be retreived from the OrbitUniversal via e.g
///     orbitPredictor.GetOrbitUniversal().eccentricity
///
/// The general N-body orbit prediction problem is significantly harder - it requires simulating the
/// entire scene into the future - re-computing whenever user input is provided. This is provided by
/// the Trajectory prediction sub-system. 
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class OrbitPredictor: MonoBehaviour  {

    //! Number of points to be used in the line renderer for the orbit plot
	public int numPoints = 100; 
    //! The body for which an orbit is to be predicted
	public GameObject body;
    //! The object which body is in orbit around. 
	public GameObject centerBody;
    //! Script code will set the velocity explicitly. Do not retreive automatically
    public bool velocityFromScript = false;

    //! display radius for hyperbola. If zero will use position of body to define orbit position
    public float hyperDisplayRadius = 0;

    public int numPlaneProjections;
    public Vector3 planeNormal = Vector3.forward;

    // velocity of body when set explicitly by script
    private Vector3 velocity;

	private NBody nbody;  
	private NBody aroundNBody; 
	private OrbitUniversal orbitU; 

	private LineRenderer lineR;

    private GravityEngine ge;

    void Awake() {
        orbitU = transform.gameObject.AddComponent<OrbitUniversal>();
        orbitU.SetNBody(nbody);
    }

    // Use this for initialization
    // Start() NOT Awake() to ensure that objects created on the fly can have centerBody etc. assigned
    // Awake is called from within Object.Instatiate()
    void Start() {
        nbody = body.GetComponent<NBody>();
        if (nbody == null) {
            Debug.LogWarning("Cannot show orbit - Body requires NBody component");
            return;
        }
        if (centerBody == null) {
            Debug.LogError("Center body is null " + gameObject.name);
        }
        if (body == null) {
            Debug.LogError("Body is null " + gameObject.name);
        }
        aroundNBody = centerBody.GetComponent<NBody>();
        orbitU.SetNBody(nbody);
        orbitU.centerNbody = aroundNBody;

        ge = GravityEngine.Instance();

        lineR = GetComponent<LineRenderer>();
        lineR.positionCount = numPoints+ 2 * numPlaneProjections;
    }

    // if other scripts enable/disable this OP then turn off line renderer as well
    void OnEnable() {
        if (lineR != null)
            lineR.enabled = true;
    }

    void OnDisable() {
        if (lineR != null)
            lineR.enabled = false;
    }

    public void SetNBody(NBody nbody) {
        orbitU.SetNBody(nbody);
    }

    public void SetCenterObject(GameObject newCenterBody) {
        centerBody = newCenterBody;
        aroundNBody = newCenterBody.GetComponent<NBody>();
        orbitU.SetNewCenter(aroundNBody);
    }

    public void SetVelocity(Vector3 v) {
        velocity = v;
    }

    public Vector3 GetVelocity() {
        return velocity;
    }

    public OrbitUniversal GetOrbitUniversal() {
        return orbitU;
    }

    /// <summary>
    /// Test method to allow setup for unit tests
    /// </summary>
    public void TestRunnerSetup() {
        Awake();
        Start();
        orbitU.Init();
        Update();
    }

 
    // Update is called once per frame
    void Update () {
        // on a deferred add (while running) may get an OP update before actually added to GE. This would be bad.
        if (nbody.engineRef == null)
            return;

        // TODO: optimize/accuracy: if on rails could use orbitU (or KS) directly
        // Now there is MapToRender MUST use physics and not transform position
        Vector3 centerPos = GravityEngine.Instance().GetPhysicsPosition(aroundNBody);

        // Is the resulting orbit and ellipse or hyperbola?
        bool mapToScene = true;
        Vector3d pos = ge.GetPositionDoubleV3(nbody);
        Vector3d vel;
        if (velocityFromScript) {
            vel = new Vector3d(velocity);
        } else {
            vel = ge.GetVelocityDoubleV3(nbody);
        }

        orbitU.InitFromRVT(pos, vel, ge.GetPhysicalTimeDouble(), aroundNBody, false);

        Vector3[] points = orbitU.OrbitPositions(numPoints, centerPos, mapToScene, hyperDisplayRadius);
        int totalPoints = numPoints + 2 * numPlaneProjections;
        if (numPlaneProjections > 0) {
            // Add lines to the inclination=0 plane of the orbit
            Vector3[] pointsWithProj = new Vector3[totalPoints];
            int projEvery = numPoints / numPlaneProjections;
            int p = 0;
            int orbitP = 0; 
            while (p < totalPoints) {
                pointsWithProj[p++] = points[orbitP];
                if ((orbitP % projEvery) == 0) {
                    // add a line to plane and back
                    pointsWithProj[p++] = Vector3.ProjectOnPlane(points[orbitP], planeNormal);
                    pointsWithProj[p++] = points[orbitP];
                }
                orbitP++;
            }
            lineR.SetPositions(pointsWithProj);
        } else {
            // just draw the orbit (no projection lines to the plane)
            lineR.SetPositions(points);
        }      
	}
}
