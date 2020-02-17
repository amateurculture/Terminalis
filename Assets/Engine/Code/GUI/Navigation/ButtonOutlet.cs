using UnityEngine;
using UnityEngine.UI;

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
