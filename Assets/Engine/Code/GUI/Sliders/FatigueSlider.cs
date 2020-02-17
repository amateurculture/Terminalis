
public class FatigueSlider : StatusSlider
{
    private void Start()
    {
        InitSlider();
    }

    void Update()
    {
        if (script != null)
            slider.value = (100-script.fatigue) / 100f;
    }
}
