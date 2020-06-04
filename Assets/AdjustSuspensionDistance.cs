using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AdjustSuspensionDistance : MonoBehaviour
{
    public TextMeshProUGUI valueLabel;
    public WheelCollider[] wheels;
    Slider slider;

    private void Start()
    {
        slider = GetComponent<Slider>();
        slider.value = wheels[0].suspensionDistance;
    }

    public void UpdateValue(float val)
    {
        valueLabel.text = "" + val;

        for (var i = 0; i < wheels.Length; i++)
        {
            wheels[i].suspensionDistance = val;
        }
    }
}
