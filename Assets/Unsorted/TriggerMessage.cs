using UnityEngine;
using TMPro;

/// <summary>
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

public class TriggerMessage : MonoBehaviour
{
    public string message;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            NavigationStack.Instance.eventMessage.GetComponentInChildren<TextMeshProUGUI>().text = message;
            NavigationStack.Instance.PushView(NavigationStack.Instance.transform.Find("Event").gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player") NavigationStack.Instance.CloseMenu();
    }
}
