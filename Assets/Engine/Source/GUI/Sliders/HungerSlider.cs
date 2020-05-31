
public class HungerSlider : StatusSlider
{
    private void Start()
    {
        InitSlider();
    }

    void Update()
    {
        if (script != null)
            slider.value = (100-script.hunger) / 100f;
    }
}
