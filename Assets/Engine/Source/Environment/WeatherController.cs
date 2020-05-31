using UnityEngine;

[RequireComponent(typeof(FogController))]
[RequireComponent(typeof(WindController))]
[RequireComponent(typeof(CloudController))]
[RequireComponent(typeof(TimeController))]
[RequireComponent(typeof(WaterController))]
[RequireComponent(typeof(LightingController))]

public class WeatherController : MonoBehaviour
{
    FogController fogController;
    WindController windController;
    //public CloudController clouds;
    public int updateWeatherOnFrame;

    private void Reset()
    {
        updateWeatherOnFrame = 240;
    }

    private void Start()
    {
        fogController = GetComponent<FogController>();
        windController = GetComponent<WindController>();
        UpdateWeather();
    }

    void UpdateWeather()
    {
        /*
        fogController.fogEndDistance = fogController.fogStartDistance + (Random.value * (5000f - fogController.fogStartDistance));
        */

        windController.speed = Random.value * .7f;
        windController.direction = (int)(Random.value * 360);

        //if (clouds != null)
        //{
            // todo clouds should lerp like everything else...
            //clouds.density = Random.value;
            //clouds.cloudBreaks = Random.value;
        //}
    }

    void Update()
    {
        if (Time.frameCount % updateWeatherOnFrame == 0)
        {
            UpdateWeather();
        }
    }
}
