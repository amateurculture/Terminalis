using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple controller to demonstrate the use of DockingGroup. (Requires a DockingGroup be attached to the 
/// same object)
/// 
/// Get separation distance from the docking group and display it. 
/// 
/// See @DockingGroup
/// </summary>
[RequireComponent(typeof(DockingGroup))]
public class DockingController : MonoBehaviour {

    [SerializeField]
    private Text dockingInfo = null;

    private DockingGroup dockingGroup;


    // Use this for initialization
    void Start () {
        dockingGroup = GetComponent<DockingGroup>();

    }

 
    // Update is called once per frame
    void Update () {
        dockingInfo.text = string.Format("Separation: {0}", dockingGroup.SeparationDistance());
    }
}
