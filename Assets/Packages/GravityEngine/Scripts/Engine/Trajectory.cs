using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class Trajectory : MonoBehaviour {

	// track point and time to ensure that we remove points older than current time.
	class TrajPoint {
		public Vector3 p;
		public float worldTime;

		public TrajPoint(Vector3 p, float t) {
			this.p = p; 
			worldTime = t;
		}

	};

	class TimeMark {
		public GameObject marker;
		public GameObject textObject;
		public float time;
	};

	//! Distance interval for points to be added to the renderer
	public float minVertexDistance = 0.1f;

	//! Object to be created every timeMarkInterval
	public GameObject timeMarkerPrefab; 

	//! Interval between time markers
	public float timeMarkInterval = 2.0f; 

	//! Prefab to be added for text labels showing time along trajectory.
	public GameObject textPrefab; 

	//! Rotate text to align with trajectory
	public bool rotateText;

	// time marking
	private float lastTimeMark;
	private List<TimeMark> timeMarks;
	private List<Text> textMarks;

	private LineRenderer lineRenderer; 

	// use an array as a ring buffer to avoid shuffling as points are added. 
	private List<TrajPoint> points;
    private Vector3[] positions;

    //! maximum number of points in the line renderer
    public int maxPoints = 1000; 

    // record trajectory data (including v and t) for intercept calculations
    public bool recordData; 
	private TrajectoryData trajectoryData;

	private Vector3 lastPoint; 
	private Canvas canvas;

    private GravityEngine ge; 


	// Use this for initialization
	void Start () {
		if (GetComponent<NBody>() != null) {
			Debug.LogWarning("Trajectory should be attached to a child of an Nbody (not directly)");
		}

		// If text, check for canvas
		if (textPrefab != null) {
			if (GravityEngine.instance.trajectoryCanvas == null) {
				Debug.LogError("Text labels require GravityEngine trajectory Canvas be configured");
			} else {
				canvas = GravityEngine.instance.trajectoryCanvas.GetComponent<Canvas>();
				if (canvas == null) {
					Debug.LogError("No canvas component on " + GravityEngine.instance.trajectoryCanvas.name);
				}
			}
		}
        ge = GravityEngine.Instance();

    }

    // timeMarks are at top level in scene (so they keep an independent position)
    void OnDestroy() {
		Cleanup();
	}

	public void Cleanup() {
		if (timeMarks == null)
			return;

		foreach(TimeMark m in timeMarks) {
			Destroy(m.marker);
			if (m.textObject != null) {
				Destroy(m.textObject);
			}
		}
	}

	public TrajectoryData GetData() {
		return trajectoryData;
	}

	public void Init (float worldTime) {
		Cleanup();
		lineRenderer = GetComponent<LineRenderer>();
		points = new List<TrajPoint>();
		lastPoint = new Vector3(float.MaxValue, 0, 0);
		lineRenderer.positionCount = 2;
		lineRenderer.SetPosition(0, Vector3.zero);
		lineRenderer.SetPosition(1, Vector3.up);
		timeMarks = new List<TimeMark>();
		lastTimeMark = worldTime;
		trajectoryData = new TrajectoryData();
    }

    /// <summary>
    /// Adds a point to the trail. Points are added if they are minVertexDistance from the last point.
    /// </summary>
    /// <param name="point">Point.</param>
    public void AddPoint(Vector3 point, float pointTime, float currentTime) {

        // update trail?
        if (Vector3.Distance(point, lastPoint) < minVertexDistance)
            return;

        // add a time marker?
        if (timeMarkerPrefab != null) {
            if ((pointTime - lastTimeMark) > timeMarkInterval) {
                // add a time marker
                TimeMark mark = new TimeMark();
                mark.marker = Instantiate(timeMarkerPrefab);
                if (ge.markerParent != null) {
                    mark.marker.transform.SetParent(ge.markerParent.transform);
                }
                mark.marker.transform.position = point;
                mark.time = pointTime;
                timeMarks.Add(mark);
                lastTimeMark = pointTime;
                // align the game object so it's Z axis is along the trajectory
                Vector3 trajVector = point - lastPoint;
                mark.marker.transform.rotation = Quaternion.FromToRotation(Vector3.forward, trajVector);
                if (textPrefab != null && canvas != null) {
                    mark.textObject = Instantiate(textPrefab);
                    mark.textObject.transform.position = point;
                    Text text = mark.textObject.GetComponent<Text>();
                    text.text = string.Format("{0:F1}", pointTime);
                    mark.textObject.transform.SetParent(canvas.transform);
                    if (rotateText) {
                        mark.textObject.transform.rotation = mark.marker.transform.rotation;
                    }
                }
            }
        }
        lastPoint = point;

		// add new point to traj
		TrajPoint tp = new TrajPoint(point, pointTime);
		points.Add(tp);

	}

    /// <summary>
    /// Move a trajectory, it's data and time/text markers. 
    /// 
    /// Used by GravityEngine.MoveAll() 
    /// 
    /// Do not call from game code. 
    /// </summary>
    /// <param name="moveBy"></param>
    public void MoveAll(Vector3d moveBy3d) {
        Vector3 moveBy = moveBy3d.ToVector3();
        for (int i = 0; i < points.Count; i++) {
            points[i].p += moveBy;
        }
        foreach (TimeMark tm in timeMarks) {
            if (tm.marker != null) {
                tm.marker.transform.position += moveBy;
            }
            if (tm.textObject != null) {
                tm.textObject.transform.position += moveBy;
            }
        }
        if (trajectoryData != null) {
            trajectoryData.MoveAll(moveBy);
        }
        UpdateLineRenderer();
    }

    void Update() {
        // Better performance with bulk set of positions in LR
        if (ge.trajectoryPrediction && (points != null)) {
            float currentTime = ge.GetPhysicalTime();
            // remove any points older than current time
            while ((points.Count > 0) && (points[0].worldTime < currentTime)) {
                points.RemoveAt(0);
            }

            // add/remove a time marker?
            if (timeMarkerPrefab != null) {

                // remove any old time markers
                while ((timeMarks.Count > 0) && (timeMarks[0].time < currentTime)) {
                    Destroy(timeMarks[0].marker);
                    if (timeMarks[0].textObject != null) {
                        Destroy(timeMarks[0].textObject);
                    }
                    timeMarks.RemoveAt(0);
                }
            }

            UpdateLineRenderer();
        }
    }

    private void UpdateLineRenderer() {
        if (points != null) {
            // TODO - performance. Specify a point limit and allocate once
            positions = new Vector3[points.Count];
            for (int i = 0; i < points.Count; i++) {
                positions[i] = ge.MapToScene( points[i].p);
            }
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(positions);
        }
    }

    /// <summary>
    /// Add data point to the detailed record of the trajectory. These are later used to determine trajectory intercepts. 
    /// </summary>
    /// <param name="r"></param>
    /// <param name="v"></param>
    /// <param name="t"></param>
    public void AddData(Vector3 r, Vector3 v, float t) {
		trajectoryData.AddPoint(r, v, t);
	}
}
