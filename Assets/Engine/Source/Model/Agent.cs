using UnityEngine;
using AC_System;
using System.Collections.Generic;
using Opsive.UltimateCharacterController.Character;
using System.Collections;

public class Agent : Container
{
    #region public variables

    [Header("Details")]
    [EnumFlags] public Globals.Sex sex = Globals.Sex.Female;
    [EnumFlags] public Globals.Sex expression = Globals.Sex.Female;
    [EnumFlags] public Globals.Sex attraction = Globals.Sex.Male;

    [EnumFlags] public Globals.Disease disease;

    //[Range(0f, 100f)] public float thirst = 0f;
    [Range(0f, 100f)] public float hunger = 0f;
    [Range(0f, 100f)] public float fatigue = 0f;
    [Range(0f, 100f)] public float stress = 0f;
    
    /// Stress causes:
    /// . increase in lust
    /// . increase in insanity

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
    #endregion

    #region Initialization

    public void InitializeAgent()
    {
        brain = GameObject.FindGameObjectWithTag("GameController")?.GetComponent<Brain>();
        memory = new List<Meme>();
    }

    IEnumerator refreshLocomotionController() 
    {
        GetComponent<UltimateCharacterLocomotion>().enabled = false;    
        yield return new WaitForSeconds(.25f);
        GetComponent<UltimateCharacterLocomotion>().enabled = true;
    }

    private void OnEnable()
    {
        StartCoroutine(refreshLocomotionController());
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
        base.vitality = (base.vitality > 100f) ? 100f : base.vitality;

        if (hunger == 100) base.vitality -= .01f;
        if (fatigue == 100) stress += .01f;
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
            this.vitality += thing.vitality;
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
                value -= thing.value;
            }
            else
            {
                thing.owner = null;
                value += thing.value;
            }
        }
        else
        {
            int index = 0;

            // Recycle and increment currency
            if (thing.value < 0)
            {
                this.value -= thing.value;
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
        base.vitality = 100;
        value = 1000;
        value = 100;
        //gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
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

/// Resources that can be monopolized (turned into businesses):
/// . Wood
/// . Stone
/// . Metal
/// . Gold
/// . Silver
/// . Copper
/// . Gems
/// . Radioactive
/// . Rare 
/// . ???
/// 

// Vitality is a value used to determine things like: 
/// . encumberance (how much can be carried in kilos)
/// . resistance to insanity
/// . resistance to poison
/// . introversion/extraversion
/// . jumping height
/// . strength
/// . beauty
/// . health
/// . contentment
/// 

// Stress turns people into shadows. Prior to disruption, they exist in harmony with their environment.

// Money can buy relief from stress, but also attracts shadows. 

/// Who are all the potential agents? (IRL 20:80 predator to prey ratio)
/// . God/Queen/King to be i.e. "you"
/// . Proletariate/Fans/"Prey" (are always trying to catch predators, but typically work for them instead, cannot see shadows)
/// . Shadows/Aristocracy/"Predators" (can make other shadows, but has discovery problems)
/// . Nemesis (the opposite gender bully from childhood, the "AI" in avatar form, nearly impossible to kill, also hides)
/// . Ghosts (the residual ideas that once represented people, they are only visible to shadows, but they hold wisdom)
/// 

/// Once a shadow is made, they can always return to that state. Most agents are terrified of that line and will stay well away from it. However once a regular citizen is "turned", they will always be able to go back and forth at will and will use that ability more and more as they use their "shadow powers". Other people can try and heal them, but only healthy societies can do that. Unhealthy societies must unfortunately cope with the reality of an ever increasing number of shadows. 

/// What does a Shadow do?
/// . is always starving
/// . is always sad/angry and responds as such (depends on introversion/extraversion check)
/// . is avoided due to making people nearby stressed
/// . moves at half speed
/// . has no confidence (love is impossible)
/// . can work in vice *
/// . can see other shadows
/// 

/// * Being revealed to be a shadow while at work has disasterous consequences. Most agents will rely on stimulants of some sort to cope instead of being discovered (if they can afford it). Once "outted", the agent is immediately fired (unless they have leverage on their boss.) Jobs in the normal world will be problematic for a time, but new underground economy jobs will now be unlocked. 

/// The only variable checked is stress, but when high enough, an analysis of existing issues occurs. This should hopefully limit the number of checks required.

/// In real life male shadows would seek to limit the number of female shadows in order to control access to plentiful sexual prey. Female shadows would in turn try and turn as many other women to shadows as possible to make women aware of the plan on men. This will of course be disasterous for society as male and female shadows would now be trying to undo each other. In the case of male shadows, they will be constantly trying to give women self esteam while female shadows would be trying to make the women feel worse so they see the world the same way they do. The end result of all of this is insanity. 
/// 
/// Cisgendered agents may make all of the opposite gender expression appear as shadows. The exception are intersex/transgender people who can see both shadows correctly. However no one will believe them about the opposite due to lack of frame of reference i.e. context.
/// 

/// The reality is the shadows either need to be killed or contained. Killing requires being done in secret as otherwise it will potentially martyr the person for all those closest to them turning them into immediate enemies (vendetta). Containing is much more difficult. Making drugs freely available is one way, albiet with consequences. Or better systems, or no systems? So hard to say. 
/// 


// As stress goes up, lust decreases, anger goes up
// As money goes up lust increases, anger goes down (but requires more and more money to feed it or anger will go up again)

// money makes you a target, which also increases stress, with increases pressure for more money to counteract it

// This competition between stress and money is what drives income inequality

// Wellness as a corrective does not seem to be working... this is the epicurean solution. 

// The problem is trying to create utopia makes it worse for everyone else. How can I demonstrate this?
// The invention of the clock was an attempt at regulating society, but caused more problems than it solved

// The game starts in a primordial but balanced state. It is also self corrective... to a point. Discoveries lead
// to disruption which allows you to organize society in different ways.

// Research, once unlocked, cannot be removed
// Certain core technologies would need to be researched:

// - The Clock (Unlocks schedules)
// - The Microscope (Unlocks medicine; this costs more and more as time goes on as each discovery requires more resources than the last until immortality is reached, which requires bankrupting the entire planet.)
// - The Quill (Unlocks law; this constrains behavior more and more until the system breaks and turns everyone into shadows, as each law passed affects behavior of all citizens and induces stress. It is also the primary way you can attemp to "rectify" the flaws in the system.)
// - Fiat Money (Unlocks business; this allows the creation of trades centered around certain resources, it also creates the economy. Without the clock though, the flow of business is slower... but also less stressful. Without money, barter is the only rule, and to the best con artist go the spoils.)
// - Culture (Creates moods. There are times where being sad may be good, don't be afraid of expressing who you are! affects those around you. Actually you can just pick a "style" and your whole world changes... but these must be unlocked or learned through observation. If you go against the dominant style though, you will be punished, so be warned. If you survive though, the world is yours.)

// DRUGS
// Drugs are basically just ways to shift perception. What that means in the conext of the game is that certain drugs "reveal" certain aspets of reality that may be imperceptible. Or in otherwords, like the graph view in sim city. For example acid might reveal "spirit" representations of people roughly cooresponding to an animal or demonic temperament. They may also attack you in this state, but in actuality it will be you attacking them. If you do nothing, nothing bad will happen to you, but it could be scary. In a better world perhaps all you would see are angels, but... yeah that's not our world. Meth for example allows you to see the shadows. Marijuana reduces stress and lets you control the passage of time. etc. 




//pride, greed, envy, sloth

// God sends messages through whispers, as these propogate and ripple back and forth, that is karma
// what happens if you hear God? 
