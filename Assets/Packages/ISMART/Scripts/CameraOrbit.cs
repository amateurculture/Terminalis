using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOrbit : MonoBehaviour {

	public float speed = 2.0f;                  // movement speed when scrolling on the side of the screen
	public float zoom_speed = 2.0f;             // zoom speed
	public float speed_x = 200.0f;              // Rotation speed
	float rotation_y = 0.0f;                    // variable used for rotation function
	private int edge_threshold = 5;             // area before the end of the screen where scrolling activate
	// limits
	public float scroll_limit_x = 5f;                // how much you can scroll from the center of the scene on the X axis.
	public float scroll_limit_z = 5f;                // how much you can scroll from the center of the screen on the Y axis.


	void Start()
	{
		// adapt the limits based on the starting position of the camera.
		// in this way, there will always be an equal amount to the limit value
		// independently from where the starting position is.
		if (transform.position.x > 0)
			scroll_limit_x += transform.position.x;
		else
			scroll_limit_x -= transform.position.x;

		if (transform.position.z > 0)
			scroll_limit_z += transform.position.z;
		else
			scroll_limit_z -= transform.position.z;
	}

	void Update()
	{
		float scrollwheel = Input.GetAxis("Mouse ScrollWheel");
		float mouse_x = Input.mousePosition.x;
		float mouse_y = Input.mousePosition.y;

		//zoom with scroll wheel; forward to zoom in, backward to scroll out.
		transform.Translate(0, -scrollwheel * zoom_speed, scrollwheel * zoom_speed, Space.World);

		// Orbit function using right mouse button pressed.
		if (Input.GetMouseButton(1))
		{
			rotation_y += Input.GetAxis("Mouse X") * speed_x * Time.deltaTime;
			transform.localEulerAngles = new Vector3(0, rotation_y, 0);
		}

		// movement scrolling on the side of the screen; the threshold define how far to the border
		// is the scrolling activating.
		if (mouse_x >= Screen.width - edge_threshold && transform.position.x <= scroll_limit_x)
		{
			transform.Translate((Vector3.right * speed * Time.deltaTime), Space.Self);
		}
		else if (mouse_x < edge_threshold && transform.position.x >= -scroll_limit_x)
		{
			transform.Translate((Vector3.left * speed * Time.deltaTime), Space.Self);
		}
		else if (mouse_y >= Screen.height - edge_threshold && transform.position.z <= scroll_limit_z)
		{
			transform.Translate((Vector3.forward * speed * Time.deltaTime), Space.Self);
		}
		else if (mouse_y < edge_threshold && transform.position.z >= -scroll_limit_z)
		{
			transform.Translate((Vector3.back * speed * Time.deltaTime), Space.Self);
		}

	}
}
