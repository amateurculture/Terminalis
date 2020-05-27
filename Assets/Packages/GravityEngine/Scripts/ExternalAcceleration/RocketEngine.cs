
using UnityEngine;

/// <summary>
/// Rocket Engine Interface
/// Used to update acceleration in the massless body engine on a per-integration
/// step basis. 
/// 
/// Rocket engines are used for continuous application of force over a significant time
/// (unlike Maneuver which is used for an impulse change). 
/// 
/// Only one GEExternalAcceleration component can be added to an NBody. If a RocketEngine plus atmosphere
/// drag is required, then a wrapper class (e.g. EarthRocket) is needed to combine the external acceleration
/// from the engine and the atmosphere. 
/// 
/// The acceleration provided the engine must be adjusted to the GE internal units. 
/// 
/// Concept:
/// By putting engine code deep in the integrator get several advantages:
/// - per integration step accuracy for rocket eqn
/// - ability to do trajectory prediction
/// - ability to timescale/timeZoom
/// 
/// Bear in mind that the acceleration function can be called for the live scene as well as for trajectory prediction. 
/// If the implementing class is supporting trajectory prediction then it needs to either be stateless or aware of
/// when acceleration for trajectories are being computed. 
/// 
/// </summary>
public abstract class RocketEngine : MonoBehaviour, GEExternalAcceleration {

    //! state of engine (or the active stage of a multi-stage engine)
    public bool engineOn;

    //! direction of thrust. Must be unit length. 
    public Vector3 thrustAxis;

    /// <summary>
    /// Set the thrust axis. May be over-ridden to update internals if they exist. 
    /// </summary>
    /// <param name="thrustAxis"></param>
    public abstract void SetThrustAxis(Vector3 thrustAxis);

    //! "live" thrust vector reflecting orientation of ship model 
    protected Vector3 accelDirection;

    /// <summary>
    /// Determine the acceleration at the specified time. The accleration returned must be in the physical 
    /// units used by the integrators. For GE units other than DIMENSIONLESS this will involve some use of
    /// GravityScalar conversion functions. See the MultiStageEngine implementation for an example. 
    /// 
    /// When used in trajectory mode, this function may be called more than once for a given time, and
    /// as thrust changes. It will also be called for future times to determine the future path of the rocket.
    /// </summary>
    /// <param name="time">time for which the thrust should be determined.  </param>
    /// <returns>acceleration in [0..2], mass in [3]</returns>
    public abstract double[] acceleration(double time, GravityState gravityState, ref double massKg);

    /// <summary>
    /// Set engine state to on. 
    /// </summary>
    /// <param name="state"></param>
    public abstract void SetEngine(bool state);

    public bool IsEngineOn() {
        return engineOn;
    }

    /// <summary>
    /// Set the engine throttle level (as a percent).
    /// </summary>
    /// <param name="throttle"></param>
    public abstract void SetThrottlePercent(float throttle);

    public abstract float GetThrottlePercent();

    /// <summary>
    /// Get the current fuel remaining
    /// </summary>
    /// <returns>Amount of fuel in the current stage</returns>
    public abstract float GetFuel();

    public void SetRotation(Quaternion rotation) {
        // TODO: Add gimbal angle changes
        accelDirection = rotation * (-1f * thrustAxis);
        Vector3 angles = rotation.eulerAngles;
        //Debug.LogFormat("Thrust direction:({0}, {1}, {2} ) angles=({3}, {4}, {5} ) thrustMag={6}", 
        //      accelDirection.x, accelDirection.y, accelDirection.z, angles.x, angles.y, angles.z, Vector3.Magnitude(accelDirection) );
        GravityEngine.Instance().TrajectoryRestart();
    }

}
