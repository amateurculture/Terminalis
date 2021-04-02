using UnityEngine;
using System.Collections;
public class AIContoller : MonoBehaviour
{

    public static AIContoller manager;
    public bool showStatus = true;
	public int currentVehicles = 0;
	[Range(0f, 100f)]
	public int maxVehicles ;
    public GameObject[] vehiclesPrefabs;
	public GameObject AiVehicleCreated;

//	[HideInInspector]
	public WayRoad WayBase;
	public WayRoad[] WayBases;


	public GameObject randomWay;
	//	[HideInInspector]
	public GameObject NextTargetWay ;
	public bool RandmWaypointBase;
	public Transform[] Ways ;


//    [HideInInspector]
    public Transform player;

    private int frameCount = 0;
    private float dt = 0.0f;
    private float fps = 0.0f;
    private float updateRate = 10.0f; // 10 updates per sec.
	public bool CanCreateVehicle = true;
	[HideInInspector]
	public float vehicleTimer;
	public float InstantiateTime = 2.0f;
	[HideInInspector]
	public int currentWaypoint = 0;
	[HideInInspector]
	public int currentWaypoint2 = 0;



	void Start()
	{
		if(RandmWaypointBase)
		{
			WayBase = FindObjectOfType (typeof(WayRoad)) as WayRoad;

			//	WayBase = Ways [Random.Range (0, WayBases.Length)].gameObject ;
		}


		if(WayBase)
		  {	
		  Ways = new Transform[WayBase.transform.childCount];
		   for (int i = 0; i < WayBase.transform.childCount; i++)
		    {
			Ways[i] = WayBase.transform.GetChild(i).gameObject.transform;          
		    }
		  }
		  else
		    WayBase = FindObjectOfType (typeof(WayRoad)) as WayRoad;


	}


    void Awake()
    {
        manager = this;
    }


    void OnGUI()
    {
        if (showStatus)
        {
            GUI.color = Color.black;
            GUI.Label(new Rect(10, 30, 200, 20), "Max Vehicles: " + currentVehicles + "/" + maxVehicles);
            GUI.Label(new Rect(10, 50, 200, 20), "FPS: " + fps.ToString("F1"));
        }
    }


    void Update()
    {


		if (currentVehicles < maxVehicles && WayBase)
		vehicleTimer = vehicleTimer+0.1f;

		RaycastHit hit;
		if (Physics.Raycast(transform.position, -Vector3.up, out hit))
		{

			if (CanCreateVehicle && currentVehicles < maxVehicles && vehicleTimer > InstantiateTime)
			{
				
				vehicleTimer = 0;

				if(WayBases.Length > 0)
					for (int i = 0; i < WayBases.LongLength; i++)
					{
						WayBase = WayBases [Random.Range (0, WayBases.Length)];


					}

				if(WayBase)
				{	
					Ways = new Transform[WayBase.transform.childCount];
					for (int i = 0; i < WayBase.transform.childCount; i++)
					{
							Ways[i] = WayBase.transform.GetChild(i).gameObject.transform;          
					}
				}

				foreach(Transform go in Ways)  {

				//Ways = GameObject.FindGameObjectsWithTag("Way");
				int index = Random.Range (0, Ways.Length);
					randomWay = Ways[index].gameObject;


					if (index + 2 > Ways.Length)
						NextTargetWay = Ways [0].gameObject;
					else
						NextTargetWay = Ways [index + 1].gameObject;
						



				}

				AiVehicleCreated = Instantiate(vehiclesPrefabs[Random.Range(0, vehiclesPrefabs.Length)],randomWay.transform.position +  (Vector3.up * 3f),Quaternion.identity) as GameObject;

				ISMART iSMART;
				iSMART = AiVehicleCreated.GetComponent<ISMART> ();
				iSMART.Waypoints = WayBase;
				iSMART.RotateNextWaypoint = true;

				currentVehicles++;

			}


		}


      

        if (!showStatus) return;

        frameCount++;
        dt += Time.deltaTime;
        if (dt > 1.0 / updateRate)
        {
            fps = frameCount / dt;
            frameCount = 0;
            dt -= 1.0f / updateRate;
        }

    }


}