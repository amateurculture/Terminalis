using UnityEngine;

/// <summary>
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

public class TriggerOutlet : MonoBehaviour
{
    public string moduleName;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            NavigationStack.Instance.PushView(NavigationStack.Instance.transform.Find(moduleName).gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            NavigationStack.Instance.CloseMenu();
        }
    }
}
