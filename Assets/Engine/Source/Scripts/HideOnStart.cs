using UnityEngine;

public class HideOnStart : MonoBehaviour
{
    void Start()
    {
        gameObject.SetActive(false);
    }
}
