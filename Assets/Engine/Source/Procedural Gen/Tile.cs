using UnityEngine;

public class Tile : MonoBehaviour
{
    // Each family can have any pairing (or not) of 2 parents and up to 3 children; each head of household family marker represents between 100-500 people

    [SerializeField] [Tooltip("100's of people")] public int population;
    [SerializeField] [Tooltip("100's of people")] public int vacancies;
    [SerializeField] [Tooltip("100's of people per month")] public int immigration;

    [Space(10)]
    [SerializeField] [Tooltip("Bushels per month")] public int foodProduction;
    [SerializeField] public float foodPrice;
    [SerializeField] [Tooltip("Widgets per month")] public int widgetProduction;
    [SerializeField] public float widgetPrice;

    [Space(10)]
    [SerializeField] 
    [Tooltip("AQI Index -- 0-50 Good : 51-100 Moderate : 101-150 Sensitive : 151-200 Unhealthy : 201-300 Very Unhealthy : 301+ Hazardous")] 
    [Range(0, 500)] public int pollution;

    [Space(10)]
    public Globals.Government philosophy;
}
