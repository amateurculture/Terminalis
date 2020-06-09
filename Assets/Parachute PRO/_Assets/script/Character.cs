using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Very simple Character Controller (for Demonstration purposes).
/// </summary>
public class Character : MonoBehaviour
{
	
	[Space(15)][Header("Movement")]
	public float Speed = 5f;
	public float JumpHeight = 2f;
	public float Gravity = -9.81f;
	public float GroundDistance = 0.2f;
	public LayerMask Ground;


	[Space(15)][Header("Camera Rotation")]
	public float sensitivityX = 15F;
	public float sensitivityY = 15F;
	public float minimumX = -360F;
	public float maximumX = 360F;
	public float minimumY = -60F;
	public float maximumY = 60F;



	// private fields
	CharacterController _controller;
	Transform _groundChecker;
	Transform _camera;
    Vector3 move;
    Quaternion originalRotation;
    float vel_y;
	float rotationX = 0F;
	float rotationY = 0F;
    bool _isGrounded = true;
    bool _isParachutePlugged = false;




    void Start()
	{
        // Cache components
		_controller = GetComponent<CharacterController>();
		_groundChecker = transform.Find("_groundChecker");
		_camera = transform.Find("Camera");

		originalRotation = transform.localRotation;
		//Cursor.lockState = CursorLockMode.Locked;
	}



	void Update()
	{
		
		/* ROTATION */
		rotationX += Input.GetAxis("Mouse X") * sensitivityX;
		rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
		rotationX = ClampAngle (rotationX, minimumX, maximumX);
		rotationY = ClampAngle (rotationY, minimumY, maximumY);

		Quaternion xQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
		Quaternion yQuaternion = Quaternion.AngleAxis (rotationY, -Vector3.right);

		_camera.localRotation = Quaternion.Slerp (_camera.localRotation, originalRotation * yQuaternion, 0.2f);
		transform.localRotation = Quaternion.Slerp (transform.localRotation, originalRotation * xQuaternion, 0.2f);
		//--------------
		
	
		/* MOVE */
		move = Vector3.zero;

		if(Input.GetKey(KeyCode.W))
			move = new Vector3(transform.forward.x , 0  , transform.forward.z);
		else
		if(Input.GetKey(KeyCode.S))
			move = new Vector3(-transform.forward.x , 0  , -transform.forward.z);
		else
		if(Input.GetKey(KeyCode.A))
			move = new Vector3(-transform.right.x , 0  , -transform.right.z);
		else
		if(Input.GetKey(KeyCode.D))
			move = new Vector3(transform.right.x , 0  , transform.right.z);
		//-------------


		/* JUMP */
		_isGrounded = Physics.CheckSphere(_groundChecker.position, GroundDistance, Ground, QueryTriggerInteraction.Ignore);
		if (_isGrounded && vel_y < 0)
			vel_y = 0f;
		
		if (Input.GetButtonDown("Jump") && _isGrounded)
			vel_y += Mathf.Sqrt(JumpHeight * -2f * Gravity);

        // If parachute plugged , slow the falling
        if (_isParachutePlugged)
            vel_y = -10f;
        else
            vel_y += Gravity * Time.deltaTime;
        //-----------


        // pass all calculations to character controller
        _controller.Move(new Vector3(move.x*Speed , vel_y , move.z*Speed) * Time.deltaTime);
	}





	float ClampAngle (float angle, float min, float max)
	{
		if (angle < -360F)
			angle += 360F;
		if (angle > 360F)
			angle -= 360F;
		return Mathf.Clamp (angle, min, max);
	}


    // Pass the parachute state to the character controller (logical control)
    public void PlugInParachute(bool plugIn)
    {
        _isParachutePlugged = plugIn;
    }
}