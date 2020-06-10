using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Aeroplane;

public class ThrottleScript : MonoBehaviour
{
    public AeroplaneUserControl4Axis aeroplaneController;
    Slider slider;

    void Start()
    {
        slider = GetComponent<Slider>();
    }

    void Update()
    {
        if (Time.frameCount % 1 == 0)
        {
            slider.value = aeroplaneController.m_Aeroplane.Throttle;
        }
    }
}
