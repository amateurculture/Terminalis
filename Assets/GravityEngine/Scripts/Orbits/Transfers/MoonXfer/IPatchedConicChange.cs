using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface that defines a callback to be run when there is a change of the NBody providing the most
/// force on an object. See PatchedConicSOI for details. 
/// </summary>
public interface IPatchedConicChange  {

    /// <summary>
    /// Callback from an OrbitPredictor when it performs a change in the object it uses for conic
    /// orbit prediction. 
    /// 
    /// </summary>
    /// <param name="newObject"></param>
    /// <param name="oldObject"></param>
    void OnNewInfluencer(NBody newObject, NBody oldObject);

}
