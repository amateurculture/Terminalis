using UnityEngine;
using System.Collections;

/// <summary>
/// Interface defining method to initialize an NBody object prior to evolution beginning. Called by GravityEngine. 
/// </summary>
public interface INbodyInit  {

	/// <summary>
	/// Inits the N body.
	/// Called prior to evolution starting. Allows NBody object to adjust its position and velocity prior to 
	/// evolution beginning. 
	/// </summary>
	/// <param name="physicalScale">Physical scale.</param>
	void InitNBody(float physicalScale, float massScale);
}
