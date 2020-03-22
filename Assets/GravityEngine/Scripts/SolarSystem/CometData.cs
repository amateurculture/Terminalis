using UnityEngine;
using System.Collections;

public class CometData {

//	   COMETS
//	   from the Comet file at http:  ssd.jpl.nasa.gov ?sb_elem

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

	public static void SetSolarBody(SolarBody sbody, int index) {
		string line = jplData[index];
		SetSolarBody(sbody, line);
	}

	/// <summary>
	/// Parses the JPL asteroid string
	/// </summary>
	/// <returns>The JPL asteroid.</returns>
	/// <param name="line">Line.</param>
	public static void SetSolarBody(SolarBody sbody, string line) {
		// String is e.g
//Num  Name                                     Epoch      q           e        i         w        Node          Tp       Ref
//------------------------------------------- ------- ----------- ---------- --------- --------- --------- -------------- ------------
//  1P/Halley                                  49400  0.58597811 0.96714291 162.26269 111.33249  58.42008 19860205.89532 JPL J863/77
		// Column widths are important - since names can contain spaces
		try {
			sbody.name = line.Substring(1, 45).Trim(); // start and LENGTH
			// slashes end up making subdirs in list groups, so replace them 
			sbody.name = sbody.name.Replace("/", " ");
			string[] data = line.Substring(46).Trim().Split(new char[0], System.StringSplitOptions.RemoveEmptyEntries);
			float q = float.Parse(data[1]);
			sbody.ecc = float.Parse(data[2]);
			sbody.a = q/(1-sbody.ecc);
			sbody.inclination = float.Parse(data[3]);
			sbody.omega_lc = float.Parse(data[4]);
			sbody.omega_uc = float.Parse(data[5]);
			sbody.longitude = 0f;
			// Mis-use of epoch field. For asteroids it is used for time of elements (incl. mean anomoly)
			// For comets set epoch as time of perih. and set true anomoly to zero
			sbody.epoch = TimePerihelionToEpoch(data[6]);
			sbody.mass_1E24 = 0f;
			sbody.radiusKm = 2000f; // arbitrary to give them some visible size
			sbody.bodyType = SolarSystem.Type.COMET;
		} catch {
			Debug.Log("Parse error:" + line);
		}
	}

	private static string ParseName(string line) {
		try {
			return line.Substring(1, 45).Trim(); // start and LENGTH
		} catch {
			return null;
		}
	}


	private static float TimePerihelionToEpoch(string timeP ) {
		// JPL give time perihelion for comets
		// Time of perihelion passage (comets only), formatted as a calendar date (YYYYMMDD.DDD) where 
		// "YYYY" is the year, "MM" is the numeric month, and "DD.DDD" is the day and day fraction.
		float year = float.Parse(timeP.Substring(0,4));
		int month = int.Parse(timeP.Substring(4,2));
		float days = float.Parse(timeP.Substring(7,6));
		System.DateTime datetime = new System.DateTime( (int) year, month, 1);
		float date = year + (datetime.DayOfYear + days - 1f)/365.25f;
		return date;
	}


	public static void SetEllipse(float epoch, EllipseBase ellipseBase, SolarBody sb) {
		// orbital element variation is not provided - only phase to determine
		// What is the delta to the reference epoch?
		float deltaYears = epoch - sb.epoch;
		float period = SolarUtils.GetPeriodYears(sb);
		float numPeriod = deltaYears/period;
		// epoch was for time at perihelion (i.e. longitude = 0)
		// w = w_bar - Omega, 
		ellipseBase.phase = OrbitEllipse.MeanToTrueAnomoly(NUtils.DegreesMod360(360f*numPeriod), ellipseBase.ecc); 
//		Debug.Log(" numP=" + numPeriod + " M=" + NUtils.DegressMod360(360f*numPeriod) +
//			 " phase=" + ellipseBase.phase);
	}


	private static string[] jplData = {
"  1P/Halley                                   49400  0.58597811 0.96714291 162.26269 111.33249  58.42008 19860205.89532 JPL J863/77",
"  2P/Encke                                    57260  0.33595724 0.84833535  11.78146 186.54601 334.56820 20170310.01570 JPL 75",
"  3D/Biela                                    -9480  0.87907300 0.75129900  13.21640 221.65880 250.66900 18321126.61520 IAUCAT03",
"  4P/Faye                                     57262  1.65187634 0.56961755   9.07029 205.12582 199.14398 20140529.86872 JPL K144/7",
"  5D/Brorsen                                   7440  0.58984700 0.80979600  29.38210  14.94680 102.96760 18790331.03410 IAUCAT03",
"  6P/d'Arrest                                 57200  1.36145455 0.61142192  19.48168 178.11523 138.93371 20150302.41870 JPL K155/3",
"  7P/Pons-Winnecke                            57380  1.23896144 0.63761701  22.33509 172.50006  93.40960 20150130.53022 JPL K088/17",
"  8P/Tuttle                                   54374  1.02711659 0.81979975  54.98318 207.50925 270.34165 20080127.02555 JPL K074/27",
"  9P/Tempel 1                                 57375  1.54236947 0.50981920  10.47332 179.18727  68.76828 20160802.55955 JPL 160",
" 10P/Tempel 2                                 57262  1.41764523 0.53735240  12.02891 195.54767 117.80539 20151114.25866 JPL K1013/12",
" 11P/Tempel-Swift-LINEAR                      57164  1.54848289 0.54623616  13.57343 164.11497 240.40243 20140826.98472 JPL 6",
" 12P/Pons-Brooks                              35000  0.77366709 0.95481239  74.17689 199.02847 255.89114 19540522.88085 JPL 15",
" 13P/Olbers                                   35760  1.17845061 0.93029715  44.60989  64.64121  86.10313 19560619.13533 JPL 10",
" 14P/Wolf                                     56981  2.74085898 0.35563034  27.91358 159.16212 202.13061 20171202.65886 JPL K078/7",
" 15P/Finlay                                   57164  0.97594407 0.72016905   6.79902 347.55924  13.77506 20141227.05599 JPL K085/15",
" 16P/Brooks 2                                 57164  1.46679247 0.56322341   4.26119 219.74884 159.28043 20140607.74336 JPL K074/9",
" 17P/Holmes                                   57164  2.05748284 0.43169182  19.09196  24.56130 326.76334 20140327.56327 JPL K077/34",
" 18D/Perrine-Mrkos                            40240  1.27224808 0.64258098  17.75898 166.05042 240.87555 19681101.54213 JPL J682/18",
" 19P/Borrelly                                 57300  1.34896731 0.62550569  30.36670 353.46533  75.37301 20150528.92295 JPL K157/3",
" 20D/Westphal                                 20080  1.25401276 0.91983119  40.89006  57.08095 348.00645 19131126.79026 JPL 19",
" 21P/Giacobini-Zinner                         56498  1.03069627 0.70681789  31.90810 172.58442 195.39701 20120211.62594 JPL K123/6",
" 22P/Kopff                                    57380  1.55823105 0.54775312   4.73701 162.90286 120.86760 20151025.09434 JPL K154/8",
" 23P/Brorsen-Metcalf                          47800  0.47875271 0.97195226  19.33394 129.61068 311.58546 19890911.93743 JPL 13",
" 24P/Schaumasse                               52120  1.20501004 0.70480036  11.75153  57.87449  79.83104 20010502.66093 JPL K014/19",
" 25D/Neujmin 2                                24960  1.33816965 0.56681860  10.63871 193.70403 328.71695 19270116.22411 JPL 7",
" 26P/Grigg-Skjellerup                         56978  1.08524778 0.64027714  22.42889   2.14669 211.53755 20130706.05911 JPL K139/6",
" 27P/Crommelin                                56364  0.74828747 0.91898109  28.96687 196.02540 250.62641 20110803.89109 JPL K113/4",
" 28P/Neujmin 1                                53080  1.55215690 0.77541331  14.18564 346.91625 347.02892 20021227.33432 JPL 23",
" 29P/Schwassmann-Wachmann 1                   57380  5.76279202 0.04159567   9.37738  49.49589 312.40671 20190331.73518 JPL K043/68",
" 30P/Reinmuth 1                               55760  1.88319075 0.50119535   8.12261  13.17397 119.74116 20100419.50909 JPL K103/2",
" 31P/Schwassmann-Wachmann 2                   56364  3.42306655 0.19390496   4.54663  18.01932 114.15481 20100930.19359 JPL K084/11",
" 32P/Comas Sola                               57262  2.00061100 0.55604227   9.97068  53.30413  57.82413 20141017.45964 JPL K144/6",
" 33P/Daniel                                   54952  2.16951544 0.46196108  22.37457  18.96397  66.56464 20080720.30814 JPL K084/7",
" 34D/Gale                                     29080  1.18290700 0.76071900  11.72810 209.15720  67.92350 19380618.48320 SAO/1938",
" 35P/Herschel-Rigollet                        29480  0.74849000 0.97405000  64.20700  29.29800 355.98000 19390809.46400 SAO/1939",
" 36P/Whipple                                  56771  3.07239588 0.26170951   9.92733 200.62020 181.98685 20111223.45497 JPL K118/5",
" 37P/Forbes                                   56364  1.57822308 0.54058015   8.95531 329.64367 314.92802 20111211.14212 JPL K114/4",
" 38P/Stephan-Oterma                           44720  1.57442075 0.86002249  17.98142 358.19104  79.18838 19801205.17613 JPL 10",
" 39P/Oterma                                   52200  5.47114748 0.24551396   1.94283  56.27834 331.58112 20021221.02035 JPL 14",
" 40P/Vaisala 1                                57262  1.81977116 0.63161520  11.49264  47.28886 133.84017 20141115.84235 JPL K148/7",
" 41P/Tuttle-Giacobini-Kresak                  53821  1.04777763 0.66041266   9.22803  62.19322 141.09293 20060611.79139 JPL K064/9",
" 42P/Neujmin 3                                57380  2.02775520 0.58417475   3.98459 147.12039 150.27997 20150408.20955 JPL K155/2",
" 43P/Wolf-Harrington                          55740  1.35755634 0.59466516  15.96805 191.49198 249.88266 20100701.79504 JPL K094/9",
" 44P/Reinmuth 2                               57375  2.11855703 0.42635872   5.89593  58.26865 286.45820 20150324.07827 JPL K105/7",
" 45P/Honda-Mrkos-Pajdusakova                  56060  0.52976142 0.82467208   4.25243 326.25804  89.00218 20110928.78226 JPL K112/2",
" 46P/Wirtanen                                 56799  1.05226208 0.65920256  11.75714 356.34021  82.16439 20130709.36917 JPL K073/17",
" 47P/Ashbrook-Jackson                         57262  2.81724804 0.31672674  13.03119 357.76221 356.98023 20170610.59012 JPL 63",
" 48P/Johnson                                  57164  2.03000058 0.42521282  13.05900 210.37025 114.91649 20180803.60346 JPL 57",
" 49P/Arend-Rigaux                             56908  1.42564501 0.60027414  19.05616 332.91871 118.83202 20111019.29125 JPL 69",
" 50P/Arend                                    57380  1.91879467 0.53026064  19.13920  49.22172 355.17625 20160208.18710 JPL 44",
      };
}
