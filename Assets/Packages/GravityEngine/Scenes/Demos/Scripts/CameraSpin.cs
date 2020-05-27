using UnityEngine;
using System.Collections;

/// <summary>
/// Key control to rotate camera boom using Arrow keys for rotation and < > keys for zoom.
///
/// Assumes the Main Camara is a child of the object holding this script with a local position offset
/// (the boom length) and oriented to point at this object. Then pressing the keys will spin the camera
/// around the object this script is attached to.
/// 
/// If a LineScalar is present, zoom change call will be made to the LineScalar. 
/// 
/// </summary>
public class CameraSpin : MonoBehaviour {

	//! Rate of spin (degrees per Update)
	public float spinRate = 1f;
    public float mouseSpinRate = 3f;
    public float zoomSize = 1f;
    public float mouseWheelZoom = 0.5f;

    private Vector3 initialBoom; 
	// factor by which zoom is changed 
	private float zoomStep = 0.02f;
	private Camera boomCamera;

    //! Optional Line Scalar (auto-detected)
    private LineScaler lineScaler;

    private const float minZoomSize = 0.01f;

	// Use this for initialization
	void Start () {
		boomCamera = GetComponentInChildren<Camera>();
		if (boomCamera != null) {
			initialBoom = boomCamera.transform.localPosition;
		}

        lineScaler = GetComponent<LineScaler>();
	}
	
	// Update is called once per frame
	void Update () {
        float lastZoom = zoomSize;

		if (Input.GetKey(KeyCode.UpArrow)) {
			transform.rotation *= Quaternion.AngleAxis( spinRate, Vector3.right);
		} else if (Input.GetKey(KeyCode.DownArrow)) {
			transform.rotation *= Quaternion.AngleAxis( -spinRate, Vector3.right);
		} else if (Input.GetKey(KeyCode.RightArrow)) {
			transform.rotation *= Quaternion.AngleAxis( spinRate, Vector3.up);
		} else if (Input.GetKey(KeyCode.LeftArrow)) {
			transform.rotation *= Quaternion.AngleAxis( -spinRate, Vector3.up);
		} else if (Input.GetKey(KeyCode.Comma)) {
			// change boom length
			zoomSize += zoomStep; 
			boomCamera.transform.localPosition = zoomSize * initialBoom;
		} else if (Input.GetKey(KeyCode.Period)) {
			// change boom lenght
			// change boom length
			zoomSize -= zoomStep;
            zoomSize = Mathf.Max(zoomSize, minZoomSize);
			boomCamera.transform.localPosition = zoomSize * initialBoom;
		}

        // Mouse commands - middle mouse button to spin
        if (Input.GetMouseButton(2))
        {
            float h = mouseSpinRate * Input.GetAxis("Mouse X");
            float v = mouseSpinRate * Input.GetAxis("Mouse Y");
            transform.Rotate(v, h, 0);
        }
        // scroll speed typically +/- 0.1
        float scrollSpeed = Input.GetAxis("Mouse ScrollWheel");
        if (scrollSpeed != 0) {
            zoomSize += mouseWheelZoom * scrollSpeed;
            zoomSize = Mathf.Max(zoomSize, minZoomSize);
            boomCamera.transform.localPosition = zoomSize * initialBoom;
        }

        // if LineScalar is around, tell it about the new zoom setting
        if (lastZoom != zoomSize) {
            if (lineScaler != null) {
                lineScaler.SetZoom(zoomSize);
            }
        }
    }
}
