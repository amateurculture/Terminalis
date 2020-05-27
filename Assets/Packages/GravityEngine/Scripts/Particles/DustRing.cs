using UnityEngine;
using System.Collections;

/// <summary>
/// Dust ring.
/// Create a ring of particles in orbit around an NBody mass. Allows full control over the orbital attributes
/// of the ring particles. 
///
/// Must be attached to a particle system with a GravityParticles component.
/// </summary>
[RequireComponent(typeof(GravityParticles))]
public class DustRing : EllipseBase, IGravityParticlesInit {
	
	//
	// Create a ring of particles in orbit around a specific GameObject with an attached NBody script
	// Must be called once the position and velocity of the NBody has been initialized

	//! Width of particle ring as a percent of ring radius. 
	public float ringWidthPercent;

	// Use this for initialization
	void Start () {

		Init();	// call EllipseBase.Init()
		ApplyScale( GravityEngine.Instance().GetLengthScale());

		// if the ring is too small will break particle system. 
		if (a < 1E-3) {
			Debug.LogError("Ring radius too small - setting to 1");
			a = 1f;
		}

	}
	
	public void InitNewParticles(int numLastActive, int numActive, ref double [,] r, ref double[,] v) {
		// 
		// init the particles and put them in the ring positions, then determine the velocity required
		// position:
		// pick random positions within the radius +/- width/2
		for (int i=numLastActive; i < numActive; i++) {
			float f = Random.Range(0, 2*Mathf.PI);

			float physicalScale = GravityEngine.instance.physToWorldFactor;
			float a_phy =  a_scaled/physicalScale;
			a_phy = Random.Range( a_phy * (1f-0.5f*ringWidthPercent), a_phy * (1f + 0.5f*ringWidthPercent));
			// Phase is TRUE anomoly f
			// Murray and Dermott(2.26)
			// This should really be (M+m), but particles have m=0
			float massScale = GravityEngine.Instance().massScale;
			float n = Mathf.Sqrt( (float)(centerNbody.mass * massScale) /(a_phy*a_phy*a_phy));
			// (2.36)
			float denom = Mathf.Sqrt( 1f - ecc*ecc);
			float xdot = -1f * n * a_phy * Mathf.Sin(f)/denom;
			float ydot = n * a_phy * (ecc + Mathf.Cos(f))/denom;

			// Murray and Dermot (2.20)
			float radius = a_phy * (1f - ecc*ecc)/(1f + ecc * Mathf.Cos(f));
			Vector3 position_xy = new Vector3( radius * Mathf.Cos(f), radius * Mathf.Sin(f), 0);
			// move from XY plane to the orbital plane and scale to world space
			// orbit position is WRT center
			Vector3 pos =  ellipse_orientation * position_xy;
			pos += centerObject.transform.position/physicalScale;
			pos = physicalScale * pos;
			r[i,0] = pos.x;
			r[i,1] = pos.y;
			r[i,2] = pos.z;
		
			Vector3 v_xy = new Vector3( xdot, ydot, 0);
			Vector3 vVec = ellipse_orientation * v_xy + centerNbody.vel_phys;
			v[i,0] = vVec.x;
			v[i,1] = vVec.y;
			v[i,2] = vVec.z;
		}
	}

	/// <summary>
	/// Apply scale to the orbit. This is used by the inspector scripts during
	/// scene setup. Do not use at run-time.
	/// </summary>
	/// <param name="scale">Scale.</param>
	public void ApplyScale(float scale) {
		if (paramBy == ParamBy.AXIS_A){
			a_scaled = a * scale;
		} else if (paramBy == ParamBy.CLOSEST_P) {
			p_scaled = p * scale; 
		}
	}

}
