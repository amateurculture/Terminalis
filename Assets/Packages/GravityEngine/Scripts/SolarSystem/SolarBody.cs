using UnityEngine;
using System.Collections;

/// <summary>
/// Solar body.
/// A "data container" delegate used to hold information about a Solar System body. 
/// This is a collection of orbital parameters and some additional facts (mass, radius)
/// that is useful for scaling and configuring the solar system. 
///
/// This object acts as the keeper of the "real data" and it's values are used when the 
/// "Restore Defaults" is triggered in the SolarSystem inspector or the scale is changed. 
///
/// Typically only referenced in SolarSystem and filled in from one of the creation classes:
/// PlanetData, AsteroidData or CometData. 
/// </summary>
public class SolarBody : MonoBehaviour  {

	// Orbit parameters (user control via FixedEllipseEditor)
	// These parameters are in world space. 
	//! eccentricity (0..1, 0=circle, 1=linear)
	public float ecc; 			

	// Allow EITHER a or p to specify size (JPL data uses a for planets and p for comets, so both are useful)
	//! semi-major axis - based on paramBy user can specify a OR p. a = p/(1-ecc)
	public float a = 10f; 
	public float p = 10f; 
				
	//! "longitude of ascending node" - angle from x-axis to line from focus to pericenter
	public float omega_uc; 		
	//! "argument of perienter" - angle from ascending node to pericenter
	public float omega_lc; 		
	public float omega_lc_bar;	
	//! inclination (degrees!)
	public float inclination; 	
	//! longitude
	public float longitude; 	

	//! Orbit element variations
	public float a_dot;	
	public float ecc_dot;	
	public float inclination_dot;	
	public float omega_lc_bar_dot;	
	public float omega_uc_dot;	
	public float longitudeDot;	

	/// <summary>
	/// Mass/name/size are filled in when created via SolarSystemData. These hold the original values
    /// to allow the values to be restored after "what-if" experiments tweaking with values in the NBody.
	/// </summary>
	//! absolute mass in 10^24 kg (when created via SolarSystemData)
	public float mass_1E24; 

	//! Scale size of the object
	public float radiusKm; 

	public SolarSystem.Type bodyType; 
	//public float meanAnomoly; 
	public float epoch; 

	//! Solar System this object belongs to
	public SolarSystem solarSystem; 

	public void UpdateOrbit() {
		solarSystem.InitOrbit(this);
	}

	public void CopyFrom(SolarBody sb) {
		this.a = sb.a;
		this.omega_lc = sb.omega_lc;
		this.omega_uc = sb.omega_uc;
		this.inclination = sb.inclination;
		this.ecc = sb.ecc;
		this.longitude = sb.longitude;
		this.name = sb.name;
		this.radiusKm = sb.radiusKm;
		this.bodyType = sb.bodyType;
		this.mass_1E24 = sb.mass_1E24;
		this.epoch = sb.epoch;

		this.a_dot = sb.a_dot;
		this.ecc_dot = sb.ecc_dot;
		this.inclination_dot = sb.inclination_dot;
		this.omega_lc_bar_dot = sb.omega_lc_bar_dot;
		this.omega_uc_dot = sb.omega_uc_dot;
		this.longitudeDot = sb.longitudeDot;

	}

	/// <summary>
	/// Sets the ellipse parameters for epoch provided.
	/// </summary>
	/// <param name="epoch">Epoch.</param>
	/// <param name="ellipseBase">Ellipse base.</param>
	public void SetEllipseForEpoch(float epoch, EllipseBase ellipseBase) {

		switch(bodyType) {
		case SolarSystem.Type.PLANET:
			PlanetData.SetEllipse(epoch, ellipseBase, this);
			break;
		case SolarSystem.Type.ASTEROID:
			AsteroidData.SetEllipse(epoch, ellipseBase, this);
			break;
		case SolarSystem.Type.COMET:
			CometData.SetEllipse(epoch, ellipseBase, this);
			break;

		}
	}


}

