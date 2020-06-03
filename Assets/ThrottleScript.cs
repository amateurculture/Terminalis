using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Aeroplane;

public class ThrottleScript : MonoBehaviour
{
    public AeroplaneController airController;
    Slider slider;

    void Start()
    {
        slider = GetComponent<Slider>();
    }

    void Update()
    {
        if (Time.frameCount % 3 == 0)
        {
            slider.value = airController.Throttle;
        }
    }
}
