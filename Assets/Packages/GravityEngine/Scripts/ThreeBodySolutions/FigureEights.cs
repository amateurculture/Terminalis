using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FigureEights : ISolutionSet {

	string[] eightStr = {
		// cut & paste from paper
		"M8 0.3471128135672417 0.532726851767674 6.3250 1 I.A",
		"S8 0.3393928985595663 0.536191205596924 6.2917 1 I.A",
		"NC1 0.2554309326049807 0.516385834327506 35.042 7 II.A",
		"NC2 0.4103549868164067 0.551985438720704 57.544 7 II.A",
		"O1 0.2034916865234370 0.5181128588867190 32.850 7 IV.A",
		"O2 0.4568108129224680 0.5403305086130216 64.834 7 IV.A",
		"O3 0.2022171409759519 0.5311040339355467 53.621 11 IV.A",
		"O4 0.2712627822083244 0.5132559436920279 55.915 11 IV.A",
		"O5 0.2300043496704103 0.5323028446350102 71.011 14 IV.A",
		"O6 0.2108318037109371 0.5174100244140625 80.323 17 IV.A",
		"O7 0.2132731670875545 0.5165434524230961 80.356 17 IV.A",
		"O8 0.2138543002929687 0.5198665707397461 81.217 17 III.A",
		"O9 0.2193730914764402 0.5177814195442197 81.271 17 III.A",
		"O10 0.2272123532714848 0.5200484344272606 82.671 17 IV.A",
		"O11 0.2199766127929685 0.5234338500976567 82.743 17 IV.A",
		"O12 0.2266987607727048 0.5246235168190009 83.786 17 III.A",
		"O13 0.2686383642458915 0.5227270888731481 88.674 17 III.A",
		"O14 0.2605047016601568 0.5311685141601564 89.941 17 IV.A",
		"O15 0.2899041109619139 0.5226240653076171 91.982 17 IV.A" };

	protected class Solution {
		public string designation;
		public string name; 
		// data for modern solutions
		public double x1dot; 
		public double y1dot; 
		
		// constructor that parses data dumped from paper2
		public Solution(string s) {
			string[] splitString = s.Split();
			name = splitString[0]; 
			designation = splitString[5];
			x1dot = double.Parse(splitString[1]);
			y1dot = double.Parse(splitString[2]);
		}
	}
	
	private Dictionary<string, Solution> solutionByName; 
	private string[] names;
	
	public FigureEights() {
		// Figure 8 style solutions from Suvakov (2013)
		solutionByName = new Dictionary<string,Solution>();
		names = new string[eightStr.Length];
		for (int i=0; i < eightStr.Length; i++ ) {
			Solution sol = new Solution(eightStr[i]); 
			solutionByName.Add(sol.name, sol);
			names[i] = sol.name;
		}
		
	}
		
	public string GetName() { 
		return "Figure Eights"; 
	}
	
	public string[] GetSolutionNames() {
		return names;
	}
	
	public bool GetDataForSolutionName(string name, ref double[,] x, ref double[,] v, 
										ref GravityEngine.Algorithm algorithm, ref float scale) {
		Solution s = null; 
		scale = 1.0f;
		if (!solutionByName.TryGetValue(name, out s)) {
		Debug.LogError("Solution name " + name + " not found.");
			return false;
		}
		GetData(s, ref x, ref v, ref algorithm);
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
