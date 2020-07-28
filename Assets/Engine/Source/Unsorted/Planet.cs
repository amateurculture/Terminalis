using UnityEngine;

public class Planet : MonoBehaviour
{
    [Tooltip("Celcius - 50 is terminal for humans")] [Range(0,50)] public float averageTemperature = 14.6f;

    [Tooltip("PPM - 1200 is lethal tipping point")] [Range(0,1200)] public float averageCO2 = 407.4f;

    private void Start()
    {
        CurrentTemperature();
    }

    public float CurrentTemperature()
    {
        return averageTemperature = (5f * averageCO2) / 120f;
    }
}
