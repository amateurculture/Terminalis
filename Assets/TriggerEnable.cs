using UnityEngine;

/// <summary>
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

public class TriggerEnable : MonoBehaviour
{
    public GameObject[] gameObjects;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player") foreach (var obj in gameObjects) obj.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player") foreach (var obj in gameObjects) obj.SetActive(false);
    }
}
