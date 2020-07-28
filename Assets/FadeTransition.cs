using System.Collections;
using UnityEngine;

/// <summary>
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

public class FadeTransition : Transition
{
    public float speed = 10f;

    float current, target, direction;

    private void Start()
    {
        target = 0;
        direction = 1;
    }

    private void OnEnable()
    {
        Open();
    }

    public override void Open()
    {
        current = 0;
        target = 1;
        direction = 1;
        StartCoroutine(TransitionCoroutine());
    }

    public override void Close()
    {
        if (!gameObject.activeSelf) return;

        current = 1;
        target = 0;
        direction = -1;
        StartCoroutine(TransitionCoroutine());
    }
    private IEnumerator TransitionCoroutine()
    {
        while ((direction > 0 && current < target) || (direction < 0 && current >= target))
        {
            current += Time.deltaTime * direction * speed;
            yield return new WaitForEndOfFrame();
        }
    }
}
