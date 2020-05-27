using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface GEExternalAcceleration  {


    /// <summary>
    /// Determine the acceleration at the specified time for the provided gravity state. 
    /// 
    /// Mass at time t may also be returned in in a[3] by rocket engine implementations. This allows
    /// the per time slice mass to be passed back without additional overhead in time critical code. 
    /// 
    /// This routine may also be called by trajectory prediction states, so implementations should not keep 
    /// state. 
    /// </summary>
    /// <param name="time"></param>
    /// <param name="gravityState"></param>
    /// <param name="massKg (ref)"></param>
    /// <returns></returns>


    double[] acceleration(double time, GravityState gravityState, ref double massKg);

}
