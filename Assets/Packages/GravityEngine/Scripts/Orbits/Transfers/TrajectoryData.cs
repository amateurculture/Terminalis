using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trajectory data.
/// Class to hold points in a trajectory in "sorted" order.
/// Each point contains a position, velocity and time. 
///
/// Insertion maintains order. This facilitates determining if
/// two trajectory paths get close enough to be considered as
/// "crossing". 
///
/// </summary>
public class TrajectoryData  {


	public class Tpoint {
		public Vector3 r;
		public Vector3 v;
		public float  t;
	}

    /// <summary>
    /// Container class to hold the points on two trajectories at an intercept point. 
    /// 
    /// Can be sorted in order of time at which they occur
    /// </summary>
	public class Intercept {
        public Tpoint tp1;
        public Tpoint tp2;
        public int tp1_index;
        public int tp2_index;
        public float dR;
        public float dV;
        public float dT;

        public void Log() {
            Debug.LogFormat("Intercept: tp1: r={0} t={1} tp2: r={2} t={3}", tp1.r, tp1.t, tp2.r, tp2.t);
        }
    }

    protected class InterceptComparer : IComparer<Intercept> { 

        public int Compare(Intercept i1, Intercept i2) {
            if (i1.tp1.t < i2.tp1.t)
                return -1;
            if (i1.tp1.t > i2.tp1.t)
                return 1;
            return 0;
        }
    }

    private InterceptComparer interceptComparer;

    private TpointCompare comparer;

    protected class TpointCompare : IComparer<Tpoint> {

		public int Compare(Tpoint o1, Tpoint o2) {
			Tpoint t1 = (Tpoint) o1;
			Tpoint t2 = (Tpoint) o2;
			// order by x, y then z
			if (t1.r.x < t2.r.x) {
				return -1; 
			} else if (t1.r.x > t2.r.x) {
				return 1;
			} else {
				if (t1.r.y < t2.r.y) {
					return -1; 
				} else if (t1.r.y > t2.r.y) {
					return 1;
				} else {
					if (t1.r.z < t2.r.z) {
						return -1; 
					} else if (t1.r.z > t2.r.z) {
						return 1;
					} else {
						return 0;
					}
				}
			}
		}
	}

    // private System.Collections.SortedList tpoints;
    private List<Tpoint> tpoints;
    private Tpoint lastPoint;

    public TrajectoryData() {
		comparer = new TpointCompare();
        tpoints = new List<Tpoint>();
		// tpoints = new SortedList(comparer);

        interceptComparer = new InterceptComparer();
	}


	public void AddPoint(Vector3 r, Vector3 v, float t) {
		Tpoint tpoint = new Tpoint();
		tpoint.r = r;
		tpoint.v = v;
		tpoint.t = t;
        // Slow launching rocket can end up at same point for two frames - avoid adding a duplicate
        // Performance: Is there a way to not add an if inside a highly used method?
        try {
            // tpoints.Add(tpoint, tpoint);
            tpoints.Add(tpoint);
        } catch(System.ArgumentException) {
            // just skip this point if it already exists
        }
    }

	public int Count() {
		return tpoints.Count;
	}

	public Tpoint GetByIndex(int index) {
		// return (Tpoint) tpoints.GetByIndex(index);
        return tpoints[index];
    }

    public void MoveAll(Vector3 moveBy) {
        foreach(Tpoint tp in tpoints) {
            tp.r += moveBy;
        }
    }

    // Can get a sequence of points as curves come to an intersection. Take the point
    // of closest approach.
    private List<Intercept> RemoveDuplicates(List<Intercept> intercepts, float deltaDistance, float deltaTime) {
		List<Intercept> unique = new List<Intercept>();
		List<Intercept>[] neighbours = new List<Intercept>[intercepts.Count];
		for (int i=0; i < intercepts.Count; i++) {
			neighbours[i] = new List<Intercept>();
		}
		bool[] hasNeighbour = new bool[intercepts.Count];

		// idea: start at top, find all entries from there that are within
		// deltaDistance and deltaTime mark them as neighbours
		// Each neighbourhood contributes the closest approach to the unique list
		const float expandDelta = 3f;	// awkward fudge to gather a neighbourhood. 

		// find neighbourhoods
		for (int i=0; i < intercepts.Count; i++) {
			if (!hasNeighbour[i]) {
				neighbours[i].Add(intercepts[i]);
				for (int j=i+1; j < intercepts.Count; j++) {
					if (!hasNeighbour[j]) {
						// a neighbour of my neighbour is my...
						Intercept toAdd = null;
						foreach(Intercept intercept in neighbours[i]) {
							if (Vector3.Distance(intercept.tp1.r, intercepts[j].tp1.r) < expandDelta*deltaDistance &&
								(Mathf.Abs(intercept.tp1.t - intercepts[j].tp1.t) < deltaTime)) {
								toAdd = intercepts[j];
								hasNeighbour[j] = true;
								break;
							} 
						}
						if (toAdd != null) {
							neighbours[i].Add(toAdd);
						}
					}
				}
			}
		}
		// find closest pair in each neighbourhood
		foreach(List<Intercept> nList in neighbours) {
			if (nList.Count > 0) {
				Intercept intercept = nList[0];
				float closestDistance = nList[0].dR;
				for (int i=1; i < nList.Count; i++) {
					if (nList[i].dR < closestDistance) {
						closestDistance = nList[i].dR;
						intercept = nList[i];
					}
				}
				unique.Add(intercept);
			}
		}
		return unique;
	}

    public void Sort() {
        tpoints.Sort(new TpointCompare());
    }

    /// <summary>
    /// Compare this trajectory data set to another and find those points that are within
    /// the specified deltaDistance. DeltaDistance denotes the seperation in EACH
    /// co-ordinate (i.e. they are within in a BOX of size delta distance) to reduce CPU
    /// cost of calculating exact distance.)
    /// 
    /// DeltaTime specifies the time within which multiple intercept points should be regarded
    /// as duplicates (in which case the intercept with the closest approach is used)
    ///
    /// List of intercepts is provided in time order (earliest first)
    /// </summary>
    /// <param name="tdata"></param>
    /// <param name="deltaDistance"></param>
    /// <param name="deltaTime"></param>
    /// <returns></returns>
    public List<Intercept> GetIntercepts(TrajectoryData tdata, float deltaDistance, float deltaTime) {
		List<Intercept> intercepts = new List<Intercept>();
		tpoints.Sort( new TpointCompare());
        tdata.Sort();
		// Concept: Lists are ordered so can walk each
		int i = 0; 
		int j = 0; 
		while ( (i < tpoints.Count) && (j < tdata.Count())) {
			Tpoint tp_i = (Tpoint) tpoints[i];
			Tpoint tp_j = (Tpoint) tdata.GetByIndex(j);
			// Debug.Log(string.Format("i={0} j={1} r_i={2} r_j={3}", i, j, tp_i.r, tp_j.r));
			if (Mathf.Abs(tp_i.r.x - tp_j.r.x) < deltaDistance) {
				if (Mathf.Abs(tp_i.r.y - tp_j.r.y) < deltaDistance) {
					if (Mathf.Abs(tp_i.r.z - tp_j.r.z) < deltaDistance) {
						Intercept intercept = new Intercept();
						intercept.tp1 = tp_i;
						intercept.tp2 = tp_j;
						intercept.dR = Vector3.Distance(tp_i.r, tp_j.r);
						intercept.dV = Vector3.Distance(tp_i.v, tp_j.v);
						intercept.dT = tp_i.t - tp_j.t;
						intercept.tp1_index = i; 
						intercept.tp2_index = j;
						intercepts.Add(intercept);
						i++;
						j++;
						continue;
					}
				}
			}
			if (comparer.Compare(tp_i,tp_j) > 0) {
				j++;
			} else {
				i++;
			}
		}
		List<Intercept> uniqueIntercepts = RemoveDuplicates(intercepts, deltaDistance, deltaTime);
        // sort
        uniqueIntercepts.Sort(interceptComparer);
		return uniqueIntercepts;
	}

}
