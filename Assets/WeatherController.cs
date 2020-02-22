using UnityEngine;


public class WeatherController : MonoBehaviour
{
    [Range(0,360)] public float direction;
    [Range(0, 1)] public float speed;
    public int frameSkip;
    public WindZone windZone;

    float currentDirection;
    float currentSpeed;

    private void Reset()
    {
        direction = 0;
        speed = .1f;
        frameSkip = 100;

        UpdateWind();
    }

    private void UpdateWind()
    {
        currentSpeed = speed;
        currentDirection = direction;

        if (windZone != null) { 
            windZone.windMain = speed;
            windZone.mode = WindZoneMode.Directional;
            windZone.windPulseFrequency = 0f;
            windZone.windPulseMagnitude = 0f;
            windZone.windTurbulence = 0f;
        }
    }

    private void Update()
    {
        if (Time.frameCount % frameSkip == 0 && 
            (currentDirection != direction || currentSpeed != speed))
            UpdateWind();
    }
}
