using UnityEngine;
using System.Collections;

/// <summary>
/// Interface defining methods to be implemented to define particle positions and velocities for GravityParticles.
/// </summary>
public interface IGravityParticlesInit  {

	/// <summary>
	/// Provide the initial positions and velocity for a range of particles. This method will be called
	/// as particles are created by the particle system.  The implementing class must fill in the r[,] and
	/// v[,] arrays for the range specified. These arrays are indexed by [particle_num, dimension] where
	/// dimension 0,1,2 correspond to x,y,z.
	///
	/// See the DustBox script for a sample usage of this interface.
	///
	/// </summary>
	/// <param name="fromParticle">From particle number.</param>
	/// <param name="toParticle">To particle number.</param>
	/// <param name="r">(out) 2D array [numParticles, 3] to hold physics position (x,y,z) per particle</param>
	/// <param name="v">(out) 2D array [numParticles, 3] to hold velocity (x,y,z) per particle</param></param>
	void InitNewParticles(int fromParticle, int toParticle, ref double [,] r, ref double[,] v);
}
