
public class HealthSlider : StatusSlider
{
    private void Start()
    {
        InitSlider();
    }

    void FixedUpdate()
    {
        if (script != null)
            slider.value = script.health / 100f;
    }
}
