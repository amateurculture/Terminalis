using UnityEngine;
using System.Collections;

public class PlanetData {

	// PLANETS

	// see http://ssd.jpl.nasa.gov/txt/p_elem_t1.txt
	// and http://ssd.jpl.nasa.gov/txt/aprx_pos_planets.pdf
	// in 10^24 kg
	//
	private static OrbitData[] planetData; 

	private static string[] names = {"Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune", "Pluto"};

	private static bool inited; 

	public static void SetSolarBody (SolarBody sbody, int index)
	{

		switch (index) {
		case 0:
			sbody.name = "Mercury";
			sbody.a = 0.38709927f;
			sbody.ecc = 0.20563593f;
			sbody.inclination = 7.00497902f;
			sbody.omega_lc_bar = 77.45779628f;
			sbody.omega_uc = 48.33076593f;
			sbody.longitude = 252.25032350f;
	
			sbody.a_dot = 0.00000037f;
			sbody.ecc_dot = 0.00001906f;
			sbody.inclination_dot = -0.00594749f;
			sbody.omega_lc_bar_dot = 0.16047689f;
			sbody.omega_uc_dot = -0.12534081f;
			sbody.longitudeDot = 149472.67411175f;
			sbody.mass_1E24 = 0.3301f;
			sbody.radiusKm = 2439.7f;
			break;
	

		case 1:
			sbody.name = "Venus";
			sbody.a = 0.72333566f;
			sbody.ecc = 0.00677672f;
			sbody.inclination = 3.39467605f;
			sbody.omega_lc_bar = 131.60246718f;
			sbody.omega_uc = 76.67984255f;
			sbody.longitude = 181.97909950f;
	
			sbody.a_dot = 0.00000390f;
			sbody.ecc_dot = -0.00004107f;
			sbody.inclination_dot = -0.00078890f;
			sbody.omega_lc_bar_dot = 0.00268329f;
			sbody.omega_uc_dot = -0.27769418f;
			sbody.longitudeDot = 58517.81538729f;
			sbody.mass_1E24 = 4.867f;
			sbody.radiusKm = 6051.8f;
			break;

		case 2:
			sbody.name = "Earth";
			sbody.a = 1.00000261f;
			sbody.ecc = 0.01671123f;
			sbody.inclination = -0.00001531f;
			sbody.omega_lc_bar = 102.93768193f;
			sbody.omega_uc = 0.0f;
			sbody.longitude = 100.46457166f;
	
			sbody.a_dot = 0.00000562f;
			sbody.ecc_dot = -0.00004392f;
			sbody.inclination_dot = -0.01294668f;
			sbody.omega_lc_bar_dot = 0.32327364f;
			sbody.omega_uc_dot = 0.0f;
			sbody.longitudeDot = 35999.37244981f;
			sbody.mass_1E24 = 5.972f;
			sbody.radiusKm = 6378.14f;
			break;
	
		case 3:
			sbody.name = "Mars";
			sbody.a = 1.52371034f;
			sbody.ecc = 0.09339410f;
			sbody.inclination = 1.84969142f;
			sbody.omega_lc_bar = -23.94362959f;
			sbody.omega_uc = 49.55953891f;
			sbody.longitude = -4.55343205f;
	
			sbody.a_dot = 0.00001847f;
			sbody.ecc_dot = 0.00007882f;
			sbody.inclination_dot = -0.00813131f;
			sbody.omega_lc_bar_dot = 0.44441088f;
			sbody.omega_uc_dot = -0.29257343f;
			sbody.longitudeDot = 19140.30268499f;
			sbody.mass_1E24 = 0.6416f;
			sbody.radiusKm = 3396.19f;
			break;
	
		case 4:
			sbody.name = "Jupiter";
			sbody.a = 5.20288700f;
			sbody.ecc = 0.04838624f;
			sbody.inclination = 1.30439695f;
			sbody.omega_lc_bar = 14.72847983f;
			sbody.omega_uc = 100.47390909f;
			sbody.longitude = 34.39644051f;
	
			sbody.a_dot = -0.00011607f;
			sbody.ecc_dot = -0.00013253f;
			sbody.inclination_dot = -0.00183714f;
			sbody.omega_lc_bar_dot = 0.21252668f;
			sbody.omega_uc_dot = 0.20469106f;
			sbody.longitudeDot = 3034.74612775f;
			sbody.mass_1E24 = 1898.1f;
			sbody.radiusKm = 71492f;
			break;
	

		case 5:
			sbody.name = "Saturn";
			sbody.a = 9.53667594f;
			sbody.ecc = 0.05386179f;
			sbody.inclination = 2.48599187f;
			sbody.omega_lc_bar = 92.59887831f;
			sbody.omega_uc = 113.66242448f;
			sbody.longitude = 49.95424423f;
	
			sbody.a_dot = -0.00125060f;
			sbody.ecc_dot = -0.00050991f;
			sbody.inclination_dot = 0.00193609f;
			sbody.omega_lc_bar_dot = -0.41897216f;
			sbody.omega_uc_dot = -0.28867794f;
			sbody.longitudeDot = 1222.49362201f;
			sbody.mass_1E24 = 368.319f;
			sbody.radiusKm = 60268f;
			break;
	
		case 6:
			sbody.name = "Uranus";
			sbody.a = 19.18916464f;
			sbody.ecc = 0.04725744f;
			sbody.inclination = 0.77263783f;
			sbody.omega_lc_bar = 170.95427630f;
			sbody.omega_uc = 74.01692503f;
			sbody.longitude = 313.23810451f;
	
			sbody.a_dot = -0.00196176f;
			sbody.ecc_dot = -0.00004397f;
			sbody.inclination_dot = -0.00242939f;
			sbody.omega_lc_bar_dot = 0.40805281f;
			sbody.omega_uc_dot = 0.04240589f;
			sbody.longitudeDot = 428.48202785f;
			sbody.mass_1E24 = 86.81f;
			sbody.radiusKm = 25559f;
			break;
	
		case 7:
			sbody.name = "Neptune";
			sbody.a = 30.06992276f;
			sbody.ecc = 0.00859048f;
			sbody.inclination = 1.77004347f;
			sbody.omega_lc_bar = 44.96476227f;
			sbody.omega_uc = 131.78422574f;
			sbody.longitude = -55.12002969f;
	
			sbody.a_dot = 0.00026291f;
			sbody.ecc_dot = 0.00005105f;
			sbody.inclination_dot = 0.00035372f;
			sbody.omega_lc_bar_dot = -0.32241464f;
			sbody.omega_uc_dot = -0.00508664f;
			sbody.longitudeDot = 218.45945325f;
			sbody.mass_1E24 = 102.4f;
			sbody.radiusKm = 24764f;
			break;
	
		case 8:
			sbody.name = "Pluto";
			sbody.a = 39.48211675f;
			sbody.ecc = 0.24882730f;
			sbody.inclination = 17.14001206f;
			sbody.omega_lc_bar = 224.06891629f;
			sbody.omega_uc = 110.30393684f;
			sbody.longitude = 238.92903833f;
	

			sbody.a_dot = -0.00031596f;
			sbody.ecc_dot = 0.00005170f;
			sbody.inclination_dot = 0.00004818f;
			sbody.omega_lc_bar_dot = -0.04062942f;
			sbody.omega_uc_dot = -0.01183482f;
			sbody.longitudeDot = 145.20780515f;
			sbody.mass_1E24 = 0.0139f;
			sbody.radiusKm = 1151f;
			break;



		default:
			break;
		}

		sbody.epoch = 2000f;
		sbody.bodyType = SolarSystem.Type.PLANET;
	}

	public static string[] GetNames ()
	{
		return names;
	}

	/// <summary>
	/// Sets the ellipse parameters for planet.
	/// </summary>
	/// <param name="epoch">Epoch.</param>
	/// <param name="ellipseBase">Ellipse base.</param>
	public static void SetEllipse(float epoch, EllipseBase ellipseBase, SolarBody sb) {
		// Based on JPL data, apply the per century drift to the orbital parameters
		// T = number of centuries past J2000
		float T = (epoch - 2000f)/100f;
		ellipseBase.a = sb.a + sb.a_dot * T;
		ellipseBase.ecc = sb.ecc + sb.ecc_dot * T;
		ellipseBase.inclination = sb.inclination + sb.inclination_dot * T;
		ellipseBase.omega_uc = NUtils.DegreesMod360( sb.omega_uc + sb.omega_uc_dot * T );
		// following JPL doc (http://ssd.jpl.nasa.gov/txt/aprx_pos_planets.pdf)
		float current_omega_bar = sb.omega_lc_bar + sb.omega_lc_bar_dot * T;
		ellipseBase.omega_lc = NUtils.DegreesMod360(current_omega_bar - ellipseBase.omega_uc);
		float L = sb.longitude + sb.longitudeDot * T;
		float mean_anomoly = NUtils.DegreesMod360(L - current_omega_bar);
		ellipseBase.phase = mean_anomoly;
	}

}
/*
Code auto-generated from the data from JPL
=====================================================================
  These data are to be used as described in the related document
  titled "Keplerian Elements for Approximate Positions of the
  Major Planets" by E.M. Standish (JPL/Caltech) available from
  the JPL Solar System Dynamics web site (http://ssd.jpl.nasa.gov/).
=====================================================================


Table 1.

Keplerian elements and their rates, with respect to the mean ecliptic
and equinox of J2000, valid for the time-interval 1800 AD - 2050 AD.

               a              e               I                L            long.peri.      long.node.
           AU, AU/Cy     rad, rad/Cy     deg, deg/Cy      deg, deg/Cy      deg, deg/Cy     deg, deg/Cy
-----------------------------------------------------------------------------------------------------------
Mercury   0.38709927      0.20563593      7.00497902      252.25032350     77.45779628     48.33076593
          0.00000037      0.00001906     -0.00594749   149472.67411175      0.16047689     -0.12534081
Venus     0.72333566      0.00677672      3.39467605      181.97909950    131.60246718     76.67984255
          0.00000390     -0.00004107     -0.00078890    58517.81538729      0.00268329     -0.27769418
EM Bary   1.00000261      0.01671123     -0.00001531      100.46457166    102.93768193      0.0
          0.00000562     -0.00004392     -0.01294668    35999.37244981      0.32327364      0.0
Mars      1.52371034      0.09339410      1.84969142       -4.55343205    -23.94362959     49.55953891
          0.00001847      0.00007882     -0.00813131    19140.30268499      0.44441088     -0.29257343
Jupiter   5.20288700      0.04838624      1.30439695       34.39644051     14.72847983    100.47390909
         -0.00011607     -0.00013253     -0.00183714     3034.74612775      0.21252668      0.20469106
Saturn    9.53667594      0.05386179      2.48599187       49.95424423     92.59887831    113.66242448
         -0.00125060     -0.00050991      0.00193609     1222.49362201     -0.41897216     -0.28867794
Uranus   19.18916464      0.04725744      0.77263783      313.23810451    170.95427630     74.01692503
         -0.00196176     -0.00004397     -0.00242939      428.48202785      0.40805281      0.04240589
Neptune  30.06992276      0.00859048      1.77004347      -55.12002969     44.96476227    131.78422574
          0.00026291      0.00005105      0.00035372      218.45945325     -0.32241464     -0.00508664
Pluto    39.48211675      0.24882730     17.14001206      238.92903833    224.06891629    110.30393684
         -0.00031596      0.00005170      0.00004818      145.20780515     -0.04062942     -0.01183482
*/
