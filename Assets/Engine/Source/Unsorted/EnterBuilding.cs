using UnityEngine;

public class EnterBuilding : MonoBehaviour
{
    public Transform stuff;

    private void Start()
    {
        stuff.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player") stuff.gameObject.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player") stuff.gameObject.SetActive(false);
    }
}
