using UnityEngine;

public class TimeController : MonoBehaviour
{
    public Light sun;
    public float day;
    public float hour;
    public float minute;
    public Gradient sunLight;
    public Gradient ambientLight;
    public Gradient fogColor;
    public float secondsInHour;
    public int frameRate;
    bool didChangeReflectionProbeSetting;

    [Header("Global Reflection Probe")]
    public ReflectionProbe reflectionProbe;
    public int frameSkip;
    public bool enableReflections;

    [Header("Experimental")]
    Light testLight;

    GameObject player;
    bool EndOfDay;
    bool nextDay;
    bool isLerpingBack;
    bool isLerpingUp;
    bool inRange;
    float lightLerpTime;
    float adjustedSecondsInHour; 
    float actualHour;
    float secondsRemainingInMinute;
    float prev;
    float t;

    private void Reset()
    {
        didChangeReflectionProbeSetting = false;
        enableReflections = false;
        isLerpingBack = false;
        isLerpingUp = false;
        inRange = false;
        secondsRemainingInMinute = 0;
        adjustedSecondsInHour = 0;
        secondsInHour = 60f;
        lightLerpTime = 0;
        frameRate = 60;
        frameSkip = 50;
        actualHour = 0;
        minute = 0;
        hour = 6;
        prev = 0;
        t = 0;

        // todo add default gradient settings
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (sun != null)
        {
            sun.transform.eulerAngles = Vector3.zero;
            var angles = sun.transform.eulerAngles;
            angles.y = 45;
            angles.x = 15f * ((hour + (minute / 60f)) - 6f);
            sun.transform.Rotate(angles);

            actualHour = hour + (minute / 60f);
            secondsRemainingInMinute = Time.time + (secondsInHour / 60);
            adjustedSecondsInHour = secondsInHour / 60;

            RenderSettings.fogColor = fogColor.Evaluate(sun.transform.eulerAngles.x / 360);
            RenderSettings.ambientLight = ambientLight.Evaluate(sun.transform.eulerAngles.x / 360);
            sun.intensity = sunLight.Evaluate(sun.transform.eulerAngles.x / 360).grayscale;
        }

        didChangeReflectionProbeSetting = enableReflections;

        if (reflectionProbe != null)
        {
            if (enableReflections)
            {
                reflectionProbe.enabled = true;
                reflectionProbe.RenderProbe();
            }
            else
                reflectionProbe.enabled = false;
        }
        Application.targetFrameRate = frameRate;
    }

    void LateUpdate()
    {
        if (sun == null) return;

        if (Time.frameCount % frameSkip == 0 && reflectionProbe != null)
        {
            if (didChangeReflectionProbeSetting != enableReflections)
            {
                didChangeReflectionProbeSetting = enableReflections;
                reflectionProbe.enabled = (enableReflections) ? true : false;
            }

            if (enableReflections)
            {
                reflectionProbe.backgroundColor = RenderSettings.fogColor;
                reflectionProbe.RenderProbe();
            }
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
            float colorIndex = sun.transform.eulerAngles.x / 360f;
            RenderSettings.fogColor = fogColor.Evaluate(colorIndex);
            sun.intensity = sunLight.Evaluate(colorIndex).grayscale;
            secondsRemainingInMinute = Time.time + adjustedSecondsInHour;
            sun.transform.Rotate(.25f, 0, 0);
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
            else if (testLight == null || distance >= testLight.range)
            {
                RenderSettings.ambientLight = ambientLight.Evaluate(colorIndex);
            }

            actualHour += .0166f;
            hour = ((int)actualHour) % 24;
            minute = ((int)(60f * (actualHour - ((int)actualHour))));

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
}

//public float second = 0;
//second = (int)((3600 / secondsInHour) * Time.time) % 60;