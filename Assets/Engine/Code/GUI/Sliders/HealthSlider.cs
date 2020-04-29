
public class HealthSlider : StatusSlider
{
    private void Start()
    {
        InitSlider();
    }

    void FixedUpdate()
    {
        if (script != null)
            slider.value = script.vitality / 100f;
    }
}
