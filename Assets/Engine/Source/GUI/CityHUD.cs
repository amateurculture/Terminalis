using TMPro;
using UnityEngine;

public class CityHUD : MonoBehaviour
{
    public City city;
    public TextMeshProUGUI populationText;
    public TextMeshProUGUI wealthText;
    public TextMeshProUGUI productionText;
    public TextMeshProUGUI foodText;
    public TextMeshProUGUI widgetsText;
    public TextMeshProUGUI pollutionText;
    public TextMeshProUGUI taxRateText;
    public TextMeshProUGUI revenueText;

    public void UpdateStatistics()
    {
        if (city == null) return;
        populationText.text = city.AggregatePopulation().ToString("n0");
        wealthText.text = "$" + city.wealth.ToString("n0");
        productionText.text = "$" + city.AggregateRevenue().ToString("n0") + " / Month";
        foodText.text = city.AggregateFood().ToString("n0") + " Bushels / Month";
        widgetsText.text = city.AggregateWidgets().ToString("n0") + " Widgets / Month";
        pollutionText.text = city.AggregatePollution().ToString("n0") + " PPM";
        taxRateText.text = (city.taxRate * 100f).ToString("n0") + "%";
        revenueText.text = "$" + (city.AggregateRevenue() * city.taxRate).ToString("n0") + " / Month";
    }

    private void OnEnable()
    {
        UpdateStatistics();
    }
}
