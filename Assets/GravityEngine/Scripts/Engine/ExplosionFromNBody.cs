using UnityEngine;
using System.Collections;


/// <summary>
/// Explosion from N body.
/// Generates the particle positions/velocities for an "ejection cone" of debris when NBody objects collide. 
///
/// This script is typically part of an explosion prefab object (with a GravityParticles component) that is then
/// referenced by an NBodyCollision object. The NBodyCollision component will initialize via calling Init() and then
/// the particles will be created according to the inspector parameters. 
///
/// </summary>
public class ExplosionFromNBody : MonoBehaviour, IGravityParticlesInit {

	private Vector3 contactPoint; 
	private Vector3 normal; // Axis indicating direction of ejection cone
	private Vector3 bodyVelocity; 

	//! Cone width (half-angle in degrees from cone axis)
	public float coneWidth = 30f; 
	//! Size of the explosion (will control initial velocity based on mass of source NBody object)
	public float explosionSize = 20f; 
	//! Range of values in particle velocity. 
	public float velocitySpread = 0; //! Velocity distribution factor (standard deviation of Gaussian)

	public float soften = 1.0f;

	private float explosionVelocity; 

	Quaternion rotateToNormal; 

	void Start() {

	}

	public void Init(NBody fromNbody, Vector3 contactPoint) {
		Vector3 zAxis = new Vector3(0, 0, 1f);
		this.normal = Vector3.Normalize(contactPoint - fromNbody.transform.position);
		rotateToNormal = Quaternion.FromToRotation(zAxis, normal);
		this.contactPoint = contactPoint;
		float startRadius = Vector3.Distance(contactPoint, fromNbody.transform.position);
		// ensure particles start outside the capture radius of the body
		// size is the diameter
		if (startRadius < fromNbody.size/2f) {
			startRadius = 1.2f * (float) fromNbody.size/2f;
			this.contactPoint = fromNbody.transform.position + startRadius * normal;
			#pragma warning disable 162		// disable unreachable code warning
			if (GravityEngine.DEBUG)
				Debug.Log("DEBUG: setting contact based on capture size. ");
			#pragma warning restore 162		
		}
		explosionVelocity = ExplosionVelocity(fromNbody, startRadius);
		bodyVelocity = GravityEngine.instance.GetScaledVelocity( fromNbody.transform.gameObject);
	}

	public void InitNewParticles(int fromParticle, int toParticle, ref double [,] r, ref double[,] v) {
		#pragma warning disable 162		// disable unreachable code warning
			if (GravityEngine.DEBUG)
				Debug.Log("Create explosion: contact=" + contactPoint + " normal=" + normal);
		#pragma warning restore 162		

		for (int i=fromParticle; i < toParticle; i++) {
			r[i,0] = (float) contactPoint.x; 
			r[i,1] = (float) contactPoint.y; 
			r[i,2] = (float) contactPoint.z; 
			// to generate the velocity for cone around z-axis then rotate into place
			float offset = Mathf.Sin(Mathf.Deg2Rad * coneWidth) * Random.value;
			float angle = 2f * Mathf.PI * Random.value;
			Vector3 velocity = new Vector3( offset * Mathf.Sin(angle), offset * Mathf.Cos(angle), 1f);
			float velocityScale = NUtils.GaussianValue(explosionVelocity, velocitySpread);
			Vector3 scaledVelocity =  velocityScale * Vector3.Normalize(velocity);
			Vector3 finalVelocity = rotateToNormal * scaledVelocity + bodyVelocity;
			v[i,0] = (float) finalVelocity.x;
			v[i,1] = (float) finalVelocity.y; 
			v[i,2] = (float) finalVelocity.z;
		}
		#pragma warning disable 162		
		if (GravityEngine.DEBUG)
			Debug.Log(string.Format("Explosion particle 0: v=({0} ,{1}, {2}) x=({3}, {4}, {5} vesc={6} vmag={7})", 
							v[0,0], v[0,1], v[0,2], r[0,0], r[0,1], r[0,2], explosionVelocity, 
							System.Math.Sqrt(v[0,0]*v[0,0] +  v[0,1]*v[0,1]+ v[0,2]*v[0,2]) ));
		#pragma warning restore 162		
	}

	private float ExplosionVelocity(NBody nbody, float startRadius) {
		// Determine the explosion velocity based on the size requested for the explosion
		// At the time of maximum size, kinetic energy is zero, potential energy is - M m/R
		// (Particle mass cancels out of the delta eqn)
		#pragma warning disable 162		
		if (GravityEngine.DEBUG)
			Debug.Log("start=" + startRadius + " esize=" + explosionSize);
		#pragma warning restore 162		
		// integrators apply mass scale to velocity - so skip it here
		float energy = -1f * nbody.mass /(explosionSize/GravityEngine.instance.physToWorldFactor);
		float peStart = -1f * nbody.mass /(startRadius/GravityEngine.instance.physToWorldFactor);
//		float energy = -1f * nbody.mass * GravityEngine.instance.massScale/(explosionSize/GravityEngine.instance.physToWorldFactor);
//		float peStart = -1f * nbody.mass * GravityEngine.instance.massScale/(startRadius/GravityEngine.instance.physToWorldFactor);
		float keStart = energy - peStart;
		//
		// Numerical issues:
		// A high initial velocity can result in particles tunneling out of the potential in a single step 
		// and the resulting explosion size is not acheived. (In practice it would get held back more during this evolution)
		return soften * Mathf.Sqrt(2f * keStart);
	}



}

