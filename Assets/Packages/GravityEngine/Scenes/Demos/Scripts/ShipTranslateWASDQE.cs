using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller to make a velocity change in XYZ based on user input
/// using the WASDQE keys:
/// AD: x-axis
/// WS: y-axis
/// QE: z-axis
/// 
/// Must be attached to an NBody conponent. 
/// </summary>
[RequireComponent(typeof(NBody))]
public class ShipTranslateWASDQE : MonoBehaviour
{
	public float impulse;

	private NBody nbody;

	private KeyCode[] keyCodes; 
	private Vector3[] direction;

    // Start is called before the first frame update
    void Start()
    {
        nbody = GetComponent<NBody>();
        keyCodes = new KeyCode[]{ KeyCode.A, KeyCode.D, KeyCode.W, KeyCode.S, KeyCode.Q, KeyCode.E};
        direction = new Vector3[]{
        	new Vector3(-1f,0,0),
        	new Vector3(1f,0,0),
        	new Vector3(0,1f,0),
        	new Vector3(0,-1f,0),
        	new Vector3(0,0,-1),
        	new Vector3(0,0,1)
        };
    }

    // Update is called once per frame
    void Update()
    {
        for(int i=0; i < keyCodes.Length; i++) {
        	if (Input.GetKeyDown(keyCodes[i])) {
        		GravityEngine.Instance().ApplyImpulse(nbody, impulse * direction[i]);
        	}
        }
    }
}
