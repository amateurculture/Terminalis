using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Solar system.
/// This component is attached to a game object that is the Sun. It then manages the children 
/// of the Sun allowing editing and global scaling of planets, asteroids and comets. 
///
/// The atributes of the planets can be altered (for "what if" experiments) by editing the 
/// children game objects that are created. They can also be reset to defaults by a call to 
/// RestoreDefaults()
/// </summary>
[RequireComponent(typeof(NBody))]
public class SolarSystem : MonoBehaviour {

	public enum Type { PLANET, ASTEROID, COMET}; 

	// Fields that need to run code when updated have setters. Private field is serialized
	// so that Undo works from the Editor. 

	[SerializeField]
	private float _planetScale = 1f;
	//! Planet scale in Unity units per 1000km
	public float planetScale {
		get { return _planetScale; }
		set { UpdatePlanetScale(value);}
	}

	[SerializeField]
	private float _epochTime = 2016f;
	public float epochTime {
		get { return _epochTime; }
		set { if (value != _epochTime) UpdateEpochTime(value);}
	}

	//! Prefab to be used to create bodies in the SolarSystem via the editor or script calls
	public GameObject planetPrefab;
	public GameObject asteroidPrefab;
	public GameObject cometPrefab;

    // Field for editor script
    public bool epochFoldout = false;

    //!Game object for the Sun. It's NBody component is attached to this script 
    private GameObject sun; 
	private NBody sunNbody; 

	public const float mass_sun = 1.989E6f; // really E30 but units are E24 kg

	private bool inited = false;

	public void Init() {
		if (!inited) {
			sun = transform.gameObject;
			sunNbody = GetComponent<NBody>();
			if (sunNbody == null) {
				Debug.LogError("Sun needs to have an NBody component");
			}
			sunNbody.mass = mass_sun;
			inited = true;
		}
	}

	void Awake() {
		Init();
	}


	/// <summary>
	/// Sets the starting epoch time and adjusts the phase parameters of all bodies in the system accordingly. 
	///
	/// Phase is set by using the original orbit data reference. Any hand-tuning of phase by manual changes
	/// in the editor will be lost. 
    /// 
    /// This method is typically called by the scene inspector. Not at run-time.
	/// </summary>
	/// <param name="newTime">New time.</param>
	public void UpdateEpochTime(float newTime) {
		_epochTime = newTime;
		foreach (EllipseBase ellipseBase in EllipsesInSystem()) {
			SolarBody sbody = ellipseBase.transform.gameObject.GetComponent<SolarBody>();
			if (sbody != null) {
				SetPhase(ellipseBase, sbody);
				ellipseBase.Init();
			} 
		}

	}

	public void UpdatePlanetScale(float scale) {
		_planetScale = scale;
		foreach (EllipseBase ellipseBase in EllipsesInSystem()) {
			SolarBody sbody = ellipseBase.transform.gameObject.GetComponent<SolarBody>();
			if (sbody != null) {
				ScaleModel( ellipseBase.transform.gameObject, sbody);
			} 
		}
	}

	private List<EllipseBase> EllipsesInSystem() {
		List<EllipseBase> ellipses = new List<EllipseBase>();
		NBody[] nbodies = GetComponentsInChildren<NBody>();
		foreach (NBody nbody in nbodies) {
			EllipseBase ellipseBase = nbody.transform.gameObject.GetComponent<EllipseBase>();
			if (ellipseBase != null) {
				ellipses.Add(ellipseBase);
			}
		}
		return ellipses;
	}

	private void ScaleModel(GameObject bodyGO, SolarBody sbody) {
		// is there a direct child with a sphere? If so, set scale from size
		MeshRenderer renderer = bodyGO.GetComponentInChildren<MeshRenderer>();
		if (renderer != null && sbody.radiusKm > 0) {
			// scale is per 10^5 km
			float scaleSize = _planetScale * sbody.radiusKm/10000f;
			renderer.transform.localScale = scaleSize * Vector3.one;
		}
	}


	private void SetPhase(EllipseBase ellipseBase, SolarBody sbody) {
		sbody.SetEllipseForEpoch(_epochTime, ellipseBase);
	}


	public GameObject AddObject(Type type, SolarBody sbody) {
		// Can be called from Editor
		Init();
		GameObject prefab = null; 
		switch(type) {
		case Type.PLANET:
			prefab = planetPrefab;
			break;
		case Type.ASTEROID:
			prefab = asteroidPrefab;
			break;
		case Type.COMET:
			prefab = cometPrefab;
			break;
		default:
			break;
		}
		if (prefab == null) {
			Debug.LogError("Prefab not defined for " + type);
			return null;
		}
		if (sun == null) {
			Debug.LogError("Sun game object not defined.");
			return null;
		}
		GameObject bodyGO = Instantiate(prefab) as GameObject;
		bodyGO.transform.parent = sun.transform;
		// check there is an NBody
		NBody nbody = bodyGO.GetComponent<NBody>();
		if (nbody == null) {
			Debug.LogError("Prefab must have NBody");
			return null;
		}
		// check there is a SolarBody, fill with data and init orbit
		SolarBody solarBody = bodyGO.GetComponent<SolarBody>();
		if (solarBody == null) {
			Debug.LogError("Prefab must have SolarBody component");
			return null;
		}
		solarBody.CopyFrom(sbody);
		InitOrbit(solarBody);
		solarBody.solarSystem = this;

		bodyGO.name = solarBody.name;
		nbody.mass = solarBody.mass_1E24;
		// Prefab may optionally have a UI Text object attached. If it does, set the name
		Text[] text = bodyGO.GetComponentsInChildren<Text>();
		if (text.Length > 0) {
			text[0].text = solarBody.name;
		}
		return bodyGO;
	}

	public void InitOrbit(SolarBody sbody) {
		// check there is an Ellipse Base
		OrbitEllipse orbitEllipse = sbody.GetComponent<OrbitEllipse>();
		if (orbitEllipse != null) {
            orbitEllipse.InitFromSolarBody(sbody);
            ScaleModel(sbody.transform.gameObject, sbody);
            SetPhase(orbitEllipse, sbody);
            return;
 		}
        // TODO: Need to work through SetPhase stuff. Likely ditch OrbitEllipse completely?
        // - this would break backwards compatibility for epoch updates, so NO!
        //OrbitUniversal orbitU = sbody.GetComponent<OrbitUniversal>();
        //if (orbitU != null) {
        //    orbitU.InitFromSolarBody(sbody);
        //    ScaleModel(sbody.transform.gameObject, sbody);
        //    SetPhase(orbitU, sbody);
        //    return;
        //}
        Debug.LogError("Prefab must have OrbitEllipse component");
        return;
    }

    //*****************************************************************
    // Run-time methods
    //*****************************************************************

    /// <summary>
    /// Run-time update of the solar system time. The new time must be a time later
    /// than the initial start time of the solar system.
    /// 
    /// This API makes use of @GravityEngine#SetPhysicalTime. This requires that ALL
    /// objects in the scene be "on-rails" i.e. have an OrbitXXX component and be in Kepler
    /// evolution mode. 
    /// 
    /// </summary>
    /// <param name="year"></param>
    /// <param name="month"></param>
    /// <param name="day"></param>
    /// <param name="dayFrac"></param>
    public void SetTime(int year, int month, int day, float dayFrac) {
        // TODO - use dayFrac

        // C# DateTime/TimeSpan to find the number of seconds to add
        System.DateTime newTime = new System.DateTime(year, month, day);
        System.DateTime startTime = SolarUtils.DateForEpoch(_epochTime);
        double secsFromStart = (newTime - startTime).Ticks / (1E7);

        if (secsFromStart < 0) {
            Debug.LogError("Cannot evolve earlier than solar system start time.");
        }
        Debug.LogFormat("Secs from start={0} phys={1} newPhys={2}", 
            secsFromStart, 
            GravityEngine.Instance().GetPhysicalTimeDouble(),
            GravityScaler.WorldSecsToPhysTime(secsFromStart));
        GravityEngine.Instance().SetPhysicalTime(GravityScaler.WorldSecsToPhysTime(secsFromStart));
    }

   public float GetStartEpochTime() {
        return _epochTime;
   }

}
