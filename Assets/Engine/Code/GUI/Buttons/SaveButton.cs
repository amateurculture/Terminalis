using UnityEngine;
using UnityEngine.UI;

public class SaveButton : MonoBehaviour
{
    void Start()
    {
        transform.GetComponent<Button>()?.onClick.AddListener(Action);
    }
    
    void Action()
    {
        Serializer.Save();
        NavigationStack.Instance.CloseMenu();
    }
}
