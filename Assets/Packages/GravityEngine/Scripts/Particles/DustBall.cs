using UnityEngine;
using System.Collections;

/// <summary>
/// Dust ball.
/// Create particles in a sphere with a given initial velocity.
///
/// Must be attached to a particle system with a GravityParticles component.
/// </summary>
[RequireComponent(typeof(GravityParticles))]
public class DustBall : MonoBehaviour, IGravityParticlesInit {

	//! Velocity of each particle when it is initalized. 
	public Vector3 velocity; 

	//! Radius of the ball of particles.
	public float radius = 1f;


	public void InitNewParticles(int numLastActive, int numActive, ref double [,] r, ref double[,] v) {
		for (int i=numLastActive; i < numActive; i++) {
			Vector3 pos = transform.position/GravityEngine.instance.physToWorldFactor + 
					Random.insideUnitSphere * radius/GravityEngine.instance.physToWorldFactor;
			// track randomSeed values as a way to see if particle has expired and been re-emitted
			r[i,0] = (float) pos.x; 
			r[i,1] = (float) pos.y; 
			r[i,2] = (float) pos.z; 
			v[i,0] = (float) velocity.x;
			v[i,1] = (float) velocity.y; 
			v[i,2] = (float) velocity.z;
		}
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.DrawSphere(transform.position, radius);
		// TODO - draw velocity as a Ray
	}
}

