
public class InsanitySlider : StatusSlider
{
    private void Start()
    {
        InitSlider();
    }

    void Update()
    {
        if (script != null)
            slider.value = (100-script.anxiety) / 100f;
    }
}
