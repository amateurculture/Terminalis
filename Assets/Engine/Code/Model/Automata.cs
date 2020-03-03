using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Agent))]

public class Tilt : MonoBehaviour
{
    public LayerMask mask;
    public Transform referencePos;
    [HideInInspector]
    public Vector3 hitPoint;

    void Update()
    {
        
    }
}

public class Automata : MonoBehaviour
{
    #region Property Inspector Variables

    [Header("Behavior")] public Globals.AIType aiStyle;
    [Header("Testing")] [EnumFlags] public Globals.AITestingFlags show;
    public float minWanderRange;
    public float maxWanderRange;

    #endregion

    #region Class Variables

    protected GameObject navigationLocus;
    protected NavMeshAgent navMeshAgent;
    protected Animator animatorComponent;
    protected GameObject player;
    protected float updateTime;
    protected float _ai_wait_;

    private Spawner spawner;
    private Agent agent;
    private Vector3 waypoint;

    float preferredSpeed;
    float checkStuckAfterThisTime;
    float checkStuckInterval;
    Coroutine robotCoroutine;
    Vector2 smoothDeltaPosition = Vector2.zero;
    Vector3 originalPosition;

    #endregion

    #region Initialize

    private void Reset()
    {
        minWanderRange = 10;
        maxWanderRange = 30;
    }

    void Start()
    {
        agent = GetComponent<Agent>();
        agent.InitializeAgent();
        updateTime = 0;
        _ai_wait_ = 1f;

        player = GameObject.FindGameObjectWithTag("Player");
        navMeshAgent = GetComponentInChildren<NavMeshAgent>();
        animatorComponent = this.GetComponent<Animator>();

        navigationLocus = transform.gameObject;
        navMeshAgent.avoidancePriority = (int)(UnityEngine.Random.value * 100f);
        navMeshAgent.updateRotation = true;
        navMeshAgent.updatePosition = true;
        navMeshAgent.autoRepath = false;
        navMeshAgent.autoBraking = true;
        navMeshAgent.updateUpAxis = false;

        preferredSpeed = navMeshAgent.speed;
        robotCoroutine = null;
        checkStuckInterval = 2.5f;

        StopWalking();
    }

    /*
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
        */
    #endregion

    #region Debugging

#if DEBUG
    void OnDrawGizmosSelected()
    {
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
    }
#endif

    #endregion

    #region Navigation

    public Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        NavMeshHit navHit;
        bool isValidPath;
        float failsafe = 0;
        
        do
        {
            var localfailsafe = 0;
            Vector3 randDirection = Vector3.zero;
            do
            {
                randDirection = (UnityEngine.Random.insideUnitSphere * dist);
                randDirection += origin;
                localfailsafe++;
            }
            while (Vector3.Distance(randDirection, origin) < 15f && localfailsafe < 1000);

            NavMesh.SamplePosition(randDirection, out navHit, dist, -1);

            if (navHit.position.x == Mathf.Infinity ||
                navHit.position.y == Mathf.Infinity ||
                navHit.position.z == Mathf.Infinity)
                isValidPath = false;
            else
                isValidPath = true;

            failsafe ++;
        } 
        while (!isValidPath && failsafe < 1000);

        return navHit.position;
    }

    public Vector3 GetNextWaypoint(Vector3 locus, float range = 10f)
    {
        LayerMask mask = LayerMask.GetMask("Terrain", "Earth");
        return RandomNavSphere(locus, (spawner != null) ? spawner.wander : range, mask);
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
            
            if (navMeshAgent.remainingDistance < 1.5f || navMeshAgent.velocity.sqrMagnitude == 0)
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
                /*
                Building selectedBuilding = null;

                if (agent.hunger >= 100) selectedBuilding = FindClosestBuilding(Globals.BuildingType.Restaurant);
                else if (agent.fatigue >= 100) selectedBuilding = (agent.home != null) ? agent.home : FindClosestBuilding(Globals.BuildingType.Residence);
                else navMeshAgent.SetDestination(GetNextWaypoint());

                if (selectedBuilding != null) navMeshAgent.SetDestination(selectedBuilding.transform.position);
                else navMeshAgent.SetDestination(GetNextWaypoint());
                */

                //waypoint = futureWaypoint;
                //futureWaypoint = GetNextWaypoint(waypoint, 50);
                //navMeshAgent.SetDestination(waypoint);
            }
    }

    bool InRange(Vector3 a, Vector3 b, float min)
    {
        if (Mathf.Abs(a.x - b.x) < min &&
            Mathf.Abs(a.y - b.y) < min &&
            Mathf.Abs(a.z - b.z) < min)
            return true;
        return false;
    }

    bool IsValidTerrainWaypoint()
    {
        return waypoint.x == Mathf.Infinity || InRange(waypoint, transform.position, minWanderRange);
    }

    IEnumerator WaitForDecision()
    {
        waypoint = Vector3.positiveInfinity;
        bool isValidWaypoint = false;

        while (!isValidWaypoint)
        {
            yield return new WaitForSeconds(5);

            if (IsValidTerrainWaypoint())
            {
                //animatorComponent.SetFloat("Speed", navMeshAgent.velocity.magnitude);
                waypoint = RandomNavmeshLocation(maxWanderRange);

                NavMeshPath navPath = new NavMeshPath();
                if (navMeshAgent.CalculatePath(waypoint, navPath)) 
                    isValidWaypoint = true;
            }
        }
        //animatorComponent.SetFloat("Speed", preferredSpeed);
        navMeshAgent.SetDestination(waypoint);
        robotCoroutine = null;
        checkStuckAfterThisTime = Time.time + checkStuckInterval;
    }

    public Vector3 RandomNavmeshLocation(float radius)
    {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius;
        randomDirection += transform.position;
        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;

        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
            finalPosition = hit.position;

        return finalPosition;
    }

    void StopWalking()
    {
        navMeshAgent.velocity = Vector3.zero;

        if (animatorComponent.isInitialized) animatorComponent.SetFloat("Speed", 0);

        if (robotCoroutine != null) StopCoroutine(robotCoroutine);

        robotCoroutine = StartCoroutine(WaitForDecision());
    }

    float previousSpeed = 0;
    float t = 0;

    private void Update()
    {
        if (robotCoroutine == null) 
        {
            var s = navMeshAgent.velocity.magnitude;
            t += .0001f;
            var speed = Mathf.Lerp(previousSpeed, s, t);
            previousSpeed = speed;
            animatorComponent.SetFloat("Speed", speed);

            if (navMeshAgent.isActiveAndEnabled && !navMeshAgent.pathPending)
            {
                if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                {
                    if (navMeshAgent.velocity.sqrMagnitude == 0) 
                    {
                        StopWalking();
                    }
                }
            }
        }
    }

    #endregion
}
