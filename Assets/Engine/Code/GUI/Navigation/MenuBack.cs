using UnityEngine;
using UnityEngine.UI;

public class MenuBack : MonoBehaviour
{
    void Start()
    {
        transform.GetComponent<Button>()?.onClick.AddListener(Action);
    }

    void Action()
    {
        NavigationStack.Instance.PopView();
    }
}
