using UnityEngine;
using UnityEngine.UI;

public class MenuClose : Outlet
{
    void Start()
    {
        transform.GetComponent<Button>()?.onClick.AddListener(Action);
    }

    public override void Action()
    {
        NavigationStack.Instance.CloseMenu();
    }
}
