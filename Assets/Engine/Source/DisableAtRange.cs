using UnityEngine;

public class DisableAtRange : MonoBehaviour
{
    private int interval = 5;
    private GameObject player;
    public GameObject[] targets;
    public float range;
    float distanceSqr;
    float rangeSqr;
    float previousRangeSqr;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        previousRangeSqr = rangeSqr = range * range;
    }

    void Update()
    {
        if (Time.frameCount % interval == 0)
        {
            distanceSqr = Vector3.SqrMagnitude(player.transform.position - transform.position);

            if (previousRangeSqr != rangeSqr)
            {
                rangeSqr = range * range;
                previousRangeSqr = rangeSqr;
            }

            foreach (var target in targets)
            {
                if (target.activeSelf && distanceSqr >= rangeSqr)
                    target.SetActive(false);
                else if (!target.activeSelf && distanceSqr < rangeSqr)
                    target.SetActive(true);
            }
        }
    }
}
