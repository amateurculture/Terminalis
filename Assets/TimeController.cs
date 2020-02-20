using UnityEngine;

public class TimeController : MonoBehaviour
{
    public float day;
    public float hour = 6;
    public float minute = 0;
    public Gradient sunLight;
    public Gradient ambientLight;
    public Gradient fogColor;
    public float secondsInHour = 60f;
    public int frameRate = 60;
    //public float second = 0;

    [Header("Global Reflection Probe")]
    public ReflectionProbe reflectionProbe;
    public int frameSkip = 50;

    float actualHour = 0;
    bool EndOfDay;
    bool nextDay;
    float secondsRemainingInMinute = 0;
    Light sun;
    GameObject player;

    bool isLerpingBack = false;
    bool isLerpingUp = false;
    float lightLerpTime = 0;
    float prev = 0;
    bool inRange = false;
    float t = 0;
    float adjustedSecondsInHour = 0;

    [Header("Experimental")]
    public Light testLight;
    public int lightIndex = 0;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        sun = GetComponent<Light>();

        transform.eulerAngles = Vector3.zero;
        var angles = transform.eulerAngles;
        angles.y = 45;
        angles.x = 15f * ((hour + (minute / 60f))-6f);
        transform.Rotate(angles);

        actualHour = hour + (minute / 60f);
        secondsRemainingInMinute = Time.time + (secondsInHour / 60);
        adjustedSecondsInHour = secondsInHour / 60;

        RenderSettings.fogColor = fogColor.Evaluate(transform.eulerAngles.x / 360);
        RenderSettings.ambientLight = ambientLight.Evaluate(transform.eulerAngles.x / 360);
        sun.intensity = sunLight.Evaluate(transform.eulerAngles.x / 360).grayscale;

        reflectionProbe.RenderProbe();

        Application.targetFrameRate = frameRate;
    }

    void LateUpdate()
    {
        //second = (int)((3600 / secondsInHour) * Time.time) % 60;
        if (Time.frameCount % frameSkip == 0)
        {
            reflectionProbe.RenderProbe();
        }
        else if (isLerpingUp && Time.time > lightLerpTime)
        {
            var luminosity = Mathf.Lerp(prev, .25f, t);
            RenderSettings.ambientLight = new Color(luminosity, luminosity, luminosity);

            t += Time.deltaTime;

            if (RenderSettings.ambientLight.r >= .24f)
            {
                RenderSettings.ambientLight = new Color(.25f, .25f, .25f);
                isLerpingUp = false;
                inRange = true;
            }
        }
        else if (isLerpingBack && Time.time > lightLerpTime)
        {
            var luminosity = Mathf.Lerp(.25f, prev, t);
            RenderSettings.ambientLight = new Color(luminosity, luminosity, luminosity);

            t += Time.deltaTime;

            if (RenderSettings.ambientLight.r <= prev + .01f)
            {
                RenderSettings.ambientLight = new Color(prev, prev, prev);
                isLerpingBack = false;
                inRange = false;
            }
        }
        else if (!isLerpingBack && !isLerpingUp && Time.time > secondsRemainingInMinute)
        {
            float colorIndex = transform.eulerAngles.x / 360f;

            RenderSettings.fogColor = fogColor.Evaluate(colorIndex);
            sun.intensity = sunLight.Evaluate(colorIndex).grayscale;
            secondsRemainingInMinute = Time.time + adjustedSecondsInHour;
            transform.Rotate(.25f, 0, 0);

            var distance = (testLight == null) ? Mathf.Infinity : Vector3.Distance(player.transform.position, testLight.transform.position);

            if (testLight != null)
            {
                if (!inRange && distance < testLight.range)
                {
                    isLerpingUp = true;
                    isLerpingBack = false;
                    prev = RenderSettings.ambientLight.r;
                    t = 0;
                }
                else if (inRange && distance >= testLight.range && !isLerpingBack)
                {
                    isLerpingBack = true;
                    isLerpingUp = false;
                    t = 0;
                }
            }
            else if (!testLight || distance >= testLight.range)
            {
                RenderSettings.ambientLight = ambientLight.Evaluate(colorIndex);
            }

            actualHour += .0166f;
            hour = ((int)actualHour) % 24;
            minute = ((int)(60f * (actualHour - ((int)actualHour))));

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
}
