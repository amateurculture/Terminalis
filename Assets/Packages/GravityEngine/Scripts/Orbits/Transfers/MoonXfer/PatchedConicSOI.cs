using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Determine the location of the sphere of influence of body2 with respect to body1. Monitor the
/// position and trigger OnNewInfluencer when the spaceship moves across the sphere of influence boundary. 
/// 
/// The definition of the sphere of influence is not simply which has more force but it defined as:
///     D (m_2/m_1)^(2/5)  where m_1 > m_2 and D is the distance between the bodies
///     
/// See https://en.wikipedia.org/wiki/Sphere_of_influence_%28astrodynamics%29 and the first reference (Bate et. al.)
/// 
/// </summary>
public class PatchedConicSOI : MonoBehaviour {

    //! assumed body1.mass > body2.mass
    public NBody body1;
    public NBody body2;

    //! object travelling in fields of body1/body2
    public NBody spaceship;

    //! Object implementing IPatchedConicChange
    public GameObject conicChange;
    private IPatchedConicChange patchChanged;

    //! mass ratio to power 2/5 for sphere of influence calculation
    private float massRatio25 = 0f;

    // make the SOI a bit smaller so do not jitter across the boundary
    private const float ENTER_DERATE = 1.00f; 

    private NBody lastInfluencer; 

	// Use this for initialization
	void Start () {
        // check mass order
        if (body1.mass < body2.mass) {
            Debug.LogWarning("Patched conic requires body1 mass > body2 mass");
        }
        // mass scaling will cancel in this ratio
        massRatio25 = Mathf.Pow(body2.mass / body1.mass, 0.4f);

        // assume start with body1 in influence
        lastInfluencer = body1;

        patchChanged = conicChange.GetComponent<IPatchedConicChange>();
	}
	
	// Update is called once per fixed frame by GE
	public void FixedUpdate () {
        // need to get physics positions for everything
        GravityEngine ge = GravityEngine.Instance();
        Vector3 shipPos = ge.GetPhysicsPosition(spaceship);
        Vector3 body1Pos = ge.GetPhysicsPosition(body1);
        Vector3 body2Pos = ge.GetPhysicsPosition(body2);

        float D = Vector3.Distance(body1Pos, body2Pos);

        NBody influencer = lastInfluencer;
        float d = Vector3.Distance(shipPos, body2Pos);
        // add a bit of hysteresis
        if (d  < ENTER_DERATE * D *massRatio25) {
            influencer = body2;
        } else if (d > D * massRatio25) {
            influencer = body1;
        }
        if (influencer != lastInfluencer) {
            if (patchChanged != null) {
                patchChanged.OnNewInfluencer(influencer, lastInfluencer);
            }
#pragma warning disable 162        // disable unreachable code warning
            if (GravityEngine.DEBUG) {
                Debug.LogFormat("Influencer changed from {0} to {1} at d={2} t={3}", 
                    lastInfluencer, influencer, d, ge.GetPhysicalTime() );
            }
#pragma warning restore 162
            lastInfluencer = influencer;
        }

    }
}
