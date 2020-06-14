using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdjustForceAppPointDistance : MonoBehaviour
{
    public TextMeshProUGUI valueLabel;
    public WheelCollider[] wheels;
    Slider slider;

    private void Start()
    {
        slider = GetComponent<Slider>();
        slider.value = wheels[0].forceAppPointDistance;
    }

    public void UpdateValue(float val)
    {
        valueLabel.text = "" + val;

        for (var i = 0; i < wheels.Length; i++)
        {
            wheels[i].forceAppPointDistance = val;
        }
    }
}
