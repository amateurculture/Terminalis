using UnityEngine;
using System.Collections;

/** \brief Sample script to follow a target */
public class BasicFollow : MonoBehaviour 
{
	/** Target object */
	public Transform target;
	/** Minimal distance to move to the target*/
	public int minDist = 2;
	/** Speed player */
	public int speed = 3;
	
	// Use this for initialization
	void Start () 
	{
	}
	
	// Update is called once per frame
	void Update () 
	{		
		transform.LookAt(target);		
		if(Vector3.Distance(transform.position,target.position) >= minDist)
		{
			transform.position += transform.forward*speed*Time.deltaTime;
			
		}
	}
}
