using UnityEngine;
using System;
using System.Collections;

[Serializable]
public enum DriveType1
{
	RearWheelDrive,
	FrontWheelDrive,
	AllWheelDrive
}

	public class WheelDrive1 : MonoBehaviour
	{
		[Tooltip("Maximum steering angle of the wheels")]
		public float maxAngle = 35f;
		[Tooltip("Maximum torque applied to the driving wheels")]
		public float maxTorque = 300f;
		[Tooltip("Maximum brake torque applied to the driving wheels")]
		public float brakeTorque = 30000f;
		[Tooltip("If you need the visual wheels to be attached automatically, drag the wheel shape here.")]
		public GameObject wheelShape;

		[Tooltip("The vehicle's speed when the physics engine can use different amount of sub-steps (in m/s).")]
		public float criticalSpeed = 5f;
		[Tooltip("Simulation sub-steps when the speed is above critical.")]
		public int stepsBelow = 5;
		[Tooltip("Simulation sub-steps when the speed is below critical.")]
		public int stepsAbove = 1;

		[Tooltip("The vehicle's drive type: rear-wheels drive, front-wheels drive or all-wheels drive.")]
		public DriveType1 driveType1;

		private WheelCollider[] m_Wheels;
		[HideInInspector] public bool handbrakeEnabled;

		public bool isDisabled;

		// Find all the WheelColliders down in the hierarchy.
		void Start()
		{
			m_Wheels = GetComponentsInChildren<WheelCollider>();

			for (int i = 0; i < m_Wheels.Length; ++i)
			{
				var wheel = m_Wheels[i];

				// Create wheel shapes only when needed.
				if (wheelShape != null)
				{
					var ws = Instantiate(wheelShape);
					ws.transform.parent = wheel.transform;
				}
			}

			handbrakeEnabled = true;
		}

		private void OnEnable()
		{
			m_Wheels = GetComponentsInChildren<WheelCollider>();

			for (int i = 0; i < m_Wheels.Length; ++i)
			{
				var wheel = m_Wheels[i];
				wheel.enabled = true;
			}
		}

		// This is a really simple approach to updating wheels.
		// We simulate a rear wheel drive car and assume that the car is perfectly symmetric at local zero.
		// This helps us to figure our which wheels are front ones and which are rear.
		void Update()
		{
			m_Wheels[0].ConfigureVehicleSubsteps(criticalSpeed, stepsBelow, stepsAbove);

			if (!isDisabled)
			{
				float angle = maxAngle * Input.GetAxis("Horizontal");
				float accelerator = (Input.GetAxis("Fire1") - Input.GetAxis("Fire2"));

				if (Input.GetKey(KeyCode.W)) accelerator = 1f;
				if (Input.GetKey(KeyCode.S)) accelerator = -1f;

				float torque = maxTorque * accelerator;

				bool handbrakePressed = Input.GetKeyDown(KeyCode.X) || Input.GetButtonDown("Submit");
				if (handbrakePressed) handbrakeEnabled = !handbrakeEnabled;
				float handBrake = (handbrakeEnabled) ? brakeTorque : 0;

				foreach (WheelCollider wheel in m_Wheels)
				{
					// A simple car where front wheels steer while rear ones drive.
					if (wheel.transform.localPosition.z > 0) wheel.steerAngle = angle;
					if (wheel.transform.localPosition.z < 0) wheel.brakeTorque = handBrake;
					if (wheel.transform.localPosition.z < 0 && driveType1 != DriveType1.FrontWheelDrive) wheel.motorTorque = torque;
					if (wheel.transform.localPosition.z >= 0 && driveType1 != DriveType1.RearWheelDrive) wheel.motorTorque = torque;

					Quaternion q;
					Vector3 p;
					wheel.GetWorldPose(out p, out q);

					// Assume that the only child of the wheelcollider is the wheel shape.
					Transform shapeTransform = wheel.transform.GetChild(0);

					if (wheel.name == "a0l" || wheel.name == "a1l" || wheel.name == "a2l")
					{
						shapeTransform.rotation = q * Quaternion.Euler(0, 180, 0);
						shapeTransform.position = p;
					}
					else
					{
						shapeTransform.position = p;
						shapeTransform.rotation = q;
					}
				}
			}
		}
	}