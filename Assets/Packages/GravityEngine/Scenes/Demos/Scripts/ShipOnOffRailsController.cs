using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Demonstration of how to integrate rocket engine (or atmosphere) elements that use GEExternal acceleration in 
/// a scene that otherwise wants to keep everything on rails. 
/// 
/// The approach is to transition the ship to off-rails when the detailed physics of the rocket engine and/or
/// atmosphere runs and then to return to rails once done. This does affect time-reversability because the information
/// of the off-rails intermediate values is not recorded. 
/// 
/// In this demo the ship has a one stage engine that is controlled by SPACE. While firing the ship is off rails. 
/// 
/// </summary>
/// 
public class ShipOnOffRailsController : MonoBehaviour {

    [SerializeField]
    public NBody shipNbody;

    [SerializeField]
    public NBody centerNbody;

    private OneStageEngine rocketEngine;
    private GravityEngine ge;

    // Use this for initialization
    void Start () {
        rocketEngine = shipNbody.GetComponent<OneStageEngine>();
        if (rocketEngine == null)
            Debug.LogError("Did not find rocket engine attached to ship.");
        ge = GravityEngine.Instance();
	}

    // Update is called once per frame
    void Update () {

		if (Input.GetKeyDown(KeyCode.Space)) {
            // toggle the engine
            if (rocketEngine.engineOn) {
                rocketEngine.SetEngine(false);
                ge.BodyOnRails(shipNbody, centerNbody);
            } else {
                rocketEngine.SetEngine(true);
                Vector3d vel = ge.GetVelocityDoubleV3(shipNbody);
                rocketEngine.SetThrustAxis(vel.ToVector3().normalized);
                ge.BodyOffRails(shipNbody, ge.GetPositionDoubleV3(shipNbody), vel);
            }
        }
	}
}
