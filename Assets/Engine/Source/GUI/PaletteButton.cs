using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]

public class PaletteButton : MonoBehaviour
{
    Image colorSelection;
    Image image;

    void Awake()
    {
        transform.GetComponent<Button>()?.onClick.AddListener(Action);
        colorSelection = transform.parent.GetComponent<Image>();
        image = transform.GetComponent<Image>();
    }
    
    void Action()
    {
        colorSelection.color = image.color;
    }
}
