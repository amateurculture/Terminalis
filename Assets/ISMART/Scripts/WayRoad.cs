using UnityEngine;

public class WayRoad : MonoBehaviour
{
	public Color colorRoad = new Color(0, 1, 0, 0.5f);
	public bool ShowLabels;
	public bool ShowLines;

	public Transform[] waypoints;
	private void Reset()
	{
		waypoints = new Transform[transform.childCount];
		for (int i = 0; i < transform.childCount; i++)
		{
			//waypoints[i].gameObject.AddComponent<BoxCollider>();
			waypoints[i] = transform.GetChild(i).gameObject.transform;
			//RaycastHit hit;
		}
		Next();
	}

	void Start()
	{
		waypoints = new Transform[transform.childCount];
		for (int i = 0; i < transform.childCount; i++)
		{
			//waypoints[i].gameObject.AddComponent<BoxCollider>();
			waypoints[i] = transform.GetChild(i).gameObject.transform;
			//RaycastHit hit;
		}
		Next();
	}

	void Next()
	{
		int index = 0;
		foreach (Transform child in transform)
		{
			child.gameObject.tag = "Way";
			child.gameObject.name = "Waypoint" + index.ToString().PadLeft(3, '0');
			index++;
		}
	}

	void OnDrawGizmos()
	{
		Transform[] waypoints = gameObject.GetComponentsInChildren<Transform>();

		for (int i = 0; i < waypoints.Length; i++)
		{
			foreach (Transform wayroad in waypoints)
			{
				Gizmos.color = colorRoad;
				Gizmos.DrawWireCube(waypoints[i].position, new Vector3(1, 1, 1));

				if (ShowLines == true && i < waypoints.Length - 2)
					Gizmos.DrawLine(waypoints[i + 1].position, waypoints[i + 2].position);
#if UNITY_EDITOR
				if (Debug.isDebugBuild && ShowLabels == true && i < waypoints.Length)
						UnityEditor.Handles.Label(waypoints[i].position, waypoints[i].gameObject.name);
#endif
			}
		}
	}
}