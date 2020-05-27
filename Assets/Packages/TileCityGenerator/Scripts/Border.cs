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
}
