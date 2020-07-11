using UnityEngine;

public class EnterBuilding : MonoBehaviour
{
    public Transform stuff;

    private void Start()
    {
        foreach(Transform obj in stuff)
        {
            obj.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            foreach(Transform obj in stuff) obj.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            foreach(Transform obj in stuff) obj.gameObject.SetActive(false);
        }
    }
}
