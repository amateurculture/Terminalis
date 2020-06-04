using UnityEngine;

public class TimeControllerExperimental : MonoBehaviour
{
    public Light sun;
    public float day;
    public float hour;
    public float minute;
    public Gradient sunLight;
    public Gradient ambientLight;
    public Gradient fogColor;
    public float transitionSpeed;

    public float secondsInHour;
    public int frameRate;
    [Range(0, 1024)]
    public float startFogDistance;
    float previousStartFog;
    [Range(0, 1024)]
    public float endFogDistance;
    float previousEndFog;

    [Header("Global Reflection Probe")]
    public ReflectionProbe reflectionProbe;
    public int frameSkip;
    public bool enableReflections;

    bool didChangeReflectionProbeSetting;
    bool EndOfDay;
    bool nextDay;
    bool isLerpingBack;
    bool isLerpingUp;
    bool isLerping;
    //float lightLerpTime;
    float adjustedSecondsInHour; 
    float actualHour;
    float secondsRemainingInMinute;
    float prev;
    float t;
    float t1;

    /*
    [Header("Experimental")]
    Light testLight;

    float previousMinute;
    float previousHour;
    float previousDay;
    bool inRange;

    GameObject player;
    */

    enum WhatIsLerping
    {
        startFog,
        endFog
    }
    WhatIsLerping whatIsLerping;

    private void Reset()
    {
        didChangeReflectionProbeSetting = false;
        enableReflections = false;
        isLerpingBack = false;
        isLerpingUp = false;
        isLerping = false;
        //inRange = false;
        frameRate = 60;
        frameSkip = 100;
        secondsInHour = 60f;
        startFogDistance = 0;
        endFogDistance = 256;
        transitionSpeed = .001f;
        
        //previousMinute = minute;
        //previousHour = hour;
        //previousDay = day;
        previousStartFog = startFogDistance;
        previousEndFog = endFogDistance;

        GradientAlphaKey[] alphaKeys = {
            new GradientAlphaKey(1, 0),
            new GradientAlphaKey(1, 1)
        };

        GradientColorKey[] keys = { 
            new GradientColorKey(Color.black, 0),
            new GradientColorKey(Color.white, .08f), 
            new GradientColorKey(Color.white, .46f),
            new GradientColorKey(Color.black, .54f),
            new GradientColorKey(Color.black, 1)
        };
        sunLight = new Gradient();
        sunLight.SetKeys(keys, alphaKeys);

        GradientColorKey[] keys1 = {
            new GradientColorKey(new Color32(25,25,25,1), 0),
            new GradientColorKey(Color.gray, .08f), 
            new GradientColorKey(Color.gray, .5f),
            new GradientColorKey(new Color32(25,25,25,1), .58f),
            new GradientColorKey(new Color32(25,25,25,1), 1)
        };
        ambientLight = new Gradient();
        ambientLight.SetKeys(keys1, alphaKeys);

        GradientColorKey[] keys2 = {
            new GradientColorKey(new Color32(15, 15, 15, 1), 0),
            new GradientColorKey(new Color32(60, 134, 135, 1), .04f),
            new GradientColorKey(new Color32(83, 165, 255, 1), .08f),
            new GradientColorKey(new Color32(83, 165, 255, 1), .5f),
            new GradientColorKey(new Color32(149, 77, 79, 1), .54f),
            new GradientColorKey(new Color32(15, 15, 15, 1), .58f),
            new GradientColorKey(new Color32(15, 15, 15, 1), 1)
        };
        fogColor = new Gradient();
        fogColor.SetKeys(keys2, alphaKeys);

        var possibleLights = FindObjectsOfType<Light>();
        foreach (var light in possibleLights)
        {
            if (light.type == LightType.Directional)
            {
                sun = light;
                break;
            }
        }
        
        // todo add default gradient settings
    }

    private void Start()
    {
        //player = GameObject.FindGameObjectWithTag("Player");

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

            RenderSettings.fogStartDistance = startFogDistance;
            RenderSettings.fogEndDistance = endFogDistance;
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

        isLerping = false;
        previousStartFog = startFogDistance;
        previousEndFog = endFogDistance;
    }

    void LateUpdate()
    {
        if (sun == null) return;

        if (isLerping)
        {
            t1 += transitionSpeed * Time.deltaTime;

            switch (whatIsLerping) {
                case WhatIsLerping.startFog:
                    previousStartFog = Mathf.Lerp(previousStartFog, startFogDistance, t1);
                    RenderSettings.fogStartDistance = previousStartFog;
                    break;
                case WhatIsLerping.endFog:
                    previousEndFog = Mathf.Lerp(previousEndFog, endFogDistance, t1); 
                    RenderSettings.fogEndDistance = previousEndFog;
                    break;
                default:
                    break;
            }
            if (t1 >= 1) isLerping = false;
        }
        else if (Time.frameCount % frameSkip == 0)
        {
            if (reflectionProbe != null)
            {
                // todo add a check to see if the player changed x,y coordinates, because if
                // not, there isn't any point to rendering the reflection probe again.
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

            if (previousStartFog != startFogDistance)
            {
                t1 = 0;
                isLerping = true;
                whatIsLerping = WhatIsLerping.startFog;
            }
            else if (previousEndFog != endFogDistance)
            {
                t1 = 0;
                isLerping = true;
                whatIsLerping = WhatIsLerping.endFog;
            }

            /* todo these values should all lerp to prevent jumping
            previousMinute = minute;
            previousHour = hour;
            previousDay = day;
            previousStartFog = startFogDistance;
            previousEndFog = endFogDistance;
            */
        }
        /*
        else if (isLerpingUp && Time.time > lightLerpTime)
        {
            var luminosity = Mathf.Lerp(prev, .25f, t);
            RenderSettings.ambientLight = new Color(luminosity, luminosity, luminosity);
            t += Time.deltaTime;

            if (RenderSettings.ambientLight.r >= .24f)
            {
                RenderSettings.ambientLight = new Color(.25f, .25f, .25f);
                isLerpingUp = false;
                //inRange = true;
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
                //inRange = false;
            }
        }*/
        else if (!isLerpingBack && !isLerpingUp && !isLerping && Time.time > secondsRemainingInMinute)
        {
            float colorIndex = sun.transform.eulerAngles.x / 360f;
            RenderSettings.fogColor = fogColor.Evaluate(colorIndex);
            sun.intensity = sunLight.Evaluate(colorIndex).grayscale;
            secondsRemainingInMinute = Time.time + adjustedSecondsInHour;
            sun.transform.Rotate(.25f, 0, 0);

            /*
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
            */
            RenderSettings.ambientLight = ambientLight.Evaluate(colorIndex);

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
