using UnityEngine;
using System.Collections;

public class CreateAI : MonoBehaviour
{


    public LayerMask nodeMask = -1;
    public float InstantiateTime = 2.0f;



    private float vehicleTimer, humanTimer;

    public bool createVehicles = true;

	public AIContoller AICScript;
	public GameObject AiVehicleCreated;
	public GameObject AIVehicle;
	public float offsetDistance = 25;
    private int randomWay;



    public void InstantiateVehicle(WayRoad CurrentNode)
    {

        Collider[] vehicles = Physics.OverlapSphere(CurrentNode.transform.position, offsetDistance);

        bool CanCreateVehicle = true;

        foreach (Collider vehicle in vehicles)
        {
            if (vehicle.CompareTag("Vehicle"))
                CanCreateVehicle = false;
        }


//        AIVehicle = AIContoller.manager.vehiclesPrefabs[Random.Range(0, AIContoller.manager.vehiclesPrefabs.Length)];

        if (AIVehicle)
        {
            if (CanCreateVehicle && AIContoller.manager.currentVehicles < AIContoller.manager.maxVehicles)
            {
                RaycastHit hit;
                if (Physics.Raycast(CurrentNode.transform.position, -Vector3.up, out hit))
                {
                    AIContoller.manager.currentVehicles++;
                    AiVehicleCreated = Instantiate(AIVehicle, hit.point + (Vector3.up / 2.0f), Quaternion.identity) as GameObject;
                }



            }


        }
    }





    void Awake()
    {
        AICScript = AIContoller.manager;
    }



    void Update()
    {





        if (createVehicles)
        {
            if (vehicleTimer == 0)
            {
                Collider[] nodes = Physics.OverlapSphere(transform.position, 300, nodeMask);

                    foreach (Collider node in nodes)
                    {
                        float Dist = Vector3.Distance(transform.position, node.transform.position);

                        if (Dist < 250 && Dist > 200)
                        {
                            if (node.GetComponent<WayRoad>() && AIContoller.manager.vehiclesPrefabs.Length > 0)
                           {
                                if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(Camera.main), node.bounds))
                                {
							InstantiateVehicle(node.GetComponent<WayRoad>());
                                    vehicleTimer = InstantiateTime;

                                }


                            }

                        }
                    }
                
            }
            else
            {
                vehicleTimer = Mathf.MoveTowards(vehicleTimer, 0.0f, Time.deltaTime);
            }
        }


    }



}
