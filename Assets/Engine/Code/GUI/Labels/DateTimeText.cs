using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

public class DateTimeText : MonoBehaviour
{
    public TimeController Planet;
    private Text statusText;
    private TextMeshProUGUI statusUGUIText;
    private string textString;
    private string ampmString;
    private string hourString;

    void Start()
    {
        statusUGUIText = transform.GetComponent<TextMeshProUGUI>();
        statusText = transform.GetComponent<Text>();
    }

    void Update()
    {
        if (Time.frameCount % 2 == 0)
        {
            if (Planet != null)
            {
                ampmString = (Planet.hour > 12) ? "pm" : "am";
                hourString = "" + ((ampmString == "am") ? ((int)Planet.hour).ToString("D2") : (((int)Planet.hour - 12)).ToString("D2"));
                textString = "Day " + Planet.day + ", " + hourString + ":" + ((int)Planet.minute).ToString("D2") + " " + ampmString;
            }
            else textString = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();
            
            if (statusUGUIText != null) statusUGUIText.text = textString;

            if (statusText != null) statusText.text = textString;
        }
    }
}
