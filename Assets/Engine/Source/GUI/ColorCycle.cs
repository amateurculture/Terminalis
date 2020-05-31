using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ColorCycle : MonoBehaviour
{
    Image image;
    TextMeshProUGUI text;
    Text oldText;
    float incrementer;

    void Start()
    {
        image = GetComponent<Image>();
        text = GetComponent<TextMeshProUGUI>();
        oldText = GetComponent<Text>();
    }

    void Update()
    {
        if (Brain.instance != null && Brain.instance.colorCycle1 != null) {
            if (image != null)
                image.color = Brain.instance.colorCycle1;

            if (text != null)
                text.color = Brain.instance.colorCycle1;

            if (oldText != null)
                oldText.color = Brain.instance.colorCycle2;
        }
    }
}
