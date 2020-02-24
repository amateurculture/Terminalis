using UnityEngine;

[RequireComponent(typeof(TimeController))]

public class LightingController : MonoBehaviour
{
    public Gradient sunLight;
    public Gradient ambientLight;
    [Tooltip("Make sure the reflection probe you are assigning to here is attached to the player object first!")]
    public ReflectionProbe reflectionProbe;

    TimeController timeController;
    float adjustedSecondsInHour;
    float secondsRemainingInMinute;
    int frameSkip;

    private void Reset()
    {
        timeController = GetComponent<TimeController>();

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
    }

    private void Start()
    {
        timeController = GetComponent<TimeController>();
        frameSkip = 60;

        if (timeController.sun != null)
        {
            RenderSettings.ambientLight = ambientLight.Evaluate(timeController.sun.transform.eulerAngles.x / 360);
            timeController.sun.intensity = sunLight.Evaluate(timeController.sun.transform.eulerAngles.x / 360).grayscale;
        }

        if (reflectionProbe != null)
        {
            reflectionProbe.enabled = true;
            reflectionProbe.RenderProbe();
        }
        else reflectionProbe.enabled = false;
    }

    void Update()
    {
        if (timeController.sun == null) return;

        if (Time.frameCount % frameSkip == 0)
        {
            if (reflectionProbe != null)
            {
                reflectionProbe.backgroundColor = RenderSettings.fogColor;
                reflectionProbe.RenderProbe();
            }
        }
        else if (Time.time > secondsRemainingInMinute)
        {
            secondsRemainingInMinute = Time.time + adjustedSecondsInHour;
            UpdateSunLight();
            UpdateAmbientLight();
        }
    }

    void UpdateAmbientLight()
    {
        float colorIndex = timeController.sun.transform.eulerAngles.x / 360f;
        RenderSettings.ambientLight = ambientLight.Evaluate(colorIndex);
    }

    void UpdateSunLight()
    {
        float colorIndex = timeController.sun.transform.eulerAngles.x / 360f;
        timeController.sun.intensity = sunLight.Evaluate(colorIndex).grayscale;
    }
}
