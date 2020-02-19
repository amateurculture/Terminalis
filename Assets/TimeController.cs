using UnityEngine;

public class TimeController : MonoBehaviour
{
    public float DayCounter;
    public float startHour = 6;
    public float secondsInHour = 10f;
    public float time;
    public Gradient sunLight;
    public Gradient ambientLight;
    public Gradient fogColor;
    public int frameSkip = 50;
    public ReflectionProbe reflectionProbe;

    bool isDayTime = true; 
    bool isNightTime; 
    bool EndOfDay;
    bool nextDay;
    float secondsRemainingInMinute = 0;
    Light sun;
    
    private void Start()
    {
        sun = GetComponent<Light>();

        transform.eulerAngles = Vector3.zero;
        var angles = transform.eulerAngles;
        angles.y = 45;
        angles.x = 15 * (startHour-6);
        transform.Rotate(angles);
        time = Time.time;
    }

    void Update()
    {
        if (Time.frameCount % 200 == 0)
        {
            reflectionProbe.RenderProbe();
        }
        else if (Time.time > secondsRemainingInMinute)
        {
            secondsRemainingInMinute = Time.time + (secondsInHour / 60);

            if (transform.eulerAngles.x > 0 && transform.eulerAngles.x < 180)
            {
                isNightTime = false;
                isDayTime = true;
            }

            else if (transform.eulerAngles.x > 180 && transform.eulerAngles.x < 360)
            {
                isDayTime = false;
                isNightTime = true;
            }

            if (isDayTime)
                transform.Rotate(.25f, 0, 0);

            if (isNightTime)
                transform.Rotate(.25f, 0, 0);

            RenderSettings.fogColor = fogColor.Evaluate(transform.eulerAngles.x / 360);
            RenderSettings.ambientLight = ambientLight.Evaluate(transform.eulerAngles.x / 360);
            sun.intensity = sunLight.Evaluate(transform.eulerAngles.x / 360).grayscale;


            IncreaseNumberOfDaysSurvived();
        }
    }

    void IncreaseNumberOfDaysSurvived()
    {
        if (transform.eulerAngles.x > 270 && transform.eulerAngles.x < 280)
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
            DayCounter += 1;
        }
    }
}
