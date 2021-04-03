using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Collections;

[Serializable]
public enum CType
{
	NormalCar,
	SportCar,
	Truck,
	Police,
	Custom
}

public enum WDriveType
{
	RearWheelDrive,
	FrontWheelDrive,
	Wheeled8x8,
	AllWheelDrive
}

public enum Rays
{
	No,
	Yes
}

public class ISMART : MonoBehaviour
{
	[Header("Car Mode")]
	public CType CarType;
	public WDriveType driveType;
	public Rays rayMod;
	public LayerMask HitLayer = -1;
	public float rayAdjust = .2f;
	public bool DynamicRay = false;
	public bool Restart = false;
	public int StackTime = 1000;
	public int stack = 0;

	private float AngleStack;

	[Header("Car Settings")]
	public AnimationCurve power = AnimationCurve.Linear(0.0f, 5000.0f, 8000f, 0.0f);
	[Range(100f, 100000f)] public float torque = 350f;
	[Range(5f, 240f)] public float maxSpeed = 50f;
	[Range(20f, 240f)] public float maxAngle = 45f;
	[Range(1000f, 10000f)] public float brakeTorque = 7500f;
	[Range(0.1f, 1f)] public float SmoothSteerAngle = 0.3F;
	[Range(5f, 25f)] public float RaySize = 10F;
	[Range(10f, 75f)] public float RayAngle = 35F;
	public GameObject BrakeLights;
	public WheelCollider[] m_Wheels;
	public GameObject[] m_WheelMeshes;
	public GameObject wheelShape;
	public AudioClip EngineAudio;

	public Vector3 centerOfMass;

	private float tmptorque = 0;
	private Rigidbody rb;
	private AudioSource audioS;

	[Header("Gears")]
	public int currentGear = 0;
	public float[] gears = { 10f, 9f, 6f, 4.5f, 3f, 2.5f };
	public float shiftDownRPM = 1500.0f;
	public float shiftUpRPM = 2500.0f;
	public float accel = 0.0f;

	[HideInInspector] public bool NeutralGear = true;
	[HideInInspector] public bool automaticGear = true;
	private float shiftDelay = 0.0f;

	[Header("Car Info")]
	public bool Brake;
	public bool Slow;
	[Range(-10f, 175f)] public float Speed = 0F;
	[Range(-90f, 90f)] public float angle = 0F;
	[Range(0f, 10000f)] public float MotorRPM;

	private float Rpm = 0;
	private float criticalSpeed = 5f;
	private int stepsBelow = 5;
	private int stepsAbove = 1;

	[Header("Waypoints Setting")]
	public bool FindWaypoint;
	public WayRoad Waypoints;
	public int currentWaypoint = 0;
	public float distance;
	public int currentWaypoint2 = 0;
	public bool Parking;
	public bool ParkingOnEnd;
	public bool DynamicNextWay;
	public Transform closestWay;
	public Transform NextTargetWay;
	[Range(0f, 10f)] public float NextWay = 5f;
	[Range(0f, 50f)] public float NextWayDynamic;

	[HideInInspector] public bool RotateNextWaypoint;
	//private float alt = 0;

	[Header("Police Setting")]
	[SerializeField] public PoliceAddons policeAddons;

	[Serializable]
	public class PoliceAddons
	{
		public int ChaseRange = 50;
		public string EnemyTag = "Enemy";
		public GameObject PoliceLights;
		public GameObject Target;
		public float EnemyRange;
		public bool Chase;
		[HideInInspector] public Transform Target2;
		[HideInInspector] public GameObject[] Enemys;
		[HideInInspector] public Transform[] Enemys2;
	}

	[Header("Police Addons")]
	[Header("Debug")]
	//private int i = 0;

	#pragma warning disable
	private bool RayC;
	private bool RayLF;
	private bool RayLN;
	private bool RayRN;
	private bool RayRF;
	public float slowTorque = 250; // 250f
	public float maxTurnSpeed = 100; // 80 kph ~= 50 mph

	Vector3 originalPos;
	Quaternion originalRot;

	public bool usePassingSensors;

	void Start()
	{
		originalPos = transform.position;
		originalRot = transform.rotation;

		m_Wheels = GetComponentsInChildren<WheelCollider>();
		rb = GetComponent<Rigidbody>();
		if (rb != null && centerOfMass != Vector3.zero) rb.centerOfMass = centerOfMass;

		if (EngineAudio)
		{
			if (!audioS)
			{
				audioS = this.gameObject.AddComponent<AudioSource>();
				audioS.enabled = false;
			}
			else
			{
				audioS = GetComponent<AudioSource>();
			}
			audioS.clip = EngineAudio;
			audioS.loop = true;
			audioS.volume = 0;
			audioS.enabled = true;
			audioS.spatialBlend = 1;
		}

		if (Waypoints == null && FindWaypoint == true)
			Waypoints = FindObjectOfType(typeof(WayRoad)) as WayRoad;


		for (int i = 0; i < m_Wheels.Length; ++i)
		{
			var wheel = m_Wheels[i];

			if (wheelShape != null)
			{
				var ws = Instantiate(wheelShape);
				ws.transform.parent = wheel.transform;

			}
			else
			{
				var ws = Instantiate(Resources.Load("FamilyCarTyre") as GameObject);
				wheelShape = ws;
				ws.transform.parent = wheel.transform;
			}
		}

		if (CarType == CType.NormalCar)
		{
			driveType = WDriveType.FrontWheelDrive;
			//	torque = 350f;
			//	maxSpeed = 40f;
			//	brakeTorque = 5000f;
			rayMod = Rays.Yes;
		}
		if (CarType == CType.Police)
		{
			driveType = WDriveType.FrontWheelDrive;
			//		torque = 350f;
			//		maxSpeed = 40f;
			//		brakeTorque = 5000f;
			//rayMod = Rays.No;
			if (policeAddons.PoliceLights != null) policeAddons.PoliceLights.SetActive(false);

		}
		if (CarType == CType.SportCar)
		{
			driveType = WDriveType.RearWheelDrive;
			//		torque = 1000f;
			//		maxSpeed = 135f;
			//		brakeTorque = 10000f;
			//		rayMod = Rays.Yes;
		}
		if (CarType == CType.Truck)
		{

			driveType = WDriveType.AllWheelDrive;
			//		torque = 500f;
			//		maxSpeed = 40f;
			//		brakeTorque = 7500f;
			//		rayMod = Rays.No;
		}
		tmptorque = torque;

		FindClosestWay();

		if (RotateNextWaypoint)
		{
			Vector3 relativePos;

			if (NextTargetWay)
			{
				NextTargetWay = AIContoller.manager.NextTargetWay.gameObject.transform;
				//		if(Waypoints.waypoints.LongLength >= currentWaypoint)
				relativePos = NextTargetWay.transform.position - transform.position;
				//		else
				//			relativePos = Waypoints.waypoints[0].transform.position - transform.position;
				transform.rotation = Quaternion.LookRotation(relativePos);
			}
		}

		/*
		currentPos = Vector3.negativeInfinity;
		previousPos = originalPos;
		*/
	}

	public void ShiftUp()
	{
		float now = Time.timeSinceLevelLoad;

		if (now < shiftDelay) return;

		if (currentGear < gears.Length - 1)
		{
			if (!automaticGear)
			{
				if (currentGear == 0)
				{
					if (NeutralGear) { 
						currentGear++; 
						NeutralGear = false; 
					}
					else
						NeutralGear = true;
				}
				else
					currentGear++;
			}
			else
				currentGear++;
			
			shiftDelay = now + 1.0f;
		}
	}

	public void ShiftDown()
	{
		float now = Time.timeSinceLevelLoad;

		if (now < shiftDelay) return;

		if (currentGear > 0 || NeutralGear)
		{
			if (!automaticGear)
			{
				if (currentGear == 1)
				{
					if (!NeutralGear) { 
						currentGear--; 
						NeutralGear = true; 
					}
				}
				else if (currentGear == 0) 
					NeutralGear = false;
				else 
					currentGear--; 
			}
			else
				currentGear--;
			
			shiftDelay = now + 0.1f;
		}
	}

	void FixedUpdate()
	{
		Navigation();

		accel = Speed * (gears[currentGear]);

		if (audioS)
		{
			audioS.volume = Speed * 0.05f;
			float pitchRpm = (MotorRPM / 100);
			audioS.pitch = pitchRpm * 0.01f;

			if (audioS.pitch > 2f)
				audioS.pitch = 2f;

			if (pitchRpm < 1f)
				audioS.pitch = 1f;

			if (MotorRPM == 0)
				audioS.pitch = 0.5f;
		}

		if (automaticGear && (MotorRPM > shiftUpRPM) && (accel > 0.0f) && Speed > 10.0f && !Brake && !Slow)
		{
			ShiftUp();
		}
		else if ((automaticGear && (MotorRPM < shiftDownRPM) && (currentGear > 1)))
		{
			ShiftDown();
		}

		if (CarType == CType.Police)
		{
			var diff = FindClosestEnemy();
			//float dot = Vector3.Dot(transform.TransformDirection(Vector3.forward), diff);
			float dot = Vector3.Dot(transform.forward, diff);

			//Debug.Log("Enemy (" + policeAddons.Target.name + ") Dot = " + dot);

			if (policeAddons.EnemyRange > policeAddons.ChaseRange && Parking == false)
			{
				if (policeAddons.Chase)
				{
					rb.velocity = Vector3.zero;
					rb.angularVelocity = Vector3.zero;

					for (int i = 0; i < m_Wheels.Length; ++i)
					{
						var wheel = m_Wheels[i];
						if (wheelShape != null)
						{
							wheel.brakeTorque = 0;
							wheel.motorTorque = 0;
							wheel.steerAngle = 0;
						}
					}
					angle = 0;
					transform.rotation = originalRot;
					transform.position = originalPos;
				}
				policeAddons.Chase = false;

				if (policeAddons.PoliceLights != null) policeAddons.PoliceLights.SetActive(false);
			}
			if (dot > .8f && policeAddons.EnemyRange < policeAddons.ChaseRange)
			{
				policeAddons.Chase = true;
				Parking = false;

				if (policeAddons.PoliceLights != null) policeAddons.PoliceLights.SetActive(true);
			}
		}
	}

	bool isMovingCarNow;

	IEnumerator resetCarPosition()
	{
		FindClosestWay();

		transform.position = Waypoints.waypoints[currentWaypoint].position;
		transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.position, Waypoints.waypoints[(currentWaypoint + 1) % (Waypoints.waypoints.Length-1)].position, 0, 1));

		//isResetting = false;
		isMovingCarNow = false;
		yield return null;
	}

	// Update Vehicle Physics
	void Update()
	{
		/*
		if (isMovingCarNow) return;

		else if (isResetting && !isMovingCarNow && Time.time > timeOfStop)
		{
			isMovingCarNow = true;
			StartCoroutine(resetCarPosition());
			return;
		}
		else if (!isResetting && previousPos == currentPos)
		{
			isResetting = true;
			timeOfStop = Time.time + 30;
			return;
		}
		else if (isResetting && previousPos != currentPos)
		{
			isResetting = false;
		}
		else if (isResetting) return;
		previousPos = transform.position;
		*/

		m_Wheels[0].ConfigureVehicleSubsteps(criticalSpeed, stepsBelow, stepsAbove);
		rb = GetComponent<Rigidbody>();
		rb.drag = rb.velocity.magnitude / 200;
		rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
		//rb.AddForce(-transform.up * rb.velocity.magnitude);
		Speed = (rb.velocity.magnitude) * 3.6f;
		var ray = transform.TransformDirection(Vector3.forward) * RaySize;
		var rayL = transform.TransformDirection(Vector3.forward) * RaySize * Speed;
		float F = (Speed * 0.01f + 0.5f); // Speed * .01f + .5f
		var rayF = transform.TransformDirection(Vector3.forward) * RaySize * F;

		float f2 = (Speed * 0.1f + 0.5f);
		var rayF2 = transform.TransformDirection(Vector3.forward) * RaySize * f2;

		if (!DynamicRay)
			rayF = transform.TransformDirection(Vector3.forward) * RaySize * F;
		else
			rayF = transform.TransformDirection(Vector3.forward) * Speed / 2;

		//if(!Slow)
		MotorRPM = Rpm * gears[currentGear];

		foreach (WheelCollider wheel in m_Wheels)
		{
			if (wheel.transform.localPosition.z > 0)
			{
				m_Wheels[0].steerAngle = angle;
				m_Wheels[1].steerAngle = angle;

				Rpm = m_Wheels[0].rpm * 3.14f;

				if (driveType == WDriveType.Wheeled8x8)
				{
					m_Wheels[0].steerAngle = angle;
					m_Wheels[1].steerAngle = angle;
					m_Wheels[2].steerAngle = angle;
					m_Wheels[3].steerAngle = angle;
				}
			}

			for (int i = 0; i < m_Wheels.Length; ++i)
			{
				if (wheelShape)
				{
					Quaternion q;
					Vector3 p;
					m_Wheels[i].GetWorldPose(out p, out q);

					Transform shapeTransform = m_Wheels[i].transform.GetChild(0);
					shapeTransform.position = p;
					shapeTransform.rotation = q;

					//shapeTransform.gameObject.transform.GetChild (0).rotation = q;
				}
			}

			if (Parking == false && Brake != true)
			{
				if (driveType == WDriveType.FrontWheelDrive)
				{
					m_Wheels[0].motorTorque = torque;
					m_Wheels[1].motorTorque = torque;

				}
				if (driveType == WDriveType.RearWheelDrive)
				{
					m_Wheels[2].motorTorque = torque;
					m_Wheels[3].motorTorque = torque;
				}
				if (driveType == WDriveType.AllWheelDrive)
				{
					m_Wheels[0].motorTorque = torque;
					m_Wheels[1].motorTorque = torque;
					m_Wheels[2].motorTorque = torque;
					m_Wheels[3].motorTorque = torque;

				}
				if (driveType == WDriveType.Wheeled8x8)
				{
					m_Wheels[0].motorTorque = torque;
					m_Wheels[1].motorTorque = torque;
					m_Wheels[2].motorTorque = torque;
					m_Wheels[3].motorTorque = torque;
				}
			}

			if (Slow == true)
			{
				m_Wheels[0].brakeTorque = slowTorque;
				m_Wheels[1].brakeTorque = slowTorque;
			}
			else
			{
				wheel.brakeTorque = 0;
				Slow = false;
			}

			if (Parking == true)
			{
				torque = 0;
				wheel.brakeTorque = brakeTorque;
				wheel.steerAngle = 0;
				Brake = true;
			}
			else
			{
				torque = tmptorque;

				if (BrakeLights != null)
					BrakeLights.SetActive(false);

				Parking = false;
				wheel.brakeTorque = 0;
			}

			if (Brake == true)
			{
				if (BrakeLights != null)
					BrakeLights.SetActive(true);

				wheel.brakeTorque = brakeTorque;
			}

			if (Brake == false)
			{
				torque = tmptorque;

				if (BrakeLights != null)
					BrakeLights.SetActive(false);

				Brake = false;
				wheel.brakeTorque = 0;
			}

			Vector3 posAdj = transform.position;
			posAdj.y += rayAdjust;
			RaycastHit hit;
			Ray downRay = new Ray(transform.position, Vector3.up);
			bool isRedLight = false;

			Debug.DrawRay(posAdj, rayF2, Color.magenta);
			if (Physics.Raycast(posAdj, rayF2, out hit, RaySize * f2, LayerMask.GetMask("TrafficLight")))
			{
				var t = hit.transform.GetComponent<TrafficLights>();
				if (t.Red.activeSelf || t.Yellow.activeSelf)
				{
					torque = -150000;
					wheel.brakeTorque = 500000;
					Brake = true;
					isRedLight = true;
					RayC = false;
				}
			}
			else
			{
				Brake = false;
				wheel.brakeTorque = 0;
				torque = tmptorque;
				isRedLight = false;
				RayC = true;
			}

			if (!isRedLight)
			{
				Debug.DrawRay(posAdj, rayF, Color.yellow);
				if (Physics.Raycast(posAdj, rayF, out hit, RaySize * F, HitLayer))
				{
					Debug.DrawRay(posAdj, rayF, Color.red);
					wheel.brakeTorque = brakeTorque;
					Brake = true;
					stack++;
					RayC = true;

					if (Physics.Raycast(posAdj, rayF, RaySize * F, HitLayer))
						if (stack >= StackTime && Physics.Raycast(transform.position, rayF, RaySize * F, HitLayer))
						{
							torque = -15000;
							wheel.brakeTorque = brakeTorque;
							Brake = false;

							if (stack >= StackTime + 500)
							{
								torque = tmptorque;
								stack = 0;
							}
						}
				}
				else
				{
					Brake = false;
					RayC = false;
				}
			}

			if (Restart == true && stack > StackTime)
			{
				transform.Rotate(Vector3.up, AngleStack * Time.deltaTime);
				stack = 0;
			}

			if (rayMod == Rays.Yes)
			{
				Debug.DrawRay(posAdj, Quaternion.AngleAxis(RayAngle / 2, transform.up) * ray, Color.white);
				if (Physics.SphereCast(posAdj, .2f, Quaternion.AngleAxis(RayAngle / 2, transform.up) * ray, out hit, RaySize, HitLayer))
				{
					Debug.DrawRay(posAdj, Quaternion.AngleAxis(RayAngle / 2, transform.up) * ray, Color.red);

					m_Wheels[0].steerAngle = -(7 * (SmoothSteerAngle));
					m_Wheels[1].steerAngle = -(7 * (SmoothSteerAngle));

					if (driveType == WDriveType.Wheeled8x8)
					{
						m_Wheels[0].steerAngle = -(7 * (SmoothSteerAngle));
						m_Wheels[1].steerAngle = -(7 * (SmoothSteerAngle));
						m_Wheels[2].steerAngle = -(7 * (SmoothSteerAngle));
						m_Wheels[3].steerAngle = -(7 * (SmoothSteerAngle));
					}
					RayRN = true;
				}
				else
					RayRN = false;

				Debug.DrawRay(posAdj, Quaternion.AngleAxis(RayAngle, transform.up) * ray, Color.white);
				if (Physics.Raycast(posAdj, Quaternion.AngleAxis(RayAngle, transform.up) * ray, out hit, RaySize, HitLayer))
				{
					Debug.DrawRay(posAdj, Quaternion.AngleAxis(RayAngle, transform.up) * ray, Color.red);

					m_Wheels[0].steerAngle = -(12 * (SmoothSteerAngle));
					m_Wheels[1].steerAngle = -(12 * (SmoothSteerAngle));
					
					if (driveType == WDriveType.Wheeled8x8)
					{
						m_Wheels[0].steerAngle = -(12 * (SmoothSteerAngle));
						m_Wheels[1].steerAngle = -(12 * (SmoothSteerAngle));
						m_Wheels[2].steerAngle = -(12 * (SmoothSteerAngle));
						m_Wheels[3].steerAngle = -(12 * (SmoothSteerAngle));
					}
					RayRF = true;
				}
				else
					RayRF = false;

				Debug.DrawRay(posAdj, Quaternion.AngleAxis(-RayAngle / 2, transform.up) * ray, Color.white);
				if (Physics.SphereCast(posAdj, .2f, Quaternion.AngleAxis(-RayAngle / 2, transform.up) * ray, out hit, RaySize, HitLayer))
				{
					Debug.DrawRay(posAdj, Quaternion.AngleAxis(-RayAngle / 2, transform.up) * ray, Color.red);

					m_Wheels[0].steerAngle = (7 * (SmoothSteerAngle));
					m_Wheels[1].steerAngle = (7 * (SmoothSteerAngle));

					if (driveType == WDriveType.Wheeled8x8)
					{
						m_Wheels[0].steerAngle = (7 * (SmoothSteerAngle));
						m_Wheels[1].steerAngle = (7 * (SmoothSteerAngle));
						m_Wheels[2].steerAngle = (7 * (SmoothSteerAngle));
						m_Wheels[3].steerAngle = (7 * (SmoothSteerAngle));
					}
					RayLN = true;
				}
				else
					RayLN = false;

				Debug.DrawRay(posAdj, Quaternion.AngleAxis(-RayAngle, transform.up) * ray, Color.white);
				if (Physics.Raycast(posAdj, Quaternion.AngleAxis(-RayAngle, transform.up) * ray, out hit, RaySize, HitLayer))
				{
					Debug.DrawRay(posAdj, Quaternion.AngleAxis(-RayAngle, transform.up) * ray, Color.red);

					if (torque > 0)
					{
						m_Wheels[0].steerAngle = (12 * (SmoothSteerAngle));
						m_Wheels[1].steerAngle = (12 * (SmoothSteerAngle));
					} 
					else
					{
						m_Wheels[0].steerAngle = -(12 * (SmoothSteerAngle));
						m_Wheels[1].steerAngle = -(12 * (SmoothSteerAngle));
					}
					if (driveType == WDriveType.Wheeled8x8)
					{
						m_Wheels[0].steerAngle = (12 * (SmoothSteerAngle));
						m_Wheels[1].steerAngle = (12 * (SmoothSteerAngle));
						m_Wheels[2].steerAngle = (12 * (SmoothSteerAngle));
						m_Wheels[3].steerAngle = (12 * (SmoothSteerAngle));
					}
					RayLF = true;
				}
				else
					RayLF = false;

				if (usePassingSensors)
				{
					// Check directly to the left
					Vector3 ta = Quaternion.AngleAxis(-30, transform.up) * ray * .35f;
					Debug.DrawRay(posAdj, ta, Color.white);
					if (Physics.SphereCast(posAdj, .2f, ta, out hit, RaySize, LayerMask.GetMask("Vehicle")))
					{
						Debug.DrawRay(posAdj, ta, Color.red);

						if (driveType == WDriveType.Wheeled8x8)
						{
							m_Wheels[0].steerAngle += (2f * (SmoothSteerAngle));
							m_Wheels[1].steerAngle += (2f * (SmoothSteerAngle));
							m_Wheels[2].steerAngle += (2f * (SmoothSteerAngle));
							m_Wheels[3].steerAngle += (2f * (SmoothSteerAngle));
						}
						else
						{
							m_Wheels[0].steerAngle += (2f * (SmoothSteerAngle));
							m_Wheels[1].steerAngle += (2f * (SmoothSteerAngle));
						}
					}

					// Check directly to the right
					ta = Quaternion.AngleAxis(30, transform.up) * ray * .35f;
					Debug.DrawRay(posAdj, ta, Color.white);
					if (Physics.SphereCast(posAdj, .2f, ta, out hit, RaySize, LayerMask.GetMask("Vehicle")))
					{
						Debug.DrawRay(posAdj, ta, Color.red);
						if (driveType == WDriveType.Wheeled8x8)
						{
							m_Wheels[0].steerAngle += -(2f * (SmoothSteerAngle));
							m_Wheels[1].steerAngle += -(2f * (SmoothSteerAngle));
							m_Wheels[2].steerAngle += -(2f * (SmoothSteerAngle));
							m_Wheels[3].steerAngle += -(2f * (SmoothSteerAngle));
						}
						else
						{
							m_Wheels[0].steerAngle += -(2f * (SmoothSteerAngle));
							m_Wheels[1].steerAngle += -(2f * (SmoothSteerAngle));
						}
					}
				}
			}
		}
	}

	void Navigation()
	{
		if (Waypoints != null)
		{
			if (currentWaypoint >= Waypoints.waypoints.LongLength)
			{
				currentWaypoint = 0;
				currentWaypoint2 = 1;
			}

			if (currentWaypoint == Waypoints.waypoints.Length - 1 && ParkingOnEnd == true) Parking = true;

			// Check to see if the waypoint is no longer in front, if not then move towards that instead of chase vehicle

			Vector3 diff2 = Vector3.zero;
			float dot2 = 0;

			if (currentWaypoint2 < Waypoints.waypoints.Length && currentWaypoint2 >= 0)
			{
				diff2 = Waypoints.waypoints[currentWaypoint2].position - transform.position;
				dot2 = Vector3.Dot(diff2.normalized, transform.forward);
			}

			if (policeAddons.Chase == false)
			{
				Vector3 nextWaypointPosition = transform.InverseTransformPoint(new Vector3(Waypoints.waypoints[currentWaypoint].position.x, transform.position.y, Waypoints.waypoints[currentWaypoint].position.z));

				if (nextWaypointPosition.x < maxAngle || nextWaypointPosition.x > -maxAngle)
					angle = nextWaypointPosition.x;

				if (angle > maxAngle)
					angle = maxAngle;

				if (angle < -maxAngle)
					angle = -maxAngle;

				FindClosestWay();

				if (nextWaypointPosition.magnitude < Speed && Speed > maxTurnSpeed)
					Slow = true;
				else
					Slow = false;

				distance = Vector3.Distance(transform.position, Waypoints.waypoints[currentWaypoint].transform.position);

				if (DynamicNextWay && Speed / 2 < NextWay)
					NextWayDynamic = Speed / 2 + 1f;

				if (DynamicNextWay)
				{
					if (NextWayDynamic > NextWay) NextWayDynamic = NextWay;

					if (nextWaypointPosition.magnitude < NextWayDynamic)
					{
						currentWaypoint++;
						currentWaypoint2 = currentWaypoint + 1;
					}
				}
				else if (nextWaypointPosition.magnitude < NextWay)
				{
					currentWaypoint++;
					currentWaypoint2 = currentWaypoint + 1;
				}
			}
			else
			{
				Vector3 nextWaypointPosition = transform.InverseTransformPoint(new Vector3(policeAddons.Target.transform.position.x, transform.position.y, policeAddons.Target.transform.position.z));

				angle = nextWaypointPosition.x;

				if (nextWaypointPosition.x < maxAngle || nextWaypointPosition.x > -maxAngle)
					angle = nextWaypointPosition.x;

				if (angle > maxAngle)
					angle = maxAngle;
				if (angle < -maxAngle)
					angle = -maxAngle;

				//	policeAddons.Enemys2 = GameObject.FindGameObjectsWithTag ("Way");
				policeAddons.Enemys2 = new Transform[Waypoints.transform.childCount];

				for (int i = 0; i < Waypoints.transform.childCount; i++)
				{
					policeAddons.Enemys2[i] = Waypoints.transform.GetChild(i).gameObject.transform;
				}
				var distance2 = Mathf.Infinity;
				var position = transform.position;

				foreach (Transform go in policeAddons.Enemys2)
				{
					var diff = (go.transform.position - position);
					var curDistance2 = diff.sqrMagnitude;
					closestWay = go;
					policeAddons.Target2 = go;
					distance2 = curDistance2;
					int index = closestWay.transform.GetSiblingIndex();

					//	if(currentWaypoint2 <= index)
					//	currentWaypoint2 = index;
					//	currentWaypoint = currentWaypoint2;
				}
			}
		}
		else if (policeAddons.Chase == true)
		{
			Vector3 nextWaypointPosition = transform.InverseTransformPoint(new Vector3(policeAddons.Target.transform.position.x, transform.position.y, policeAddons.Target.transform.position.z));

			angle = nextWaypointPosition.x;

			if (nextWaypointPosition.x < maxAngle || nextWaypointPosition.x > -maxAngle)
				angle = nextWaypointPosition.x;

			if (angle > maxAngle)
				angle = maxAngle;
			if (angle < -maxAngle)
				angle = -maxAngle;
		}
		else
			Parking = true;
	}

	Vector3 FindClosestEnemy()
	{
		policeAddons.Enemys = GameObject.FindGameObjectsWithTag(policeAddons.EnemyTag);
		GameObject closest;
		distance = Mathf.Infinity;
		Vector3 diff = Vector3.zero;
		Vector3 finalDiff = Vector3.zero;

		foreach (GameObject go in policeAddons.Enemys)
		{
			diff = go.transform.position - transform.position;
			var alt = go.transform.position.y - transform.position.y;
			var curDistance = diff.sqrMagnitude;

			if (curDistance < distance)
			{
				finalDiff = diff;
				closest = go;
				policeAddons.Target = go;
				distance = curDistance;
			}
		}

		if (policeAddons != null && policeAddons.Target != null)
			policeAddons.EnemyRange = Vector3.Distance(transform.position, policeAddons.Target.transform.position);

		return finalDiff;
	}

	void FindClosestWay()
	{
		if (Waypoints != null && Waypoints.transform.childCount > 0 && currentWaypoint == currentWaypoint2)
		{
			//policeAddons.Enemys2 = GameObject.FindGameObjectsWithTag("Way");
			policeAddons.Enemys2 = new Transform[Waypoints.transform.childCount];

			for (int i = 0; i < Waypoints.transform.childCount; i++)
				policeAddons.Enemys2[i] = Waypoints.transform.GetChild(i).gameObject.transform;

			var distance2 = Mathf.Infinity;
			var position = transform.position;

			foreach (Transform go in policeAddons.Enemys2)
			{
				var diff = (go.transform.position - position);
				var alt = (go.transform.position.y - position.y);
				var curDistance2 = diff.sqrMagnitude;
				float dot = Vector3.Dot(transform.forward, diff);

				if (curDistance2 < distance2)
				{
					closestWay = go;
					distance2 = curDistance2;
					int index = closestWay.transform.GetSiblingIndex();

					//	closestWay = policeAddons.Enemys2 [index];
					currentWaypoint = index;
					currentWaypoint2 = currentWaypoint + 1;
				}
			}
			currentWaypoint = (currentWaypoint + 1) % (policeAddons.Enemys2.Length - 1);
			currentWaypoint2 = ((currentWaypoint + 1) % (policeAddons.Enemys2.Length - 1)) + 1;
		}
	}
}
