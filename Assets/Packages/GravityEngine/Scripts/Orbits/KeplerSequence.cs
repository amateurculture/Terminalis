using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kepler elements maintains a time ordered list of OrbitUniversal segments. This allows a seqence of orbits
/// around different bodies to be specified. For example, a free return trajectory around a moon would have: 
/// ellipse around Earth, hyperbola around moon, ellipse around Earth as three segements. By putting everything
/// "on-rails" the scene can jump in time to any value requested without running NBody calculations for all 
/// the intermediate positions. 
/// 
/// On Init the KeplerSequence will automatically find and add the existing OrbitUniversal at the start of the list.
/// 
/// Note that this is NOT real physics since it is a series of two body evolutions and it is not what would
/// really happen. It may be a good enough model for a game, depending on the importance of accuracy vs utility
/// of jumping in time and getting results that do not vary based on numerical accuracy chosen for the GE.
/// 
/// The individual orbital elements are created as components and attached to a synthesized child object (this
/// ensures the KeplerSequence can be unambigously attached to an NBody. This requires that the active body 
/// and center body for the sequence elements be set explicitly (they cannot be inferred). 
/// 
/// </summary>

// Must have some initial information that says what orbit we are on
[RequireComponent(typeof(OrbitUniversal))]
public class KeplerSequence : MonoBehaviour, IFixedOrbit, INbodyInit {

    //! Optional callback to indicate when a specific element in the sequence starts
    public delegate void ElementStarted(OrbitUniversal orbitU);

    // inner class holding each of the conic sections and a time at which they start
    public class KeplerElement
    {
        //! time at which the sequence element starts (internal physics time)
        public double timeStart;
        public OrbitUniversal orbit;
        //! flag to indicate if at this time object should go off-rails
        public bool returnToGE;
        //! callback when a new sequence is started. Will be called each time transition to sequence occurs. 
        public ElementStarted callback;
        //! optional field used when element added via a maneuver. Allows maneuver callback on sequence change.
        public Maneuver maneuver;
    }

    private NBody nbody; 

    private List<KeplerElement> keplerElements;

    private GameObject orbitsGO;


    public void InitNBody(float physicalScale, float massScale) {
        if (keplerElements == null) {
            keplerElements = new List<KeplerElement>();
            orbitsGO = new GameObject("Orbit Sequence");
            orbitsGO.transform.parent = gameObject.transform;
            this.nbody = GetComponent<NBody>();
            OrbitUniversal initialOrbit = GetComponent<OrbitUniversal>();
            if (initialOrbit.evolveMode == OrbitUniversal.EvolveMode.GRAVITY_ENGINE) {
                Debug.LogError("Initial orbit set to off-rails. Cannot use Kepler sequence.");
                return;
            }
            AppendElementExistingOrbitU(initialOrbit, null);
        }
        keplerElements[activeElement].orbit.InitNBody(physicalScale, massScale);
    }

    private int activeElement = 0;

    /// <summary>
    /// Check the time of the appended element is later than the last element in the sequence. 
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    private bool BadTime(double time) {
        if (keplerElements.Count == 0)
            return false;
        bool bad = time < (keplerElements[keplerElements.Count - 1].timeStart);
        if (bad)
            Debug.LogError(string.Format("Time is earlier than an existing orbit data entry. last={0} time={1}",
                keplerElements[keplerElements.Count - 1].timeStart, time));
        return bad;
    }

    /// <summary>
    /// Add an element to the sequence that begins at time and evolves based on the orbit data provided. 
    /// 
    /// Orbit elements must be added in increasing time order. 
    /// </summary>
    /// <param name="time"></param>
    /// <param name="orbitData"></param>
    /// <param name="body"></param>
    /// <param name="centerBody"></param>
    /// <param name="callback">(Optional) Method to call when sequence starts</param>
    /// <returns></returns>
    public OrbitUniversal AppendElementOrbitData(double time,  
                        OrbitData orbitData, 
                        NBody body, 
                        NBody centerBody,
                        ElementStarted callback) {
        if (BadTime(time)) {
            return null;
        }
        KeplerElement ke = new KeplerElement
        {
            timeStart = time,
            returnToGE = false,
            callback = callback
        };
        OrbitUniversal orbit = orbitsGO.AddComponent<OrbitUniversal>();
        orbit.centerNbody = centerBody;
        orbit.SetNBody(body);
        orbit.InitFromOrbitData(orbitData, time);
        orbit.evolveMode = OrbitUniversal.EvolveMode.KEPLERS_EQN;
        ke.orbit = orbit;
        keplerElements.Add(ke);
        return orbit;
    }

    /// <summary>
    /// Add an element to the sequence using r0/v0/t0 initial conditions. 
    /// 
    /// Position and velocity are with respect to the center body (NOT world/physics space!). 
    /// 
    /// Orbit segements must be added in increasing time order. 
    /// </summary>
    /// <param name="r0"></param>
    /// <param name="v0"></param>
    /// <param name="time"></param>
    /// <param name="relativePos"></param>
    /// <param name="body"></param>
    /// <param name="centerBody"></param>
    /// <param name="callback">(Optional) Method to call when sequence starts</param>
    /// <returns></returns>
    public OrbitUniversal AppendElementRVT(Vector3d r0, 
                                            Vector3d v0, 
                                            double time, 
                                            bool relativePos,
                                            NBody body, 
                                            NBody centerBody,
                                            ElementStarted callback) {
        if (BadTime(time)) {
            return null;
        }
        KeplerElement ke = new KeplerElement
        {
            timeStart = time,
            callback = callback,
            returnToGE = false
        };
        OrbitUniversal orbit = orbitsGO.AddComponent<OrbitUniversal>();
        orbit.centerNbody = centerBody;
        orbit.SetNBody(body);
        orbit.InitFromRVT(r0, v0, time, centerBody, relativePos);
        orbit.evolveMode = OrbitUniversal.EvolveMode.KEPLERS_EQN;
        ke.orbit = orbit;
        keplerElements.Add(ke);
        return orbit;
    }


    /// <summary>
    /// Add an element using an existing OrbitUniversal instance
    /// 
    /// Orbit elements must be added in increasing time order. 
    /// </summary>
    /// <param name="orbitU"></param>
    /// <param name="callback">(Optional) Method to call when sequence starts</param>
    public void AppendElementExistingOrbitU(OrbitUniversal orbitU, ElementStarted callback) {
        if (BadTime(orbitU.GetStartTime())) {
            return;
        }
        KeplerElement ke = new KeplerElement
        {
            timeStart = orbitU.GetStartTime(),
            returnToGE = false
        };
        ke.orbit = orbitU;
        ke.callback = callback;
        keplerElements.Add(ke);
    }

    public void AppendReturnToGE(double time, NBody body) {
        KeplerElement ke = new KeplerElement();
        ke.timeStart = time;
        ke.returnToGE = true;
        keplerElements.Add(ke);
    }


    public void Evolve(double physicsTime, ref double[] r) {
        if ((activeElement < keplerElements.Count - 1) &&
            (physicsTime > keplerElements[activeElement + 1].timeStart)) {
            // Advance to next element
            activeElement++;
            GravityEngine ge = GravityEngine.Instance();
            KeplerElement activeKE = keplerElements[activeElement];
            if (activeKE.returnToGE) {
#pragma warning disable 162     // disable unreachable code warning
                if (GravityEngine.DEBUG)
                    Debug.Log("return to GE:" + gameObject.name);
#pragma warning restore 162
                KeplerElement priorKE = keplerElements[activeElement - 1];
                priorKE.orbit.Evolve(physicsTime, ref r);
                Vector3d pos = priorKE.orbit.GetPositionDouble();
                Vector3d vel = priorKE.orbit.GetVelocityDouble();
                ge.BodyOffRails(nbody, pos, vel);
                return;
            } else {
                // if the center object of the orbit changes, need to recompute the KeplerDepth and update
                if (keplerElements[activeElement-1].orbit.centerNbody != activeKE.orbit.centerNbody) {
                    NewCenter( activeKE.orbit);
                }
                // move on to the next orbit in the sequence
                activeKE.orbit.PreEvolve(ge.physToWorldFactor, ge.massScale);
                if (activeKE.callback != null) {
                    activeKE.callback(activeKE.orbit);
                }
                if ((activeKE.maneuver != null) && (activeKE.maneuver.onExecuted != null)) {
                    activeKE.maneuver.onExecuted(activeKE.maneuver);
                }
            }
#pragma warning disable 162     // disable unreachable code warning
            if (GravityEngine.DEBUG)
                Debug.LogFormat("Changed to segment {0} tnow={1} tseg={2} ", activeElement, physicsTime,
                    keplerElements[activeElement].timeStart);
#pragma warning restore 162
        } else if (physicsTime < keplerElements[activeElement].timeStart) {
            // Use an earlier element (happens if time set to earlier)
            int lastElement = activeElement;
            while(physicsTime < keplerElements[activeElement].timeStart && (activeElement >= 0)) {
                activeElement--;
                // if the center object of the orbit changes, need to recompute the KeplerDepth and update
                if (keplerElements[lastElement].orbit.centerNbody != keplerElements[activeElement].orbit.centerNbody) {
                    NewCenter( keplerElements[activeElement].orbit);
                }
            }

        }

        if (!keplerElements[activeElement].returnToGE) {
            // time for evolve is absolute - up to OrbitUniversal to make it relative to their time0
            keplerElements[activeElement].orbit.Evolve(physicsTime, ref r);
        }
    }

    /// <summary>
    /// New center. Update the Kepler depth and any children holding orbit predictors or segments
    /// </summary>
    /// <param name="orbitU"></param>
    private void NewCenter(OrbitUniversal orbitU) {
        GravityEngine.Instance().UpdateKeplerDepth(nbody, orbitU);
        foreach(OrbitPredictor op in gameObject.GetComponentsInChildren<OrbitPredictor>()) {
            op.SetCenterObject(orbitU.centerNbody.gameObject);
        }
        foreach (OrbitSegment os in gameObject.GetComponentsInChildren<OrbitSegment>()) {
            os.SetCenterObject(orbitU.centerNbody.gameObject);
        }
    }

    public Vector3 GetPosition() {
        return keplerElements[activeElement].orbit.GetPosition();
    }

    public Vector3 GetVelocity() {
        return keplerElements[activeElement].orbit.GetVelocity();
    }

    /// <summary>
    /// Get the current OrbitUniversal for the orbit
    /// </summary>
    /// <returns></returns>
    public OrbitUniversal GetCurrentOrbit() {
        return keplerElements[activeElement].orbit;
    }

    /// <summary>
    /// Return the index of the current orbit sequence. 
    /// (Used in the editor script for in-scene display)
    /// </summary>
    /// <returns></returns>
    public int GetCurrentOrbitIndex() {
        return activeElement;
    }

    public void GEUpdate(GravityEngine ge) {
        keplerElements[activeElement].orbit.GEUpdate(ge);
    }

    public void Move(Vector3 position) {
        // Not sure this works. Apply to all segments ???
        keplerElements[activeElement].orbit.Move(position);
    }

    public void PreEvolve(float physicalScale, float massScale) {
        keplerElements[activeElement].orbit.PreEvolve(physicalScale, massScale);
    }

    /// <summary>
    /// Not valid for a Kepler Sequence. 
    /// </summary>
    /// <param name="nbody"></param>
    public void SetNBody(NBody nbody) {
        throw new System.NotImplementedException();
    }

    public void SetTimeoffset(double timeOffset) {
        throw new System.NotImplementedException();
    }

    public NBody GetCenterNBody() {
        return keplerElements[activeElement].orbit.centerNbody;
    }

    /// <summary>
    /// Apply the impulse to the current OrbitUniversal element.
    /// 
    /// This will break time-reversal because this change in impulse is not recorded. 
    /// </summary>
    /// <param name="impulse"></param>
    /// <returns></returns>
    public Vector3 ApplyImpulse(Vector3 impulse) {
        Debug.LogWarning("Not supported");
        return keplerElements[activeElement].orbit.ApplyImpulse(impulse);
    }

    /// <summary>
    /// Usual case is that a KeplerSequence is on rails but can be a misconfiguration where the
    /// base OrbitUniversal is set to GE mode. In this case assume the OrbitUnversal config is
    /// intentional, warn about the misconfig and continue. 
    /// </summary>
    /// <returns></returns>
    public bool IsOnRails() {
        OrbitUniversal initialOrbit = GetComponent<OrbitUniversal>();
        return (initialOrbit.evolveMode == OrbitUniversal.EvolveMode.KEPLERS_EQN);
    }

    /// <summary>
    /// Set the position and velocity at the current time. 
    /// 
    /// This will break time reversal since the change is not recorded.
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="vel"></param>
        public void UpdatePositionAndVelocity(Vector3 pos, Vector3 vel) {
        keplerElements[activeElement].orbit.InitFromRVT(new Vector3d(pos),
                                                        new Vector3d(vel),
                                                        GravityEngine.Instance().GetPhysicalTime(),
                                                        keplerElements[activeElement].orbit.centerNbody, 
                                                        false);
    }

    /// <summary>
    /// Add orbit segments for each of the maneuvers. The maneuvers must have been created by transfer code
    /// that populated the fields: relativeTo, relativePos, relativeVel and time fields. 
    /// 
    /// </summary>
    /// <param name="list"></param>
    public void AddManeuvers(List<Maneuver> maneuverList) {
        foreach (Maneuver m in maneuverList) {
            if (m.relativeTo != null) {
                Debug.LogFormat("Maneuver: relPos={0} phyPos={1} t={2}", m.relativePos, m.physPosition, m.worldTime);
                AppendElementRVT(m.relativePos, m.relativeVel, m.worldTime, true, m.nbody, m.relativeTo, null);
                // add the maneuver to the KE so callback can be used
                KeplerElement ke = keplerElements[keplerElements.Count - 1];
                ke.maneuver = m;
            } else {
                Debug.LogError("Could not add maneuver. Missing relativeTo information. Skipped.");
            }
        }
    }

    /// <summary>
    /// Remove maneuvers from the Kepler sequence IF they have not started yet.
    /// 
    /// If they have started or are in the past, leave in place and return an error. 
    /// 
    /// </summary>
    /// <param name="maneuverList"></param>
    public bool RemoveManeuvers(List<Maneuver> maneuverList) {

        int numElements = keplerElements.Count;
        foreach (Maneuver m in maneuverList) { 
            for (int i = activeElement+1; i < keplerElements.Count; i++) {
                if (keplerElements[i].maneuver == m) {
                    keplerElements.RemoveAt(i);
                    break;
                }
            }
        }
        if ((numElements - maneuverList.Count) != keplerElements.Count) {
            Debug.LogWarning("Could not delete all maneuvers. Not present or already applied.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Remove all segments that occur after the current time
    /// </summary>
    public void RemoveFutureSegments() {
        RemoveSegmentsAfterTime(GravityEngine.Instance().GetPhysicalTime());
    }

    /// <summary>
    /// Remove all segments after the specified time.
    /// </summary>
    /// <param name="time"></param>
    public void RemoveSegmentsAfterTime(double time) {
        int removeFrom = -1; 
        for (int i = activeElement + 1; i < keplerElements.Count; i++) {
            if (keplerElements[i].timeStart > time) {
                removeFrom = i;
                break;
            }
        }
        // Cannot remove the first segment
        if (removeFrom > 0) {
            keplerElements.RemoveRange(removeFrom, (keplerElements.Count - removeFrom));
        }

    }

    /// <summary>
    /// Remove all previous and current segments. Used when a body is put back on rails after a period of NBody
    /// evolution. The on-rails code will explicitly add an orbitU.
    /// </summary>
    public void Reset() {
        activeElement = 0;
        keplerElements.Clear();
    }

    public string DumpInfo() {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append(string.Format("  Kepler Sequence: numElements= {0} activeSeq={1}\n", 
            keplerElements.Count, activeElement));
        for (int i = 0; i < keplerElements.Count; i++) {
            sb.Append(string.Format("    {0} t={1:0.0}  {2}", 
                    i, keplerElements[i].timeStart, keplerElements[i].orbit.DumpInfo()));
        }
        sb.Append("\n");
        return sb.ToString();
    }

}
