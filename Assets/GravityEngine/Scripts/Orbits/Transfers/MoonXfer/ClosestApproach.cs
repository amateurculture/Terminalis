using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Monitor the distance between two NBody objects and trigger when the closest approach occurs. The
/// trigger event is communicated to an array of listener objects that implement the OnClosestApproachTrigger
/// defined in the IClosestApproach interface. 
/// 
/// A minimum distance threshold must have been met to generate a trigger. 
/// 
/// A trigger is only generated on the first closest approach. 
/// 
/// </summary>
public class ClosestApproach : MonoBehaviour {

    public GameObject[] listeners;

    public NBody body1;
    public NBody body2;

    //! closest approach must be smaller than this value to trigger (scaled units)
    public float approachDistance = 100f;

    //! distance threshold in Unity units
    private float approachDistancePhysics; 

    //! previous distance. Since we only care about detecting the minimum,scale etc. don't matter
    private float lastDistance;

    private bool triggered = false; 

    // Use this for initialization
    void Start() {

        approachDistancePhysics = approachDistance / GravityEngine.Instance().GetLengthScale();

        lastDistance = Vector3.Distance(body1.transform.position, body2.transform.position);

        foreach (GameObject go in listeners) {
            IClosestApproach iClosest = go.GetComponent<IClosestApproach>();
            if (iClosest == null) {
                Debug.LogError("Listener " + go.name + " missing IClosestApproach - will be ignored");
            }
        }
    }

    public void SetTriggered(bool value) {
        triggered = value;
    }

    private void NotifyListeners(float distance) {
        foreach (GameObject go in listeners) {
            IClosestApproach iClosest = go.GetComponent<IClosestApproach>();
            if (iClosest != null) {
                iClosest.OnClosestApproachTrigger(body1, body2, distance);
            }
        }
        // If there is a GE console, log there
        if (GEConsole.Instance() != null) {
            GEConsole.Instance().AddToHistory(string.Format("Closest approach={0}", distance));
        }
        triggered = true; 
    }

    void FixedUpdate () {
        if (!triggered) {
            Vector3 pos1 = GravityEngine.Instance().GetPhysicsPosition(body1);
            Vector3 pos2 = GravityEngine.Instance().GetPhysicsPosition(body2);
            float distance = Vector3.Distance(pos1, pos2);
            float delta = distance - lastDistance;

            // Need to be within approach distance to care (screens out cases where ship is in orbit around
            // planet and sign change in delta is triggered)
            if (distance < approachDistancePhysics) {
                if (delta > 0f) {
                    NotifyListeners(distance);
                    Debug.Log("Closest approach at " + distance);
                }
            }
            lastDistance = distance;
        }
    }
}
