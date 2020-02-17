public class Consumable : Thing
{
    public float hunger;
    public float fatigue;

    [EnumFlags]
    public Globals.Status causes;

    [EnumFlags]
    public Globals.Status cures;
}
