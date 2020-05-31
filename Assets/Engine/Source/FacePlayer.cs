using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class FacePlayer : MonoBehaviour
{
    public float turningRate;

    GameObject player;
    Transform lookAtTransform;
    Coroutine coroutine;
    float t;
    NavMeshAgent navmeshAgent;


    private void Reset()
    {
        turningRate = .1f;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        navmeshAgent = GetComponent<NavMeshAgent>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (coroutine != null || other.tag != "Player") return;

        t = 0;
        lookAtTransform = transform;
        lookAtTransform.LookAt(player.transform);
        lookAtTransform.eulerAngles = new Vector3(0, lookAtTransform.eulerAngles.y + 180, 0);

        coroutine = StartCoroutine(TurnTowards());
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
            navmeshAgent.enabled = true;
    }

    IEnumerator TurnTowards()
    {
        // todo totally does not lerp the rotation towards
        while (transform.rotation != lookAtTransform.rotation)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookAtTransform.rotation, t);
            t += turningRate;
            yield return new WaitForSeconds(Time.deltaTime);
        }
        navmeshAgent.enabled = false;
        coroutine = null;
    }
}
