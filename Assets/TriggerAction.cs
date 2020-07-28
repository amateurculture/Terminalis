using UnityEngine;

public class TriggerAction : MonoBehaviour
{
    public GameObject obj;
    bool isOn;
    bool isSelectable;

    private void Start()
    {
        obj.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        isSelectable = true;
    }

    private void OnTriggerExit(Collider other)
    {
        isSelectable = false;
    }

    private void Update()
    {
        var button = Input.GetButtonDown("Jump");
        Debug.Log("Action button state = " + button);

        if (isSelectable && button)
        {
            isOn = !isOn;
            if (isOn) obj.SetActive(true); else obj.SetActive(false);
        }
    }
}
