using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Callback for EarthAtmosphere external acceleration script to register
/// an impact with the surface of the Earth. 
/// 
/// This is needed since on long timezoom scales the ship may get far inside the Earth before an 
/// Update cycle runs and traditional collision detection triggers can fire. 
/// 
/// Need to stop integrating the spaceship immediatly (in the middle of the numerical integration). 
/// 
/// See EarthImpact for a typical implementation. 
/// 
/// (Abstract class since it needs to be specified in the inspector, so it needs to extend MonoBehaviour, 
///  an Interface would have been preferred)
/// 
/// </summary>
public abstract class  ImpactTrigger: MonoBehaviour  {

    public abstract void Impact(NBody nbody, GravityState gravityState);

}
