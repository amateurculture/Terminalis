using UnityEngine;
using UnityEngine.UI;

public class MenuExit : Outlet
{
    void Start()
    {
        transform.GetComponent<Button>()?.onClick.AddListener(Action);
    }

    public override void Action()
    {
        Debug.Break();
        Application.Quit();
    }
}
