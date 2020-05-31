using UnityEngine;

public class WaterController : FluidsController
{
    public LightingController lightingController;
    bool isDaytime;

    private void Reset()
    {
        speed = .5f;
        direction = 0;
        meshRenderer = GetComponent<Renderer>();
        windController = GetComponent<WindController>();
    }

    private void Start()
    {
        //lightingController = GetComponent<LightingController>();
    }

    bool isDay()
    {
        return (lightingController.timeController.hour > 6 && 
            lightingController.timeController.hour < 18) ? true : false;
    }

    private void Update()
    {
        if (meshRenderer != null)
            meshRenderer.materials[0].SetColor("_ReflectionColor", lightingController.fogController.fogColor.Evaluate(lightingController.fogController.GetGradientIndex()));
    }

    public override void UpdateController()
    {
        if (meshRenderer != null)
        {
           //meshRenderer.material.SetColor("ReflectionColor", lightingController.fogController.fogColor.Evaluate(lightingController.fogController.GetGradientIndex()));


            /*
            var speedMultiplier = 1f;
            var bumpMultiplier = .3f;

            if (isDay() && !isDaytime)
            {
                isDaytime = true;
                //lightingController.reflectionProbe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
            }
            else if (!isDay() && isDaytime)
            {
                isDaytime = false;
                //lightingController.reflectionProbe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;
            }

            UpdateFluid(speedMultiplier, bumpMultiplier);
            */
        }
    }
}
