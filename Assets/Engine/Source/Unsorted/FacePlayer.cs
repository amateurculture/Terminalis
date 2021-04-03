using System.Collections;
using UnityEngine;

/// <summary>
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

public class FacePlayer : MonoBehaviour
{
    public float turningRate;

    GameObject player;
    Transform lookAtTransform;
    Coroutine coroutine;

    private void Reset()
    {
        turningRate = .1f;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        coroutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag != "Player") return;

        if (coroutine != null) StopCoroutine(coroutine);
        coroutine = StartCoroutine(TurnTowards());
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag != "Player") return;

        if (coroutine != null) StopCoroutine(coroutine);
        coroutine = null;
    }

    IEnumerator TurnTowards()
    {
        while (true)
        {
            yield return new WaitForSeconds(.02f);
            lookAtTransform = transform;
            lookAtTransform.LookAt(player.transform);
        }
    }
}
