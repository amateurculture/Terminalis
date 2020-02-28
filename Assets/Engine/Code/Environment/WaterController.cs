using UnityEngine;

public class WaterController : FluidsController
{
    private void Reset()
    {
        speed = .5f;
        direction = 0;
        meshRenderer = GetComponent<Renderer>();
        windController = GetComponent<WindController>();
    }

    public override void UpdateController()
    {
        if (meshRenderer != null)
        {
            var speedMultiplier = 1f;
            var bumpMultiplier = .3f;

            UpdateFluid(speedMultiplier, bumpMultiplier);
        }
    }
}
