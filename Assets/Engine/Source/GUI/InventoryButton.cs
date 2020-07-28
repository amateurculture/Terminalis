using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Author: Fiona Schultz
/// Last Modified: July-26-2019
/// </summary>

namespace UnityEngine.UI
{
    public class InventoryButton : Button
    {
        public TextMeshProUGUI text;
        public TextMeshProUGUI number;
        public Text oldText;
        public Color enterColor;
        public Color exitColor;
        public InventoryPanel inventoryPanel;
        public Color inventoryColor;

        public enum buttonStates
        {
            unselected,
            selected,
            equipped
        }
        public buttonStates buttonState = buttonStates.unselected;

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

            image = GetComponent<Image>();
            inventoryPanel = transform.parent.GetComponent<InventoryPanel>();
            number = transform.Find("Number").GetComponent<TextMeshProUGUI>();
            inventoryColor = GetComponent<Image>().color;
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            image.color = Brain.instance.hoverColor;
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);

            switch (buttonState)
            {
                case buttonStates.equipped:
                    image.color = Brain.instance.equippedColor;
                    break;
                case buttonStates.unselected:
                    image.color = inventoryColor;
                    break;
                default:
                    image.color = Brain.instance.unusableColor;
                    break;
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);

            var buttonIndex = int.Parse(number.text) - 1;

            if (eventData.button.ToString() == "Left")
            {
                if (buttonState == buttonStates.selected)
                {
                    image.color = inventoryColor;
                    buttonState = buttonStates.unselected;
                    inventoryPanel.selected = -1;
                }
                else if (buttonState == buttonStates.equipped)
                {
                    transform.parent.GetComponent<InventoryPanel>().clearSelections();
                    image.color = inventoryColor;
                    buttonState = buttonStates.unselected;

                    var equipment = Brain.instance.player.inventory[buttonIndex].GetComponent<Equipment>();
                    if (equipment != null)
                    {
                        var slidePanel = equipment.slidePanel;
                        if (slidePanel != null) slidePanel.Close();
                    }

                    UMAMountObject mount = Brain.instance.player.GetComponent<UMAMountObject>();
                    mount.UnMountObject(Brain.instance.player.inventory[buttonIndex].name);
                    inventoryPanel.selected = -1;
                }
                else
                {
                    GameObject item = Brain.instance.player.inventory[buttonIndex];
                    Equipment equipment = item.GetComponent<Equipment>();

                    if (equipment != null)
                    {
                        equipment.Use(Brain.instance.player, inventoryPanel, buttonIndex);
                    }
                    else
                    {
                        transform.parent.GetComponent<InventoryPanel>().clearSelections();
                        image.color = Brain.instance.unusableColor;
                        buttonState = buttonStates.selected;
                        inventoryPanel.selected = buttonIndex;
                    }
                }
            }
            else if (eventData.button.ToString() == "Right")
            {
                transform.parent.GetComponent<InventoryPanel>().clearSelections();
                image.color = inventoryColor;
                buttonState = buttonStates.unselected;

                UMAMountObject mount = Brain.instance.player.GetComponent<UMAMountObject>();
                mount.UnMountObject(Brain.instance.player.inventory[buttonIndex].name);

                Brain.instance.player.Throws(buttonIndex);

                inventoryPanel.Unselect(buttonIndex);
                inventoryPanel.selected = -1;
                inventoryPanel?.remove(buttonIndex);
            }
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
