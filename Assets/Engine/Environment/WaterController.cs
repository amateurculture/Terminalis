using UnityEngine;

public class WaterController : MonoBehaviour
{
    public Renderer water;
    public WindController windController;

    float direction;
    float speed; 
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
        speed = .1f;
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

                textureMovement.x += directionOffset.x;
                textureMovement.y += directionOffset.y;
                textureMovement2.x += directionOffset2.x;
                textureMovement2.y += directionOffset2.y;
                water.material.SetTextureOffset("_MainTex", new Vector2(textureMovement.x, textureMovement.y));
                water.material.SetTextureOffset("_DetailAlbedoMap", new Vector2(textureMovement2.x, textureMovement2.y));
                water.material.SetFloat("_BumpScale", windController.currentSpeed * .25f);
                water.material.SetFloat("_DetailNormalMapScale", windController.currentSpeed * .25f);
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
        slowOffset = offset * .125f;

        directionOffset.x = Mathf.Sin(direction * 0.0174532925f) * offset;
        directionOffset.y = Mathf.Cos(direction * 0.0174532925f) * offset;

        direction += 45;
        direction = direction % 360;
        directionOffset2.x = Mathf.Cos(direction * 0.0174532925f) * slowOffset;
        directionOffset2.y = Mathf.Sin(direction * 0.0174532925f) * slowOffset;
    }
}
