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
            if ((windController != null && (currentDirection != windController.direction || currentSpeed != windController.speed)))
                UpdateSelection();

            textureMovement.x += directionOffset.x;
            textureMovement.y += directionOffset.y;
            textureMovement2.x += directionOffset2.x;
            textureMovement2.y += directionOffset2.y;

            water.material.SetTextureOffset("_MainTex", new Vector2(textureMovement.x, textureMovement.y));
            water.material.SetTextureOffset("_DetailAlbedoMap", new Vector2(textureMovement2.x, textureMovement2.y));

            if (windController != null)
            {
                water.material.SetFloat("_BumpScale", windController.speed * .15f);
                water.material.SetFloat("_DetailNormalMapScale", windController.speed * .15f);
            }
        }
    }

    void UpdateSelection()
    {
        currentDirection = direction;
        currentSpeed = speed;

        if (windController != null)
        {
            direction = currentDirection = windController.direction;
            speed = currentSpeed = windController.speed;
        }

        combined = speed * 2 * .25f;
        offset = Time.deltaTime * combined;
        slowOffset = offset * .125f;
        directionOffset.x = Mathf.Cos(direction * 0.0174532925f) * slowOffset;
        directionOffset.y = Mathf.Sin(direction * 0.0174532925f) * slowOffset;
        directionOffset2.x = Mathf.Sin(direction * 0.0174532925f) * offset;
        directionOffset2.y = Mathf.Cos(direction * 0.0174532925f) * offset;
    }
}
