using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

/// <summary>
/// Binary Pair
///
/// Configures the initial velocities for two roughly equal masses to establish their
/// elliptical orbits around the center of mass of the pair. 
///
/// Must have two NBody objects as immediate children.
/// 
/// The BinaryPair object can optionally have a zero-mass NBody and Orbit component attached. This will be used to 
/// determine the initial conditions for the binary pair (position and velocity) so the binary pair can be placed in orbit
/// around another body. This "dummy" NBody is only used during the setup. (It will continue to evolve but there is no model 
/// attached and nothing in the scene will be affected by its zero mass.
/// 
/// </summary>
public class BinaryPair :  EllipseBase, IOrbitScalable {

    //! Velocity of center of mass of the binary pair
    public Vector3 initialPosition;

    // position of the center of mass in physics co-ordinates
    private Vector3 cmPosition;

	public Vector3 velocity;

	private NBody body1; 
	private NBody body2;

    //! optional NBody used if this binary is to be placed in orbit around another body
    private NBody binaryNbody;

	public void SetupBodies () {

		if (transform.childCount != 2) {
			// Binary must have 2 NBody children
			Debug.LogError("Must have exactly two Nbody objects attached");
			return;
		}
		body1 = transform.GetChild(0).GetComponent<NBody>();
		body2 = transform.GetChild(1).GetComponent<NBody>();
		if (body1 == null || body2 == null) {
			Debug.LogError("Binary requires children to have NBody scripts attached.");
			return;
		}

        if (transform.parent) {
            binaryNbody = transform.parent.GetComponent<NBody>();
            if (binaryNbody) {
                binaryNbody.InitPosition(GravityEngine.Instance());
                cmPosition = GravityEngine.Instance().GetPhysicsPosition(binaryNbody);
            }
        }

		// mass is scaled by GE
		float m_total = (body1.mass + body2.mass);
		float mu1 = body1.mass/m_total;
		float mu2 = body2.mass/m_total;
		SetupBody( body1, a_scaled * mu2,  mu2 * mu2 * body2.mass, false);
		SetupBody( body2, a_scaled * mu1,  mu1 * mu1 * body1.mass, true);
	}

	/// <summary>
	/// Calculate velocity for binary members and assign.
	/// reflect: One of the bodies positions and velocities must be reflected to opposite side of ellipse
	/// </summary>
	private void SetupBody(NBody nbody, float a_n, float mu, bool reflect) {
		float a_phy = a_n/GravityEngine.instance.physToWorldFactor;
		// Phase is TRUE anomoly f
		float f = phase * Mathf.Deg2Rad;
		float reflect_in_y = 1f;
		if (reflect) {
			reflect_in_y = -1f;
			//f = f + Mathf.PI;
		}

		// Murray and Dermott (2.20)
		float r = a_phy * (1f - ecc*ecc)/(1f + ecc * Mathf.Cos(f));
		// (2.26) mu = n^2 a^3  (n is period, aka T)
		float n = Mathf.Sqrt( mu * GravityEngine.Instance().massScale/(a_phy*a_phy*a_phy));
		// (2.36)
		float denom = Mathf.Sqrt( 1f - ecc*ecc);
		float xdot = -1f * n * a_phy * Mathf.Sin(f)/denom;
		float ydot = n * a_phy * (ecc + Mathf.Cos(f))/denom;

		Vector3 position_xy = new Vector3( reflect_in_y * r * Mathf.Cos(f), r* Mathf.Sin(f), 0);
		// move from XY plane to the orbital plane and scale to world space
		// orbit position is WRT center
		Vector3 position =  ellipse_orientation * position_xy;
        // ISSUE: Problematic in case where added after the fact
		position += cmPosition;
		nbody.initialPhysPosition = position;

		Vector3 v_xy = new Vector3( xdot, ydot, 0);
		v_xy *= reflect_in_y;
        // velocity will be scaled when NBody is scaled
        Vector3 vel_phys = Vector3.zero;
        if (binaryNbody != null) {
            // use CM velocity from parent for e.g. binary in orbit around something
            vel_phys = binaryNbody.vel_phys;
        } else {
            vel_phys = velocity * GravityScaler.GetVelocityScale();
        }
		nbody.vel_phys = ellipse_orientation * v_xy + vel_phys;

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
        if (binaryNbody != null) {
            // this binary will have orbit w.r.t something. Make sure that update has been done
            // check for initial condition setup (e.g. initial conditions for elliptical orbit)
            GravityEngine ge = GravityEngine.Instance();
            if (binaryNbody.engineRef == null) {
                INbodyInit initNbody = GetComponent<INbodyInit>();
                if (initNbody != null) {
                    initNbody.InitNBody(ge.physToWorldFactor, ge.massScale);
                }
                cmPosition = binaryNbody.initialPhysPosition;
            } else {
                cmPosition = ge.GetPhysicsPosition(binaryNbody);
            }
        } else {
            cmPosition = transform.position * scale / GravityEngine.instance.physToWorldFactor;
        }
        UpdateOrbitParams();
        // if there is a binaryNbody, then need to determine the CM position and velocity before the 
		SetupBodies();
	}

#if UNITY_EDITOR

	/// <summary>
	/// Displays the path of the elliptical orbit when the object is selected in the editor. 
	/// </summary>
	void OnDrawGizmosSelected()
	{
		if (transform.childCount != 2)
			return;

		// need to have a center to draw gizmo.
		body1 = transform.GetChild(0).GetComponent<NBody>();
		body2 = transform.GetChild(1).GetComponent<NBody>();
		if (body1 == null || body2 == null) {
			return;
		}
		// only display if this object is directly selected
		if (Selection.activeGameObject != transform.gameObject) {
			return;
		}
		float m_total = (float) (body1.mass + body2.mass);
		float mu1 = body1.mass/m_total;
		float mu2 = body2.mass/m_total;

        if (transform.parent != null) {
            binaryNbody = transform.parent.GetComponent<NBody>();
        }

        UpdateOrbitParams();
		CalculateRotation();

		DrawEllipse( a_scaled * mu2, transform.position, false );
		DrawEllipse( a_scaled * mu1, transform.position, true );
		// move bodies to location specified by parameters
		SetTransform( a_scaled * mu2, body1, false);
		SetTransform( a_scaled * mu1, body2, true);
	}

	private void DrawEllipse(float a_n, Vector3 focusPos, bool reflect) {
		Vector3 oldPosition = transform.position;
		const float NUM_STEPS = 100f; 
		const int STEPS_PER_RAY = 10; 
		float reflect_in_y = 1f;
		if (reflect) {
			reflect_in_y = -1f;
		}
		int rayCount = 0; 
		float dtheta = 2f*Mathf.PI/NUM_STEPS;
		float r = 0; 
		float phaseRad = phase * Mathf.Deg2Rad;
		for (float theta=0f; theta < 2f*Mathf.PI; theta += dtheta) {
			// draw a ellipe - using equation centered on focus (true anomoly)
			r = a_n * ( 1f - ecc * ecc)/(1f + ecc * Mathf.Cos(theta+phaseRad));
			
			Vector3 position = new Vector3(reflect_in_y * r * Mathf.Cos (theta+phaseRad), 
			                               r * Mathf.Sin (theta+phaseRad), 
			                               0);
			// move from XY plane to the orbital plane
			Vector3 newPosition = ellipse_orientation * position; 
			// orbit position is WRT center
			newPosition += focusPos;
			Gizmos.DrawLine(oldPosition, newPosition );
			rayCount = (rayCount+1)%STEPS_PER_RAY;
			if (rayCount == 0) {
				Gizmos.DrawLine(focusPos, newPosition );
			}
			oldPosition = newPosition;
		}

	}

	private void SetTransform(float a_n, NBody nbody, bool reflect) {
		float phaseRad = phase * Mathf.Deg2Rad;
		float reflect_in_y = 1f;
		if (reflect) {
			reflect_in_y = -1f;
			phaseRad = 2f * Mathf.PI - phaseRad;
		}
		// evolution uses true anomoly (angle from  center)
		float r = a_n * ( 1f - ecc* ecc)/(1f + ecc * Mathf.Cos(phaseRad));
		
		Vector3 pos = new Vector3( reflect_in_y * r * Mathf.Cos(phaseRad), 
		                          r * Mathf.Sin(phaseRad), 
		                          0);
		// move from XY plane to the orbital plane
		Vector3 new_p = ellipse_orientation * pos;
        // orbit position is WRT center
        if (binaryNbody != null) {
            nbody.transform.position = new_p + binaryNbody.transform.position;
        } else {
            nbody.transform.position = new_p + transform.position;
        }
	}

#endif
}
