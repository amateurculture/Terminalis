using UnityEngine;

[RequireComponent(typeof(TimeController))]

public class FogController : MonoBehaviour
{
    TimeController timeController;
    float secondsInHour;
    int frameSkip;

    [Header("Global Fog")]
    [Range(0, 1024)] float startFogDistance;
    [Range(0, 1024)] public float fogDistance;
    public Gradient fogColor;
    float fogLerp;

    bool isLerping;
    float previousStartFog;
    float previousEndFog;
    float t1;
    float secondsRemainingInMinute;
    float adjustedSecondsInHour;

    enum WhatIsLerping
    {
        startFog,
        endFog,
        time
    }
    WhatIsLerping whatIsLerping;

    private void Reset()
    {
        GradientAlphaKey[] alphaKeys = {
            new GradientAlphaKey(1, 0),
            new GradientAlphaKey(1, 1)
        };

        GradientColorKey[] keys2 = {
            new GradientColorKey(new Color32(15, 15, 15, 1), 0),
            new GradientColorKey(new Color32(114, 124, 135, 1), .04f),
            new GradientColorKey(new Color32(172, 207, 245, 1), .08f),
            new GradientColorKey(new Color32(172, 207, 245, 1), .5f),
            new GradientColorKey(new Color32(149, 77, 79, 1), .54f),
            new GradientColorKey(new Color32(15, 15, 15, 1), .58f),
            new GradientColorKey(new Color32(15, 15, 15, 1), 1)
        };
        fogColor = new Gradient();
        fogColor.SetKeys(keys2, alphaKeys);

        fogDistance = 256;
        timeController = GetComponent<TimeController>();
    }

    private void Start()
    {
        frameSkip = 100;
        secondsInHour = 60;
        fogDistance = 256;
        fogLerp = .25f;
        isLerping = false;
        timeController = GetComponent<TimeController>();
        RenderSettings.fogStartDistance = 0;
        RenderSettings.fogEndDistance = fogDistance;
        RenderSettings.fogColor = fogColor.Evaluate(timeController.sun.transform.eulerAngles.x / 360);
        previousStartFog = startFogDistance;
        previousEndFog = fogDistance;
        adjustedSecondsInHour = secondsInHour / 60;
    }

    private void UpdateFog()
    {
        if (isLerping)
        {
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
                        previousEndFog = Mathf.Lerp(previousEndFog, fogDistance, t1);
                        RenderSettings.fogEndDistance = previousEndFog;
                        if (Mathf.Abs(previousEndFog - fogDistance) < .1f)
                        {
                            previousEndFog = fogDistance;
                            isLerping = false;
                        }
                        break;
                    default:
                        break;
                }
                secondsRemainingInMinute = Time.time + adjustedSecondsInHour;
            }
        } 
        else
        {
            secondsRemainingInMinute = Time.time + adjustedSecondsInHour;
            float colorIndex = timeController.sun.transform.eulerAngles.x / 360f;
            RenderSettings.fogColor = fogColor.Evaluate(colorIndex);
        }
    }

    private void Update()
    {
        if (Time.frameCount % frameSkip == 0)
        {
            if (Time.time > secondsRemainingInMinute)
            {
                if (previousStartFog != startFogDistance)
                {
                    whatIsLerping = WhatIsLerping.startFog;
                    isLerping = true; 
                    t1 = 0;
                    secondsRemainingInMinute = 0;
                }
                else if (previousEndFog != fogDistance)
                {
                    whatIsLerping = WhatIsLerping.endFog;
                    isLerping = true; 
                    t1 = 0;
                    secondsRemainingInMinute = 0;
                }
            }

            UpdateFog();

            /*
            
            if (isLerping)
            {
                t1 = 0;
                secondsRemainingInMinute = 0;
            }
            else
            {
                UpdateFog();
            }
            */
        }
    }
}
