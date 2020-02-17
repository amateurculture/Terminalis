using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*** 
 * Class InventoryPanel
 * 
 * Developer: Fiona Schultz
 * Last modified: Oct-19-2019
 * 
 * This class connects the inventory with the player class.
 *
 */

public class InventoryObject
{
    public InventoryButton button;
    public TextMeshProUGUI textGUI;
    public TextMeshProUGUI number;
    public Image image;
    public GameObject reticle;
    public bool isSelected;
    public bool isEquipped;

    public InventoryObject(TextMeshProUGUI text, Image image, InventoryButton button)
    {
        this.textGUI = text;
        this.image = image;
        this.button = button;
    }
}

public class InventoryPanel : MonoBehaviour
{
    public List<InventoryObject> inventory;
    public Player player;
    public Agent agent;
    public int selected = -1;

    void Awake()
    {
        inventory = new List<InventoryObject>();
        agent = player.GetComponent<Agent>();
        //gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2((100 * agent.inventory.Length), 100);

        for (int i = 0; i < 10; i++)
            addPanel(i);
    }
    
    public void Unselect(int index)
    {
        if (index < 0)
            return;

        inventory[index].button.interactable = false;
        inventory[index].button.interactable = true;

        for (var i = 0; i < inventory.Count; i++)
            inventory[i].isSelected = false;
    }

    public void clearSelections()
    {
        for (var i = 0; i < inventory.Count; i++)
        {
            if (inventory[i].button.buttonState != InventoryButton.buttonStates.equipped)
            {
                inventory[i].isSelected = false;
                inventory[i].button.image.color = inventory[i].button.inventoryColor;
                inventory[i].button.buttonState = InventoryButton.buttonStates.unselected;
            }
        }
    }

    public void Select(int index)
    {
        for (var i = 0; i < inventory.Count; i ++)
            inventory[i].isSelected = false;

        if (index >= 0)
        {
            inventory[index].button.Select();
            inventory[index].isSelected = true;
            selected = index;
        }
    }
    
    public void Equip(Agent agent, int index)
    {
        inventory[index].isSelected = true;
        inventory[index].button.image.color = Brain.instance.equippedColor;
        inventory[index].button.buttonState = InventoryButton.buttonStates.equipped;
    }

    public void Unequip(Agent agent, int index)
    {
        inventory[index].isSelected = false;
        inventory[index].button.image.color = inventory[index].button.inventoryColor;
        inventory[index].button.buttonState = InventoryButton.buttonStates.equipped;
    }

    public void addPanel(int index)
    {
        int textIndex = index + 1;

        Transform item = transform.Find("Item" + (textIndex == 10 ? 0 : textIndex).ToString());
        TextMeshProUGUI text = (item.Find("Text")).GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI number = (item.Find("Number")).GetComponent<TextMeshProUGUI>();

        Image image = (item.Find("Image")).GetComponent<Image>();
        inventory.Add(new InventoryObject(text, image, item.GetComponent<InventoryButton>()));

        inventory[index].textGUI.text = "";
        inventory[index].image.gameObject.SetActive(false);

        number.text = (index+1).ToString();
        number.text = index > 9 ? "0" : number.text;

        if (textIndex > agent.inventory.Count)
            inventory[index].textGUI.transform.parent.gameObject.SetActive(false);
    }

    public void add(int index, Thing thing)
    {
        if (inventory == null)
            return;

        if (thing.sprite != null)
        {
            inventory[index].textGUI.gameObject.SetActive(false);
            inventory[index].image.gameObject.SetActive(true);
            inventory[index].image.enabled = true;
            inventory[index].image.sprite = thing.sprite;
        }
        else
        {
            inventory[index].image.gameObject.SetActive(false);
            inventory[index].textGUI.gameObject.SetActive(true);
            inventory[index].textGUI.text = thing.name;
        }
    }

    public void remove(int index)
    {
        inventory[index].textGUI.gameObject.SetActive(false);
        inventory[index].image.gameObject.SetActive(false);
        inventory[index].image.sprite = null;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) player.Selects(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) player.Selects(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) player.Selects(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) player.Selects(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) player.Selects(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) player.Selects(5);
        if (Input.GetKeyDown(KeyCode.Alpha7)) player.Selects(6);
        if (Input.GetKeyDown(KeyCode.Alpha8)) player.Selects(7);
        if (Input.GetKeyDown(KeyCode.Alpha9)) player.Selects(8);
        if (Input.GetKeyDown(KeyCode.Alpha0)) player.Selects(9);
        if (Input.GetKeyDown(KeyCode.Escape) && selected != -1) player.Selects(selected);
    }
}
