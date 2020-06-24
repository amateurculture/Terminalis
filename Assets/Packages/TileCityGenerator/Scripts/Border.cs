using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/** Tile component
* This component must be added to each tile.
* 
* The border pattern of the tile must be defined inside the inspector, select the connection of each border side
* 
*/

/** \brief Side pattern representation */
[System.Serializable]
public class Side
{
	/** connections */
	public List<bool> values;
}

/** \brief Border pattern representation */
[System.Serializable]
public class Border  : MonoBehaviour 
{
	/** connections by side*/
	public int size;
	/** left side*/
	public Side left;
	/** top side*/
	public Side top;
	/** right side*/
	public Side right;
	/** down side*/
	public Side down;

	private void Reset()
	{
		size = 1;
		left = new Side();
		right = new Side();
		down = new Side();
		top = new Side();
		left.values = new List<bool>() { false };
		right.values = new List<bool>() { false };
		top.values = new List<bool>() { false };
		down.values = new List<bool>() { false };

		switch (name)
		{
			case "Roundabout":
			case "Crossroad":
			case "Crossroads": 
				for (var i = 0; i < left.values.Count; i ++) left.values[i] = right.values[i] = top.values[i] = down.values[i] = true; 
				break;
			case "Straight":
				for (var i = 0; i < left.values.Count; i++) left.values[i] = right.values[i] = true;
				break;
			case "Corner":
				for (var i = 0; i < left.values.Count; i++) right.values[i] = down.values[i] = true;
				break;
			case "Intersection":
				for (var i = 0; i < left.values.Count; i++) top.values[i] = right.values[i] = down.values[i] = true;
				break;
			case "Culdesac":
				for (var i = 0; i < left.values.Count; i++) right.values[i] = true;
				break;
			default:
				break;
		}
	}
}
