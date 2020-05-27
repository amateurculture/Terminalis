using UnityEngine;
using System.Collections;
using System;	// double Math 

// Solutions from Euler (Linear) and Lagrange (Triangle)

public class Lagrange : ISolutionSet {
		
	public string GetName() { 
		return "Lagrange"; 
	}
	
	public string[] GetSolutionNames() {
		string[] names = new string[11];
		float eccentricity = 0.0f;
		for (int i=0; i <= 9; i++) {
			names[i] = string.Format("E=" + eccentricity.ToString("0.0"));
			eccentricity += 0.1f;
		}
		names[10] = "Linear";
		return names;
	}
	
	public bool GetDataForSolutionName(string name, ref double[,] x, ref double[,] v, 
										ref GravityEngine.Algorithm algorithm, ref float scale) {	
		// E=0.8 is unstable after several iterations with Hermite and AZT
		scale = 1.0f;
		if (name == "Linear") {
			EulerLinear(ref x, ref v); 
			// AZTRIPLE becomes unstable after several iterations.
		} else {
			string numStr = name.Replace("E=","");
			double eccentricity= 0;
			if (!Double.TryParse(numStr, out eccentricity)) {
				Debug.LogError("Solution string " + numStr + " not of the form E=<0..1>");
				return false;
			}
			LagrangeTriple(eccentricity, ref x, ref v); 
			return true; 
		}
		algorithm = GravityEngine.Algorithm.HERMITE8;
		return false;
	}
	
	
	// Create a Lagrane triple for m=1 and l=1
	private void LagrangeTriple(double e, ref double[,] x, ref double[,] v) {
		// Orbital Mechanics, Roy,  (5.39)
		//        (m_2^2 + m_3^2 + m_2 m_3)^(3/2)
		// M1 =   -------------------------------
		//          (m_1 + m_2 + m_3)^2
		//
		// For equal masses: M1 = m/sqrt(3)	 <= (3^3/2)/3^2 = 3(3/2 - 2) = 3(^(-1/2))
		double mcooeff = 1.0/Math.Sqrt(3.0); 
		
		// place objects at equal theta apart
		double dTheta = 2.0*Math.PI/3;
		double theta;
		
		for (int i=0; i < 3; i++)
		{
			theta = i * dTheta;
			x[i,0] = Math.Cos(theta); 
			x[i,1] = Math.Sin(theta); 
			x[i,2] = 0;
			// calulate velocity (assume equal mass, equal r, eccentric orbit)
			double vel = Math.Sqrt( mcooeff* (1-e)/(1+e));
			v[i,0] =  vel * Math.Cos (theta + Math.PI/2);
			v[i,1] =  vel * Math.Sin (theta + Math.PI/2);
			v[i,2] =  0;		
		}
		
	}

				
	private void EulerLinear(ref double[,] x, ref double[,] v) {
		
		// See Roy (5.45) 
		// For equal masses: 2 X^5 + 5 X^4 + 4 X^3 - 4 X^2 -5 X - 2 = 0 
		// so X=1 is a solution. masses at x=0, 1, 2
		for (int i=0; i < 3; i++) {
			for (int j=0; j < 3; j++) { 
				x[i,j] = 0.0;
				v[i,j] = 0.0;
			}
		}
		// offset by -1 to center in world
		x[0,0] = -1.0;
		x[1,0] = 0.0;
		x[2,0] = 1.0;
		// for m=1 omega=sqrt(5/12) (see Broucke et.al Cel. Mech. 23:p63-82 (1981))
		// There is more that could be done with pertubations...
		double omega = Math.Sqrt(5.0/12.0);
		v[0,1] =  omega;
		v[2,1] = -omega;
		Debug.Log("LInear setup");
	}
	
	
	
	// Incorrect implementation that looks interesting....
	private void WrongLagrangeTriple(double e, ref double[,] x, ref double[,] v) {
		// Roy (5.39)
		//        (m_2^2 + m_3^2 + m_2 m_3)^(3/2)
		// M1 =   -------------------------------
		//          (m_1 + m_2 + m_3)^2
		//
		// For equal masses: M1 = m/sqrt(3)	
		double mcooeff = 1.0/Math.Sqrt(3.0); 
		
		double dTheta = 2.0*Math.PI/3;
		double theta;
		
		for (int i=0; i < 3; i++)
		{
			theta = i * dTheta;
			// build with template of this object (color, size, etc.)
			x[i,0] = Math.Cos(theta); 
			x[i,1] = Math.Sin(theta); 
			x[i,2] = 0;
			// calulate velocity (assume equal mass, equal r, eccentricity)
			double vel = Math.Sqrt( mcooeff* (1-e)/(1+e));
			v[i,0] =  vel * Math.Cos (theta + Math.PI/2);
			v[i,0] =  vel * Math.Sin (theta + Math.PI/2); // <= this is the error, re-asigned xdot
			v[i,2] =  0;		
		}
		
	}
	
}	