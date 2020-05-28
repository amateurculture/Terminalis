using TMPro;
using UnityEngine;

public class SpeedometerScript : MonoBehaviour 
{
    public float angle = 0;
    private float percentage = 100;
    private float startingAngle = -120;
    private float maximumDegrees = 240;
    private float capValue = 240;
    public ObjectVelocity objectVelocity;
    public TextMeshProUGUI digitalText;
    public RectTransform needleRect;

    private void Start() 
    {
        maximumDegrees *= 0.01f;
    }

    private void Update() 
    {
        angle = startingAngle + (((objectVelocity.oldVelocity / capValue) * percentage) * maximumDegrees);
        digitalText.text = "" + Mathf.Floor(objectVelocity.oldVelocity);
        needleRect.transform.eulerAngles = new Vector3(0, 0, -angle);
    }
}
