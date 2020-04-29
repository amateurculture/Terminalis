using UnityEngine;

public class CloudController : FluidsController
{
    protected Color cloudColor;
    //[Range(0, 1)] public float cloudBreaks;
    [Range(0, 1)] public float density;

    private void Reset()
    {
        speed = .5f;
        direction = 0;
        meshRenderer = GetComponent<Renderer>();
        windController = GetComponent<WindController>();
        //cloudBreaks = 1;
        density = 1;
    }

    public override void UpdateController()
    {
        var speedMultiplier = .005f;
        //var bumpMultiplier = cloudBreaks * 10;

        cloudColor = RenderSettings.fogColor;
        cloudColor.a = density;
        //meshRenderer.material.SetColor("_Color", cloudColor);
        //meshRenderer.material.SetColor("_EmissionColor", cloudColor);

        UpdateFluid(speedMultiplier, -1);
    }
}
