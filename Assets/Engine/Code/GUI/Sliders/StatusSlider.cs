using UnityEngine;
using UnityEngine.UI;

public class StatusSlider : MonoBehaviour
{
    protected GameObject agent;
    protected Agent script;
    protected Slider slider;

    protected void InitSlider()
    {
        agent = Brain.instance.player.gameObject;

        script = agent?.GetComponent<Agent>();
        if (script == null)
            script = agent?.GetComponent<Agent>();
        
        slider = transform.GetComponent<Slider>();
    }
}
