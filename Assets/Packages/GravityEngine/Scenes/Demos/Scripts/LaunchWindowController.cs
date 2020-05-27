using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller for the SolarSystemLaunchWindow scene. 
/// 
/// The controller allows the selection of a destination planet and the computes the launch window times. 
/// The transfer time is for the most efficient (Hohmann) transfer. 
/// 
/// The destinations are determined by checking for orbit components in the scene that are not the 
/// fromNbody. 
/// 
/// The launch windows here assume a direct transfer. The times do not include the effects of leaving
/// Earth orbit and arriving at the destination (they assume the Earth and destination planet are massless). 
/// The launch window determined is a reasonable start, but needs a patched conic or three body trajectory 
/// design to be more accurate. 
/// 
/// </summary>
public class LaunchWindowController : MonoBehaviour {

    [SerializeField]
    [Tooltip("Planet to calculate transfers from")]
    private NBody fromNbody = null;

    [SerializeField]
    [Tooltip("The Sun")]
    private NBody centerNbody = null;

    [SerializeField]
    [Tooltip("UI element to list destination planets")]
    private Dropdown destDropdown = null;

    [SerializeField]
    [Tooltip("The number of launch windows to determine for a compute request.")]
    private int numWindows = 2;

    private OrbitEllipse[] destinations;

    [SerializeField]
    [Tooltip("UI element to list launch tranfer windows")]
    private Dropdown launchTimes = null;


    void Start () {

        destinations = (OrbitEllipse[])Object.FindObjectsOfType(typeof(OrbitEllipse));
        FillDropdown();

    }

    private void FillDropdown() {
        // fill dropdown with destinations
        destDropdown.ClearOptions();
        List<Dropdown.OptionData> items = new List<Dropdown.OptionData>();
        foreach(OrbitEllipse oe in destinations) {
            if (oe.gameObject != fromNbody.gameObject) {
                Dropdown.OptionData item = new Dropdown.OptionData();
                item.text = oe.gameObject.name;
                items.Add(item);
            }
        }
        destDropdown.AddOptions(items);

    }

    // Update is called once per frame
    void Update () {
		
	}

    /// <summary>
    /// Callback to compute the launch transfer times and display them in the scene. 
    /// </summary>
    public void Compute() {
        int itemNo = destDropdown.value;
        NBody toNbody = destinations[itemNo].GetComponent<NBody>();
        OrbitData fromOrbit = new OrbitData();
        fromOrbit.SetOrbit(fromNbody, centerNbody);
        OrbitData toOrbit = new OrbitData();
        toOrbit.SetOrbit(toNbody, centerNbody);

        HohmannXfer hohmannXfer = new HohmannXfer(fromOrbit, toOrbit, true);
        // Find launch windows
        double[] times = hohmannXfer.LaunchTimes(numWindows);
        List<Dropdown.OptionData> items = new List<Dropdown.OptionData>();
        foreach (double t in times) {
            Dropdown.OptionData item = new Dropdown.OptionData();
            item.text = GravityScaler.GetWorldTimeFormatted(t, GravityScaler.Units.SOLAR);
            items.Add(item);
        }
        launchTimes.ClearOptions();
        launchTimes.AddOptions(items);
    }
}
