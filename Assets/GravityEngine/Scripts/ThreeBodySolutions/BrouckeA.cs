using System.Collections;
using UnityEngine;

public class BrouckeA : ISolutionSet {

	public string GetName() { 
		return "BrouckeA"; 
	}
	
	
	public string[] GetSolutionNames() {
		string[] names = new string[16];
		for (int i=0; i < 16; i++) {
			names[i] = "A" + (i+1);
		}
		return names;
	}
	
	public bool GetDataForSolutionName(string name, ref double[,] x, ref double[,] v, 
										ref GravityEngine.Algorithm algorithm, ref float scale) {
		// data from http://suki.ipb.ac.rs/3body/ 
		// original work: R. Broucke, On relative periodic solutions of the planar general three-body problem, 
		// Celest. Mech. 12, 439 (1975).
		scale = 0.8f;
		algorithm = GravityEngine.Algorithm.AZTRIPLE;
		string numStr = name.Replace("A", "");

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
				// var text = 'NAME: Broucke A 1 %
				x[0,0] =  -0.9892620043;
				x[1,0] = 2.2096177241;
				x[2,0] = -1.2203557197;
				v[0,1] = 1.9169244185;
				v[1,1] = 0.1910268738;
				v[2,1] = -2.1079512924 ;
				break;
			case 1:
				// var text = 'NAME: Broucke A 2 %
				x[0,0] =  0.3361300950;
				x[1,0] = 0.7699893804;
				x[2,0] = -1.1061194753;
				v[0,1] = 1.5324315370;
				v[1,1] = -0.6287350978;
				v[2,1] = -0.9036964391 ;
				break;
			case 2:
				// var text = 'NAME: Broucke A 3 %
				x[0,0] =  0.3149337497;
				x[1,0] = 0.8123820710;
				x[2,0] = -1.1273158206;
				v[0,1] = 1.4601869417;
				v[1,1] = -0.5628292375;
				v[2,1] = -0.8973577042 ;
				break;
			case 3:
				// var text = 'NAME: Broucke A 4 %
				x[0,0] =  0.2843198916;
				x[1,0] = 0.8736097872;
				x[2,0] = -1.1579296788;
				v[0,1] = 1.3774179570;
				v[1,1] = -0.4884226932;
				v[2,1] = -0.8889952638 ;
				break;
			case 4:
				// var text = 'NAME: Broucke A 5 %
				x[0,0] =  0.2355245585;
				x[1,0] = 0.9712004534;
				x[2,0] = -1.2067250118;
				v[0,1] = 1.2795329643;
				v[1,1] = -0.4021329019;
				v[2,1] = -0.8774000623 ;
				break;
			case 5:
				// var text = 'NAME: Broucke A 6 %
				x[0,0] =  0.1432778606;
				x[1,0] = 1.1556938491;
				x[2,0] = -1.2989717097;
				v[0,1] = 1.1577475241;
				v[1,1] = -0.2974667752;
				v[2,1] = -0.8602807489 ;
				break;
			case 6:
				// var text = 'NAME: Broucke A 7 %
				x[0,0] =  -0.1095519101;
				x[1,0] = 1.6613533905;
				x[2,0] = -1.5518014804;
				v[0,1] = 0.9913358338;
				v[1,1] = -0.1569959746;
				v[2,1] = -0.8343398592 ;
				scale = 0.7f;
				break;
			case 7:
				// var text = 'NAME: Broucke A 8 %
				x[0,0] =  0.1979259967;
				x[1,0] = 1.0463975768;
				x[2,0] = -1.2443235736;
				v[0,1] = 1.2224733132;
				v[1,1] = -0.3527351133;
				v[2,1] = -0.8697381999 ;
				scale = 0.7f;
				break;
			case 8:
				// var text = 'NAME: Broucke A 9 %
				x[0,0] =  0.0557080334;
				x[1,0] = 1.3308335036;
				x[2,0] = -1.3865415370;
				v[0,1] = 1.0824099428;
				v[1,1] = -0.2339059386;
				v[2,1] = -0.8485040042 ;
				scale = 0.7f;
				break;
			case 9:
				// var text = 'NAME: Broucke A 10 %
				x[0,0] =  -0.5426216182;
				x[1,0] = 2.5274928067;
				x[2,0] = -1.9848711885;
				v[0,1] = 0.8750200467;
				v[1,1] = -0.0526955841;
				v[2,1] = -0.8223244626 ;
				scale = 0.5f;
				break;
			case 10:
				// var text = 'NAME: Broucke A 11 %
				x[0,0] =  0.0132604844;
				x[1,0] = 1.4157286016;
				x[2,0] = -1.4289890859;
				v[0,1] = 1.0541519210;
				v[1,1] = -0.2101466639;
				v[2,1] = -0.8440052572 ;
				scale = 0.7f;
				break;
			case 11:
				// var text = 'NAME: Broucke A 12 %
				x[0,0] =  -0.3370767020;
				x[1,0] = 2.1164029743;
				x[2,0] = -1.7793262723;
				v[0,1] = 0.9174260238;
				v[1,1] = -0.0922665014;
				v[2,1] = -0.8251595224 ;
				scale = 0.5f;
				break;
			case 12:
				// var text = 'NAME: Broucke A 13 %
				x[0,0] =  -0.8965015243;
				x[1,0] = 3.2352526189;
				x[2,0] = -2.3387510946;
				v[0,1] = 0.8285556923;
				v[1,1] = -0.0056478094;
				v[2,1] = -0.8229078829 ;
				scale = 0.3f;
				break;
			case 13:
				// var text = 'NAME: Broucke A 14 %
				x[0,0] =  -0.2637815221;
				x[1,0] = 1.9698126146;
				x[2,0] = -1.7060310924;
				v[0,1] = 0.9371630895;
				v[1,1] = -0.1099503287;
				v[2,1] = -0.8272127608 ;
				scale = 0.5f;
				break;
			case 14:
				// var text = 'NAME: Broucke A 15 %
				x[0,0] =  -1.1889693067;
				x[1,0] = 3.8201881837;
				x[2,0] = -2.6312188770;
				v[0,1] = 0.8042120498;
				v[1,1] = 0.0212794833;
				v[2,1] = -0.8254915331 ;
				scale = 0.4f;
				break;
			case 15:
				// var text = 'NAME: Broucke A 16 %
				x[0,0] =  -0.7283341038;
				x[1,0] = 2.8989177778;
				x[2,0] = -2.1705836741;
				v[0,1] = 0.8475982451;
				v[1,1] = -0.0255162097;
				v[2,1] = -0.8220820354 ;
				scale = 0.3f;
				break;
			default:
				Debug.LogError("Unknown solution " + name);
				break;
		}
		return true;
	}
	
}
