using UnityEngine;
using UnityEngine.UI;

public class MenuExit : MonoBehaviour
{
    void Start()
    {
        transform.GetComponent<Button>()?.onClick.AddListener(Action);
    }

    void Action()
    {
        Debug.Break();
        Application.Quit();
    }
}
