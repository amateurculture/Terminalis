using UnityEngine;

public class LightingController : MonoBehaviour
{
    public Gradient skyColor;
    public Gradient sunLight;
    public Gradient ambientLight;
    public FogController fogController;
    public TimeController timeController;
    public Light moon;
    public ReflectionProbe reflectionProbe;
    public int reflectionFrameSkip;
    
    float gradientIndex;
    float lightLevel;
    Camera cameraMain;

    private void Reset()
    {
        var possibleLights = FindObjectsOfType<Light>();

        foreach (var light in possibleLights)
        {
            if (light.type == LightType.Directional && !light.name.Contains("Indirect"))
            {
                RenderSettings.sun = light;
                break;
            }
        }

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

        timeController = GetComponent<TimeController>();
        ambientLight = new Gradient();
        ambientLight.SetKeys(keys1, alphaKeys);
        reflectionFrameSkip = 30;
    }

    private void Start()
    {
        cameraMain = Camera.main;

        if (RenderSettings.sun != null)
        {
            RenderSettings.sun.transform.eulerAngles = Vector3.zero;
            RenderSettings.sun.transform.Rotate(new Vector3(15f * ((timeController.hour + (timeController.minute / 60f)) - 6f), 0, 0));

            lightLevel = GetGradientIndex();
            RenderSettings.sun.intensity = sunLight.Evaluate(lightLevel).grayscale;
            RenderSettings.ambientLight = ambientLight.Evaluate(lightLevel);
        }

        if (reflectionProbe != null)
        {
            reflectionProbe.enabled = true;
            reflectionProbe.RenderProbe();
        }
    }

    void Update()
    {
        if (Time.frameCount % reflectionFrameSkip == 0 && reflectionProbe != null)
        {
            reflectionProbe.backgroundColor = cameraMain.backgroundColor;
            reflectionProbe.RenderProbe();
        }
    }

    public void UpdateLighting()
    {
        UpdateSunLight();
        UpdateGeocentricSun();
        UpdateAmbientLight();
        UpdateSkyColor();
    }

    float GetGradientIndex()
    {
        gradientIndex = timeController.minute * .017f;
        gradientIndex += timeController.hour;
        gradientIndex *= 0.04f;
        return gradientIndex;
    }

    void UpdateSkyColor()
    {
        gradientIndex = GetGradientIndex();
        cameraMain.backgroundColor = skyColor.Evaluate(GetGradientIndex());
    }

    void UpdateAmbientLight()
    {
        gradientIndex = GetGradientIndex();
        RenderSettings.ambientLight = ambientLight.Evaluate(gradientIndex);
    }

    void UpdateSunLight()
    {
        gradientIndex = GetGradientIndex();
        lightLevel = RenderSettings.sun.intensity = sunLight.Evaluate(gradientIndex).grayscale;

        RenderSettings.sun.enabled = (lightLevel <= 0) ? false : true;

        if (moon != null) moon.enabled = (lightLevel <= 0) ? true : false;
    }

    void UpdateGeocentricSun()
    {
        RenderSettings.sun.transform.Rotate(.25f, 0, 0);
    }
}
