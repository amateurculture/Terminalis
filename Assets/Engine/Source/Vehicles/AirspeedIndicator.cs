using TMPro;
using UnityEngine;

public class AirspeedIndicator : MonoBehaviour
{
    public float angle = 0;
    public TextMeshProUGUI digitalText;
    public RectTransform needleRect;
    public BlackBox blackBox;
    private float angleConversion = 360f / 200f;

    private void Update()
    {
        if (Time.frameCount % 3 == 0)
        {
            var newVelocity = blackBox.oldVelocity * .87f;
            angle = newVelocity * angleConversion;
            digitalText.text = "" + Mathf.Floor(newVelocity);
            needleRect.transform.eulerAngles = new Vector3(0, 0, -angle);
        }
    }
}