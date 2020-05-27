using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trajectory intercepts.
/// Given two trajectories that have recorded data, determine the points at
/// which they cross in space. There are two types:
///		intercept - the paths cross at the same time, the spaceship intercepts
///			the target and depending on deltaV may rendezvoud
///     match - the paths cross but at different times. The spaceship can 
///			choose to match the path of the target (with appropriate deltaV)
///         but the target is not at the match point
///
/// </summary>
public class TrajectoryIntercepts : MonoBehaviour {

	public Trajectory	spaceship;
	public Trajectory	target;

	//! Object to place at a trajectory intercept location
	public GameObject	interceptSymbol;

	//! Object to place at a trajectory rendezvous location
	public GameObject rendezvousSymbol;

	private List<TrajectoryData.Intercept>  intercepts;
	private List<GameObject> markers;

	void Start() {
		markers = new List<GameObject>();
	}

    /// <summary>
    /// Computes intercepts and marks them intercepts.
    /// </summary>
    /// <param name="deltaDistance">Distance that separates sets of intercept points.</param>
    /// <param name="deltaTime">Time that separates sets of intercept points.</param>
    /// <param name="rendezvousDT">If less than this dT, regard as rendezvous, otherwise a traj. match</param>
    public void ComputeAndMarkIntercepts(float deltaDistance, float deltaTime, float rendezvousDT) {
		ClearMarkers();
		intercepts = 
				spaceship.GetData().GetIntercepts(target.GetData(), deltaDistance, deltaTime);
		int count = 0; 
        // with long trajectory times can get same interesction on subsequent orbits. Keep choices simple
        // and pick the two earliest intercepts. 
		foreach (TrajectoryData.Intercept intercept in intercepts) {
			count += 1;
			GameObject marker = null;
			if (Mathf.Abs(intercept.dT) < rendezvousDT) {
				marker = Instantiate(interceptSymbol) as GameObject;
			} else {
				marker = Instantiate(rendezvousSymbol) as GameObject;
			}
			marker.transform.position = intercept.tp1.r;
			markers.Add(marker);
		}
	}

	public List<TrajectoryData.Intercept> GetIntercepts() {
		return intercepts;
	}

	public void ClearMarkers() {
		// clear old markers
		foreach (GameObject marker in markers) {
			Destroy(marker);
		}
		markers.Clear();


	}
}
