using UnityEngine;

[RequireComponent(typeof(LightingController))]

public class FogController : MonoBehaviour
{
    [Header("Global Fog")]
    [Range(0, 10000)] public float fogStartDistance;
    [Range(1, 5000)] public float fogEndDistance;
    public Gradient fogColor;
    
    LightingController lightingController;
    Light sun;
    float fogLerp;
    float currentFogStartDistance;
    float currentFogEndDistance;
    float t1;
    bool isLerping;
    int frameSkip;

    [Range(0, 1)] float gradientIndex;

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
            new GradientColorKey(new Color32(255, 255, 255, 1), .08f),
            new GradientColorKey(new Color32(255, 255, 255, 1), .5f),
            new GradientColorKey(new Color32(149, 77, 79, 1), .54f),
            new GradientColorKey(new Color32(15, 15, 15, 1), .58f),
            new GradientColorKey(new Color32(15, 15, 15, 1), 1)
        };
        fogColor = new Gradient();
        fogColor.SetKeys(keys2, alphaKeys);
        fogStartDistance = 0;
        fogEndDistance = 1024;
    }

    private void Start()
    {
        lightingController = GetComponent<LightingController>();
        sun = lightingController.sun;
        fogLerp = .0001f;
        frameSkip = 60;
        isLerping = false;
        RenderSettings.fogStartDistance = fogStartDistance;
        RenderSettings.fogEndDistance = fogEndDistance;
        RenderSettings.fogColor = fogColor.Evaluate(sun.transform.eulerAngles.x / 360);
        currentFogStartDistance = fogStartDistance;
        currentFogEndDistance = fogEndDistance;

        UpdateFogColor();
    }

    public void UpdateFogColor()
    {
        if (sun == null) return;
        gradientIndex = lightingController.planet.minute / 60f;
        gradientIndex += lightingController.planet.hour;
        gradientIndex /= 24f;
        RenderSettings.fogColor = fogColor.Evaluate(gradientIndex);
    }

    void UpdateStartFog()
    {
        currentFogStartDistance = Mathf.Lerp(currentFogStartDistance, fogStartDistance, t1);
        RenderSettings.fogStartDistance = currentFogStartDistance;

        if (Mathf.Abs(currentFogStartDistance - fogStartDistance) <= .35f)
        {
            RenderSettings.fogStartDistance = fogStartDistance;
            currentFogStartDistance = fogStartDistance;
            isLerping = false;
        }
    }

    void UpdateEndFog()
    {
        currentFogEndDistance = Mathf.Lerp(currentFogEndDistance, fogEndDistance, t1);
        RenderSettings.fogEndDistance = currentFogEndDistance;

        if (Mathf.Abs(currentFogEndDistance - fogEndDistance) <= .35f)
        {
            RenderSettings.fogEndDistance = fogEndDistance;
            currentFogEndDistance = fogEndDistance;
            isLerping = false;
        }
    }

    private void Update()
    {
        if (isLerping)
        {
            t1 += Time.deltaTime * fogLerp;

            switch (whatIsLerping)
            {
                case WhatIsLerping.startFog: UpdateStartFog(); break;
                case WhatIsLerping.endFog: UpdateEndFog(); break;
                default: break;
            }
        }
        else if (Time.frameCount % frameSkip == 0 && !isLerping)
        {
            if (currentFogStartDistance != fogStartDistance)
            {
                whatIsLerping = WhatIsLerping.startFog;
                isLerping = true;
                t1 = 0;
            }
            else if (currentFogEndDistance != fogEndDistance)
            {
                whatIsLerping = WhatIsLerping.endFog;
                isLerping = true;
                t1 = 0;
            }
        }
    }
}
