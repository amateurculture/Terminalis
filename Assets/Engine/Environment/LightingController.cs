using UnityEngine;

public class LightingController : MonoBehaviour
{
    public Gradient sunLight;
    public Gradient ambientLight;
    public FogController fogController;
    public PlanetaryController planet;
    public Light sun;
    public Light moon;
    public int reflectionFrameSkip;

    public ReflectionProbe reflectionProbe;
    float gradientIndex;
    float lightLevel;
    bool isSunDisabled;

    private void Reset()
    {
        var possibleLights = FindObjectsOfType<Light>();

        foreach (var light in possibleLights)
        {
            if (light.type == LightType.Directional && !light.name.Contains("Indirect"))
            {
                sun = light;
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

        planet = GetComponent<PlanetaryController>();
        ambientLight = new Gradient();
        ambientLight.SetKeys(keys1, alphaKeys);
        reflectionFrameSkip = 30;
    }

    private void Start()
    {
        if (sun != null)
        {
            sun.transform.eulerAngles = Vector3.zero;
            sun.transform.Rotate(new Vector3(15f * ((planet.hour + (planet.minute / 60f)) - 6f), 0, 0));
            lightLevel = sun.transform.eulerAngles.x / 360;
            sun.intensity = sunLight.Evaluate(lightLevel).grayscale;
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
        if (Time.frameCount % reflectionFrameSkip == 0)
        {
            if (reflectionProbe != null)
            {
                reflectionProbe.backgroundColor = RenderSettings.fogColor;
                reflectionProbe.RenderProbe();
            }
        }
        
        /*
        if (sun == null || !sun.gameObject.activeSelf) {

            if (!isSunDisabled)
            {
                RenderSettings.ambientLight = Color.black;
                sun.transform.eulerAngles = new Vector3(-90, 0, 0);
                RenderSettings.fogColor = Color.black;
                isSunDisabled = true;

                reflectionProbe.backgroundColor = Color.black;
                reflectionProbe.RenderProbe();
            }
            if (isSunDisabled)
            {
                isSunDisabled = false;
                sun.transform.eulerAngles = Vector3.zero;
                sun.transform.Rotate(new Vector3(15f * ((planet.hour + (planet.minute / 60f)) - 6f), 0, 0));
            }
        }
        */
    }

    public void UpdateLighting()
    {
        UpdateGeocentricSun();
        UpdateSunLight();
        UpdateAmbientLight();
    }

    void UpdateAmbientLight()
    {
        gradientIndex = planet.minute * .017f;
        gradientIndex += planet.hour;
        gradientIndex *= 0.04f;
        RenderSettings.ambientLight = ambientLight.Evaluate(gradientIndex);
    }

    void UpdateSunLight()
    {
        gradientIndex = planet.minute * .017f;
        gradientIndex += planet.hour;
        gradientIndex *= 0.04f;
        lightLevel = sun.intensity = sunLight.Evaluate(gradientIndex).grayscale * 1.25f;

        if (lightLevel <= 0)
        {
            sun.enabled = false;
            moon.enabled = true;
        }
         else
        {
            sun.enabled = true;
            moon.enabled = false;
        }
    }

    void UpdateGeocentricSun()
    {
        sun.transform.Rotate(.25f, 0, 0);
    }
}
