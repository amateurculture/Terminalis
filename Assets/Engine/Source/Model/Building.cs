using UnityEngine;
using System;

public class Building : Container
{
    public Globals.BuildingType buildingType;

    [Tooltip("Per hour")]
    public float salary;

    [Range(0, 23)]
    public int startHour;
    [Range(0, 23)]
    public int endHour;

    public int capacity = 5;
    private int currentCapacity = 0;

    [EnumFlags]
    public DayOfWeek employeeSchedule;

    private void Reset()
    {
        vitality = 10000f;
        salary = 10f;
        value = 100000f;
        startHour = 9;
        endHour = 17;
        employeeSchedule = DayOfWeek.Monday | DayOfWeek.Tuesday | DayOfWeek.Wednesday | DayOfWeek.Thursday | DayOfWeek.Friday;
    }

    private void OnTriggerEnter(Collider other)
    {
        Agent agent = other.GetComponent<Agent>();

        if (agent != null)
        {
            switch (buildingType)
            {
                case Globals.BuildingType.Restaurant:
                    agent.hunger = 0;
                    break;
                case Globals.BuildingType.Residence:
                    agent.fatigue = 0;
                    break;
                default:
                    break;
            }
            currentCapacity++;
        }
    }

    private void OnTriggerExit(Collider other) {
        
        if (other.tag == "Automata")
            currentCapacity--;

        other.GetComponent<Agent>()?.OutRange();
    }

    public bool atCapacity() {
        return currentCapacity >= capacity;
    }
    
    private void OnTriggerStay(Collider other)
    {
        other.GetComponent<Agent>()?.InRange(this);
    }
}
