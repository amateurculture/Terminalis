using UnityEngine;
using System.Collections;

public class Walking : MonoBehaviour 
{
	[SerializeField] private float speed = 1.0f;
	private Vector3 direction = -Vector3.right;

	void Awake ()
	{
		transform.forward = direction;
	}

	void Update () 
	{
		Vector3 newPos = transform.position + direction * speed * Time.deltaTime;
		if(Mathf.Abs(newPos.x) >= 5f)
		{
			newPos.x = direction.x * 5f;
			direction = -direction;
			transform.forward = direction;
		}

		transform.position = newPos;
	}
}
