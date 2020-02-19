using UnityEngine;

public class TimeController : MonoBehaviour
{
    public float day;
    public float hour = 6;
    public float minute = 0;
    //public float second = 0;
    public Gradient sunLight;
    public Gradient ambientLight;
    public Gradient fogColor;
    public float secondsInHour = 60f;

    [Header("Global Reflection Probe")]
    public ReflectionProbe reflectionProbe;
    public int frameSkip = 50;

    float actualHour = 0;
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
        angles.x = 15f * ((hour + (minute / 60f))-6f);
        transform.Rotate(angles);

        RenderSettings.fogColor = fogColor.Evaluate(transform.eulerAngles.x / 360);
        RenderSettings.ambientLight = ambientLight.Evaluate(transform.eulerAngles.x / 360);
        sun.intensity = sunLight.Evaluate(transform.eulerAngles.x / 360).grayscale;

        reflectionProbe.RenderProbe();
            
        actualHour = hour + (minute / 60f);
        secondsRemainingInMinute = Time.time + (secondsInHour / 60);
    }

    void Update()
    {
        //second = (int) ((3600 / secondsInHour) * Time.time) % 60;
        
        if (Time.frameCount % frameSkip == 0)
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

            actualHour += 1f/60f;
            hour = ((int)actualHour) % 24;
            minute = ((int)(60f * (actualHour - ((int)actualHour))));
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
            day += 1;
        }
    }
}
