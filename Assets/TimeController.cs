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
    public float fogLerp;
     float timeLerp;

     float gameTime;
     float previousGameTime;
     float previousMinute;
     float previousHour;
     float t1;

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

    [Header("Experimental")]
    Light testLight;

    GameObject player;
    bool didChangeReflectionProbeSetting;
    bool EndOfDay;
    bool nextDay;
    bool isLerpingBack;
    bool isLerpingUp;
    bool isLerping;
    bool inRange;
    float lightLerpTime;
    float adjustedSecondsInHour; 
    float secondsRemainingInMinute;
    float prev;
    float t;
    float previousDay;

    enum WhatIsLerping
    {
        startFog,
        endFog,
        time
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
        endFogDistance = 256;
        fogLerp = .25f;
        timeLerp = 1;

        /*
        previousMinute = minute;
        previousHour = hour;
        previousDay = day;
        previousStartFog = startFogDistance;
        previousEndFog = endFogDistance;
        */

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
        player = GameObject.FindGameObjectWithTag("Player");

        if (sun != null)
        {
            sun.transform.eulerAngles = Vector3.zero;
            var angles = sun.transform.eulerAngles;
            angles.y = 45;
            angles.x = 15f * ((hour + (minute / 60f)) - 6f);
            sun.transform.Rotate(angles);

            gameTime = hour + (minute / 60f);
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
        previousMinute = minute;
        previousHour = hour;
    }

    void Update()
    {
        if (sun == null) return;

        if (Time.frameCount % frameSkip == 0)
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
        }
        else if (isLerping)
        {
            // todo can time lerping be fixed or would this introduce a time paradox?
            if (Time.time > secondsRemainingInMinute)
            {
                switch (whatIsLerping)
                {
                    case WhatIsLerping.startFog:
                        t1 += Time.deltaTime * fogLerp;
                        previousStartFog = Mathf.Lerp(previousStartFog, startFogDistance, t1);
                        RenderSettings.fogStartDistance = previousStartFog;
                        if (Mathf.Abs(previousStartFog - startFogDistance) < .1f)
                        {
                            previousStartFog = startFogDistance;
                            isLerping = false;
                        }
                        break;
                    case WhatIsLerping.endFog:
                        t1 += Time.deltaTime * fogLerp;
                        previousEndFog = Mathf.Lerp(previousEndFog, endFogDistance, t1);
                        RenderSettings.fogEndDistance = previousEndFog;
                        if (Mathf.Abs(previousEndFog - endFogDistance) < .1f)
                        {
                            previousEndFog = endFogDistance;
                            isLerping = false;
                        }
                        break;
                    case WhatIsLerping.time:
                        t1 += Time.deltaTime * timeLerp;
                        previousGameTime = Mathf.Lerp(previousGameTime, gameTime, t1);
                        hour = ((int)gameTime) % 24;
                        minute = ((int)(60f * (gameTime - ((int)gameTime))));

                        if (Mathf.Abs(gameTime - previousGameTime) < .1f)
                        {
                            previousGameTime = gameTime;
                            isLerping = false;
                        }
                        RotateSun();

                        break;
                    default:
                        break;
                }
                secondsRemainingInMinute = Time.time + adjustedSecondsInHour;
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
            if (previousStartFog != startFogDistance)
            {
                whatIsLerping = WhatIsLerping.startFog;
                isLerping = true;
            }
            else if (previousEndFog != endFogDistance)
            {
                whatIsLerping = WhatIsLerping.endFog;
                isLerping = true;
            }
            /*
            else if (hour != previousHour || minute != previousMinute)
            {
                whatIsLerping = WhatIsLerping.time;
                isLerping = true;
            }
            */

            if (isLerping)
            {
                t1 = 0;
                secondsRemainingInMinute = 0;
                isLerping = true;
            }
            else
            {
                UpdateTime();
                RotateSun();
            }
        }
    }

    void RotateSun()
    {
        float colorIndex = sun.transform.eulerAngles.x / 360f;
        RenderSettings.fogColor = fogColor.Evaluate(colorIndex);
        sun.intensity = sunLight.Evaluate(colorIndex).grayscale;
        sun.transform.Rotate(.25f, 0, 0);

        RenderSettings.ambientLight = ambientLight.Evaluate(colorIndex);

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

    void UpdateTime()
    {
        secondsRemainingInMinute = Time.time + adjustedSecondsInHour;
        gameTime += .0166f;
        hour = ((int)gameTime) % 24;
        minute = ((int)(60f * (gameTime - ((int)gameTime))));

        previousHour = hour;
        previousMinute = minute;
        previousGameTime = gameTime;
    }
}
