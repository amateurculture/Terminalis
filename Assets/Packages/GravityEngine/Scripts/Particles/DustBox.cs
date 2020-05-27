using UnityEngine;
using System.Collections;

/// <summary>
/// Dust box.
/// Create particles in a 3D rectangle with a specified initial velocity. 
///
/// Must be attached to a particle system with a GravityParticles component.
/// </summary>
[RequireComponent(typeof(GravityParticles))]
public class DustBox : MonoBehaviour, IGravityParticlesInit {
	
	//! box size in X (-X/2..X/2)
	public float x_size; 
	//! box size in Y (-Y/2..Y/2)
	public float y_size; 
	//! box size in Z (-Z/2..Z/2)
	public float z_size;
	//! uniform velocity added to each particle
	public Vector3 velocity; 

	//! Box rotation (Use instead of transform rotation) 
	public Vector3 rotation;

	private GravityEngine nbodyEngine;

	public void InitNewParticles(int fromParticle, int toParticle, ref double[,] r, ref double[,] v) {
		Quaternion rotationQ = Quaternion.Euler(rotation);

		for (int i=fromParticle; i < toParticle; i++) {

			Vector3 position = transform.position + rotationQ * 
						new Vector3(Random.Range(-x_size, x_size), 
									Random.Range(-y_size, y_size), 
									Random.Range(-z_size, z_size));
			r[i,0] = position.x/GravityEngine.instance.physToWorldFactor;
			r[i,1] = position.y/GravityEngine.instance.physToWorldFactor;
			r[i,2] = position.z/GravityEngine.instance.physToWorldFactor;
			v[i,0] = (float) velocity.x;
			v[i,1] = (float) velocity.y; 
			v[i,2] = (float) velocity.z;		
		}
	}

	void OnDrawGizmosSelected()
	{
		Quaternion rotationQ = Quaternion.Euler(rotation);
		// eight vertices
		Vector3 cornerTLF = transform.position + rotationQ *  new Vector3(-x_size, y_size, -z_size);
		Vector3 cornerTRF = transform.position + rotationQ *  new Vector3(x_size, y_size, -z_size);
		Vector3 cornerBLF = transform.position + rotationQ *  new Vector3(-x_size, -y_size, -z_size);
		Vector3 cornerBRF = transform.position + rotationQ *  new Vector3(x_size, -y_size, -z_size);
		Vector3 cornerTLB = transform.position + rotationQ *  new Vector3(-x_size, y_size, z_size);
		Vector3 cornerTRB = transform.position + rotationQ *  new Vector3(x_size, y_size, z_size);
		Vector3 cornerBLB = transform.position + rotationQ *  new Vector3(-x_size, -y_size, z_size);
		Vector3 cornerBRB = transform.position + rotationQ *  new Vector3(x_size, -y_size, z_size);
		// "front" face
		Gizmos.DrawLine(cornerBLF, cornerTLF );
		Gizmos.DrawLine(cornerTLF, cornerTRF );
		Gizmos.DrawLine(cornerTRF, cornerBRF );
		Gizmos.DrawLine(cornerBRF, cornerBLF );
		// "back" face
		Gizmos.DrawLine(cornerBLB, cornerTLB );
		Gizmos.DrawLine(cornerTLB, cornerTRB );
		Gizmos.DrawLine(cornerTRB, cornerBRB );
		Gizmos.DrawLine(cornerBRB, cornerBLB );
		// Between
		Gizmos.DrawLine(cornerBLF, cornerBLB );
		Gizmos.DrawLine(cornerTLF, cornerTLB );
		Gizmos.DrawLine(cornerTRF, cornerTRB );
		Gizmos.DrawLine(cornerBRF, cornerBRB );

	}
}
