using UnityEngine;

public class WaterController : MonoBehaviour
{
    float direction;
    float speed;
    public int frameSkip;
    public Renderer water;
    WeatherController weatherController;

    int currentFrameSkip;
    float currentSpeed;
    float currentDirection;
    float combined;
    float offset;
    float slowOffset;
    Vector2 directionOffset;
    Vector2 textureMovement;
    Vector2 textureMovement2;
    Vector2 directionOffset2;
    Renderer rend;

    private void Reset()
    {
        direction = 0;
        speed = .1f;
        frameSkip = 2;
    } 

    void Start()
    {
        if (water != null) rend = water.GetComponent<Renderer>();
        weatherController = GetComponent<WeatherController>();

        UpdateSelection();
    }

    void Update()
    {
        if (rend != null && Time.frameCount % frameSkip == 0)
        {

            if ((weatherController != null && (currentDirection != weatherController.direction || currentSpeed != weatherController.speed)) ||
                currentFrameSkip != frameSkip)
                UpdateSelection();

            textureMovement.x += directionOffset.x;
            textureMovement.y += directionOffset.y;
            textureMovement2.x += directionOffset2.x;
            textureMovement2.y += directionOffset2.y;

            rend.material.SetTextureOffset("_MainTex", new Vector2(textureMovement.x, textureMovement.y));
            rend.material.SetTextureOffset("_DetailAlbedoMap", new Vector2(textureMovement2.x, textureMovement2.y));

            if (weatherController != null)
            {
                rend.material.SetFloat("_BumpScale", weatherController.speed * .15f);
                rend.material.SetFloat("_DetailNormalMapScale", weatherController.speed * .15f);
            }
        }
    }

    void UpdateSelection()
    {
        currentDirection = direction;
        currentSpeed = speed;

        if (weatherController != null)
        {
            direction = currentDirection = weatherController.direction;
            speed = currentSpeed = weatherController.speed;
        }

        currentFrameSkip = frameSkip;
        combined = speed * frameSkip * .25f;
        offset = Time.deltaTime * combined;
        slowOffset = offset * .125f;
        directionOffset.x = Mathf.Cos(direction * 0.0174532925f) * slowOffset;
        directionOffset.y = Mathf.Sin(direction * 0.0174532925f) * slowOffset;
        directionOffset2.x = Mathf.Sin(direction * 0.0174532925f) * offset;
        directionOffset2.y = Mathf.Cos(direction * 0.0174532925f) * offset;
    }
}
