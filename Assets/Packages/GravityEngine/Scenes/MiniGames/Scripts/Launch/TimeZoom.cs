using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeZoom : MonoBehaviour {

	
	/// <summary>
    /// User can press 1-5 to set a run-time evolution speed
    /// </summary>
    
    // indexed by key press 1-5
    float[] timeZoomForKey = new float[] { 1f, 1f, 2f, 5f, 10f, 20f, 100f, 200f };

    public int initialZoomKey = 1;

    //! Optional UI Text element to display time and zoom factor
    public Text timeText; 

    void Start() {
        GravityEngine.Instance().SetTimeZoom(timeZoomForKey[initialZoomKey]);
    }

    void Update () {
        int keyPressed = -1;
        for (int i = 1; i < timeZoomForKey.Length; i++)
        {
            if (Input.GetKeyUp(KeyCode.Alpha0 + i) || Input.GetKeyUp(KeyCode.Keypad0 + i))
            {
                keyPressed = i;
                break;
            }
        }
        if (keyPressed >= 0)
        {
            GravityEngine.Instance().SetTimeZoom(timeZoomForKey[keyPressed]);
        }
        // update displayed time (if present)
        if (timeText != null) {
            timeText.text = string.Format("{0} (x{1})", 
                GravityEngine.Instance().GetScaledTimeFormatted(), 
                GravityEngine.Instance().GetTimeZoom());
        }
    }
}
