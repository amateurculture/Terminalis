using UnityEngine;

public class Meme
{
    public enum Action
    {
        spoke,
        murdered,
        took,
        attacked
    }
    public Action action;

    public GameObject subject;
    public GameObject dobj;
    public GameObject iobj;

    public Meme(GameObject subject, Action action, GameObject dobj = null, GameObject iobj = null)
    {
        this.subject = subject;
        this.action = action;
        this.dobj = dobj;
        this.iobj = iobj;
    }
    
    // todo add ability to transmit cultural information; agents will be randomized with an ethos and will have to navigate in-group out-group behavior
}













    /*
    public enum activity
    {
        speak,
        murder,
        steal,
        kiss,
        sex,
        seen,
        attack,
        hug
    }
    
    public GameObject subject;
    public GameObject obj;
    public GameObject dobj;
    public GameObject location;
    public activity verb;
    
    private string constructPhrase(string verb, string preposition)
    {
        string sentence = subject.name + " " + verb + " " + obj.name;
        if (dobj != null && !dobj.name.Equals(""))
            sentence += " " + preposition + " " + dobj;
        if (location != null && !location.name.Equals(""))
            sentence += " at the " + location;
        return sentence;
    }

    public override string ToString()
    {
        string sentence = "";
        switch (verb)
        {
            case activity.murder: sentence = constructPhrase("murdered", "with the"); break;
            case activity.steal: sentence = constructPhrase("stole the", "from"); break;
            case activity.kiss: sentence = constructPhrase("kissed", null); break;
            case activity.sex: sentence = constructPhrase("had sex with", "using a"); break;
            case activity.speak: sentence = constructPhrase("spoke with", "about"); break;
            case activity.seen: sentence = constructPhrase("seen with", null); break;
            case activity.hug: sentence = constructPhrase("hugged", null); break;
            case activity.attack: sentence = constructPhrase("attacked", "with the"); break;
            default: break;
        }
        return sentence;
    }
    */
