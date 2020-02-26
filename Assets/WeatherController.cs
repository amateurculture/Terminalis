using UnityEngine;

[RequireComponent(typeof(FogController))]
[RequireComponent(typeof(WindController))]

public class WeatherController : MonoBehaviour
{
    FogController fogController;
    WindController windController;
    public float frame;

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
    }

    void Update()
    {
        frame = Time.frameCount % 1000;
        if (frame == 0)
        {
            UpdateWeather();
        }
    }
}
