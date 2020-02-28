using UnityEngine;

[RequireComponent(typeof(FogController))]
[RequireComponent(typeof(WindController))]

public class WeatherController : MonoBehaviour
{
    FogController fogController;
    WindController windController;
    public CloudController clouds;
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
        fogController.fogEndDistance = Random.value * 1000f;
        windController.speed = Random.value;
        windController.direction = (int)(Random.value * 360);

        if (clouds != null)
        {
            clouds.density = Random.value;
            clouds.cloudBreaks = Random.value;
        }
    }

    void Update()
    {
        if (Time.frameCount % updateWeatherOnFrame == 0)
        {
            UpdateWeather();
        }
    }
}
