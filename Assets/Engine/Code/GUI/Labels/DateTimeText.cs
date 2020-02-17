using UnityEngine;
using System;
using TMPro;

public class DateTimeText : MonoBehaviour
{
    private TextMeshProUGUI statusText;
    
    void Start()
    {
        statusText = transform.GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
#if ENVIRO_HD && ENVIRO_LW
        if (Globals.Instance.enviro != null)
        {
            DateTime currentDate = new DateTime((int)Globals.Instance.enviro.currentYear, Globals.Instance.GetMonth((int)Globals.Instance.enviro.currentDay), (int)Globals.Instance.enviro.currentDay);
            statusText.text = currentDate.ToLongDateString() + " " + Globals.Instance.enviro.GetTimeStringWithSeconds();
        } else
            statusText.text = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();
#else
        statusText.text = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();
#endif
    }
}
