using UnityEngine;

public class DisableAtRange : MonoBehaviour
{
    private int interval = 5;
    private GameObject player;
    public GameObject target;
    public float range;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        if (Time.frameCount % interval == 0)
        {
            if (target.activeSelf && Vector3.Distance(player.transform.position, target.transform.position) >= range)
                target.SetActive(false);
            else if (!target.activeSelf && Vector3.Distance(player.transform.position, target.transform.position) < range)
                target.SetActive(true);
        }
    }
}
