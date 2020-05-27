using UnityEngine;
using System.Collections;

/// <summary>
/// Solar utils.
/// Utility functions for determining planet/comet positions. 
/// </summary>
public class SolarUtils  {

	public static float JulianDate(int year, int month, int day, float utime) {
		// M&D A.3
		float y = 0; 
		float m = 0; 
		if (month <= 2) {
			y = year - 1f;
			m = month + 12f;
		} else {
			y = year; 
			m = month;
		}
		// Shift to Gregorian Calendar
		float B = -2f;
		if ((year > 1582) || 
			((year == 1582) && (month > 10)) ||
			((year == 1582) && (month == 10) && (day >= 15)) ) {
				B = Mathf.Floor( y/400f) - Mathf.Floor(y/100f);
		}
		Debug.Log("B=" + B + " y=" + y + "m="+m);
		float julianDay = Mathf.Floor( 365.25f * y) + Mathf.Floor(30.6001f * (m+1f))
			+ B + 1720996.5f + day + utime/24f;
		return julianDay;
	}

	// Utility used by asteroid/comet to find period of orbit
	private const float AU_METERS = 1.496e11f;
	private const float G = 6.67408e-11f; // m^3 s^(-2) kg^(-1)
	private const float SECS_PER_YEAR = 31557600f;

	/// <summary>
	/// Gets the period of a solar body around the Sun in Years
	/// </summary>
	/// <returns>The period.</returns>
	/// <param name="sbody">Sbody.</param>
	public static float GetPeriodYears(SolarBody sbody) {
		float a = sbody.a * AU_METERS;
		float mu = G * SolarSystem.mass_sun * 1e24f;
		float periodYrs = Mathf.Sqrt((4f*Mathf.PI*Mathf.PI*a*a*a)/mu)/SECS_PER_YEAR;
		return periodYrs;
	}

	// Utility Methods used by Custom Inspector.
	public static System.DateTime DateForEpoch(float epoch) {

		int years = (int) Mathf.Floor(epoch);
		int numDays = 365; 
		if (System.DateTime.IsLeapYear(years))
			numDays = 366;
		float days = Mathf.Round((epoch - years)*numDays); 
		System.DateTime date = new System.DateTime(years, 1, 1); 
		return date.AddDays(days);
	}

	public static float DateTimeToEpoch(System.DateTime datetime) {
		float numDays = 365f; 
		if (System.DateTime.IsLeapYear(datetime.Year))
			numDays = 366f;
		float epoch = datetime.Year + (datetime.DayOfYear-1)/numDays; 
		return epoch;
	}

	// JD are days from -4712 (noon)
	// MJD start from midnight and remove the leading digits of 2400000 (this makes them valid
	// for 300 years from Nov 17, 1858.)
	public static float ConvertMJDtoAD(float mjd) {
		float years = (mjd + 2400000.5f)/365.25f;
		return years - 4712f;
	}

}
