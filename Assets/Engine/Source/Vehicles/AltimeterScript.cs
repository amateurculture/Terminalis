using UnityEngine;
using TMPro;

public class AltimeterScript : MonoBehaviour
{
    public float angle = 0;
    public BlackBox aircraftData;
    public TextMeshProUGUI digitalText;
    public RectTransform bigNeedle;
    public RectTransform smallNeedle;

    private void Update()
    {
        if (Time.frameCount % 3 == 0)
        {
            angle = (aircraftData.oldAltitude / 100f) * 36;
            digitalText.text = "" + Mathf.Floor(aircraftData.oldAltitude);
            smallNeedle.transform.eulerAngles = new Vector3(0, 0, -angle);
            bigNeedle.transform.eulerAngles = new Vector3(0, 0, -(angle / 10));
        }
    }
}
