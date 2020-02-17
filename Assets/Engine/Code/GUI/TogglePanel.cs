using UnityEngine;

public class TogglePanel : MonoBehaviour
{
    public void togglePanel()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
