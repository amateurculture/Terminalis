using UnityEngine;
using System.Collections;

public interface ISolutionSet  {

	string GetName();
	
	string[] GetSolutionNames();
	
	bool GetDataForSolutionName(string name, ref double[,] x, ref double[,] v, 
								ref GravityEngine.Algorithm algorithm, ref float scale);
	
	
}
