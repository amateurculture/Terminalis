using UnityEngine;
using System.Collections;

/// <summary>
/// Key control to rotate camera boom using WASD and +/- keys for zoom.
///
/// Should be attached to the GravityEngine and have Use Transform To Reposition enabled.
/// 
/// If a LineScalar is present, zoom change call will be made to the LineScalar. 
/// 
/// </summary>
public class UniverseSpin : MonoBehaviour {

	//! Rate of spin (degrees per Update)
	public float spinRate = 1f;
    public float zoomSize = 1f;
    public float minZoom = 0.001f;
    public float maxZoom = 1f;

    private Vector3 initialBoom; 
	// factor by which zoom is changed 
	public float zoomStep = 0.02f;

    //! Optional Line Scalar (auto-detected)
    private LineScaler lineScaler;

	// Use this for initialization
	void Start () {

        lineScaler = GetComponent<LineScaler>();
	}
	
	// Update is called once per frame
	void Update () {
        float lastZoom = zoomSize;

		if (Input.GetKey(KeyCode.W)) {
			transform.rotation *= Quaternion.AngleAxis( spinRate, Vector3.right);
		} else if (Input.GetKey(KeyCode.S)) {
			transform.rotation *= Quaternion.AngleAxis( -spinRate, Vector3.right);
		} else if (Input.GetKey(KeyCode.D)) {
			transform.rotation *= Quaternion.AngleAxis( spinRate, Vector3.up);
		} else if (Input.GetKey(KeyCode.A)) {
			transform.rotation *= Quaternion.AngleAxis( -spinRate, Vector3.up);
		} else if (Input.GetKey(KeyCode.Equals)) {
			zoomSize += zoomStep; 
		} else if (Input.GetKey(KeyCode.Minus)) {
			zoomSize -= zoomStep; 
		}

        // Mouse commands - middle mouse button to spin

        // if LineScalar is around, tell it about the new zoom setting
        if (lastZoom != zoomSize) {
            zoomSize = Mathf.Max(minZoom, zoomSize);
            zoomSize = Mathf.Min(maxZoom, zoomSize);
            transform.localScale = zoomSize * Vector3.one;
            if (lineScaler != null) {
                lineScaler.SetZoom(zoomSize);
            }
        }
    }
}
