using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityStandardAssets.Vehicles.Car;
using UnityStandardAssets.Vehicles.Aeroplane;

public class DiagnosticSlider : MonoBehaviour
{
    public TextMeshProUGUI valueLabel;
    public CarController carController;
    public AeroplaneController aeroplaneController;
    WheelCollider[] wheels;
    Slider slider;

    private void OnEnable()
    {
        slider = GetComponent<Slider>();
        
        if (carController != null) 
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

        if (aeroplaneController != null)
        {
            switch (name)
            {
                case "Max Engine Power": slider.value = aeroplaneController.m_MaxEnginePower; break;
                case "Lift": slider.value = aeroplaneController.m_Lift; break;
                case "Zero Lift Speed": slider.value = aeroplaneController.m_ZeroLiftSpeed; break;
                case "Roll Effect": slider.value = aeroplaneController.m_RollEffect; break;
                case "Pitch Effect": slider.value = aeroplaneController.m_PitchEffect; break;
                case "Yaw Effect": slider.value = aeroplaneController.m_YawEffect; break;
                case "Banked Turn Effect": slider.value = aeroplaneController.m_BankedTurnEffect; break;
                case "Aerodynamic Effect": slider.value = aeroplaneController.m_AerodynamicEffect; break;
                case "Auto Turn Pitch": slider.value = aeroplaneController.m_AutoTurnPitch; break;
                case "Auto Roll Level": slider.value = aeroplaneController.m_AutoRollLevel; break;
                case "Auto Pitch Level": slider.value = aeroplaneController.m_AutoPitchLevel; break;
                case "Air Brakes Effect": slider.value = aeroplaneController.m_AirBrakesEffect; break;
                case "Throttle Change Speed": slider.value = aeroplaneController.m_ThrottleChangeSpeed; break;
                case "Drag Increase Factor": slider.value = aeroplaneController.m_DragIncreaseFactor; break;
                default: break;
            }
        }

        if (carController != null)
        {
            switch (name)
            {
                case "Wheel Torque": slider.value = carController.m_WheelTorque; break;
                case "Brake Torque": slider.value = carController.m_BrakeTorque; break;
                case "Downforce": slider.value = carController.m_Downforce; break;
                case "Slip Limit": slider.value = carController.m_SlipLimit; break;
                case "Steerer Helper": slider.value = carController.m_SteerHelper; break;
                case "Traction Control": slider.value = carController.m_TractionControl; break;
                default: break;
            }
        }

        if (slider == null) slider = GetComponent<Slider>();

        valueLabel.text = "" + slider.value;
    }

    public void UpdateValue(float val)
    {
        valueLabel.text = "" + val;
        UpdateText();

        if (carController)
        {
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
                    case "Torque": carController.m_WheelTorque = val; break;
                    case "Downforce": carController.m_Downforce = val; break;
                    case "Slip Limit": carController.m_SlipLimit = val; break;
                    case "Reverse Torque": carController.m_BrakeTorque = val; break;
                    case "Brake Torque": carController.m_BrakeTorque = val; break;
                    case "Steerer Helper": carController.m_SteerHelper = val; break;
                    case "Traction Control": carController.m_TractionControl = val; break;
                    default: break;
                }
                wheels[i].forwardFriction = forwardFriction;
                wheels[i].sidewaysFriction = sidewaysFriction;
                wheels[i].suspensionSpring = suspensionSpring;
            }
        }

        if (aeroplaneController)
        {
            switch (name)
            {
                case "Max Engine Power": aeroplaneController.m_MaxEnginePower = val; break;
                case "Lift": aeroplaneController.m_Lift = val; break;
                case "Zero Lift Speed": aeroplaneController.m_ZeroLiftSpeed = val; break;
                case "Roll Effect": aeroplaneController.m_RollEffect = val; break;
                case "Pitch Effect": aeroplaneController.m_PitchEffect = val; break;
                case "Yaw Effect": aeroplaneController.m_YawEffect = val; break;
                case "Banked Turn Effect": aeroplaneController.m_BankedTurnEffect = val; break;
                case "Aerodynamic Effect": aeroplaneController.m_AerodynamicEffect = val; break;
                case "Auto Turn Pitch": aeroplaneController.m_AutoTurnPitch = val; break;
                case "Auto Roll Level": aeroplaneController.m_AutoRollLevel = val; break;
                case "Auto Pitch Level": aeroplaneController.m_AutoPitchLevel = val; break;
                case "Air Brakes Effect": aeroplaneController.m_AirBrakesEffect = val; break;
                case "Throttle Change Speed": aeroplaneController.m_ThrottleChangeSpeed = val; break;
                case "Drag Increase Factor": aeroplaneController.m_DragIncreaseFactor = val; break;
                default: break;
            }
        }
    }
}
