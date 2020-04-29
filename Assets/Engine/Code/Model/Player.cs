using UnityEngine;
using TMPro;
using System.Linq;

[RequireComponent(typeof(Agent))]

public class Player : MonoBehaviour
{
    Thing thingInRange = null;
    public Transform hud = null;
    Transform events = null;
    Transform message = null;
    Transform inventoryPanel = null;
    TextMeshProUGUI hudText = null;
    Agent agent = null;
    protected Brain brain;
    public GameObject cameraRig;
    public GameObject reticle;
    int[] select;
    InventoryPanel panel;

    private void Start()
    {
        this.transform.tag = "Player";

        agent = GetComponent<Agent>();
        events = Camera.main.transform.Find("Events");

        if (events != null)
        {
            message = events.Find("Message");
            hudText = message.Find("Text").GetComponent<TextMeshProUGUI>();
            if (hudText != null)
                hudText.text = "";
        }

        if (hud != null)
            inventoryPanel = hud.Find("Inventory");

        brain = GameObject.FindGameObjectWithTag("GameController")?.GetComponent<Brain>();

        if (message != null)
            message.gameObject.SetActive(false);

        if (panel != null)
        {
            panel = inventoryPanel.GetComponent<InventoryPanel>();
            panel.selected = -1;
        }

        if (reticle != null)
            reticle.SetActive(false);
    }

    public void HudMessage(string text, Thing itemToPickup)
    {
        events.transform.gameObject.SetActive(true);
        message.gameObject.SetActive(true);

        if (hudText != null)
            hudText.text = text;

        if (hudText.text == "")
            message.gameObject.SetActive(false);

        thingInRange = itemToPickup;
    }

    public void InRange(Thing thing)
    {
        //base.InRange(thing);
        //Rigidbody rigidbody = thing.transform.GetComponent<Rigidbody>();

        if (thing != null) 
        {
            if (thing.value < 0) // you can always pick up money
                HudMessage("Press E to pick up " + thing.transform.name, thing);

            else if (thing.owner == gameObject) // I own this
            {
                if (thing.GetType() == typeof(Building))
                    HudMessage(thing.transform.name + " is worth $" + thing.value + "\nPress E to sell", thing);
                
                else if (agent.inventory.Count(s => s == null) > 0)
                    HudMessage("Press E to pick up " + thing.transform.name, thing);
                
                else
                    HudMessage("Inventory is full", null);
            }

            else if (thing.owner == null) // nobody owns this
            {
                if (thing.GetType() == typeof(Building))
                {
                    if (agent.value >= thing.value)
                        HudMessage(thing.transform.name + " costs $" + thing.value + "\nPress E to purchase", thing);

                    else
                        HudMessage(thing.transform.name + " costs $" + thing.value + "\nYou are too poor to buy this", null);
                }
                else
                {
                    if (agent.inventory.Count(s => s == null) > 0)
                        HudMessage("Press E to pick up " + thing.transform.name, thing);

                    else
                        HudMessage("Inventory is full", null);
                }
            }
            else // somebody owns this
            {
                if (thing.GetType() == typeof(Building))
                {
                    if (agent.value >= thing.value)
                        HudMessage(thing.transform.name + " is owned by " + thing.owner.name + " and costs $" + thing.value + "\nPress E to purchase", thing);

                    else
                        HudMessage(thing.transform.name + " costs $" + thing.value + "\nYou are too poor to buy this", null);
                }

                else
                {
                    if (agent.inventory.Count(s => s == null) == 0)
                        HudMessage("inventory is full", null);

                    else if (agent.value >= thing.value)
                        HudMessage(thing.transform.name + " is owned by " + thing.owner.name + " and costs $" + thing.value + "\nPress E to purchase", thing);

                    else
                        HudMessage(thing.transform.name + " is too expensive \nPress E to steal", thing);
                }
            } 

            /*
            else if (rigidbody == null)
                HudMessage(thing.transform.name + " cannot be lifted", null);

            else if (inventory.Count(s => s == null) == 0)
                HudMessage(thing.transform.name + " inventory full", null);

            else if (thing.owner != null && thing.owner != gameObject)
                HudMessage("*** press E to steal " + thing.transform.name + " ***", thing);

            else if (strength > rigidbody.mass && getEncumberance() + rigidbody.mass <= strength)
                HudMessage("press E to pick up " + thing.transform.name, thing);

            else
                HudMessage(thing.transform.name + " is too heavy for you to pick up", null);
                */
        }
    }

    public void Selects(int index)
    {
        if (agent.inventory[index] != null)
        {
            gameObject.SetActive(true);

            if (index == panel.selected)
            {
                reticle.SetActive(false);
                panel.Unselect(index);
                panel.selected = -1;
                inventoryPanel.GetComponent<InventoryPanel>().inventory[index].button.interactable = true;

                /*
                Thing thing = agent.inventory[index].GetComponent<Thing>();
                thing.transform.parent = transform;
                thing.gameObject.SetActive(false);
                agent.inventory[index] = thing.gameObject;
                */
            }
            else
            { 
                panel.Select(index);
                panel.selected = index;

                /*
                reticle.SetActive(true);
                inventoryPanel.GetComponent<InventoryPanel>().inventory[index].button.interactable = false;
                
                Thing thing = agent.inventory[index].GetComponent<Thing>();
                thing.transform.parent = reticle.transform;
                thing.transform.localPosition = Vector3.zero;
                thing.gameObject.SetActive(true);
                */
            }
        }
    }
    
    public void Uses(int index)
    {
        if (index < 0 || index >= agent.inventory.Count())
            return;

        if (agent.inventory[index] != null)
        {
            Thing thing = agent.inventory[index].GetComponent<Thing>();
            thing.Use(agent, panel, index);
            OutRange();
        }
    }

    public void Takes(Thing thing, InventoryPanel inventoryPanel)
    {
        HudMessage("", null);

        if (thing.GetType() == typeof(Building))
        {
            if (thing.owner != this.gameObject)
            {
                thing.owner = this.gameObject;
                agent.value -= thing.value;
            }
            else
            {
                thing.owner = null;
                agent.value += thing.value;
            }
        }
        else
        {
            int index = 0;

            // Recycle and increment currency
            if (thing.value < 0)
            {
                agent.value -= thing.value;
                GameObject clone = thing.gameObject;
                //brain.TrackableEvent(new Meme(gameObject, Meme.Action.took, clone));
                Destroy(thing.gameObject);
                return;
            }

            // Move non-currency to inventory
            foreach (var obj in agent.inventory)
            {
                if (obj == null)
                {
                    thing.transform.parent = transform;
                    thing.gameObject.SetActive(false);
                    agent.inventory[index] = thing.gameObject;
                    inventoryPanel.add(index, thing);
                    break;
                }
                index++;
            }
            //brain.TrackableEvent(new Meme(gameObject, Meme.Action.took, thing.gameObject));
        }
    }

    public void OutRange()
    {
        events.transform.gameObject.SetActive(false);
        HudMessage("", null);
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (other.transform.name == "Terrain" || other.transform.name == "You")
            return;
        else
        {
            Thing thing = other.transform.GetComponent<Thing>();
            if (thing != null)
                InRange(thing);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (events != null)
            events.transform.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (thingInRange != null)
            {
                Takes(thingInRange, inventoryPanel.GetComponent<InventoryPanel>());
                thingInRange = null;

                if (hudText != null)
                    hudText.text = "";
            }
            else if (panel != null && panel.selected >= 0)
                Uses(panel.selected);
        }
    }
}
