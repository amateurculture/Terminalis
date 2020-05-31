using UnityEngine;
using UnityEngine.UI;

public class MenuClose : MonoBehaviour
{
    void Start()
    {
        transform.GetComponent<Button>()?.onClick.AddListener(Action);
    }

    void Action()
    {
        NavigationStack.Instance.CloseMenu();
    }
}
