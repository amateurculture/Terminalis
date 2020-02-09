using System.Collections;
using UnityEngine;

public class RecycleAgent : MonoBehaviour
{  
    public void RecycleTarget()
    {
        StartCoroutine(TimeToDeath());
    }

    IEnumerator TimeToDeath()
    {
        yield return new WaitForSeconds(15);
        Destroy(gameObject);
    }
}
