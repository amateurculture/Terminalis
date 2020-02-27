using UnityEngine;

public class FluidsController : MonoBehaviour
{
    public Renderer water;
    public WindController windController;
    [Range(0, 359)] public float direction;
    [Range(0, 1)] public float speed;
    public bool isAtmospheric;

    float currentSpeed;
    float currentDirection;
    float combined;
    float offset;
    float slowOffset;
    Vector2 directionOffset;
    Vector2 textureMovement;
    Vector2 textureMovement2;
    Vector2 directionOffset2;

    private void Reset()
    {
        direction = 0;
        speed = .5f;
        water = GetComponent<Renderer>();
        windController = GetComponent<WindController>();
    }

    private void Start()
    {
        currentDirection = -1;
    }

    void Update()
    {
        if (water != null && Time.frameCount % 2 == 0)
        {
            if (windController != null)
            {
                if (currentDirection != windController.currentDirection || 
                    currentSpeed != windController.currentSpeed)
                UpdateSelection();

                var bumpFactor = (isAtmospheric) ? 2f : .35f;
                textureMovement.x += directionOffset.x;
                textureMovement.y += directionOffset.y;
                textureMovement2.x += directionOffset2.x;
                textureMovement2.y += directionOffset2.y;
                water.material.SetTextureOffset("_MainTex", new Vector2(textureMovement.x, textureMovement.y));
                water.material.SetTextureOffset("_DetailAlbedoMap", new Vector2(textureMovement.x, textureMovement2.y));
                water.material.SetFloat("_BumpScale", windController.currentSpeed * bumpFactor);
                water.material.SetFloat("_DetailNormalMapScale", windController.currentSpeed * bumpFactor);
            } 
            else
            {
                if (currentDirection != direction || currentSpeed != speed) UpdateSelection();

                var bumpFactor = (isAtmospheric) ? 2f : .35f;
                textureMovement.x += directionOffset.x;
                textureMovement.y += directionOffset.y;
                textureMovement2.x += directionOffset2.x;
                textureMovement2.y += directionOffset2.y;
                water.material.SetTextureOffset("_MainTex", new Vector2(textureMovement.x, textureMovement.y));
                water.material.SetTextureOffset("_DetailAlbedoMap", new Vector2(textureMovement.x, textureMovement2.y));
                water.material.SetFloat("_BumpScale", currentSpeed * bumpFactor);
                water.material.SetFloat("_DetailNormalMapScale", currentSpeed * bumpFactor);
            }
        }
    }

    void UpdateSelection()
    {
        currentDirection = direction;
        currentSpeed = speed;

        if (windController != null)
        {
            direction = currentDirection = windController.currentDirection;
            speed = currentSpeed = windController.currentSpeed;
        }

        combined = speed * 2 * .25f;
        offset = Time.deltaTime * combined;
        slowOffset = offset * .15f;// .125f;

        directionOffset.x = Mathf.Sin(direction * 0.0174532925f) * offset;
        directionOffset.y = Mathf.Cos(direction * 0.0174532925f) * offset;

        var counterFlowDirection = (direction + 45) % 360;
        directionOffset2.x = Mathf.Cos(counterFlowDirection * 0.0174532925f) * slowOffset;
        directionOffset2.y = Mathf.Sin(counterFlowDirection * 0.0174532925f) * slowOffset;
    }
}
