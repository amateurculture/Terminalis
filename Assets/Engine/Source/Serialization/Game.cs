using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Game
{
    [SerializeField] public string sceneName;
    public S_Agent player;
    public List<S_Agent> agentList;
    public S_Camera camera;
    public bool isSavedGame;

    public void SerializeAgentList(List<Agent> list)
    {
        agentList = new List<S_Agent>();
        foreach (var obj in list)
            agentList.Add(new S_Agent(obj));
    }

    public void DeserializeAgentList(List<Agent> automataList)
    {
        automataList.Clear();

        foreach (var obj in agentList)
        {
            // TODO: Create automata here after load
        }
    }
}
