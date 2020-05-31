using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Agent))]

public class Automata : MonoBehaviour
{
    #region Property Inspector Variables
    public float lifespan;

    public float sightRange = 25f;
    public float hearingRange = 10f;
    public float fov = 120f;

    public Globals.AIType aiStyle;
    [Tooltip("Value cooresponds to small, medium, and large animal.")] [Range(1, 3)] public int sizeClass;

    public GameObject mother;
    public GameObject father;
    public GameObject spouse;

    [Header("Diet")]
    public Globals.Diet diet;
    public GameObject stapleFood;

    //[Header("Testing")] 
    [HideInInspector] [EnumFlags] public Globals.AITestingFlags show;
    [HideInInspector] public bool isRagdollEnabled;
    [HideInInspector] public bool isAttacking;
    [HideInInspector] public bool isRunning;

    #endregion

    #region Class Variables

    protected Spawner spawner;
    protected float minWanderRange;
    protected float chargeRange;
    protected float maxWanderRange;
    protected bool isDead;
    protected GameObject navigationLocus;
    protected NavMeshAgent navMeshAgent;
    protected Animator animatorComponent;
    protected GameObject player;
    protected float updateTime;
    protected float _ai_wait_;
    protected Coroutine robotCoroutine;
    protected float previousSpeed = 0;
    protected float _run_speed;
    protected float _walk_speed;

    private Agent agent;
    private Vector3 waypoint;

    #endregion

    #region Initialize

    private void Reset()
    {
        minWanderRange = 5;
        chargeRange = 10;
        maxWanderRange = 15;
        sizeClass = 1;

        minWanderRange = 5 * sizeClass;
        chargeRange = 8 * sizeClass;
        maxWanderRange = 13 * sizeClass;

        navMeshAgent = GetComponentInChildren<NavMeshAgent>();
        navMeshAgent.autoTraverseOffMeshLink = true;
        navMeshAgent.autoRepath = true;
        navMeshAgent.autoBraking = true;
        navMeshAgent.updateUpAxis = false;
        navMeshAgent.areaMask = (1 << 0);
        navMeshAgent.height = 1;
    }

    Vector3 FindNearestTree()
    {
        Terrain terrain = Terrain.activeTerrain;
        TerrainData data = terrain.terrainData;
        TreeInstance[] trees = data.treeInstances;

        if (trees.Length > 0)
        {
            TreeInstance Nearest = trees[0];
            Vector3 NearPosition = Vector3.Scale(Nearest.position, data.size) + terrain.transform.position;

            foreach (TreeInstance Location in trees)
            {
                Vector3 position = Vector3.Scale(Location.position, data.size) + terrain.transform.position;

                if (Vector3.Distance(position, this.gameObject.transform.position) < 
                    Vector3.Distance(NearPosition, this.gameObject.transform.position))
                {
                    Nearest = Location;
                    NearPosition = position;
                }
            }
            NearPosition.y = terrain.SampleHeight(NearPosition);
            Debug.Log(NearPosition);
            return NearPosition;

            //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //cube.transform.position = NearPosition;
        }
        return Vector3.positiveInfinity;
    }

    //[System.Obsolete]
    void Start()
    {
        //DisableAllColliders();

        if (transform.parent != null) spawner = transform.parent.GetComponent<Spawner>();

        agent = GetComponent<Agent>();
        agent.InitializeAgent();
        updateTime = 0;
        _ai_wait_ = 1f;

        minWanderRange = 5 * sizeClass;
        chargeRange = 8 * sizeClass;
        maxWanderRange = 13 * sizeClass;

        player = GameObject.FindGameObjectWithTag("Player");
        navMeshAgent = GetComponentInChildren<NavMeshAgent>();
        animatorComponent = GetComponent<Animator>();

        navigationLocus = transform.gameObject;
        navMeshAgent.avoidancePriority = (int)(Random.value * 100f);
        navMeshAgent.updateRotation = true;
        navMeshAgent.updatePosition = true;
        navMeshAgent.autoTraverseOffMeshLink = true;
        navMeshAgent.autoRepath = true;
        navMeshAgent.areaMask = (1 << 0);
        navMeshAgent.autoBraking = true;
        navMeshAgent.updateUpAxis = false;
        robotCoroutine = null;
        _run_speed = navMeshAgent.speed;
        _walk_speed = _run_speed / 2;
        isDead = false;

        //StartCoroutine(_PatchAnimatorNotWorkingAtStart());
        if (isRagdollEnabled) DisableAllColliders();

        StopWalking();
    }

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
            Gizmos.DrawWireSphere(transform.position, hearingRange);
        }
        if (show.HasFlag(Globals.AITestingFlags.SightRange))
        {
            Gizmos.color = Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawFrustum(Vector3.zero, fov / 2, sightRange, 0f, 2f);
        }

        Gizmos.color = Color.yellow;
        if (navMeshAgent != null)
        {
            Gizmos.DrawWireSphere(navMeshAgent.destination, .15f);
            Debug.DrawLine(transform.position, navMeshAgent.destination, Color.yellow);
        }

        //if (navMeshAgent != null && navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid)
        //    print(transform.name + "'s path is invalid!");
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

    void DisableAllColliders()
    {
        colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders) c.enabled = false;
    }

    void EnableAllColliders()
    {
        colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders) c.enabled = true;
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

    bool InRange(Vector3 a, float min)
    {
        if (Mathf.Abs(a.x - transform.position.x) < min &&
            Mathf.Abs(a.y - transform.position.y) < min &&
            Mathf.Abs(a.z - transform.position.z) < min)
            return true;
        return false;
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
        return waypoint.x == Mathf.Infinity || InRange(waypoint, minWanderRange);
    }

    IEnumerator WaitForDecision()
    {
        bool isValidWaypoint = false;

        while (!isValidWaypoint)
        {
            yield return new WaitForSeconds(5);

            //waypoint = Vector3.positiveInfinity;
            //if (IsValidTerrainWaypoint())
            //{

            waypoint = RandomNavmeshLocation(maxWanderRange);
            NavMeshPath navPath = new NavMeshPath();

            if (navMeshAgent.isActiveAndEnabled && navMeshAgent.CalculatePath(waypoint, navPath))
            {
                isValidWaypoint = true;

                if (spawner != null && !InRange(transform.position, spawner.transform.position, spawner.range))
                {
                    isValidWaypoint = false;
                }
            }

            //}
        }
        navMeshAgent.speed = _walk_speed;
        navMeshAgent.SetDestination(waypoint);
        robotCoroutine = null;
    }

    public Vector3 RandomNavmeshLocation(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
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

    void MoveTo(Vector3 here, float speed)
    {
        navMeshAgent.speed = speed;
        navMeshAgent.SetDestination(here);
        robotCoroutine = null;
    }

    void ChildLogic()
    {
        /** Child Logic Flow:
        *
        *   1) Are any of my needs in emergency? if yes resolve, if no continue
        * X 2) Do I have a living mother and is she out of range? if yes run to her, if no continue
        * X 3) Do I have a living father and is he out of range? if yes run to him, if no continue
        *   4) Is there a female of my species nearby? if yes run to her, if no continue
        *   5) Is there a male of my species nearby? if yes run to him, if no continue
        *   6) Is there a nearby animal? if yes walk to them, if no continue
        * X 6) Wander
        */

        if (mother != null)
        {
            if (!InRange(mother.transform.position, maxWanderRange))
                MoveTo(mother.transform.position, _run_speed);
        }
        else if (father != null)
            if (!InRange(father.transform.position, maxWanderRange))
                MoveTo(father.transform.position, _run_speed);
            else if (aiStyle == Globals.AIType.Agressive && InRange(player.transform.position, chargeRange))
                MoveTo(player.transform.position, _run_speed);
    }

    void AdultLogic()
    {
        /** Adult Logic Flow:
        *
        *   1) Am I female, am I partnered, can I give birth, am I about to give birth, or in estrous and is it the first day of spring? if yes produce 0-n children, if no continue.
        *   2) Are any of my needs in emergency? If yes resolve, if no continue.
        *   3) Do I have a schedule? If yes do that, if no continue.
        * X 4) Am I agressive and is a target in range? If yes attack, if no continue.
        *   5) Am I defensive and is a target in range? If yes attack, if no continue.
        *   6) Am I passive and is anyone not of my species in range? If yes run away, if no continue.
        *   7) Are any of my needs in warning? If yes resolve, if no continue.
        * X 8) Is my partner out of range and are both of us not on a schedule? If yes run to them, if no continue.
        * X 9) Wander
        */

        if (agent.hunger == 0)
        {
            agent.vitality --;
        }
        else if (aiStyle == Globals.AIType.Agressive && InRange(player.transform.position, chargeRange))
        {
            /*** todo figure out if checking raycast is worth it enough here
            RaycastHit hit;
            int layerMask = LayerMask.NameToLayer("Player") | LayerMask.NameToLayer("Default");
            Vector3 adjustedPlayer = player.transform.position;
            adjustedPlayer.y += 1f;
            Vector3 adjustedPosition = transform.position;
            adjustedPosition.y += 1f;
            var heading = adjustedPlayer - adjustedPosition;

            if (Physics.Raycast(adjustedPosition, heading, out hit, maxWanderRange, layerMask))
                if (hit.transform.tag == "Player")
                    */
            
            MoveTo(player.transform.position, _run_speed);
        }
        else if (spouse != null && !InRange(spouse.transform.position, maxWanderRange))
            MoveTo((spouse.transform.position + transform.position) / 2, _run_speed);
    }

    Collider[] colliders;
   
    IEnumerator KillAfterTime()
    {
        isDead = true;
        animatorComponent.SetBool("isDead", true);
        animatorComponent.Update(0);
        navMeshAgent.enabled = true;
        animatorComponent.enabled = true;

        yield return new WaitForSeconds(3);

        navMeshAgent.enabled = false;
        animatorComponent.enabled = false;

        EnableAllColliders();
    }

    bool IsDead()
    {
        if (isDead && agent.vitality > 0)
        {
            StopAllCoroutines(); // may break something? maybe use a coroutine variable instead of kill all?
            robotCoroutine = null;

            animatorComponent.SetBool("isDead", false);
            animatorComponent.enabled = true;
            navMeshAgent.enabled = true; // before or after disable colliders? seems to be in the right place maybe?
            
            if (isRagdollEnabled) DisableAllColliders();

            isDead = false;
            return false;
        }
        else if (agent.vitality <= 0 && !isDead)
        {
            agent.vitality = 0;
            StartCoroutine(KillAfterTime());

            if (robotCoroutine != null) StopCoroutine(robotCoroutine); robotCoroutine = null;

            return true;
        }
        return isDead;
    }

    private void Update()
    {
        if (!IsDead())
        {
            // I am assessing if anything in my environment needs tending to.
            if (Time.frameCount % 100 == 0)
            {
                // todo get generic age slider to work
                //if (umaData == null) umaData = GetComponent<UMAData>()
                //var dnaDB = umaData.GetAllDna();
                //var names = dnaDB[0].Names;
                //print(names);

                if (aiStyle == Globals.AIType.Child)
                    ChildLogic();
                else
                    AdultLogic();
            }

            // I am in the middle of deciding what to do.
            if (!isDead && robotCoroutine == null)
            {
                // I am updating my animation blendtree.
                var s = navMeshAgent.velocity.magnitude;
                var speed = Mathf.Lerp(previousSpeed, s, Time.deltaTime);
                previousSpeed = speed;
                animatorComponent.SetFloat("Speed", speed);

                // I have arrived, make a decision.
                if ((navMeshAgent.isActiveAndEnabled && !navMeshAgent.pathPending) &&
                    (!navMeshAgent.hasPath || navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance) &&
                    navMeshAgent.velocity.sqrMagnitude == 0)
                    StopWalking();
            }
        }
    }

    private void OnTriggerEnter(Collider collider) {
        if (collider.transform.tag == "Player" || collider.transform.name.Contains("Arrow")) 
            agent.vitality = 0; 
    }

    private void OnCollisionEnter(Collision collision) {
         if (collision.transform.tag == "Player" || collision.transform.name.Contains("Arrow"))
            agent.vitality = 0; 
    }

    #endregion
}
