using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface defining the callback for listeners in the ClosestApproach script. 
/// </summary>
public interface IClosestApproach  {

    void OnClosestApproachTrigger(NBody body1, NBody body2, float distance);

}
