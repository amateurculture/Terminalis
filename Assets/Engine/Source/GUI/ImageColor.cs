using UnityEngine;
using UnityEngine.UI;

public class ImageColor : MonoBehaviour
{
    public Slider r;
    public Slider g;
    public Slider b;
    Image image;

    void Start()
    {
        image = transform.GetComponent<Image>();
    }

    void Update()
    {
        image.color = new Color(r.value, g.value, b.value);
    }
}
