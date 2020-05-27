using UnityEngine;
using System.Collections;

public class AsteroidData {

	// ASTEROIDS
	// From the number asteroids file at http://ssd.jpl.nasa.gov/?sb_elem
	private static string[] names; 

	private static bool inited; 

	public static void Init() {

		names = new string[jplData.Length];

		for (int i=0; i < jplData.Length; i++) {
			names[i] = ParseName(jplData[i]);
		}
		inited = true; 
	}

	public static string[] GetNames() {
		if (!inited)
			Init(); 
		return names;
	}

	/// <summary>
	/// Parses the JPL asteroid string and set values in the SolarBody component. 
	/// </summary>
	/// <returns>The JPL asteroid.</returns>
	/// <param name="line">Line.</param>
	public static void SetSolarBody(SolarBody sbody, int index) {
		string line = jplData[index];
		SetSolarBody(sbody, line);
	}

	public static void SetSolarBody(SolarBody sbody, string line) {
		try {
			// String is e.g
			//  Num   Name              Epoch      a          e        i         w        Node        M         H    G   Ref
			// ------ ----------------- ----- ---------- ---------- --------- --------- --------- ----------- ----- ---- ----------
			//      5 Astraea           57200  2.5734799 0.19118780   5.36855 358.96928 141.59578 307.8982858  6.85 0.15 JPL 97
			// Column widths are important - since names can contain spaces
			sbody.name = line.Substring(7, 17).Trim(); // start and LENGTH
			string[] data = line.Substring(25).Trim().Split(new char[0], System.StringSplitOptions.RemoveEmptyEntries);
			sbody.epoch = SolarUtils.ConvertMJDtoAD(float.Parse(data[0]));
			sbody.a = float.Parse(data[1]);
			sbody.ecc = float.Parse(data[2]);
			sbody.inclination = float.Parse(data[3]);
			sbody.omega_lc = float.Parse(data[4]);
			sbody.omega_uc = float.Parse(data[5]);
			// TODO: Check this
			sbody.longitude = // OrbitEllipse.MeanToTrueAnomoly(float.Parse(data[6]), sbody.ecc);
			sbody.mass_1E24 = 0f;
			sbody.radiusKm = 2000f; // arbitrary to give them some visible size
			sbody.bodyType = SolarSystem.Type.ASTEROID;
		} catch {
			Debug.Log("Parse error:" + line);
		}
	}

	private static string ParseName(string line) {
		try {
			return line.Substring(7, 17).Trim(); // start and LENGTH
		} catch {
			return null;
		}
	}

	public static void SetEllipse(float epoch, EllipseBase ellipseBase, SolarBody sb) {
		// orbital element variation is not provided - only phase to determine
		// What is the delat to the reference epoch?
		float deltaYears = epoch - sb.epoch;
		float period = SolarUtils.GetPeriodYears(sb);
		float numPeriod = deltaYears/period;
		ellipseBase.phase = OrbitEllipse.MeanToTrueAnomoly(NUtils.DegreesMod360( sb.longitude + 360f*numPeriod), ellipseBase.ecc);
	}

/*
Num   Name              Epoch      a          e        i         w        Node        M         H    G   Ref
------ ----------------- ----- ---------- ---------- --------- --------- --------- ----------- ----- ---- ----------
*/
	private static string[] jplData = {
"     1 Ceres             57200  2.7679724 0.07578254  10.59230  72.65415  80.32720 138.6621747  3.34 0.12 JPL 33",
"     2 Pallas            57200  2.7720036 0.23115767  34.84026 309.96556 173.09215 120.9190263  4.13 0.11 JPL 26",
"     3 Juno              57200  2.6710735 0.25583020  12.98662 248.39924 169.86010  78.2157434  5.33 0.32 JPL 102",
"     4 Vesta             57200  2.3619132 0.08883383   7.14010 151.18850 103.84964  75.1726759  3.20 0.32 JPL 33",
"     5 Astraea           57200  2.5734799 0.19118780   5.36855 358.96928 141.59578 307.8982858  6.85 0.15 JPL 97",
"     6 Hebe              57200  2.4260396 0.20150658  14.74819 239.48589 138.70347  81.3179389  5.71 0.24 JPL 82",
"     7 Iris              57200  2.3858038 0.23111629   5.52245 145.41010 259.58587 125.6920559  5.51 0.15 JPL 105",
"     8 Flora             57200  2.2015844 0.15668778   5.88687 285.43109 110.91382 132.1859850  6.49 0.28 JPL 103",
"     9 Metis             57200  2.3865738 0.12217763   5.57406   5.87362  68.93971 263.1426340  6.28 0.17 JPL 106",
"    10 Hygiea            57200  3.1421159 0.11469175   3.83777 312.10431 283.41231 264.4639730  5.43 0.15 JPL 85",
"    11 Parthenope        57200  2.4523255 0.10016481   4.62984 196.03961 125.56677 279.1551247  6.55 0.15 JPL 89",
"    12 Victoria          57200  2.3331883 0.22124751   8.36928  69.50661 235.48580 106.4885258  7.24 0.22 JPL 103",
"    13 Egeria            57200  2.5771274 0.08373775  16.53743  80.21551  43.24333 214.2969939  6.74 0.15 JPL 77",
"    14 Irene             57200  2.5863581 0.16623704   9.11810  98.02628  86.15318 192.9165476  6.30 0.15 JPL 69",
"    15 Eunomia           57200  2.6439231 0.18737574  11.73822  97.53941 293.18555 323.3872658  5.28 0.23 JPL 70",
"    16 Psyche            57200  2.9221666 0.13617091   3.09904 227.11245 150.27584  14.1157196  5.90 0.20 JPL 83",
"    17 Thetis            57200  2.4708059 0.13236359   5.59069 136.00312 125.56730 308.2781057  7.76 0.15 JPL 90",
"    18 Melpomene         57200  2.2950431 0.21894920  10.13370 227.90601 150.47116 230.5164283  6.51 0.25 JPL 91",
"    19 Fortuna           57200  2.4414347 0.15885487   1.57374 182.23242 211.15556 195.5179904  7.13 0.10 JPL 111",
"    20 Massalia          57200  2.4084523 0.14280178   0.70843 256.58568 206.13250 108.8032148  6.50 0.25 JPL 91",
"    21 Lutetia           57200  2.4348932 0.16464893   3.06362 250.11653  80.88481 340.9460937  7.35 0.11 JPL 95",
"    22 Kalliope          57200  2.9108009 0.09960416  13.71460 354.88446  66.07460 280.0314590  6.45 0.21 JPL 82",
"    23 Thalia            57200  2.6256230 0.23569746  10.11359  60.79703  66.87374  11.2179871  6.95 0.15 JPL 77",
"    24 Themis            57200  3.1356279 0.12575607   0.75220 106.77077  35.92034 107.0864077  7.08 0.19 JPL 95",
"    25 Phocaea           57200  2.3998655 0.25530557  21.58878  90.07937 214.21058 162.0439893  7.83 0.15 JPL 82",
"    26 Proserpina        57200  2.6545767 0.09046978   3.56380 194.20082  45.78506 145.9582417  7.50 0.15 JPL 103",
"    27 Euterpe           57200  2.3467371 0.17221738   1.58369 356.58394  94.79872 311.3428090  7.00 0.15 JPL 93",
"    28 Bellona           57200  2.7753596 0.15155097   9.43340 344.63040 144.32393 334.5031714  7.09 0.15 JPL 89",
"    29 Amphitrite        57200  2.5549604 0.07200974   6.08968  61.96096 356.41987 307.7532204  5.85 0.20 JPL 139",
"    30 Urania            57200  2.3653233 0.12660953   2.09800  86.73486 307.62978  10.4601383  7.57 0.15 JPL 96",
"    31 Euphrosyne        57200  3.1556843 0.22179830  26.30346  61.45203  31.13400 201.0559803  6.74 0.15 JPL 79",
"    32 Pomona            57200  2.5877009 0.08024726   5.52361 338.98909 220.45475  61.0205222  7.56 0.15 JPL 91",
"    33 Polyhymnia        57200  2.8670560 0.33657446   1.86878 338.09511   8.57680  58.8811488  8.55 0.33 JPL 25",
"    34 Circe             57200  2.6880635 0.10459812   5.49818 330.49524 184.42313 173.7480777  8.51 0.15 JPL 63",
"    35 Leukothea         57200  2.9937408 0.22677703   7.93575 213.30991 353.75345 308.9042190  8.50 0.15 JPL 31",
"    36 Atalante          57200  2.7487357 0.30200085  18.42628  46.98752 358.41363 357.9872097  8.46 0.15 JPL 76",
"    37 Fides             57200  2.6427554 0.17355539   3.07277  62.97398   7.29685  20.2335419  7.29 0.24 JPL 105",
"    38 Leda              57200  2.7393634 0.15557412   6.97220 169.60341 295.75047  60.2039999  8.32 0.15 JPL 88",
"    39 Laetitia          57200  2.7686427 0.11380103  10.38093 208.27446 157.11454   2.9536153  6.00 0.15 JPL 91",
"    40 Harmonia          57200  2.2668746 0.04662150   4.25767 268.62909  94.23703  64.4581110  7.00 0.15 JPL 104",
"    41 Daphne            57200  2.7598025 0.27548766  15.79633  45.97437 178.09098 193.3163127  7.12 0.10 JPL 112",
"    42 Isis              57200  2.4441514 0.22187846   8.51307 237.03842  84.27024 202.1687301  7.53 0.15 JPL 87",
"    43 Ariadne           57200  2.2031250 0.16876332   3.47034  16.31662 264.86526  75.9062958  7.93 0.11 JPL 98",
"    44 Nysa              57200  2.4229381 0.14863874   3.70687 343.28397 131.55922  76.9495451  7.03 0.46 JPL 107",
"    45 Eugenia           57200  2.7208579 0.08343267   6.60374  88.70537 147.67764  90.2822959  7.46 0.07 JPL 89",
"    46 Hestia            57200  2.5254158 0.17168605   2.34461 176.59902 181.12324  87.1568953  8.36 0.06 JPL 140",
"    47 Aglaja            57200  2.8794682 0.13269114   4.98310 313.91660   3.12390 131.1166520  7.84 0.16 JPL 93",
"    48 Doris             57200  3.1100807 0.07332513   6.54707 253.59929 183.57460 177.3897458  6.90 0.15 JPL 154",
"    49 Pales             57200  3.0932036 0.22733058   3.17367 110.21042 285.99083 349.1172803  7.80 0.15 JPL 66",
"    50 Virginia          57200  2.6493059 0.28567039   2.83783 200.22598 173.53257 198.2987206  9.24 0.15 JPL 92",
      };

}


