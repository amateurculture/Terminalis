using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI script to display the current time on a slider and allow the user to go back in time by 
/// moving the slider to an earlier value. User can slide gravity evolution back and forth up to the current
/// time. 
/// 
/// Note: This requires that the scene by all "on-rails". Every element in the scene must be on an 
/// OrbitUniversal or be a FixedObject.
/// 
/// The intent of the script is to demonostrate how scenes that are all "on-rails" can be moved back and forth 
/// in time. 
/// 
/// </summary>
public class TimeSlider : MonoBehaviour {

    [Tooltip("Time for the maximum value of the slider.")]
    [SerializeField]
    private float maxTime= 200f;

    [Tooltip("UI slider component to represent time. Max value will be set to maxTime.")]
    [SerializeField]
    private Slider slider = null;

    private GravityEngine ge;

    private float timeLast;

	// Use this for initialization
	void Start () {
        slider.maxValue = maxTime;
        slider.onValueChanged.AddListener(delegate { SliderChanged(); });
        ge = GravityEngine.Instance();
	}
	
	// Update is called once per frame
	void Update () {
        timeLast = ge.GetPhysicalTime();
        slider.value = timeLast;
	}

    /// <summary>
    /// Slider value is constantly changing due to the Update method. To distunguish changes due to user
    /// input we check the value versus the current time
    /// </summary>
    public void SliderChanged() {
        if (Mathf.Abs(slider.value - timeLast) > 1E-3)
            ge.SetPhysicalTime(slider.value);
    }
}
