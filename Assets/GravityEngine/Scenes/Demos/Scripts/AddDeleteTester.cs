using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Add/delete tester.
/// Create and remove:
/// - massive, massless, Kepler orbiting bodies (on screen buttons)
/// - binary planets (key B)
/// - add a moon to the first massive planet (key M)
/// 
/// Used to demonstrate dynamic addition and as a test case for dynamic additions during development. 
/// 
/// Objects with trail renders require some special care. The trail renders in the prefabs are disabled and
/// are only enabled once the object has been added to GE AND enough time has gone by for the GE to have set the
/// tranform. 
/// 
/// </summary>
public class AddDeleteTester : MonoBehaviour {

	public GameObject orbitingPrefab;
    public GameObject binaryPrefab;
    public GameObject hyperPrefab;
    public GameObject fixedObject;
    public GameObject dustBallPrefab;

    public GameObject star;

	public  float maxRadius= 30f;
	public  float minRadius = 5f;
    public float moonRadius = 2f;
    public float fixedStarRadius = 20f;

    public float maxEccentricity = 0f;
    public float minEccentricity = 0f;
    public float mass = 0.001f;

    //! test flag to do random add/deletes
    public bool runChaosMonkey = false;

	private List<GameObject> massiveObjects; 
	private List<GameObject> masslessObjects; 
	private List<GameObject> keplerObjects;
    private List<GameObject> binaryObjects;
    private List<GameObject> moonObjects;
    private List<GameObject> hyperObjects;
    private List<GameObject> fixedObjects;
    private List<GameObject> dustObjects;

    private Color[] colors = { Color.red, Color.white, Color.blue, Color.cyan, Color.gray, Color.green, Color.magenta, Color.yellow};
	private int colorIndex = 0; 

	// Use this for initialization
	void Awake () {
		massiveObjects = new List<GameObject>();
		masslessObjects = new List<GameObject>();
		keplerObjects = new List<GameObject>();
        binaryObjects = new List<GameObject>();
        moonObjects = new List<GameObject>();
        hyperObjects = new List<GameObject>();
        fixedObjects = new List<GameObject>();
        dustObjects = new List<GameObject>();
    }

    // strings passed in by button functions in UI
    private const string MASSIVE = "massive";
    private const string MASSLESS = "massless";
	private const string KEPLER = "kepler";
    private const string BINARY = "binary";
    private const string MOON = "moon";
    private const string HYPER = "hyper";
    private const string FIXED = "fixed";
    private const string DUST = "dust";

    private string[] adTypes;

    //! counter to give unique names to objects for GEconsole debug
    private int globalAddCnt = 0; 

    void Start() {
        adTypes = new string[] { MASSIVE, MASSLESS, KEPLER, BINARY, MOON, HYPER, FIXED, DUST };
    }

    public void AddBody(string bodyType) {
        if (bodyType != MOON) {
            AddBodyToParent(bodyType, star);
        } else {
            // Add a moon to massive body
            if (massiveObjects.Count > 0) {
                AddBodyToParent(bodyType, massiveObjects[massiveObjects.Count - 1]);
            } else {
                Debug.LogWarning("Cannot add moon - no massive objects");
            }
        }
    }


    private void EllipseInit(GameObject go, NBody nbody, GameObject parent, string bodyType) {

        OrbitEllipse eb = go.GetComponent<OrbitEllipse>();
        eb.centerObject = parent;
        if (eb == null) {
            Debug.LogError("Failed to get OrbitEllipse from prefab:" + go.name);
            return;
        }
        // Awkward test code
        if (parent == star) {
            eb.paramBy = EllipseBase.ParamBy.AXIS_A;
            eb.a = Random.Range(minRadius, maxRadius);
            eb.inclination = Random.Range(-80f, 80f);
            eb.ecc = Random.Range(minEccentricity, maxEccentricity);
        } else {
            // moon test, so keep "a" small
            eb.paramBy = EllipseBase.ParamBy.AXIS_A;
            eb.a = moonRadius;
            eb.inclination = Random.Range(-80f, 80f);
            eb.ecc = 0;
        }
        if (bodyType == KEPLER) {
            eb.evolveMode = OrbitEllipse.evolveType.KEPLERS_EQN;
        }
        eb.Init();
        OrbitPredictor op = go.GetComponentInChildren<OrbitPredictor>();
        if (op != null) {
            op.SetNBody(nbody);
            op.SetCenterObject( parent );
        }
    }

    private void HyperInit(GameObject go, NBody nbody, GameObject parent) {
        OrbitHyper hyper = go.GetComponent<OrbitHyper>();
        if (hyper == null) {
            Debug.LogError("Failed to get OrbitHyper from prefab:" + go.name);
            return;
        }
        // Awkward test code
        if (parent == star) {
            hyper.perihelion = Random.Range(minRadius, maxRadius);
            // aribitrary - start at fixed distance from peri
            hyper.r_initial = 1.0f * hyper.perihelion;
            hyper.inclination = Random.Range(-80f, 80f);
            hyper.ecc = Random.Range(1.1f, 2f);
            hyper.centerObject = parent;
        }
        hyper.SetNBody(nbody);
        hyper.Init();
        OrbitPredictor op = go.GetComponentInChildren<OrbitPredictor>();
        if (op != null) {
            op.SetNBody(nbody);
            op.SetCenterObject(parent);
        }
    }

    /// <summary>
    /// Use orbit utils to put the dust ball in a random orbit.
    /// </summary>
    /// <param name="dustBall"></param>
    /// <param name="centerNBody"></param>
    private void SetOrbitForDustBall(DustBall dustBall, NBody centerNBody) {
        OrbitUtils.OrbitElements oe = new OrbitUtils.OrbitElements();
        // must use p to set orbit scale!
        oe.p = Random.Range(minRadius, maxRadius);
        oe.incl = Random.Range(-80f, 80f);
        oe.ecc = Random.Range(minEccentricity, maxEccentricity);
        oe.raan = Random.Range(0, 2.0f * Mathf.PI);
        oe.argp = Random.Range(0, 2.0f * Mathf.PI);
        Vector3d r = new Vector3d();
        Vector3d v = new Vector3d();
        OrbitUtils.COEtoRV(oe, centerNBody, ref r, ref v, false);
        dustBall.velocity = v.ToVector3();
        dustBall.transform.position = r.ToVector3();
    }

    private void AddBodyToParent(string bodyType, GameObject parent) {

        GameObject go;
        // getting long - RF later...
		if (bodyType == MASSLESS) {
            go = Instantiate(orbitingPrefab) as GameObject;
            go.name = string.Format("massless.{0}", globalAddCnt++);
			masslessObjects.Add(go);
		} else if (bodyType == KEPLER) {
            go = Instantiate(orbitingPrefab) as GameObject;
            go.name = string.Format("kepler.{0}", globalAddCnt++);
            keplerObjects.Add(go);
        } else if (bodyType == BINARY) {
            go = Instantiate(binaryPrefab) as GameObject;
            go.name = string.Format("binary.{0}", globalAddCnt++);
            binaryObjects.Add(go);
        } else if (bodyType == MASSIVE) {
            go = Instantiate(orbitingPrefab) as GameObject;
            go.name = string.Format("massive.{0}", globalAddCnt++);
            massiveObjects.Add(go);
        } else if (bodyType == HYPER) {
            go = Instantiate(hyperPrefab) as GameObject;
            go.name = string.Format("hyper.{0}", globalAddCnt++);
            hyperObjects.Add(go);
        } else if (bodyType == MOON) {
            go = Instantiate(orbitingPrefab) as GameObject;
            go.name = string.Format("moon.{0}", globalAddCnt++);
            moonObjects.Add(go);
        } else if (bodyType == FIXED) {
            go = Instantiate(fixedObject) as GameObject;
            go.name = string.Format("fixed.{0}", globalAddCnt++);
            fixedObjects.Add(go);
        } else if (bodyType == DUST) {
            go = Instantiate(dustBallPrefab) as GameObject;
            go.name = string.Format("dust.{0}", globalAddCnt++);
            SetOrbitForDustBall(go.GetComponent<DustBall>(), parent.GetComponent<NBody>());
            dustObjects.Add(go);
            GravityEngine.instance.RegisterParticles(go.GetComponent<GravityParticles>());
            return;
        } else {
            Debug.LogWarning("Do not understand string=" + bodyType);
            return;
        }

        NBody nbody = go.GetComponent<NBody>();
        if ((bodyType == MASSLESS) || (bodyType == MOON)) {
            nbody.mass = 0; 
        } else if (bodyType != FIXED) {
            nbody.mass = mass;
        }
        if (bodyType == FIXED) {
            // Add fixed stars in the X/Y plane at a fixed distance
            float theta = Random.Range(0, 2f*Mathf.PI);
            nbody.initialPos = new Vector3(fixedStarRadius * Mathf.Cos(theta),
                                    fixedStarRadius * Mathf.Sin(theta), 
                                    0);
        }
        go.transform.parent = parent.transform;

        if (bodyType == HYPER) {
            HyperInit(go, nbody, parent);

        } else if (bodyType != FIXED) {
            EllipseInit(go, nbody, parent, bodyType);
        }

        GravityEngine.instance.AddBody(go);

        TrailRenderer[] trails = go.GetComponentsInChildren<TrailRenderer>();
        foreach (TrailRenderer trail in trails) {
            trail.material.color = colors[colorIndex];
            colorIndex = (colorIndex + 1) % colors.Length;
            trail.enabled = true;
            trail.Clear();
        }

    }

     public void RemoveBody(string bodyType) {

		List<GameObject> bodyList = null;

        if (bodyType == MASSLESS) {
            bodyList = masslessObjects;
        } else if (bodyType == KEPLER) {
            bodyList = keplerObjects;
        } else if (bodyType == BINARY) {
            bodyList = binaryObjects;
        } else if (bodyType == HYPER) {
            bodyList = hyperObjects;
        } else if (bodyType == MOON) {
            bodyList = moonObjects;
        } else if (bodyType == FIXED) {
            bodyList = fixedObjects;
        } else if (bodyType == DUST) {
            bodyList = dustObjects;
        } else {
			bodyList = massiveObjects;
		}
		if (bodyList.Count > 0) {
			int entry = (int)(Random.Range(0, (float) bodyList.Count));
			GameObject toDestroy = bodyList[entry];
            // massive body may have a moon, binary has two NBody kids
            if ((bodyType == BINARY) || (bodyType == MASSIVE)) {
                // bodies will include the NBody on the toDestroy object
                NBody[] bodies = toDestroy.GetComponentsInChildren<NBody>();
                foreach (NBody nb in bodies) {
                    GravityEngine.instance.RemoveBody(nb.gameObject);
                    if (bodyType == MASSIVE) {
                        moonObjects.Remove(nb.gameObject);
                    }
                    Destroy(nb.gameObject);
                }
            } else if (bodyType == DUST) {
                GravityEngine.instance.DeregisterParticles(toDestroy.GetComponent<GravityParticles>());
                Destroy(toDestroy);
            } else { 
                GravityEngine.instance.RemoveBody(toDestroy);
                Destroy(toDestroy);
            }
            bodyList.RemoveAt(entry);
		} else {
			Debug.Log("All objects of that type removed.");
		}

	}

    void Update() {
        if (Input.GetKeyDown(KeyCode.C)) {
            GravityEngine.Instance().Clear();
            Debug.Log("Clear all bodies");
            List<GameObject>[] lists = { moonObjects,
                                        hyperObjects,
                                        fixedObjects,
                                        massiveObjects,
                                        masslessObjects,
                                        keplerObjects,
                                        binaryObjects,
                                        dustObjects};
            foreach (List<GameObject> l in lists) {
                foreach( GameObject g in l) {
                    Destroy(g);
                }
                l.Clear();
            }
        }
        if (runChaosMonkey) {
            RunChaosMonkey();
        }
    }

    /// <summary>
    /// Chaos Monkey to do random add/deletes at random time offsets. Goal it to make sure no errors
    /// or warning occur in the console. 
    /// </summary>

    private float nextAddTime = 0f;
    private void RunChaosMonkey() {
        if (Time.time > nextAddTime) {
            bool add = false;
            if (Random.Range(0f, 1f) < 0.5f) {
                add = true;
            }
            string type = adTypes[Random.Range(0, adTypes.Length)];
            if (add) {
                AddBody(type);
            } else {
                RemoveBody(type);
            }
            nextAddTime = Time.time + Random.Range(0f, 0.2f);
        }
    }

}
