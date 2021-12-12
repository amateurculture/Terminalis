using UnityEngine;
using UnityEngine.UI;

public class ButtonOutlet : Outlet
{
    public GameObject outlet;

    void Start()
    {
        transform.GetComponent<Button>()?.onClick.AddListener(Action);
    }

    public override void Action()
    {
        NavigationStack.Instance.PushView(outlet);
    }
}
