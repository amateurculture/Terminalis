using UnityEngine;
using UnityEngine.UI;
 
public class ExitCursorLockOnPress : MonoBehaviour
{
    public GameObject cameraRig;

    void Start()
    {
        transform.GetComponent<Button>()?.onClick.AddListener(Action);
    }

    void Action()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        //cam.enabled = true;
    }
}
