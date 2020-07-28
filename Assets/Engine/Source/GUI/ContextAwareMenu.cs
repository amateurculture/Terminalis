using System.Collections;
using UnityEngine;

/// <summary>
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

[RequireComponent(typeof(BoxCollider))]

public class ContextAwareMenu : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeDistance = 3.5f;
    public float fadeSpeed = 2f;

    Coroutine coroutine;
    BoxCollider boxCollider;
    float t = 0;

    private void Reset()
    {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.center = new Vector3(0, 1, -1.2f);
        boxCollider.size = new Vector3(2, 1, 2);
        boxCollider.isTrigger = true;
    }

    void Start()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            t = 0;
            if (coroutine != null) StopCoroutine(coroutine);
            if (canvasGroup != null) canvasGroup.gameObject.SetActive(true);
            coroutine = StartCoroutine(FadeIn());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            t = 0;
            if (coroutine != null) StopCoroutine(coroutine);
            coroutine = StartCoroutine(FadeOut());
        }
    }

    IEnumerator FadeIn()
    {
        while (true)
        {
            canvasGroup.alpha = Mathf.Lerp(0, 1, t);
            t += Time.deltaTime * fadeSpeed;

            if (canvasGroup.alpha >= .95f)
            {
                canvasGroup.alpha = 1;
                StopCoroutine(coroutine);
            }
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    IEnumerator FadeOut()
    {
        while (true)
        {
            canvasGroup.alpha = Mathf.Lerp(1, 0, t);
            t += Time.deltaTime * fadeSpeed;

            if (canvasGroup.alpha <= .1f)
            {
                canvasGroup.alpha = 0f;
                if (canvasGroup != null) canvasGroup.gameObject.SetActive(false);
                StopCoroutine(coroutine);
            }
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }
}
