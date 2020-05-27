using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;	// double Math 

/// <summary>
/// Provide known three-body solutions 

/// </summary>
public class SolutionServer  {

	public static string PREF_SOLN_NAME = "solution";
	public static string PREF_SET_NAME = "set";

	private Dictionary<string, ISolutionSet> solutionSets; 
	
	private string[] setNames; 
	
	private static SolutionServer instance; 
	
	public static SolutionServer Instance() {
		if (instance == null)
			instance = new SolutionServer();
		return instance;
	}
		
	private SolutionServer() {
	
		solutionSets = new Dictionary<string, ISolutionSet>();
		ISolutionSet s = new FigureEights();
		solutionSets.Add ( s.GetName(), s);
		s = new Choreos();
		solutionSets.Add ( s.GetName(), s);
		s = new Lagrange();
		solutionSets.Add ( s.GetName(), s);
		s = new BrouckeA();
		solutionSets.Add ( s.GetName(), s);
		s = new BrouckeR();
		solutionSets.Add ( s.GetName(), s);
		s = new Henon();
		solutionSets.Add ( s.GetName(), s);
		
		setNames = new string[solutionSets.Count];
		int count = 0; 
		foreach (string key in solutionSets.Keys) {
			setNames[count] = key;
			count += 1;
		}
		
	}
	
	public string[] GetSetNames() {
		return setNames;
	}
			
	public string[] GetSolutionNamesForSet(string set) {
		ISolutionSet solutionSet = null; 
		solutionSets.TryGetValue(set, out solutionSet);
		if (solutionSet == null) {
			Debug.LogError("Could not find set named " + set);
		}
		return solutionSet.GetSolutionNames();
	}

	// Direct retrieval of a specific solution given a setname and solution name

	public bool GetData(string setName, string name, ref double[,] x, ref double[,] v, 
					ref GravityEngine.Algorithm algorithm, ref float scale) {
		// check everything (will support search someday?)
		ISolutionSet solutionSet = null; 
		if (!solutionSets.TryGetValue(setName, out solutionSet)) {
			return false;
		}
		if (solutionSet != null) {
			// Debug.Log ("Load set=" + setName + " soln=" + name);
			return solutionSet.GetDataForSolutionName(name, ref x, ref v, ref algorithm, ref scale);
		} 
		Debug.LogError("Did not find solution="+name);
		return false;
	

	}
				
}
