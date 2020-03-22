using UnityEngine;
using System.Collections;

public class Henon : ISolutionSet {

	public string GetName() { 
		return "Henon"; 
	}
	
	public string[] GetSolutionNames() {
		string[] names = new string[13];
		for (int i=0; i < 13; i++) {
			names[i] = "H" + (i+1);
		}
		return names;
	}	
	// Update is called once per frame
	public bool GetDataForSolutionName(string name, ref double[,] x, ref double[,] v, 
	                                   ref GravityEngine.Algorithm algorithm, ref float scale) {
		// data from http://suki.ipb.ac.rs/3body/ 
		// original work: R. Broucke, On relative periodic solutions of the planar general three-body problem, 
		// Celest. Mech. 12, 439 (1975).
		scale = 0.8f;
		algorithm = GravityEngine.Algorithm.AZTRIPLE;
		string numStr = name.Replace("H", "");
		int num = 0; 
		if (!int.TryParse(numStr, out num)) {
			Debug.LogError("Solution string " + numStr + " not of the form A<1-16>");
			return false;
		};
		num -= 1; // 0 based
		if ( num < 0 || num > 15) {
			Debug.LogError("Solution number " + numStr + " out of range");
			return false; 
		}
		switch(num) {
		case 0:
			// var text = 'NAME: Henon 2 %
			x[0,0] =  -1.0207041786;
			x[1,0] = 2.0532718983;
			x[2,0] = -1.0325677197;
			v[0,1] = 9.1265693140;
			v[1,1] = 0.0660238922;
			v[2,1] = -9.1925932061 ;
			break;
		case 1:
			// var text = 'NAME: Henon 3 %
			x[0,0] =  -0.9738300580;
			x[1,0] = 1.9988948637;
			x[2,0] = -1.0250648057;
			v[0,1] = 4.3072892019;
			v[1,1] = 0.1333821680;
			v[2,1] = -4.4406713699 ;
			break;
		case 2:
			// var text = 'NAME: Henon 4 %
			x[0,0] =  -0.9418961718;
			x[1,0] = 1.9620504351;
			x[2,0] = -1.0201542632;
			v[0,1] = 3.4407426089;
			v[1,1] = 0.1608086204;
			v[2,1] = -3.6015512293 ;
			break;
		case 3:
			// var text = 'NAME: Henon 5 %
			x[0,0] =  -0.9353825545;
			x[1,0] = 1.9545571553;
			x[2,0] = -1.0191746008;
			v[0,1] = 3.3166932522;
			v[1,1] = 0.1654488998;
			v[2,1] = -3.4821421520 ;
			break;
		case 4:
			// var text = 'NAME: Henon 6 %
			x[0,0] =  -0.9213822197;
			x[1,0] = 1.9384775293;
			x[2,0] = -1.0170953096;
			v[0,1] = 3.0865413013;
			v[1,1] = 0.1745212698;
			v[2,1] = -3.2610625710 ;
			break;
		case 5:
			// var text = 'NAME: Henon 7 %
			x[0,0] =  -0.8961968933;
			x[1,0] = 1.9096454316;
			x[2,0] = -1.0134485383;
			v[0,1] = 2.7626477723;
			v[1,1] = 0.1880576473;
			v[2,1] = -2.9507054196 ;
			break;
		case 6:
			// var text = 'NAME: Henon 8 %
			x[0,0] =  -0.8630680168;
			x[1,0] = 1.8719091735;
			x[2,0] = -1.0088411567;
			v[0,1] = 2.4494921664;
			v[1,1] = 0.2009780545;
			v[2,1] = -2.6504702209 ;
			break;
		case 7:
			// var text = 'NAME: Henon 9 %
			x[0,0] =  -0.8406614871;
			x[1,0] = 1.8465095288;
			x[2,0] = -1.0058480417;
			v[0,1] = 2.2849981945;
			v[1,1] = 0.2067721191;
			v[2,1] = -2.4917703136 ;
			break;
		case 8:
			// var text = 'NAME: Henon 10 %
			x[0,0] =  -0.8189887884;
			x[1,0] = 1.8220335296;
			x[2,0] = -1.0030447412;
			v[0,1] = 2.1515812682;
			v[1,1] = 0.2101820298;
			v[2,1] = -2.3617632980 ;
			break;
		case 9:
			// var text = 'NAME: Henon 11 %
			x[0,0] =  -0.8124282691;
			x[1,0] = 1.8146415679;
			x[2,0] = -1.0022132988;
			v[0,1] = 2.1152813609;
			v[1,1] = 0.2107937022;
			v[2,1] = -2.3260750631 ;
			break;
		case 10:
			// var text = 'NAME: Henon 12 %
			x[0,0] =  -0.8081346489;
			x[1,0] = 1.8098115895;
			x[2,0] = -1.0016769406;
			v[0,1] = 2.0924392285;
			v[1,1] = 0.2110889451;
			v[2,1] = -2.3035281737 ;
			break;
		case 11:
			// var text = 'NAME: Henon 30 %
			x[0,0] =  -0.1539168309;
			x[1,0] = 1.1825813762;
			x[2,0] = -1.0286645453;
			v[0,1] = 1.2071375933;
			v[1,1] = -0.1445299063;
			v[2,1] = -1.0626076870 ;
			break;
		case 12:
			// var text = 'NAME: Henon 31 %
			x[0,0] =  -0.1184831386;
			x[1,0] = 1.1644627554;
			x[2,0] = -1.0459796167;
			v[0,1] = 1.2002882491;
			v[1,1] = -0.1648687837;
			v[2,1] = -1.0354194654 ;
			break;
		case 13:
			// var text = 'NAME: Henon 39 %
			x[0,0] =  0.3492734869;
			x[1,0] = 1.1741828108;
			x[2,0] = -1.5234562977;
			v[0,1] = 1.1785487598;
			v[1,1] = -0.4017802411;
			v[2,1] = -0.7767685187 ;
			break;
		case 14:
			// var text = 'NAME: Henon 41 %
			x[0,0] =  0.8008933013;
			x[1,0] = 1.4695233462;
			x[2,0] = -2.2704166475;
			v[0,1] = 1.1815913872;
			v[1,1] = -0.5532095521;
			v[2,1] = -0.6283818352 ;
			break;
		case 15:
			// var text = 'NAME: Henon 44 %
			x[0,0] =  3.0372729887;
			x[1,0] = 3.5696472554;
			x[2,0] = -6.6069202441;
			v[0,1] = 1.1526391069;
			v[1,1] = -0.7857412148;
			v[2,1] = -0.3668978921 ;
			break;
		}
		return true;
	}
}
