using UnityEngine;
using System.Collections;

/// <summary>
/// Rendering order setter.
/// </summary>
public class GhostRenderingOrderSetter : MonoBehaviour 
{
	[SerializeField] private int order = 3004;

	void Awake () 
	{
		Renderer rd = gameObject.GetComponent<Renderer>();
		if(rd != null)
		{
			rd.material.renderQueue = order;
		}
	}
}
