using UnityEngine;
using System.Collections;

/** \brief Camera Following */
public class CamFollow : MonoBehaviour 
{
	/** Player object*/
	public Transform player;
	/** Distance Z from the player*/
	public float distance = 3.0f;
	/** Distance Y from the player*/
	public float height = 20.0f;
	/** Enable following camera*/
	public bool followplayer = false;
	
	/** user height */
	private float currentheight = 20.0f;
	
	void LateUpdate () 
	{
		Vector3 wantedPosition;
		if (followplayer) 
		{
			wantedPosition = player.position + new Vector3(0,currentheight,distance);
			transform.position = wantedPosition;
		}
	}
	
	void Start () 
	{
		currentheight = height;
	}
	
	void OnGUI() 
	{
		currentheight = GUILayout.HorizontalSlider(currentheight, height, height*10);
		GUILayout.Label("Camera height");
	}
}