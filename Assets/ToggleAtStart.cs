using UnityEngine;

public class ToggleAtStart : MonoBehaviour
{
    public GameObject target;

    void Start()
    {
        target.SetActive(true);
    }
}
