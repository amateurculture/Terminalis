using TMPro;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

public class SpeedometerScript : MonoBehaviour 
{
    public float angle = 0;
    private float percentage = 100;
    private float startingAngle = -120;
    private float maximumDegrees = 240;
    private float capValue = 240;
    public BlackBox objectVelocity;
    public TextMeshProUGUI digitalText;
    public RectTransform needleRect;
    public TextMeshProUGUI gearText;
    CarController carController;

    private void Start() 
    {
        maximumDegrees *= 0.01f;
        carController = objectVelocity.GetComponent<CarController>();
    }

    private void Update()
    {
        if (Time.frameCount % 3 == 0)
        {
            angle = startingAngle + (((objectVelocity.oldVelocity / capValue) * percentage) * maximumDegrees);
            digitalText.text = "" + Mathf.Floor(objectVelocity.oldVelocity);
            needleRect.transform.eulerAngles = new Vector3(0, 0, -angle);

            if (gearText != null)
            {
                switch (carController.gearboxSetting)
                {
                    case 0: gearText.text = "P"; break;
                    case 1: gearText.text = "R"; break;
                    case 2: gearText.text = "N"; break;
                    default: gearText.text = "D"; break;
                }
            }
        }
    }
}
