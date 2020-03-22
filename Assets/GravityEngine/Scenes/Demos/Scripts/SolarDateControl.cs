using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SolarDateControl : MonoBehaviour {

    public InputField yearText;
    public InputField monthText;
    public InputField dayText;

    public Text currentTime;

    public SolarSystem solarSystem;

    private bool clearTrail;
    private int clearTrailDelay; 

    void Start() {
        yearText.text = "2016";
        monthText.text = "6";
        dayText.text = "2";
    }

    public void SetPhysTime(string dummy) {
        int year = int.Parse(yearText.text);
        int month = int.Parse(monthText.text);
        int day = int.Parse(dayText.text);
        solarSystem.SetTime(year, month, day, 0);
        clearTrail = true;
        clearTrailDelay = 3;
    }

    private void ResetTrails() {
        foreach (TrailRenderer tr in (TrailRenderer[])Object.FindObjectsOfType(typeof(TrailRenderer))) {
            tr.Clear();
        }
    }

    void Update() {
        // space toggles evolution
        if (Input.GetKeyDown(KeyCode.Space)) {
            GravityEngine.Instance().SetEvolve(!GravityEngine.Instance().GetEvolve());
        }
        System.DateTime newTime = SolarUtils.DateForEpoch(solarSystem.GetStartEpochTime());
        newTime += GravityScaler.GetTimeSpan(GravityEngine.Instance().GetPhysicalTimeDouble(), GravityScaler.Units.SOLAR);
        currentTime.text =  newTime.ToString("yyyy:MM:dd"); // 24h format

        // ICK! Need to wait until GE moves these objects before can clear the trail
        if (clearTrail) {
            if (clearTrailDelay-- <= 0) {
                ResetTrails();
                clearTrail = false;
            }
        }
    }
}
