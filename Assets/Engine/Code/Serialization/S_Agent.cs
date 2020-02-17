using UnityEngine;
using System.Runtime.Serialization;
using System;

[Serializable]
public class S_Agent
{
    public string name;
    public S_Vector3 position;
    public S_Vector3 eulerRotation;
    public float currency = 100;
    public Globals.Sex sex = Globals.Sex.Female;
    public Globals.Gender gender = Globals.Gender.Female;
    public Globals.Attraction attraction = Globals.Attraction.Men;
    public float hunger = 0f;
    public float fatigue = 0f;
    public float anxiety = 0f;
    public float strength = 50f;

    public S_Agent(Agent agent)
    {
        name = agent.name;
        position = new S_Vector3(agent.transform.position);
        eulerRotation = new S_Vector3(agent.transform.eulerAngles);
        currency = agent.currency;
        sex = agent.sex;
        gender = agent.gender;
        attraction = agent.attraction;
        hunger = agent.hunger;
        fatigue = agent.fatigue;
        anxiety = agent.anxiety;
        strength = agent.strength;
    }

    public void Deserialize(Agent agent)
    {
        agent.GetComponent<CharacterController>().enabled = false;
        agent.name = name;
        agent.transform.position = new Vector3(position.x, position.y, position.z);
        agent.transform.eulerAngles = new Vector3(eulerRotation.x, eulerRotation.y, eulerRotation.z);
        agent.currency = currency;
        agent.sex = sex;
        agent.gender = gender;
        agent.attraction = attraction;
        agent.hunger = hunger;
        agent.fatigue = fatigue;
        agent.anxiety = anxiety;
        agent.strength = strength;
        
        // TODO: Complete serialization of Agent

        agent.GetComponent<CharacterController>().enabled = true;
    }
}
