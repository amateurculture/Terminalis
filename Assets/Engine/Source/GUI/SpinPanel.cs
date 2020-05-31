using UnityEngine;

public class SpinPanel : MonoBehaviour
{
    RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        Vector3 rotation = rectTransform.eulerAngles;
        rotation.z -= Time.fixedDeltaTime * 5f;
        transform.eulerAngles = rotation;
    }
}
