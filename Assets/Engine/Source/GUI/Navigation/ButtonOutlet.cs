using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

public class ButtonOutlet : MonoBehaviour
{
    public GameObject outlet;

    void Start()
    {
        transform.GetComponent<Button>()?.onClick.AddListener(Action);
    }

    void Action()
    {
        NavigationStack.Instance.PushView(outlet);
    }
}
