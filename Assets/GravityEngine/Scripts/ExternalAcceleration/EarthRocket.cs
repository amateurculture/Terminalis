using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Combine the acceleration from EarthAtmosphere and MultistageRocket
/// 
/// This implementation allows the work of keeping a list of objects out of the integrator. 
/// 
/// Further optimization could be done by cut & pasting the atmosphere and rocket code together
/// to save on function calls. But, ick.
/// 
/// </summary>
public class EarthRocket : MonoBehaviour, GEExternalAcceleration
{
    [SerializeField]
    private MultiStageEngine engine = null;

    [SerializeField]
    private EarthAtmosphere atmosphere = null;

    private double[] a_rocket = new double[] { 0, 0, 0 };
    private double[] a_atmosphere = new double[] { 0, 0, 0 };
    private double[] a_last = new double[] { 0, 0, 0 };

    private double[] a = new double[] { 0, 0, 0 };

    private GravityState worldState;
    double accelGEtoSI;

    void Start() {
        worldState = GravityEngine.Instance().GetWorldState();
        accelGEtoSI = GravityScaler.AccelerationScaleInternalToGEUnits() / GravityScaler.AccelSItoGEUnits();
    }


    public double GetAccel() {
        return accelGEtoSI * Mathd.Sqrt(a_last[0] * a_last[0] +
                a_last[1] * a_last[1] +
                a_last[2] * a_last[2]);
    }

    public double[] acceleration(double time, GravityState gravityState, ref double massKg) {

        a_rocket = engine.acceleration(time, gravityState, ref massKg);
        // need to update mass as fuel is consumed
        atmosphere.inertialMassKg = massKg;
        a_atmosphere = atmosphere.acceleration(time, gravityState, ref  massKg);

        a[0] = a_rocket[0] + a_atmosphere[0];
        a[1] = a_rocket[1] + a_atmosphere[1];
        a[2] = a_rocket[2] + a_atmosphere[2];
        // cache the last atmosphere accel for world state (need to check since could be trajectories
        // asking for accel as well)
        if (gravityState == worldState) {
            a_last[0] = a[0];
            a_last[1] = a[1];
            a_last[2] = a[2];
        }
        return a;
    }
    
}
