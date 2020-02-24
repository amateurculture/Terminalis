using UnityEngine;

[RequireComponent(typeof(WindZone))]

public class WindController : MonoBehaviour
{
    [Range(0,360)] public float direction;
    [Range(0, 1)] public float speed;
    WindZone windZone;

    float currentDirection;
    float currentSpeed;
    int frameSkip;

    private void Reset()
    {
        speed = .5f;
        frameSkip = 60;
        windZone = GetComponent<WindZone>();

        UpdateWind();
    }

    private void Start()
    {
        speed = .5f;
        frameSkip = 60;
    }

    private void UpdateWind()
    {
        if (currentDirection != direction || currentSpeed != speed)
        {
            currentSpeed = speed;
            currentDirection = direction;

            if (windZone != null)
            {
                windZone.windMain = speed;
                windZone.mode = WindZoneMode.Directional;
                windZone.windPulseFrequency = 0f;
                windZone.windPulseMagnitude = 0f;
                windZone.windTurbulence = 0f;
            }
        }
    }

    private void Update()
    {
        if (Time.frameCount % frameSkip == 0)
        {
            UpdateWind();
        }
    }
}
