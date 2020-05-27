using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Choreos : ISolutionSet {

	protected class Solution {
		public string designation;
		// data for modern solutions
		public double x1dot; 
		public double y1dot; 
		public float scale; // visual scale factor (tuned by hand to it fits on screen)
		
		public Solution(string d, double x1d, double y1d, float scale) {
			designation = d;
			x1dot = x1d;
			y1dot = y1d;
			this.scale = scale;
		}
	}
	
	private Dictionary<string, Solution> solutionByName; 
	private string[] names;
	
	public Choreos() {
		// Figure 8 style solutions from Suvakov (2013)
		solutionByName = new Dictionary<string,Solution>();
		
		// More precise values from Yang. 
		solutionByName.Add( "Butterfly I", new Solution("I.A.1",  0.306892758965492, 0.125506782829762, 1.4f));
		solutionByName.Add( "Butterfly II", new Solution("I.A.2", 0.39295, 0.09758, 1.4f));
		solutionByName.Add( "Bumblebee", new Solution("I.A.3", 0.18428, 0.58719, 1f));
		
		solutionByName.Add( "Moth I", new Solution("I.B.1",  0.46444, 0.39606, 1.0f));
		solutionByName.Add( "Moth II", new Solution("I.B.2", 0.43917, 0.45297, 0.95f));
		solutionByName.Add( "Butterfly III", new Solution("I.B.3", 0.40592, 0.23016, 1.2f));
		solutionByName.Add( "Moth III", new Solution("I.B.4", 0.383443534851074, 0.377363693237305, 1.0f));
		solutionByName.Add( "Goggles ", new Solution("I.B.5", 0.0833000564575194, 0.127889282226563, 1.2f));
		solutionByName.Add( "Butterfly IV", new Solution("I.B.6", 0.350112, 0.07934, 1.2f));
		solutionByName.Add( "Dragonfly", new Solution("I.B.7", 0.080584285736084, 0.588836087036132, 1.0f));
		
		solutionByName.Add( "Yarn", new Solution("II.B.1", 0.559064247131347, 0.349191558837891, 0.8f));
		
		solutionByName.Add( "Yin-Yang I", new Solution("II.C.2a", 0.513938054919243, 0.304736003875733, 1.0f));
		solutionByName.Add( "Yin-Yang Ib", new Solution("II.C.2b", 0.282698682308198, 0.327208786129952, 1.2f));
		solutionByName.Add( "Yin-Yang IIa", new Solution("II.C.3a", 0.41682, 0.33033, 1.0f));
		solutionByName.Add( "Yin-Yang IIb", new Solution("II.C.3b", 0.41734, 0.31310, 1.0f));
		
		names = new string[solutionByName.Count];
		int count = 0; 
		foreach (string key in solutionByName.Keys) {
			names[count] = key;
			count += 1;
		}
		
	}
	
	public string GetName() { 
		return "Choreographies"; 
	}
	
	
	public string[] GetSolutionNames() {
		return names;
	}
	
	public bool GetDataForSolutionName(string name, ref double[,] x, ref double[,] v, 
										ref GravityEngine.Algorithm algorithm, ref float scale) {
		Solution solution = null; 
		if (!solutionByName.TryGetValue(name, out solution)) {
			Debug.LogError("Solution name " + name + " not found.");
			return false;
		}
		scale = solution.scale;
		GetData(solution, ref x, ref v, ref algorithm);
		return true;

	}
	
	private void GetData(Solution s, ref double[,] x, ref double[,] v, ref GravityEngine.Algorithm algorithm) {
		
		algorithm = GravityEngine.Algorithm.AZTRIPLE;
		// using conservation CM velocity/position assign all values
		x[0,0] = 1;
		x[0,1] = 0;
		x[0,2] = 0;
		x[1,0] = -x[0,0];
		x[1,1] = 0;
		x[1,2] = 0;
		x[2,0] = 0;
		x[2,1] = 0;
		x[2,2] = 0;
		
		v[0,0] = s.x1dot;
		v[0,1] = s.y1dot;
		v[0,2] = 0;
		v[1,0] = v[0,0];
		v[1,1] = v[0,1];
		v[1,2] = 0;
		v[2,0] = -2*v[0,0];
		v[2,1] = -2*v[0,1];
		v[2,2] = 0;
		
	}
}
