using UnityEngine;

public class TimeController : MonoBehaviour
{
    [Header("Game Time")]
    public Light sun;
    public float day;
    public float hour;
    public float minute;

    [Header("Time Adjustment")]
    float secondsInHour;
    int frameRate;
    
    float gameTime;
    float adjustedSecondsInHour;
    float secondsRemainingInMinute;
    bool EndOfDay;
    bool nextDay;

    private void Reset()
    {
        frameRate = 60;
        secondsInHour = 60f;
        
        var possibleLights = FindObjectsOfType<Light>();
        foreach (var light in possibleLights)
        {
            if (light.type == LightType.Directional)
            {
                sun = light;
                break;
            }
        }
    }

    private void Start()
    {
        if (sun != null)
        {
            secondsInHour = 60;

            sun.transform.eulerAngles = Vector3.zero;
            var angles = sun.transform.eulerAngles;
            angles.y = 45;
            angles.x = 15f * ((hour + (minute / 60f)) - 6f);
            sun.transform.Rotate(angles);

            gameTime = hour + (minute / 60f);
            secondsRemainingInMinute = Time.time + (secondsInHour / 60);
            adjustedSecondsInHour = secondsInHour / 60;
        }            
        Application.targetFrameRate = frameRate;
    }

    void Update()
    {
        if (sun == null) return;

        if (Time.time > secondsRemainingInMinute)
        {
            secondsRemainingInMinute = Time.time + adjustedSecondsInHour;
            UpdateTime();
        }
    }

    void UpdateTime()
    {
        sun.transform.Rotate(.25f, 0, 0);

        gameTime += .0166f;
        hour = ((int)gameTime) % 24;
        minute = ((int)(60f * (gameTime - ((int)gameTime))));

        if (sun.transform.eulerAngles.x > 270 && sun.transform.eulerAngles.x < 280)
        {
            EndOfDay = true;
        }
        else
        {
            nextDay = false;
            EndOfDay = false;
        }

        if (EndOfDay && !nextDay)
        {
            nextDay = true;
            day += 1;
        }
    }
}
