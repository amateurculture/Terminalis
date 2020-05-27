using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

/// <summary>
/// Grid overlay.
///
/// Display a 2D grid of lines. Can be a useful element in a scene when providing a visual reference for
/// 3D motions of bodies. 
/// </summary>
public class GridOverlay : MonoBehaviour {
	
	//! Number of lines in each axis
	public int numLines = 20;

	//! size of grid along X-axis
	public float xSize = 10f;
	//! size of grid along T-axis
	public float ySize = 10f;

    public float lineWidth = 1;

    public Material material;

	private LineRenderer lineRenderer;

	void Start () {

		lineRenderer = gameObject.AddComponent<LineRenderer>();
		lineRenderer.startColor = Color.white;
		lineRenderer.endColor = Color.white;
		lineRenderer.startWidth = lineWidth;
		lineRenderer.endWidth = lineWidth;
		lineRenderer.material = material;
		Vector3[] points = GeneratePoints();
		lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);

	}

	private Vector3[] GeneratePoints() {
		int numPoints = 6 * (numLines+1);
		Vector3[] points = new Vector3[numPoints];
        int index = 0; 
        // add pairs of from -xSize/2..xSize/2 as Y goes from ySize/2..-ySize/2
        float yInterval = ySize/numLines;
        float xInterval = xSize/numLines;

        // line needs to be continuous, so weave over and back for each horizontal etc.
         
        for (int i=0; i < numLines+1; i++) {
        	// horizontal lines, y bottom to top
        	points[index++] = new Vector3(transform.position.x - xSize/2, transform.position.y - ySize/2 + i * yInterval);
			points[index++] = new Vector3(transform.position.x + xSize/2, transform.position.y - ySize/2 + i * yInterval);
			// back to start
			points[index++] = new Vector3(transform.position.x - xSize/2, transform.position.y - ySize/2 + i * yInterval);
        }
		for (int i=0; i < numLines+1; i++) {
        	// vertical lines, x left to right
        	points[index++] = new Vector3(transform.position.x - xSize/2 + i * xInterval, transform.position.y + ySize/2);
			points[index++] = new Vector3(transform.position.x - xSize/2 + i * xInterval, transform.position.y - ySize/2);
			// back to start
			points[index++] = new Vector3(transform.position.x - xSize/2 + i * xInterval, transform.position.y + ySize/2);
        }
		for (int i=0; i < points.Length; i++) {
        	points[i] = transform.rotation * points[i];
        }
        return points;
	}

	#if UNITY_EDITOR
	void OnDrawGizmosSelected()
	{
		Vector3[] points = GeneratePoints();
		for (int i=0; i < points.Length-1; i++) {
			Gizmos.DrawLine(points[i], points[i+1]);
		}
	}
	#endif
}