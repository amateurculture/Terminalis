using UnityEngine;
using TMPro;

public class ValueText : MonoBehaviour
{
    public Agent agent;

    private void OnEnable()
    {
        var field = transform.GetComponent<TextMeshProUGUI>();

        if (name.Contains("Health")) field.text = agent.vitality.ToString("n0");
        if (name.Contains("Hunger")) field.text = agent.hunger.ToString("n0");
        if (name.Contains("Stress")) field.text = agent.stress.ToString("n0");
        if (name.Contains("Fatigue")) field.text = agent.fatigue.ToString("n0");
    }
}
