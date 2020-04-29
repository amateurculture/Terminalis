using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class Globals
{
    #region Initialization

#if ENVIRO_HD && ENVIRO_LW
    [HideInInspector]
    public EnviroCore enviro;
#endif

    public static Globals Instance { get { return Nested.instance; } }
    private class Nested { static Nested() { } internal static readonly Globals instance = new Globals(); }
    
    public Globals()
    { 
#if ENVIRO_HD && ENVIRO_LW
        enviro = GameObject.FindObjectOfType<EnviroSkyLite>();
        if (enviro == null)
            enviro = GameObject.FindObjectOfType<EnviroSky>();
#endif
    }
    
    #endregion

    #region Game Types

    // todo move through governments every hundred years in order (barring interference); Each change of governments has a period of revolution where the game rules change and all in-groups fight each other for a year.

    public enum Government
    {
        Anarchy, // is creative and has a traditional economy
        Monarchy, // is theocratic and has feudal economy
        Democracy, // is socialist and has capitalist economy
        Communism, // is socialist and has planned economy
        Fascism, // is angry and has a war economy
    }

    public enum AIType
    {
        Agressive,
        Defensive,
        Passive,
        Child,
        Shadow,
        Nemesis
    }

    public enum Sex
    {
        Male = 1 << 0,
        Female = 1 << 1
    }

    public enum Attraction
    {
        Male = 1 << 0,
        Female = 1 << 1
    }

    public enum Language
    {
        Local,
        Foreign,
        Sign,
        Gestural,
        Animal
    }

    public enum Cycle
    {
        None = 1 << 0,
        Estrous = 1 << 1,
        Monthly = 1 << 2
    }

    // In monogamy, pairs must own each other; in polygamy, chains are possible
    public enum Proclivity
    {
        Monogamous = 1 << 0,
        Polygamous = 1 << 1
    }

    public enum Diet
    {
        Herbavore = 1 << 0,
        Carnivore = 1 << 2,
        Omnivore = 1 << 3
    }

    public enum Foliage
    {
        Grass = 1 << 0,
        Bush = 1 << 1,
        Tree = 1 << 2,
        Ornamental = 1 << 3
    } 

    public enum Disease
    {
        Addiction = 1 << 0,
        Blindness = 1 << 1,
        Coma = 1 << 2,
        Deafness = 1 << 3,
        Dysphoria = 1 << 4,
        Dystrophy = 1 << 5,
        Hallucination = 1 << 6,
        Handicapped = 1 << 7,
        Infertility = 1 << 8,
        Mutism = 1 << 9,
        Paranoia = 1 << 10,
        Pneumonia = 1 << 11,
        Poisoning = 1 << 12,
        Sociopathy = 1 << 13
    }

    public enum BodyPart
    {
        Head = 1 << 0,
        Feet = 1 << 1,
        Chest = 1 << 2,
        Legs = 1 << 3,
        RightHand = 1 << 4,
        LeftHand = 1 << 5,
        Back = 1 << 6,
        Genitals = 1 << 7
    }

    public enum AITestingFlags
    {
        WanderRange = 1 << 0,
        AttackRange = 1 << 1,
        ChaseRange = 1 << 2,
        SightRange = 1 << 3,
        HearingRange = 1 << 4
    }

    public enum ShowStatusFlags
    {
        None = 1 << 0,
        Name = 1 << 1,
        Status = 1 << 2
    }

    public enum BuildingType
    {
        Residence,
        Restaurant
    }

    internal static float _ai_wait = 2f;

    #endregion

    #region Unused Types
    /*
    public enum Abilities
    {
        air = 1 << 0,
        fire = 1 << 1,
        water = 1 << 2,
        earth = 1 << 3,
        plant = 1 << 4,
        animal = 1 << 5,
        electricity = 1 << 6
    }

    public enum FirePower
    {
        light = 1 << 0,
        fireball = 1 << 1,
        firespray = 1 << 2,
        transmute = 1 << 3,
        shield = 1 << 4
    }

    public enum WaterPower
    {
        waterwalk = 1 << 0,
        talktofish = 1 << 1,
        wave = 1 << 2,
        tsunami = 1 << 3,
        shield = 1 << 4
    }

    public enum IcePower
    {
        absorb = 1 << 0,
        icicle = 1 << 1,
        shards = 1 << 2,
        skate = 1 << 3,
        shield = 1 << 4
    }

    public enum AirPower
    {
        floating = 1 << 0,
        flying = 1 << 1,
        gale = 1 << 2,
        tornado = 1 << 3,
        shield = 1 << 4
    }

    public enum EarthPower
    {
        ground = 1 << 0,
        tremor = 1 << 1,
        quake = 1 << 2,
        shield = 1 << 3
    }

    public enum AnimalPower
    {
        cat = 1 << 0,
        dog = 1 << 1,
        bird = 1 << 2,
        reptile = 1 << 3,
        bat = 1 << 4
    }

    public enum PlantPower
    {
        communicate = 1 << 0,
        vines = 1 << 1,
        command = 1 << 2,
        sentry = 1 << 3,
        shield = 1 << 4
    }

    public enum ElectricityPower
    {
        bolt = 1 << 0,
        chain = 1 << 1,
        teleport = 1 << 2,
        techcontrol = 1 << 3,
        shield = 1 << 4
    }

    public enum MentalPower
    {
        telepathy = 1 << 0,
        psyattack = 1 << 1,
        mindcontrol = 1 << 2,
        masscontrol = 1 << 3,
        shield = 1 << 4
    }

    public enum Month
    {
        January,
        February,
        March,
        April,
        May,
        June,
        July,
        August,
        September,
        October,
        November,
        December
    }

    public enum Day
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday
    }

    public enum season
    {
        spring,
        summer,
        fall,
        winter,
    }

    public enum action
    {
        move,
        eat,
        sleep,
        gossip,
        attack,
        seppuku,
        xoxo
    }

    public enum weapon
    {
        fist,
        foot,
        claw,
        bite,
        sword,
        knife,
        dagger,
        axe,
        gun,
        machinegun,
        artillery,
        rpg,
        laser,
        railgun
    }

    public enum career
    {
        none,
        business,
        athletic,
        police,
        clerk,
        librarian,
        scholar,
        landlord,
        entertainer,
        guard,
        barista,
        soldier,
        politician,
        retailer,
        criminal,
        mayor
    }

    public enum holidays
    {
        harvest,
        saturnalia
    }

    public enum SkinColor
    {
        Black,
        Brown,
        White
    }

    public enum Race
    {
        Native = 1 << 0,
        Black = 1 << 1,
        Latin = 1 << 2,
        Indian = 1 << 3,
        Asian = 1 << 4,
        White = 1 << 5
    }

    public enum credentials
    {
        GED,
        Bachelors,
        Masters,
        PHD
    }

    public enum Flag
    {
        Black = 1 << 0,
        Red = 1 << 1,
        Orange = 1 << 2,
        Yellow = 1 << 3,
        Green = 1 << 4,
        Blue = 1 << 5,
        Indigo = 1 << 6,
        Violet = 1 << 7,
        Rainbow = 1 << 8,
        White = 1 << 9
    }

    public enum itemType
    {
        sculpture,
        armor,
        weapon,
        projectile,
        trap,
        clothing,
        chest,
        building
    }

    // 0.5 = 10 mph
    // 1.0 = 20 mph
    // 1.5 = 30 mph
    // 2.0 = 40 mph

    public class SpeciesClass
    {
        public String name;
        public float walkingSpeed;
        public float runningSpeed;
        public float flyingSpeed;
        public float swimmingSpeed;

        public SpeciesClass(String nameParam, float walkingParam, float runningParam, float flyingParam, float swimmingParam)
        {
            name = nameParam;
            walkingSpeed = walkingParam;
            runningSpeed = runningParam;
            flyingSpeed = flyingParam;
            swimmingSpeed = swimmingParam;
        }
    }

    static public IDictionary<string, SpeciesClass> speciesSpeed = new Dictionary<string, SpeciesClass>()
    {
        {"Human", new SpeciesClass("Human", .5f, 1f, 10f, .5f) },
        {"Bear", new SpeciesClass("Bear", .5f, 1f, 10f, .5f) },
        {"Butterfly", new SpeciesClass("Butterfly", .5f, 2f, 0f, 0f) },
        {"Bull", new SpeciesClass("Bull", .5f, 1f, 10f, .5f) },
        {"Cow", new SpeciesClass("Cow", .5f, 2f, 0f, .5f) },
        {"Chicken1", new SpeciesClass("Chicken1", .25f, .5f, 1f, .25f) },
        {"Chicken2", new SpeciesClass("Chicken2", .25f, .5f, 1f, .25f) },
        {"Chicken3", new SpeciesClass("Chicken3", .25f, .5f, 1f, .25f) },
        {"Frog1", new SpeciesClass("Frog1", .25f, .5f, 10f, 1f) },
        {"Frog2", new SpeciesClass("Frog2", .25f, .5f, 10f, 1f) },
        {"Frog3", new SpeciesClass("Frog3", .25f, .5f, 10f, 1f) },
        {"Crab", new SpeciesClass("Crab", .25f, .5f, 0f, .25f) },
        {"Crocodile", new SpeciesClass("Crocodile", .25f, .5f, 0f, .5f) },
        {"Buck", new SpeciesClass("Buck", .5f, 1.5f, 0f, .5f) },
        {"Doe", new SpeciesClass("Doe", .5f, 1.5f, 0f, .5f) },
        {"Salamander", new SpeciesClass("Salamander", .25f, .5f, 0f, .5f) },
        {"Goat", new SpeciesClass("Goat", .5f, 2f, 0f, .5f) },
        {"Shark", new SpeciesClass("Shark", .25f, .5f, 0f, 1f) },
        {"Ibex", new SpeciesClass("Ibex", .5f, 2f, 0f, .5f) },
        {"Pig1", new SpeciesClass("Pig1", .25f, .5f, 0f, .5f) },
        {"Pig2", new SpeciesClass("Pig2", .25f, .5f, 0f, .5f) },
        {"Octopus", new SpeciesClass("Octopus", .25f, .5f, 0f, .5f) },
        {"Perch", new SpeciesClass("Perch", 0f, 0f, 0f, .5f) },
        {"Pike", new SpeciesClass("Pike", 0f, 0f, 0f, .5f) },
        {"Rat", new SpeciesClass("Rat", .5f, 1f, 0f, .5f) },
        {"Salmon1", new SpeciesClass("Salmon1", 0f, 0f, 0f, 1f) },
        {"Salmon2", new SpeciesClass("Salmon2", 0f, 0f, 0f, 1f) },
        {"Scorpion1", new SpeciesClass("Scorpion1", .25f, .5f, 0f, .25f) },
        {"Scorpion2", new SpeciesClass("Scorpion2", .25f, .5f, 0f, .25f) },
        {"Snail", new SpeciesClass("Snail", .25f, .5f, 0f, 0f) },
        {"Goose", new SpeciesClass("Goose", .5f, 1f, 1.5f, .5f) },
        {"Snake", new SpeciesClass("Snake", .25f, .5f, 0f, .25f) },
        {"Boar", new SpeciesClass("Boar", .25f, .5f, 0f, .25f) },
        {"Rabbit1", new SpeciesClass("Rabbit1", .5f, 1.5f, 0f, .5f) },
        {"Rabbit2", new SpeciesClass("Rabbit2", .5f, 1.5f, 0f, .5f) },
        {"Wolf", new SpeciesClass("Wolf", 5f, 5f, 0f, .5f) }
    };

    public enum Species
    {
        Human,
        Bear,
        Butterfly,
        Bull,
        Cow,
        Chicken1,
        Chicken2,
        Chicken3,
        Frog1,
        Frog2,
        Frog3,
        Crab,
        Crocodile,
        Buck,
        Doe,
        Salamander,
        Goat,
        Shark,
        Ibex,
        Pig1,
        Pig2,
        Octopus1,
        Octopus2,
        Perch,
        Pike,
        Rat,
        Salmon1,
        Salmon2,
        Scorpion1,
        Scorpion2,
        Snail,
        Goose,
        Snake,
        Boar,
        Rabbit1,
        Rabbit2,
        Wolf
    }

    public class Attribute
    {
        string fullname;
        float strengthBonus;
        float intelligenceBonus;
        float armor;
        float health;
        float healRate;

        bool canFly;
    }
    
    public enum AttackMethod
    {
        None,
        Melee,
        Ranged
    }
    */

    #endregion

    #region Helper Functions

    public int GetMonth(int currentDay)
    {
#if ENVIRO_HD && ENVIRO_LW
        int accumulatedDays = 0;
        for (int month = 1; month <= 12; month++)
        {
            accumulatedDays += DateTime.DaysInMonth((int)enviro.currentYear, month);
            if (currentDay <= accumulatedDays) return month;
        }
#endif
        return -1;
    }

    public List<GameObject> AgentListPlusPlayer()
    {
        List<GameObject> completeList = GameObject.FindGameObjectsWithTag("Agent").ToList();
        completeList.Add(GameObject.FindGameObjectWithTag("Player"));
        return completeList;
    }

    private static List<GameObject> GetObjectsInLayer(GameObject root, int layer)
    {
        List<GameObject> ret = new List<GameObject>();

        foreach (Transform t in root.transform.GetComponentsInChildren(typeof(GameObject), true))
            if (t.gameObject.layer == layer)
                ret.Add(t.gameObject);

        return ret;
    }

    #endregion
}
