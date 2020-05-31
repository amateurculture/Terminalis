using UnityEngine;

public abstract class FluidsController : MonoBehaviour
{
    public Renderer meshRenderer;
    public WindController windController;
    [Range(0, 359)] public float direction;
    [Range(0, 1)] public float speed;

    protected float combined;
    protected float offset;
    protected float slowOffset;
    protected float counterFlowDirection;
    protected Vector2 directionOffset;
    protected Vector2 textureMovement;
    protected Vector2 textureMovement2;
    protected Vector2 directionOffset2;

    public abstract void UpdateController();

    public void UpdateFluid(float speedMultiplier, float bumpMultiplier)
    {
        if (windController != null)
        {
            direction = windController.currentDirection;
            speed = windController.currentSpeed;
        }

        combined = speed * 2 * .25f;
        offset = Time.deltaTime * combined;
        slowOffset = offset * .15f;
        directionOffset.x = Mathf.Sin(direction * 0.0174532925f) * offset;
        directionOffset.y = Mathf.Cos(direction * 0.0174532925f) * offset;
        counterFlowDirection = (direction + 45) % 360;
        directionOffset2.x = Mathf.Cos(counterFlowDirection * 0.0174532925f) * slowOffset;
        directionOffset2.y = Mathf.Sin(counterFlowDirection * 0.0174532925f) * slowOffset;

        textureMovement.x += directionOffset.x * speedMultiplier;
        textureMovement.y += directionOffset.y * speedMultiplier;
        textureMovement2.x += directionOffset2.x * speedMultiplier;
        textureMovement2.y += directionOffset2.y * speedMultiplier;

        meshRenderer.material.SetTextureOffset("_MainTex", new Vector2(textureMovement.x, textureMovement.y));
        meshRenderer.material.SetTextureOffset("_DetailAlbedoMap", new Vector2(textureMovement.x, textureMovement2.y));

        if (bumpMultiplier > 0)
        {
            meshRenderer.material.SetFloat("_BumpScale", bumpMultiplier * speed);
            meshRenderer.material.SetFloat("_DetailNormalMapScale", bumpMultiplier * speed);

        } 
        else
        {
            //meshRenderer.material.SetFloat("_BumpScale", 1);
            //meshRenderer.material.SetFloat("_DetailNormalMapScale", 1);
        }
    }

    private void Update()
    {
        if (Time.frameCount % 2 == 0)
        {
            UpdateController();
        }
    }
}
