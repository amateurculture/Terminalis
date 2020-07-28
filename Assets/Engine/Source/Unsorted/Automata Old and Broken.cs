using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Agent))]

public class AutomataOldAndBroken : MonoBehaviour
{
    #region Property Inspector Variables

    [Header("Behavior")]
    public Globals.AIType aiStyle;
    
    [Header("Testing")]
    [EnumFlags]
    public Globals.AITestingFlags show;

    #endregion

    #region Class Variables

    protected GameObject navigationLocus;
    protected NavMeshAgent navMeshAgent;
    protected Animator animatorComponent;
    protected float updateTime = 0;
    protected float _ai_wait_ = 1f;
    //private Spawner spawner;
    private Agent agent;
    protected GameObject player;

    #endregion

    #region Initialize

    void Start()
    {
        agent = GetComponent<Agent>();
        agent.InitializeAgent();

        player = GameObject.FindGameObjectWithTag("Player");
        navMeshAgent = GetComponentInChildren<NavMeshAgent>();
        animatorComponent = this.GetComponent<Animator>();
        navigationLocus = transform.gameObject;
        navMeshAgent.avoidancePriority = (int)(UnityEngine.Random.value * 100f);
        navMeshAgent.updateRotation = false;
        navMeshAgent.updatePosition = true;
        navMeshAgent.autoRepath = true;

        /*
        if (animatorComponent != null) animatorComponent.enabled = true;

        spawner = GetComponentInParent<Spawner>();
        if (spawner != null) navigationLocus = spawner.gameObject;
        */

    }

    void OnEnable() { StartCoroutine("_PatchAnimatorNotWorkingAtStart"); }

    IEnumerator _PatchAnimatorNotWorkingAtStart()
    {
        float timer = 0;
        while (true)
        {
            if (timer != -1)
            {
                if (timer == 0)
                {
                    if (animatorComponent != null)
                        animatorComponent.enabled = false;
                    timer = Time.time + .5f;
                }
                else if (Time.time > timer)
                {
                    if (animatorComponent != null)
                        animatorComponent.enabled = true;
                    timer = -1;
                    yield break;
                }
            }
            yield return new WaitForSeconds(.75f);
        }
    }

    #endregion

    #region Debugging

#if DEBUG
    void OnDrawGizmosSelected()
    {
        /*
        if (show.HasFlag(Globals.AITestingFlags.WanderRange))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(navigationLocus.transform.position, (spawner != null) ? spawner.wander : 10f);
        }
        if (show.HasFlag(Globals.AITestingFlags.ChaseRange))
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(navigationLocus.transform.position, (spawner != null) ? spawner.range : 20f);
        }
        if (show.HasFlag(Globals.AITestingFlags.HearingRange))
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, agent.hearingRange);
        }
        if (show.HasFlag(Globals.AITestingFlags.SightRange))
        {
            Gizmos.color = Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawFrustum(Vector3.zero, agent.fov / 2, agent.sightRange, 0f, 2f);
        }

        Gizmos.color = Color.yellow;
        if (navMeshAgent != null)
        {
            Gizmos.DrawWireSphere(navMeshAgent.destination, .15f);
            Debug.DrawLine(transform.position, navMeshAgent.destination, Color.yellow);
        }

        if (navMeshAgent != null && navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
            print(transform.name + "'s path is invalid!");
            */
    }
#endif

    #endregion

    #region Navigation

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = (UnityEngine.Random.insideUnitSphere * dist);
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }

    public Vector3 GetNextWaypoint(float range = 10f)
    {
        NavMeshPath navMeshPath = new NavMeshPath();
        Vector3 testWaypoint;

        do
        {
            Vector3 locus = navigationLocus.transform.position;
            //testWaypoint = RandomNavSphere(locus, (spawner != null) ? spawner.wander : range, -1);
            testWaypoint = RandomNavSphere(locus, range, -1);

            //float distance = Vector3.Distance(testWaypoint, transform.position);
            
        } while (!navMeshAgent.CalculatePath(testWaypoint, navMeshPath));

        return testWaypoint;
    }

    Building FindClosestBuilding(Globals.BuildingType buildingType)
    {
        List<Building> buildingList = new List<Building>();
        buildingList.AddRange(FindObjectsOfType<Building>());

        float nextChampion = -1;
        Building selectedBuilding = null;

        foreach (Building building in buildingList)
        {
            if (player != null && building != null)
            {
                float distance = Vector3.Distance(player.transform.position, building.transform.position);

                if (building.buildingType == buildingType &&
                    (nextChampion == -1 || distance < nextChampion) &&
                    !building.atCapacity())
                {
                    selectedBuilding = building;
                    nextChampion = distance;
                }
            }
        }
        return selectedBuilding;
    }

    #endregion

    #region Update Loop

    public void UpdatePosition()
    {
#if ENVIRO
        if (isWorking)
        {
            if (brain.enviro.currentHour > work.endHour)
                isWorking = false;
            else
                return;
        }
#endif
        if (!navMeshAgent.pathPending && navMeshAgent.isActiveAndEnabled)
            
            if (navMeshAgent.remainingDistance < 1.5f ||
                navMeshAgent.velocity.sqrMagnitude == 0)
            {
#if ENVIRO_HD && ENVIRO_LW
                if (Globals.Instance.enviro != null && agent.work != null && !agent.isWorking)
                {
                    DateTime currentDate = new DateTime((int)Globals.Instance.enviro.currentYear, Globals.Instance.GetMonth((int)Globals.Instance.enviro.currentDay), (int)Globals.Instance.enviro.currentDay);

                    if (agent.work.employeeSchedule.HasFlag(currentDate.DayOfWeek))
                    {
                        if (Globals.Instance.enviro.currentHour >= agent.work.startHour - 1 && Globals.Instance.enviro.currentHour <= agent.work.endHour)
                        {
                            agent.isWorking = true;
                            navMeshAgent.SetDestination(agent.work.transform.position);
                            return;
                        }
                    }
                }
#endif
                Building selectedBuilding = null;

                if (agent.hunger >= 100) selectedBuilding = FindClosestBuilding(Globals.BuildingType.Restaurant);
                else if (agent.fatigue >= 100) selectedBuilding = (agent.home != null) ? agent.home : FindClosestBuilding(Globals.BuildingType.Residence);
                else navMeshAgent.SetDestination(GetNextWaypoint());

                if (selectedBuilding != null) navMeshAgent.SetDestination(selectedBuilding.transform.position);
                else navMeshAgent.SetDestination(GetNextWaypoint());
            }
    }

    private void Update()
    {
        if (Time.frameCount % 5 == 0) UpdatePosition();
    }

    #endregion
}
