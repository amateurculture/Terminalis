using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityStandardAssets.Vehicles.Car;

public class CarDiagnosticSliders : MonoBehaviour
{
    public CarController carController;
    public TextMeshProUGUI valueLabel;  
    WheelCollider[] wheels;
    Slider slider;

    private void OnEnable()
    {
        slider = GetComponent<Slider>();
        wheels = carController.transform.GetComponentsInChildren<WheelCollider>();
        UpdateText();
    }

    void UpdateText()
    {
        if (slider == null) return;

        if (wheels != null && wheels.Length >= 1)
        {
            switch (name)
            {
                case "Wheel Damping Rate": slider.value = wheels[0].wheelDampingRate; break;
                case "Suspension Distance": slider.value = wheels[0].suspensionDistance; break;
                case "Force App Point Distance": slider.value = wheels[0].forceAppPointDistance; break;
                case "Spring": slider.value = wheels[0].suspensionSpring.spring; break;
                case "Damper": slider.value = wheels[0].suspensionSpring.damper; break;
                case "Target Position": slider.value = wheels[0].suspensionSpring.targetPosition; break;
                case "Forward Extremum Slip": slider.value = wheels[0].forwardFriction.extremumSlip; break;
                case "Forward Extremum Value": slider.value = wheels[0].forwardFriction.extremumValue; break;
                case "Forward Asymptote Slip": slider.value = wheels[0].forwardFriction.asymptoteSlip; break;
                case "Forward Asymptote Value": slider.value = wheels[0].forwardFriction.asymptoteValue; break;
                case "Forward Stiffness": slider.value = wheels[0].forwardFriction.stiffness; break;
                case "Sideways Extremum Slip": slider.value = wheels[0].sidewaysFriction.extremumSlip; break;
                case "Sideways Extremum Value": slider.value = wheels[0].sidewaysFriction.extremumValue; break;
                case "Sideways Asymptote Slip": slider.value = wheels[0].sidewaysFriction.asymptoteSlip; break;
                case "Sideways Asymptote Value": slider.value = wheels[0].sidewaysFriction.asymptoteValue; break;
                case "Sideways Stiffness": slider.value = wheels[0].sidewaysFriction.stiffness; break;
                default: break;
            }
        }

        if (carController != null)
        {
            switch (name)
            {
                case "Torque": slider.value = carController.m_FullTorqueOverAllWheels; break;
                case "Downforce": slider.value = carController.m_Downforce; break;
                case "Slip Limit": slider.value = carController.m_SlipLimit; break;
                default: break;
            }
        }

        if (slider == null) 
            slider = GetComponent<Slider>();

        valueLabel.text = "" + slider.value;
    }

    public void UpdateValue(float val)
    {
        valueLabel.text = "" + val;
        UpdateText();

        for (var i = 0; i < wheels.Length; i++)
        {
            var forwardFriction = wheels[i].forwardFriction;
            var sidewaysFriction = wheels[i].sidewaysFriction;
            var suspensionSpring = wheels[i].suspensionSpring;

            switch (name)
            {
                case "Wheel Damping Rate": wheels[i].wheelDampingRate = val; break;
                case "Suspension Distance": wheels[i].suspensionDistance = val; break;
                case "Force App Point Distance": wheels[i].forceAppPointDistance = val; break;
                case "Spring": suspensionSpring.spring = val; break;
                case "Damper": suspensionSpring.damper = val; break;
                case "Target Position": suspensionSpring.targetPosition = val; break;
                case "Forward Extremum Slip": forwardFriction.extremumSlip = val; break;
                case "Forward Extremum Value": forwardFriction.extremumValue = val; break;
                case "Forward Asymptote Slip": forwardFriction.asymptoteSlip = val; break;
                case "Forward Asymptote Value": forwardFriction.asymptoteValue = val; break;
                case "Forward Stiffness": forwardFriction.stiffness = val; break;
                case "Sideways Extremum Slip": sidewaysFriction.extremumSlip = val; break;
                case "Sideways Extremum Value": sidewaysFriction.extremumValue = val; break;
                case "Sideways Asymptote Slip": sidewaysFriction.asymptoteSlip = val; break;
                case "Sideways Asymptote Value": sidewaysFriction.asymptoteValue = val; break;
                case "Sideways Stiffness": sidewaysFriction.stiffness = val; break;
                case "Torque": carController.m_FullTorqueOverAllWheels = val; break;
                case "Downforce": carController.m_Downforce = val; break;
                case "Slip Limit": carController.m_SlipLimit = val; break;
                default: break;
            }
            wheels[i].forwardFriction = forwardFriction;
            wheels[i].sidewaysFriction = sidewaysFriction;
            wheels[i].suspensionSpring = suspensionSpring;
        }
    }
}
