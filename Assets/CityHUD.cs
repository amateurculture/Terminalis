using TMPro;
using UnityEngine;

public class CityHUD : MonoBehaviour
{
    public City city;
    public TextMeshProUGUI populationText;
    public TextMeshProUGUI wealthText;
    public TextMeshProUGUI foodText;
    public TextMeshProUGUI widgetsText;
    public TextMeshProUGUI pollutionText;

    public void UpdateStatistics()
    {
        populationText.text = city.AggregatePopulation().ToString("n0");
        wealthText.text = "$" + city.AggregateWealth().ToString("n0");
        foodText.text = city.AggregateFood().ToString("n0") + " Bushels / Month";
        widgetsText.text = city.AggregateWidgets().ToString("n0") + " Widgets / Month";
        pollutionText.text = city.AggregatePollution().ToString("n0") + " PPM";
    }

    private void OnEnable()
    {
        UpdateStatistics();
    }

    private void Update()
    {
        if (Time.frameCount % 120 == 0) UpdateStatistics();
    }
}
