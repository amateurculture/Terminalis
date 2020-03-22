using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneStageEngine : RocketEngine {

    /// <summary>
    /// Implementation for a chemical rocket engine. Inputs are in the SI units
    /// indicated.  
    /// </summary>

    //! mass of empty ship (no fuel) in kg
    [Tooltip("Dry mass of ship (kg)")]
    public double massEmpty;

    //! mass of fuel in kg
    [Tooltip("Mass of fuel (kg)")]
    public double massFuel;

    //! burn rate in kg/sec
    [Tooltip("Fuel consumption rate (kg/sec)")]
    public double burnRate; 
    private double burnRateScaled;

    //! thrust of engine in N
    [Tooltip("Thrust (N)")]
    public double thrust;

    private double accelerationConversion;

    //! "live" thrust vector reflecintg orientation of ship model 
    private Vector3 thrustDirection;

    private double lastTime;
    //! Need to know active vs trajectory updates, so keep a reference of active state
    private GravityState activeState;

    private double fuelLevel;

    // throttle scales the thrust and fuel burn rate
    private double throttle = 1.0;

    void Start()
    {
        SetEngine(engineOn);
        // Convert from real world units
        // Rocket engine thrust is given in SI units, so acceleration is in SI units
        // For Orbital scale need to convert to km/hr^2
        accelerationConversion = GravityScaler.AccelSItoGEUnits();
        // and convert to the physics scale used in the engine
        accelerationConversion = accelerationConversion * GravityScaler.AccelGEtoInternalUnits();

        burnRateScaled = burnRate * GravityScaler.GetGameSecondPerPhysicsSecond();
        thrustDirection = thrustAxis;
        fuelLevel = massFuel;
        activeState = GravityEngine.Instance().GetWorldState();

    }

    public override void SetThrottlePercent(float throttlePercent) {
        this.throttle = Mathf.Clamp(throttlePercent, 0.0f, 100.0f);
    }

    public override void SetEngine(bool on)
    {
        engineOn = on;
    }

    public override double[] acceleration(double time, GravityState gravityState, ref double massKg)
    {
        // a(t) = Thrust/m(t) 
        // Will be called by both trajectory prediction and game evolution, so needs to be a function of time
        // (i.e. cannot reduce fuel each time this routine is called!)
        double[] a = new double[3] { 0, 0, 0};
        double activeFuel = 0;
        if (engineOn)
        {
            massKg = massEmpty;
            if (gravityState == activeState) {
                fuelLevel -= throttle * burnRate * burnRateScaled * (time - lastTime);
                if (fuelLevel< 0) {
                    fuelLevel = 0;
                }
                activeFuel = fuelLevel;
                lastTime = time;
            } else {
                activeFuel = fuelLevel - throttle * burnRate * burnRateScaled * (time - lastTime);
            }
            if (activeFuel > 0)
            {
                massKg += activeFuel;
                double a_scalar = accelerationConversion * thrust / massKg;
                a[0] = a_scalar * thrustDirection.x;
                a[1] = a_scalar * thrustDirection.y;
                a[2] = a_scalar * thrustDirection.z;
            } 
            //Debug.Log(string.Format("a=({0}, {1}, {2}) fuel={3}", a[0], a[1], a[2], fuel));
        }

        return a;
    }

    public override float GetFuel()
    {
        return (float) fuelLevel;
    }

    public override void SetThrustAxis(Vector3 thrustAxis) {
        this.thrustAxis = thrustAxis;
    }

    public override float GetThrottlePercent() {
        return (float) throttle * 100f;
    }
}
