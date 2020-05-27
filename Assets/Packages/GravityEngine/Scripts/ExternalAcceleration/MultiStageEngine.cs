using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiStageEngine : RocketEngine
{

    /// <summary>
    /// Implementation for a multi-stage stage rocket with atmospheric drag that depends on velocity and altitude.
    /// 
    /// 
    /// </summary>


    public const int MAX_STAGES = 10;

    //! mass of payload (kg)
    public double payloadMass;

    // Per-stage details
    public int numStages;

    //! currently active stage - stages less than this are assumed jetisoned
    public int activeStage;

    //! Stage masses will be allocated by inspector script on first access
    // TODO - put all these parallel arrays into a struct-class

    //! mass of fuel in kg
    public double[] massFuel;

    //! mass of empty stage
    public double[] massStageEmpty;

    //! burn rate in kg/sec
    public double[] burnRate;
    // adjust burn rate the GE time base
    private double burnRateScale; 

    //! current fuel in the stage
    private double[] fuelLevel;
    private double lastTime; 

    //! cross sectional area in m^2 for drag calculations
    public double[] crossSectionalArea;

    //! drag co-efficient (dimensionless, typically about 2.1)
    public double[] coeefDrag;

 
    //! thrust of engine in N
    public double[] thrust;

    //! Particle effect object (flame etc.) for a stage. Set to active when engine is on. 
    public GameObject[] effectObject;

    //! Need to know active vs trajectory updates, so keep a reference of active state
    private GravityState activeState; 

#if UNITY_EDITOR
    // public for editor script control (persist foldout state etc.)
    public bool editorInited;
    public bool[] editorStageFoldout; 
#endif

    private double[] burnStart;
    private double accelerationConversion;

    // throttle scales the thrust and fuel burn rate
    private double throttle = 1.0; 


    void Awake() {
        // Convert from real world units
        // Rocket engine thrust is given in SI units, so acceleration is in SI units
        // For Orbital scale need to convert to km/hr^2
        accelerationConversion = GravityScaler.AccelSItoGEUnits();
        // and convert to the physics scale used in the engine
        accelerationConversion = accelerationConversion * GravityScaler.AccelGEtoInternalUnits();

        burnStart = new double[MAX_STAGES];
        burnRateScale = GravityScaler.GetGameSecondPerPhysicsSecond();

        fuelLevel = new double[MAX_STAGES];
        for (int i=0; i < numStages; i++) {
            fuelLevel[i] = massFuel[i];
        }

        // acceleration will be opposite to thrust direction
        accelDirection = -thrustAxis;
        activeStage = 0;

        activeState = GravityEngine.Instance().GetWorldState();

        SetEngine(engineOn);
    }

    public override void SetThrustAxis(Vector3 thrustAxis) {
        this.thrustAxis = thrustAxis;
        accelDirection = -thrustAxis;
    }

    public override void SetEngine(bool on) {
        double time = GravityEngine.Instance().GetPhysicalTime();
        if (on) {
            burnStart[activeStage] = time;
            if (effectObject[activeStage] != null) {
                effectObject[activeStage].SetActive(true);
            }
        } else {
            if (effectObject[activeStage] != null) {
                effectObject[activeStage].SetActive(false);
            }
        }
        engineOn = on;
    }

    public override void SetThrottlePercent(float throttlePercent) {
        this.throttle = Mathf.Clamp(throttlePercent, 0.0f, 100.0f)/100f;
    }

    public override double[] acceleration(double time, GravityState gravityState, ref double massKg) {
        // a(t) = Thrust/m(t) 
        // Called on every integration timestep - so favour speed over beauty
        // Will be called by both trajectory prediction and game evolution, so needs to be a function of time
        // (i.e. cannot reduce fuel each time this routine is called!)
        double[] a = new double[3] { 0, 0, 0 };
        double activeFuel = 0; 

        // always need the mass for drag calc (even if engine is off)
        massKg = payloadMass;
        // TODO - could optimize and precompute (mass per stage - fuel) in that stage
        // add stage masses
        for (int i = activeStage; i < numStages; i++) {
            massKg += massStageEmpty[i];
        }
        // add fuel of unused stages
        for (int i = activeStage + 1; i < numStages; i++) {
            massKg += massFuel[i];
        }
        // thrust
        if (engineOn && (fuelLevel[activeStage] > 0)) { 
            if (gravityState == activeState) {
                fuelLevel[activeStage] -= throttle * burnRateScale * burnRate[activeStage] * (time - lastTime);
                if (fuelLevel[activeStage] < 0) {
                    fuelLevel[activeStage] = 0;
                }
                activeFuel = fuelLevel[activeStage];
                lastTime = time;
            } else {
                activeFuel = fuelLevel[activeStage] - throttle * burnRateScale * burnRate[activeStage] * (time - lastTime);
            }
            if (activeFuel > 0) {
                massKg += activeFuel;
                double a_scalar = accelerationConversion * thrust[activeStage] * throttle / massKg;
                a[0] = a_scalar * accelDirection.x;
                a[1] = a_scalar * accelDirection.y;
                a[2] = a_scalar * accelDirection.z;
            } 
        }

        return a;
    }

    /// <summary>
    /// Gets the fuel for the active stage
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public override float GetFuel() {
        return (float) fuelLevel[activeStage];
    }

    /// <summary>
    /// Start the next stage
    /// </summary>
    /// <returns></returns>
    public void NextStage() {
        if (effectObject[activeStage] != null) {
            effectObject[activeStage].SetActive(false);
        }

        if (activeStage < numStages) {
            activeStage++;
            if (effectObject[activeStage] != null) {
                effectObject[activeStage].SetActive(true);
            }
        } else {
            engineOn = false;
        }
    }

    public override float GetThrottlePercent() {
        return (float) throttle * 100f;
    }
}
