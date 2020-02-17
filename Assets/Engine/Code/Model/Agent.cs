using UnityEngine;
using AC_System;
using System;
using System.Collections.Generic;

public class Agent : Container
{
    #region public variables

    [Header("Details")]
    public float currency = 100;
    public Globals.Sex sex = Globals.Sex.Female;
    public Globals.Gender gender = Globals.Gender.Female;
    public Globals.Attraction attraction = Globals.Attraction.Men;

    [EnumFlags] public Globals.Status status;

    [Range(0f, 100f)] public float hunger = 0f;
    [Range(0f, 100f)] public float fatigue = 0f;
    [Range(0f, 100f)] public float anxiety = 0f;

    [Tooltip(("in kilos carryable"))]
    public float strength = 50f;
    
    [Header("Destinations")]
    [HideInInspector] public Building home;
    [HideInInspector] public Building work;
    
    #endregion

    #region protected variables

    [HideInInspector] public PlayerController playerController;
    protected Brain brain;
    
    [HideInInspector] public List<Meme> memory;
    [HideInInspector] public bool isWorking = false;
    [HideInInspector] public bool isEating = false;
    [HideInInspector] public bool isSleeping = false;

    public float sightRange = 25f;
    public float hearingRange = 10f;
    public float fov = 120f;

    #endregion

    #region Initialization

    public void InitializeAgent()
    {
        brain = GameObject.FindGameObjectWithTag("GameController")?.GetComponent<Brain>();
        memory = new List<Meme>();
    }

    #endregion

    #region Statistics Handling

    public void UpdateStatistics()
    {
        // TODO: calculate redution of stats based on enviro time

        hunger += .01f;
        fatigue += .01f;

        hunger = (hunger > 100) ? 100 : hunger;
        fatigue = (fatigue > 100) ? 100 : fatigue;
        health = (health > 100f) ? 100f : health;

        if (hunger == 100) health -= .01f;
        if (fatigue == 100) anxiety += .01f;
    }

    #endregion

    virtual public void InRange(Thing thing)
    {
    }

    virtual public void OutRange()
    {
    }

    public float getEncumberance()
    {
        float totalMass = 0;

        foreach (GameObject i in inventory)
        {
            if (i != null)
            {
                Rigidbody rigidbody = i.GetComponent<Rigidbody>();
                totalMass += (float)rigidbody?.mass;
            }
        }
        return totalMass;
    }
    
    public void Uses(int index, InventoryPanel inventoryPanel = null)
    {
        if (inventory[index] != null) {
            Thing thing = inventory[index].GetComponent<Thing>();
            this.health += thing.health;
            Destroy(thing.gameObject);
            inventory[index] = null;
            inventoryPanel?.remove(index);
        }
    }

    virtual public void Takes(Thing thing, InventoryPanel inventoryPanel = null)
    {
        if (thing.GetType() == typeof(Building))
        {
            if (thing.owner != this.gameObject)
            {
                thing.owner = this.gameObject;
                currency -= thing.price;
            }
            else
            {
                thing.owner = null;
                currency += thing.price;
            }
        }
        else
        {
            int index = 0;

            // Recycle and increment currency
            if (thing.price < 0)
            {
                this.currency -= thing.price;
                GameObject clone = thing.gameObject;
                brain.TrackableEvent(new Meme(gameObject, Meme.Action.took, clone));
                Destroy(thing.gameObject);
                return;
            }

            // Move non-currency to inventory
            foreach (var obj in inventory)
            {
                if (obj == null)
                {
                    thing.transform.parent = transform;
                    thing.gameObject.SetActive(false);
                    inventory[index] = thing.gameObject;
                    inventoryPanel.add(index, thing);
                    break;
                }
                index++;
            }
            brain.TrackableEvent(new Meme(gameObject, Meme.Action.took, thing.gameObject));
        }
    }

    public void AddMemory(Meme meme)
    {
        memory.Add(meme);   
    }

    public void Throws(int index)
    {
        gameObject.SetActive(true);
        
        if (index < 0 || index >= inventory.Count)
            return;

        if (inventory[index] != null)
        {
            Thing thing = inventory[index].GetComponent<Thing>();
            thing.transform.parent = transform.parent;

            // TODO: item should be thrown in front of agent, not at feet... maybe?
            thing.transform.position = transform.position;
            thing.gameObject.SetActive(true);
            inventory[index] = null;
        }
    }

    private void Reset()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.mass = 80f;
        rb.drag = 0.5f;
        rb.angularDrag = 0.5f;
        rb.freezeRotation = true;
        health = 100;
        price = 1000;
        currency = 100;
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    public void UpdateAgent(Automata automata = null)
    {
        UpdateStatistics();

        if (automata != null) // automata.GetType() == typeof(Automata))
            automata.UpdatePosition();
    }

    public void Update()
    {
        UpdateAgent();
    }
}
