using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusText : MonoBehaviour
{
    protected GameObject agent;
    protected Agent script;
    protected Slider slider;
    protected TextMeshPro statusText;
    
    private void Start()
    {
        agent = transform.parent.parent.gameObject;
        if (agent == Camera.main || agent.name.ToLower().Contains("camera"))
            agent = GameObject.FindGameObjectWithTag("Player");

        script = agent.GetComponent<Agent>();
        if (script == null)
            script = agent.GetComponent<Agent>();

        statusText = transform.GetComponent<TextMeshPro>();
    }

    private void Update()
    {
        if (statusText != null)
        {
            if (script.isWorking)
            {
                statusText.color = new Color(.25f, .75f, 1, 1);
                statusText.text = "AT WORK";
            }
            else if (script.hunger == 100)
            {
                statusText.color = Color.red;
                statusText.text = "HUNGRY";
            }
            else if (script.stress == 100)
            {
                statusText.color = new Color(1, 0, 1, 1);
                statusText.text = "INSANE";
            }
            else if (script.fatigue == 100)
            {
                statusText.color = Color.cyan;
                statusText.text = "TIRED";
            }
            else
            {
                statusText.text = "";
            }
        }
    }
}
