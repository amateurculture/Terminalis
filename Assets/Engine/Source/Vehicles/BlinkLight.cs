using System.Collections;
using UnityEngine;

public class BlinkLight : MonoBehaviour
{
    public float secondSkip = 3;
    public GameObject[] lights;

    void Start()
    {
        StartCoroutine(BlinkCoroutine());
    }

    IEnumerator BlinkCoroutine()
    {
        for (var i = 0; i < lights.Length; i ++)
            lights[i].SetActive(false);
        
        yield return new WaitForSeconds(.1f);

        for (var i = 0; i < lights.Length; i++)
            lights[i].SetActive(true);

        yield return new WaitForSeconds(secondSkip);

        StartCoroutine(BlinkCoroutine());
    }
}
