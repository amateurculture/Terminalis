using UnityEngine;
using System.Collections;

/// <summary>
/// Common interface for numerical NBody integration. Used by the GravityEngine. 
///
/// Index Tracking:
/// - GravityEngine tracks adds/removes and maintains an index list. The positions and velocities are in arrays in 
///   gravity engine (since they are common to all bodies and needed for game object position updates)
///
/// This interface is not normally called from developer scripts. 
/// </summary>
public interface INBodyIntegrator {

	/// <summary>
	/// Setup the specified maxBodies and timeStep. Must be called prior to PreEvolve/Evolve
	/// </summary>
	/// <param name="maxBodies">Max bodies.</param>
	/// <param name="timeStep">Time step.</param>
    void Setup(int maxBodies, double timeStep);

    /// <summary>
    /// Clear internal information so no bodies are evolved. 
    /// </summary>
    void Clear();

    /// <summary>
    /// Clone this instance, replicating internal arrays. 
    /// Copy across any internal state required for continued evolution.
    /// </summary>
	INBodyIntegrator DeepClone();


    /// <summary>
    /// Adds the N body. Implementation may support the case where the initial number of bodies 
    /// in Setup() is exceeded, creating space, or may ignore additions that exceed this limit (e.g. AZTTriple). 
    /// </summary>
	/// <param name="nbody">NBody component</param>
	/// <param name="position">Physics position</param>
	/// <param name="velocity">Physics velocity</param>
	void AddNBody( int bodyNum, NBody nbody, Vector3 position, Vector3 velocity);

    /// <summary>
    /// Removes NBody at index i.
    /// </summary>
    /// <param name="atIndex">At index.</param>
	void RemoveBodyAtIndex(int atIndex);

	/// <summary>
	/// Grows the arrays. Called by GravityEngine when it changes internal data sizes.
	/// </summary>
	/// <param name="growBy">Grow by.</param>
	void GrowArrays(int growBy);

    Vector3 GetVelocityForIndex(int i); 

	void GetVelocityDoubleForIndex(int i, ref double[] v); 

	void SetVelocityForIndex(int i, Vector3 vel);

    void SetVelocityDoubleForIndex(int i, ref double[] v);

    Vector3 GetAccelerationForIndex(int i);

    string GetExternalAccelForIndex(int i);

    // Call ONCE after all game objects have been added to allow integrator to pre-calc
    // starting quantities required for integration
    //
    // To avoid copies of mass and position arrays in integrators provide
    // a reference to these for Evolve and PreEvolve
    //
    // Some implementations (e.g. AZTriple) may make their own copies and then 
    // copy the result back into these arrays

    /// <summary>
    ///Call ONCE after all game objects have been added to allow integrator to pre-calc
    /// starting quantities required for integration
    ///
    /// To avoid copies of mass and position arrays in integrators provide
    /// a reference to these for Evolve and PreEvolve
    ///
    /// Some implementations (e.g. AZTriple) may make their own copies and then 
    /// copy the result back into these arrays	/// </summary>
    /// <param name="m">M.</param>
    /// <param name="r">The red component.</param>
    /// <param name="info">Info.</param>
    void PreEvolve(GravityState gravityState, ref byte[] info); 

	/// <summary>
	/// Evolve the specified time, m, r and info.
	/// 
	/// Integrators will evolve for at least as long as the specified time, but can overshoot. The
	/// GravityEngine will correct on the next cycle. 
	/// </summary>
	/// <param name="time">Time.</param>
	/// <param name="m">M.</param>
	/// <param name="r">The red component.</param>
	/// <param name="info">Info.</param>
	double Evolve(double time, GravityState gravityState, ref byte[] info); 

	/// <summary>
	/// Gets the energy of the system.
	/// </summary>
	/// <returns>The energy.</returns>
	/// <param name="mass">Mass.</param>
	/// <param name="pos">Position.</param>
	float GetEnergy( GravityState gravityState);

	/// <summary>
	/// Gets the initial energy.
	/// </summary>
	/// <returns>The initial energy.</returns>
	/// <param name="mass">Mass.</param>
	/// <param name="pos">Position.</param>
	// TODO - just have GE do this at the start?
	float GetInitialEnergy( GravityState gravityState);
	
}
