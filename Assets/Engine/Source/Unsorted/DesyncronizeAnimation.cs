using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesyncronizeAnimation : MonoBehaviour
{
    Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        animator.enabled = false;
        StartCoroutine(RandomizeStartTime());
    }

    IEnumerator RandomizeStartTime()
    {
        yield return new WaitForSeconds(Random.value * 2);
        animator.enabled = true;
    }

}
