using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    public List<GameObject> spawn;
    public int start = 5;
    public int max = 5;

    [Tooltip("In seconds")]
    public int rate = 15;

    [Tooltip("Respawn this many each time")]
    public int respawn = 0;
    public float radius = 25f;
    public float wander = 50f;
    public float range = 100f;
    
    private GameObject player;
    private Vector3 point;
    private Coroutine spawnRoutine;
    private Brain brain;

    void Start()
    {
        brain = GameObject.Find("Brain")?.GetComponent<Brain>();
        player = GameObject.FindGameObjectWithTag("Player");

        for (var i = 0; i < start; i++)
            SpawnOne();

        if (spawn != null && spawn.Count > 0 && respawn > 0)
            spawnRoutine = StartCoroutine(Spawn());
    }

    #region Spawn

    void SpawnOne()
    {
        RandomPoint(this.transform.position, radius, out point);

        int r = (int)Mathf.Round(Random.Range(0, spawn.Count));

        Vector3 rotation = transform.rotation.eulerAngles;
        rotation.y = Random.Range(0, 360);

        GameObject obj = (GameObject)Instantiate(spawn.ToArray()[r], point, Quaternion.Euler(rotation));
        obj.name = obj.name.Substring(0, obj.name.Length - 7);

        // Auto randomize avoidance priority to reduce agent collisions
        NavMeshAgent navMeshAgent = obj.GetComponent<NavMeshAgent>();
        if (navMeshAgent != null)
            navMeshAgent.avoidancePriority = Mathf.RoundToInt(Random.Range(0, 99));

        obj.transform.parent = this.transform;
    }

    IEnumerator Spawn()
    {
        while (true)
        {
            int spawnThisTime = respawn;
            if (spawnThisTime + transform.childCount > max)
            {
                spawnThisTime = max - transform.childCount + 1;
                if (spawnThisTime <= 0)
                    spawnThisTime = 0;
            }

            if (spawnThisTime > 0)
                for (var i = 0; i < spawnThisTime; i++)
                    SpawnOne();

            yield return new WaitForSeconds(rate);
        }
    }

    bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * range;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }

    #endregion

    #region Debugging

#if DEBUG
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(this.transform.position, range);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(this.transform.position, wander);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, radius);
    }
#endif
    
    #endregion

    #region Loop

    //bool isFar = false;
    //bool isClose = false;

    /*
    private void LateUpdate()
    {
        if (player == null) return;

        float distance = Vector3.Distance(player.transform.position, transform.position);
        
        if (!isFar && distance > range)
        {
            for (var i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(false);

            isFar = true;
            isClose = false;
        }
        else if (!isClose && distance <= range)
        {
            for (var i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(true);
            isClose = true;
            isFar = false;
        }
    }
    */

    #endregion
}
