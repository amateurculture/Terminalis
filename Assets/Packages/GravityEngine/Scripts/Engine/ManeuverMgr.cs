using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maneuver Manager
/// Handles the GE delegation of maneuver lists for NBody objects in the
/// GE workflow. 
/// 
/// All main methods are typically called from wrappers in GE and then 
/// delegated to here. This allows some separation of concerns. 
/// 
/// </summary>
public class ManeuverMgr  {

	private SortedList<Maneuver, Maneuver> maneuvers; 

	public ManeuverMgr () {
		Maneuver mForCompare = new Maneuver();
		maneuvers = new SortedList<Maneuver, Maneuver>(mForCompare);
	}


    public ManeuverMgr(ManeuverMgr copyFrom) {
        Maneuver mForCompare = new Maneuver();
        maneuvers = new SortedList<Maneuver, Maneuver>(mForCompare);
        foreach (Maneuver m in copyFrom.maneuvers.Keys ) {
            maneuvers.Add(m, m);
        }
    }

    public void Add(Maneuver maneuver) {
		maneuvers.Add(maneuver, maneuver);
	}

    public void Add(List<Maneuver> mlist) {
        foreach (Maneuver m in mlist) {
            maneuvers.Add(m, m);
        }
    }

    /// <summary>
    /// Return a list of all maneuvers executing earlier than <time>.
    /// </summary>
    ///
    /// <param name="time">The time</param>
    ///
    /// <returns>List of all maneuvers that occur before time specified</returns>
    public List<Maneuver> ManeuversUntil(float time) {
		List<Maneuver> list = new List<Maneuver>();
		foreach (Maneuver m in maneuvers.Keys) 
		{
			if (m.worldTime < time) {
				list.Add(m);
			} else {
				break;
			}
		}
		return list;
	}


	public void Executed(Maneuver m, bool isCopy) {
        // maneuvers in a copy of the main state are "what if" projections so do not notify about
        // completed maneuver
		if (!isCopy && m.onExecuted != null) {
			m.onExecuted(m);
		}
		Remove(m);
	}

	public void Remove(Maneuver m) {
		bool removed = maneuvers.Remove(m);
		if (!removed) {
			Debug.Log("Could not remove maneuver " + m.LogString());
		}
	}

	public List<Maneuver> GetManeuvers(NBody nbody) {
		List<Maneuver> list = new List<Maneuver>();
		foreach (Maneuver m in maneuvers.Keys) {
			if (m.nbody == nbody) {
				list.Add(m);
			}
		}
		return list;
	}

    public void Clear() {
        maneuvers.Clear();
    }

    /// <summary>
    /// Indicate if there are any maneuvers
    /// </summary>
    /// <returns></returns>
    public bool HaveManeuvers() {
        return maneuvers.Count > 0; 
    }

}
