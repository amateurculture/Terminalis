using System.Collections;
using UnityEngine;

public class BrouckeR : ISolutionSet {
	
	public string GetName() { 
		return "BrouckeR"; 
	}
	
	public string[] GetSolutionNames() {
		string[] names = new string[13];
		for (int i=0; i < 13; i++) {
			names[i] = "R" + (i+1);
		}
		return names;
	}
	
	public bool GetDataForSolutionName(string name, ref double[,] x, ref double[,] v, 
										ref GravityEngine.Algorithm algorithm, ref float scale) {
		// data from http://suki.ipb.ac.rs/3body/ 
		// original work: R. Broucke, On relative periodic solutions of the planar general three-body problem, 
		// Celest. Mech. 12, 439 (1975).
		scale = 1.0f;
		algorithm = GravityEngine.Algorithm.AZTRIPLE;
		string numStr = name.Replace("R", "");
		int num = 0; 
		if (!int.TryParse(numStr, out num)) {
			Debug.LogError("Solution string " + numStr + " not of the form R<1-13>");
			return false;
		};
		num -= 1; // 0 based
		if ( num < 0 || num > 12) {
			Debug.LogError("Solution number " + numStr + " out of range");
			return false; 
		}
		switch(num) {
			case 0:
				// var text = 'NAME: Broucke R 1 %
				x[0,0] =  0.8083106230;
				x[1,0] = -0.4954148566;
				x[2,0] = -0.3128957664;
				v[0,1] = 0.9901979166;
				v[1,1] = -2.7171431768;
				v[2,1] = 1.7269452602 ;
				break;
			case 1:
				// var text = 'NAME: Broucke R 2 %
				x[0,0] =  0.9060893715;
				x[1,0] = -0.6909723536;
				x[2,0] = -0.2151170179;
				v[0,1] = 0.9658548899;
				v[1,1] = -1.6223214842;
				v[2,1] = 0.6564665942 ;
				break;
			case 2:
				// var text = 'NAME: Broucke R 3 %
				x[0,0] =  0.8920281421;
				x[1,0] = -0.6628498947;
				x[2,0] = -0.2291782474;
				v[0,1] = 0.9957939373;
				v[1,1] = -1.6191613336;
				v[2,1] = 0.6233673964 ;
				break;
			case 3:
				// var text = 'NAME: Broucke R 4 %
				x[0,0] =  0.8733047091;
				x[1,0] = -0.6254030288;
				x[2,0] = -0.2479016803;
				v[0,1] = 1.0107764436;
				v[1,1] = -1.6833533458;
				v[2,1] = 0.6725769022 ;
				break;
			case 4:
				// var text = 'NAME: Broucke R 5 %
				x[0,0] =  0.8584630769;
				x[1,0] = -0.5957197644;
				x[2,0] = -0.2627433125;
				v[0,1] = 1.0204773541;
				v[1,1] = -1.7535566440;
				v[2,1] = 0.7330792899 ;
				break;
			case 5:
				// var text = 'NAME: Broucke R 6 %
				x[0,0] =  0.8469642946;
				x[1,0] = -0.5727221998;
				x[2,0] = -0.2742420948;
				v[0,1] = 1.0275065708;
				v[1,1] = -1.8209307202;
				v[2,1] = 0.7934241494 ;
				break;
				case 6:
				// var text = 'NAME: Broucke R 7 %
				x[0,0] =  0.8378824453;
				x[1,0] = -0.5545585011;
				x[2,0] = -0.2833239442;
				v[0,1] = 1.0329242005;
				v[1,1] = -1.8840083393;
				v[2,1] = 0.8510841387 ;
				break;
			case 7:
				// var text = 'NAME: Broucke R 8 %
				x[0,0] =  0.8871256555;
				x[1,0] = -0.6530449215;
				x[2,0] = -0.2340807340;
				v[0,1] = 0.9374933545;
				v[1,1] = -1.7866975426;
				v[2,1] = 0.8492041880 ;
				algorithm = GravityEngine.Algorithm.HERMITE8;
				break;
			case 8:
				// var text = 'NAME: Broucke R 9 %
				x[0,0] =  0.9015586070;
				x[1,0] = -0.6819108246;
				x[2,0] = -0.2196477824;
				v[0,1] = 0.9840575737;
				v[1,1] = -1.6015183264;
				v[2,1] = 0.6174607527 ;
				break;
			case 9:
				// var text = 'NAME: Broucke R 10 %
				x[0,0] =  0.8822391241;
				x[1,0] = -0.6432718586;
				x[2,0] = -0.2389672654;
				v[0,1] = 1.0042424155;
				v[1,1] = -1.6491842814;
				v[2,1] = 0.6449418659 ;
				break;
			case 10:
				// var text = 'NAME: Broucke R 11 %
				x[0,0] =  0.8983487470;
				x[1,0] = -0.6754911045;
				x[2,0] = -0.2228576425;
				v[0,1] = 0.9475564971;
				v[1,1] = -1.7005860354;
				v[2,1] = 0.7530295383 ;
				break;
			case 11:
				// var text = 'NAME: Broucke R 12 %
				x[0,0] =  0.9040866398;
				x[1,0] = -0.6869668901;
				x[2,0] = -0.2171197497;
				v[0,1] = 0.9789534005;
				v[1,1] = -1.6017790202;
				v[2,1] = 0.6228256196 ;
				break;
			case 12:
				// var text = 'NAME: Broucke R 13 %
				x[0,0] =  0.9017748598;
				x[1,0] = -0.6823433302;
				x[2,0] = -0.2194315296;
				v[0,1] = 0.9526089117;
				v[1,1] = -1.6721104565;
				v[2,1] = 0.7195015448 ;
				break;
			default:
				Debug.LogError("Unknown solution " + name + " case=" + num);
				break;
		}
		return true;
	}
	
}
