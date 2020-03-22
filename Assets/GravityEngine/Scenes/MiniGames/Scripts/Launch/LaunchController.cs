using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script to control the rocket pitch and throttle during launch to orbit via Animation Curves
/// in the inspector. 
/// 
/// The time scale is set manually and the curves are taken to range over this timescale. Time 0
/// indicates when the engines have started. 
/// 
/// The pitch curve defines the ship pitch with respect to the local vertical rotated around the z-axis. 
/// (a simple one axis change).
/// 
/// The thrust curve indicates percent thrust.
/// </summary>
public class LaunchController : MonoBehaviour {

    //! maximum time for the controller in world seconds (GetTimeWorldSeconds)
    public float timeRange;
    private float timeRangePhysical; 

    public NBody ship;
    public NBody earth;

    public RocketEngine engine;

    public SpaceshipRV shipModel; 

    //! pitch with respect to the local horizon (at launch 1=90 degrees, 0 = horizontal)
    public AnimationCurve pitchCurve;

    public AnimationCurve thrustCurve;

    private float timeStarted = -1f;

    private GravityEngine ge;

    private Vector3 lastAttitude;
        
	// Use this for initialization
	void Start () {
        ge = GravityEngine.Instance();

        // scale to GE time base
        timeRangePhysical = timeRange;
	}
	
	// Update is called once per frame
	void FixedUpdate () {

        // monitor for engine start (not ideal, but avoids linking to LaunchUI script) 
		if ((timeStarted < 0) && engine.engineOn) {
            timeStarted = ge.GetPhysicalTime();
            float pitch0 = pitchCurve.Evaluate(0);
            Vector3 localVertical = Vector3.Normalize(ge.GetPhysicsPosition(ship) - ge.GetPhysicsPosition(earth));
            lastAttitude = Vector3.Cross(localVertical, Vector3.forward); // forward = (0,0,1)
            lastAttitude = Quaternion.AngleAxis(pitch0, Vector3.forward) * lastAttitude;
        }

        if (timeStarted > 0) {
            float t = (float)(ge.GetTimeWorldSeconds() - timeStarted) / timeRangePhysical;
            t = Mathf.Clamp(t, 0f, 1f);

            // pitch in range 0..90 as curve goes 0..1
            float pitch = pitchCurve.Evaluate(t) * 90.0f;
            // find the local vertical
            Vector3 localVertical = Vector3.Normalize(ge.GetPhysicsPosition(ship) - ge.GetPhysicsPosition(earth));
            // First get local horizontal (this ordering of cross product assumes we want to go East)
            Vector3 attitude = Vector3.Cross(localVertical, Vector3.forward); // forward = (0,0,1)
            attitude = Quaternion.AngleAxis(pitch, Vector3.forward) * attitude;
            engine.SetThrustAxis(-attitude);
            shipModel.RotateToVector(attitude);

            // when attitude changes by 1 degree, re-compute trajectories
            if (Vector3.Angle(attitude, lastAttitude) > 1f) {
                ge.TrajectoryRestart();
                lastAttitude = attitude;
            }

            // thrust
            float thrust = thrustCurve.Evaluate(t);
            engine.SetThrottlePercent(100f * thrust);
        }

	}
}
