public class Consumable : Thing
{
    public float hunger;
    public float fatigue;

    [EnumFlags]
    public Globals.Disease causes;

    [EnumFlags]
    public Globals.Disease cures;
}
