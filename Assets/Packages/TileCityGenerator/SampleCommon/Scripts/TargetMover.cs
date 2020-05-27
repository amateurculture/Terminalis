using UnityEngine;
using System.Collections;

/** \brief Sample script to move a target */
public class TargetMover : MonoBehaviour
{
	/** Mask for the raycast placement */
	public LayerMask mask;
	
	/** Target object */
	public Transform target;

	/** Determines if the target position should be updated every frame or only on double-click */
	public bool onlyOnDoubleClick;
	
	/** Main cam */
	private Camera cam;
	
	// Use this for initialization
	void Start () 
	{
		//Cache the Main Camera
		cam = Camera.main;
	}
			
	// Update is called on GUI event
	void OnGUI () 
	{
		if (onlyOnDoubleClick && cam != null && Event.current.type == EventType.MouseDown && Event.current.clickCount == 2)
		{
			UpdateTargetPosition ();
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
		
		if (!onlyOnDoubleClick && cam != null) 
		{
			UpdateTargetPosition ();
		}
		
	}
	
	/** Calculate new target position */
	public void UpdateTargetPosition () 
	{
		//Fire a ray through the scene at the mouse position and place the target where it hits
		RaycastHit hit;
		if (Physics.Raycast	(cam.ScreenPointToRay (Input.mousePosition), out hit, Mathf.Infinity))
		{
			if(hit.point != target.position)
				target.position = hit.point;
		}
	}
	
}