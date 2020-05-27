using UnityEngine;
using System.Collections;


/// <summary>
/// Interface defining the fixed motion of an NBody object. 
///
/// Scripts implementing this interface must be attached to a game object that also has an NBody component.
///
/// Objects with fixed motion will have their mass
/// used to create the global gravitational field but will not be affected by that field. Their motion is
/// defined by the Evolve() method. When called they are responsible for updating their position. 
/// </summary>
public interface IFixedOrbit  {

	/// <summary>
	/// Check if body is configured at scene start to be fixed. (Allows objects to be optionally configured
	/// as not fixed, to allow e.g. Kepler eqn vs initial velocity in OrbitEllipse)
	/// </summary>
	/// <returns><c>true</c> if this instance is fixed; otherwise, <c>false</c>.</returns>
	bool IsOnRails();

	/// <summary>
	/// Called for each NBody object prior to evolution beginning. Allows a chance to setup internal state. 
	/// </summary>
	/// <param name="physicalScale">Physical scale.</param>
	/// <param name="massScale">Mass scale.</param>
	void PreEvolve(float physicalScale, float massScale);

	/// <summary>
	/// Evolve the NBody. Implementating method uses physics time and scale to compute the new position, 
	/// placing it in r. Velocity is also updated so that GetVelocity() can request it if it is of interest.
	/// </summary>
	/// <param name="physicsTime">Current Physics time.</param>
	/// <param name="physicalScale">Physical scale.</param>
	/// <param name="r">Position in physics space (x, y, z). OUTPUT by the method </param>
	void Evolve(double physicsTime,  ref double[] r);

    /// <summary>
    /// Get the current velocity in physics engine units. 
    /// </summary>
    /// <returns></returns>
    Vector3 GetVelocity();

    Vector3 GetPosition();

    /// <summary>
    /// Perform and update of the world game object position and velocity based on the internal state
    /// </summary>
    void GEUpdate(GravityEngine ge);

    /// <summary>
    /// Handle dynamic origin shift
    /// </summary>
    /// <param name="position"></param>
    void Move(Vector3 position);

    /// <summary>
    /// Set the NBody that the orbit element will evolve. This is not commonly used, since the NBody is
    /// typically attached to the same game object as the FixedOrbit. The exception is when fixed orbit
    /// segments are part of a KeplerSequence (patched-conic evolution). 
    /// </summary>
    /// <param name="nbody"></param>
    void SetNBody(NBody nbody);

    /// <summary>
    /// ApplyImpulse in Kepler mode. 
    /// (Currently only OrbitUniversal does this)
    /// </summary>
    /// <returns></returns>
    Vector3 ApplyImpulse(Vector3 impulse);

    /// <summary>
    /// Return the center NBody for the fixed orbit
    /// </summary>
    /// <returns></returns>
    NBody GetCenterNBody();

    /// <summary>
    /// Update the position and velocity of the object in the orbit. 
    /// 
    /// Currently only implemented in OrbitUniversal for changes in the center body due to patched conic
    /// transitions. 
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="vel"></param>
    void UpdatePositionAndVelocity(Vector3 pos, Vector3 vel);

    /// <summary>
    /// Create a string with info for display in GEConsole
    /// </summary>
    /// <returns></returns>
    string DumpInfo();
}
