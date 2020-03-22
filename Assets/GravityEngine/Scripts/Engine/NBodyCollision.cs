using UnityEngine;
using System.Collections;

/// <summary>
/// N body collision.
/// Script that is attached to NBody models (i.e. the mesh model that is a child of an NBody object) to handle
/// collisions. Allows a choice of absorb, bounce or explode and adjusts physics as required to conserve momentum.
///
/// NBodyCollision is trigger based so the model game objects MUST:
/// - have a Collider attached with IsTrigger true
/// - have a RigidBody attached with IsKinematic true and RigidBody mass set to a very small value
///
/// Thanks to JAB for the EXPODE_OR_BOUNCE code.
///
/// </summary>
public class NBodyCollision : MonoBehaviour {

	/*
	ABSORB
	- want to wait until one body is mostly inside the other before deactivating
	- efficient to do this before they move through each other (could cause timestep to shrink, slow integration)
	- need to know size of the two objects and assume they are spheres
	- should do consv of momtm and impart impulse to other body
	BOUNCE
	- can handle immediatly -> work done in NBE
	EXPLODE
	- same as ABSORB but need to trigger particle(s) at the same time as enveloping
    EXPLODE_OR_BOUNCE
    - explode if relative velocity above a minimum, otherwise bounce (e.g. for very low-velocity collisions)

	*/
	//! Type of collision response
	public enum CollisionType {ABSORB_IMMEDIATE, ABSORB_ENVELOP, EXPLODE, BOUNCE, EXPLODE_OR_BOUNCE};

	public CollisionType collisionType = CollisionType.ABSORB_IMMEDIATE;
	//!Precedence of collision script if both bodies have them. Lower takes precedence.
	public int collisionPrecedence = 1;
    //! Game object holding the explosion prefab. Must be inactive and have a DustBallFromNBody script.
    public GameObject explosionPrefab;

    //! Velocity threshold for explosion if EXPLODE_OR_BOUNCE type
    public float explodeOrBounceVelocity = 0;

    //! (For a bounce collision) This factor is applied to determine the recoil velocities. 1 indicates no energy loss in the collision.
    public float bounceFactor = 1f;

    //! Optional collision layer. If set, require objects to be on this layer for OnTrigger processing
    public string collisionLayer = null; 


    private float lastDistance; 
	private float myRadius;
	private bool prefabOk = false;
    private bool bouncing = false; // use in explode_or_bounce to cache in TriggerEnter and avoid recalculation

	private bool inactivate; 

	// Use this for initialization
	void Start () {
		// assume a sphere - so just take one co-ordinate as the radius
		myRadius = transform.localScale.x;
		if (explosionPrefab != null) {
			prefabOk = true;
			if (explosionPrefab.GetComponent<GravityParticles>() == null) {
				Debug.LogError("Explosion prefab does not have a GravityParticles component");
				prefabOk = false;
			}
			if (explosionPrefab.GetComponent<ExplosionFromNBody>() == null) {
				Debug.LogError("Explosion prefab does not have a ExplosionFromNBody component");
				prefabOk = false;
			}
		}
        // Check that component is not directly attached to NBody (needs to be on a child)
        // This allows child scale to controlled by child transform while GE updates NBody transform.
        // http://nbodyphysics.com/blog/gravity-engine-doc-1-3-2-2-2/doc-2/#NBodyCollision
        if (GetComponent<NBody>() != null)
            Debug.LogWarning("Detected an NBody component. NBodyCollision should be attached to a child of an NBody");
	}
	
	/*
	NBE object collisions using this script are independent of the Unity collision rigid body physics
	(i.e. have isKinematic=true). The associated colliders must have isTrigger=true. 

	It's unfortunate that collisions cannot be used because then contactPoint and normal would be available. When tried
	there were visual offsets (bumps) even when object masses were set to the minimum. Hence triggers. 
	*/
	void OnTriggerEnter(Collider collider) {
        if ((collisionLayer != null) && (collisionLayer.Length > 0) &&
            (collider.gameObject.layer != LayerMask.NameToLayer(collisionLayer))) {
            return;
        }
        GameObject otherBody = collider.attachedRigidbody.gameObject;

		if (SkipThisCollider(otherBody)) {
			#pragma warning disable 162		// disable unreachable code warning
			if (GravityEngine.DEBUG)
				Debug.Log("Not handling Collision (precedence) of " + transform.gameObject.name + " with " + otherBody.transform.name);
			#pragma warning restore 162
			return;
		}
		#pragma warning disable 162		// disable unreachable code warning
		if (GravityEngine.DEBUG)
			Debug.Log("Collision type " + collisionType + " for " + gameObject.name);
		#pragma warning restore 162

		if (collisionType == CollisionType.ABSORB_IMMEDIATE) {
			// as soon as they touch remove colliding object
			GravityEngine.instance.Collision(otherBody.transform.parent.gameObject, transform.parent.gameObject, collisionType, 0f);
			GravityEngine.instance.InactivateBody(transform.parent.gameObject);
			inactivate = true;

		} else if (collisionType == CollisionType.ABSORB_ENVELOP) {
			// do nothing - handle in triggerStay()

		} else if (collisionType == CollisionType.BOUNCE) {
			GravityEngine.instance.Collision(otherBody.transform.parent.gameObject, transform.parent.gameObject, collisionType, bounceFactor);

		} else if (collisionType == CollisionType.EXPLODE) {
            ExplodeCollide(otherBody);
        }
        else if (collisionType == CollisionType.EXPLODE_OR_BOUNCE)
        {
            if (ShouldBounce(otherBody))
            {
                GravityEngine.instance.Collision(otherBody.transform.parent.gameObject, transform.parent.gameObject, CollisionType.BOUNCE, bounceFactor);
            }
            else
            {
                ExplodeCollide(otherBody);
            }
        }
        else
        {
			// Note: Normal is in direction of force on this body
			// Collider is on model that is the child object holding NBody
			lastDistance = Vector3.Distance(transform.position, otherBody.transform.position); 
			if (lastDistance < myRadius) {
				GravityEngine.instance.InactivateBody(transform.parent.gameObject);
			}
		}
	}

    private void ExplodeCollide(GameObject otherBody)
    {
        // direction away from collision
        Vector3 body1Pos = transform.position;
        Vector3 body2Pos = otherBody.transform.position;
        Vector3 normal = Vector3.Normalize(body1Pos - body2Pos);
        // determine eject point on surface of otherBody 
        // add a small offset (5%) to ensure particles are outside the body, otherwise
        // they will be immediately culled by GravityParticles
        Vector3 contactPoint = body2Pos + (0.5f * 1.05f * otherBody.transform.localScale.x) * normal;
        // collision will do velocity update from collision
        CollisionType cType = collisionType == CollisionType.EXPLODE_OR_BOUNCE ? CollisionType.EXPLODE : collisionType;
        GravityEngine.instance.Collision(otherBody.transform.parent.gameObject, transform.parent.gameObject, cType, 0f);
        StartExplosion(contactPoint, normal, otherBody.transform.parent.gameObject);
        GravityEngine.instance.InactivateBody(transform.parent.gameObject);
        inactivate = true;
    }

    /* JAB ADD */
    private bool ShouldBounce(GameObject otherBody)
    {
        Vector3 deltaV_vec = GravityEngine.instance.GetVelocity(otherBody.transform.parent.gameObject)
        	- GravityEngine.instance.GetVelocity(transform.parent.gameObject);
		float deltaV = deltaV_vec.magnitude;
		bouncing =  (deltaV < explodeOrBounceVelocity * GravityEngine.Instance().GetVelocityScale());
		return bouncing;
    }
    /* JAB END */

    private bool SkipThisCollider(GameObject otherBody) {
		// Does the other body also handle collisions? resolve who should handle this
		NBodyCollision otherNBC = otherBody.GetComponent<NBodyCollision>();
		if (otherNBC != null) {
			#pragma warning disable 162		// disable unreachable code warning
			if (GravityEngine.DEBUG)
				Debug.Log("Both objects have colliders: " + collisionPrecedence + " vs " + otherNBC.collisionPrecedence);
			#pragma warning restore 162
			if (collisionPrecedence > otherNBC.collisionPrecedence) {
				return true;
			} else if (collisionPrecedence == otherNBC.collisionPrecedence) {
				if (gameObject.GetInstanceID() > otherBody.GetInstanceID()) {
					return true;
				}
			}

		}
		return false;
	}

	void OnTriggerStay(Collider collider) {

        if ((collisionLayer != null) &&
             (collider.gameObject.layer != LayerMask.NameToLayer(collisionLayer))) {
            return;
        }
        GameObject otherBody = collider.attachedRigidbody.gameObject;
		if (SkipThisCollider(otherBody)) {
			return;
		}

		float distance = Vector3.Distance(transform.position, collider.attachedRigidbody.gameObject.transform.position);
		if (collisionType == CollisionType.ABSORB_ENVELOP) {
			// is the body enveloped? 
			float r1 = 0.5f * transform.localScale.x; 
			float r2 = 0.5f * otherBody.transform.localScale.x;
			float envelopDistance = r2 - r1; 
			if (envelopDistance < 0) {
				envelopDistance = r1 - r2;
			}
			if ( distance < envelopDistance) {
				GravityEngine.instance.Collision(otherBody.transform.parent.gameObject, transform.parent.gameObject, CollisionType.ABSORB_IMMEDIATE, 0f);
				GravityEngine.instance.InactivateBody(transform.parent.gameObject);
				// if do inactive directly Unity crashes!
				inactivate = true;
			}
		}
    	else if (ShouldCheckForInactivation())
        {
			if ((distance < myRadius) || (lastDistance < distance)) {
				GravityEngine.instance.InactivateBody(transform.parent.gameObject);
				inactivate = true;
			} else {
				lastDistance = distance;
			}
		}
	}

    bool ShouldCheckForInactivation()
    {
        if (collisionType == CollisionType.EXPLODE_OR_BOUNCE)
        {
            return !bouncing;
        }
        else
        {
            return collisionType != CollisionType.BOUNCE;
        }
    }

    void OnTriggerExit(Collider collider) {

        if ((collisionLayer != null) &&
             (collider.gameObject.layer != LayerMask.NameToLayer(collisionLayer))) {
            return;
        }
        GameObject otherBody = collider.attachedRigidbody.gameObject;
		if (SkipThisCollider(otherBody)) {
			return;
		}
        else if (ShouldCheckForInactivation())
        {
            GravityEngine.instance.InactivateBody(transform.parent.gameObject);
			inactivate = true;
		}
	}

	void Update() {
		if (inactivate) {
			transform.parent.gameObject.SetActive(false);
		}
	}

	private void StartExplosion(Vector3 contactPoint, Vector3 normal, GameObject otherNBodyGO) {
		if (!prefabOk) {
			Debug.LogError("prefab not ok");
			return;
		}
		NBody nbody = otherNBodyGO.GetComponent<NBody>();
		if (nbody == null) {
			Debug.LogError("Parent of collider does not have NBody - explosion aborted. " + otherNBodyGO.name);
			return;
		} 
		// instantiate the prefab and set it's position to that of the current object
		GameObject explosionGO = Instantiate(explosionPrefab) as GameObject;
		explosionGO.transform.localPosition = Vector3.zero;
		explosionGO.transform.position = transform.parent.position;
		// find out the velocity of this NBody and provide to dust init routine. (This becomes the CM velocity)
		// Tell engine to ignore this body
		ExplosionFromNBody explosionFromNBody = explosionGO.GetComponent<ExplosionFromNBody>();
		explosionFromNBody.Init(nbody, contactPoint);

		// Activate dust (this will inactivate the sphere model, so it will be hidden)
		// MUST remove the exploding FIRST or it's mass will accelerate the particles
		GravityEngine.instance.InactivateBody(transform.parent.gameObject);
		explosionGO.SetActive(true);
	}

}
