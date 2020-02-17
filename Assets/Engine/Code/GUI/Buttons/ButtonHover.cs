using UnityEngine.EventSystems;
using TMPro;

namespace UnityEngine.UI
{
    public class ButtonHover : Button
    {
        public TextMeshProUGUI text;
        public Text oldText;
        public Color enterColor;
        public Color exitColor;

        protected override void Start()
        {
            var buh = transform.Find("Text (TMP)");
            if (buh != null)
                text = buh.GetComponent<TextMeshProUGUI>();

            var foo = transform.Find("Text");
            if (foo != null)
                oldText = foo.GetComponent<Text>();

            if (text != null && Brain.instance != null && Brain.instance.buttonTextColor != null)
                text.color = Brain.instance.buttonTextColor;
            if (oldText != null && Brain.instance != null && Brain.instance.buttonTextColor != null)
                oldText.color = Brain.instance.buttonTextColor;
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            if (text != null && Brain.instance != null && Brain.instance.hoverColor != null)
                text.color = Brain.instance.hoverColor;
            if (oldText != null && Brain.instance != null && Brain.instance.hoverColor != null)
                oldText.color = Brain.instance.hoverColor;

            base.OnPointerEnter(eventData);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            if (text != null && Brain.instance != null && Brain.instance.buttonTextColor != null)
                text.color = Brain.instance.buttonTextColor;
            if (oldText != null && Brain.instance != null && Brain.instance.hoverColor != null)
                oldText.color = Brain.instance.buttonTextColor;

            base.OnPointerExit(eventData);
        }

        protected override void OnDisable()
        {
            if (text != null && Brain.instance != null)
                text.color = Brain.instance.buttonTextColor;
            if (oldText != null && Brain.instance != null && Brain.instance.hoverColor != null)
                oldText.color = Brain.instance.buttonTextColor;

            base.OnDisable();
        }
    }
}
