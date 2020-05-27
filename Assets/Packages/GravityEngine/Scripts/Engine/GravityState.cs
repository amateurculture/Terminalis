using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gravity state.
/// Hold "most" of the information for gravitational evolution of the system. 
/// 
/// The NBody objects added can be massive or massless. They can independently be in normal gravitational motion
/// or have their motion FIXED in some way (either by a Kepler/on-rail evolution mode or simply being not movable).
/// 
/// Massive bodies are tracked here by the arrays m[] and r[]. These arrays are then updated by passing to the 
/// selected numerical integrator. This allows a central object manager to compute the mutual gravitational 
/// interactions in the most effecient way. Each fixed frame the r[] value are copied back to the transform 
/// positions of the associated game objects (this is done in the GE class). 
/// 
/// A parallel list of FixedBodies is maintained BUT they are also in the r[] list, since their masses may
/// affect non-fixed bodies. 
/// 
/// Massless bodies are evolved seperately using a simple Leapfrog integrator (unless Optimize Massless has been
/// set to false). 
/// 
/// Particles are always evolved seperately using a simpe Leapfrog integrator. 
/// 
/// A scene may have more than one gravity state. Additional copies may be used for trajectory prediction, to 
/// determine future paths objects will take.
///
/// </summary>
public class GravityState
{

    public const int NDIM = 3; // Here to "de-magic" numbers. Some integrators have 3 baked in. Do not change.

    public int numBodies;

    //! masses of the massive bodies in the engine
    public double[] m;
    //! physics positions of massive objects in the engine
    public double[,] r;
    //! per GameObject flags for the integrator (INACTIVE, FIXED, TRAJ_DATA are current bits)
    public byte[] info;

    public List<GravityEngine.FixedBody> fixedBodies;

    // need to keep size^2 for simple collision detection between particles and 
    // massive bodies. Collisions between massive bodies are left to usual Unity
    // collider intrastructure
    public double[] size2; // size^2 used for detecting particle incursions

    //! size of the arrays (may exceed the number of bodies due to pre-allocation)
    public int arraySize;

    //! time of current state (in the engine physics time)
    public double time;

    //! State has trajectories that require updating
    public bool hasTrajectories;

    //!  physical time per evolver since start OR last timescale change
    public enum Evolvers { MASSIVE, MASSLESS, FIXED, PARTICLES };
    public double[] physicalTime;

    //! Flag to indicate running async (on a non-main thread). If true, cannot do debug logging or access scene
    public bool isAsync;

    // Integrators - these are held in GE
    public INBodyIntegrator integrator;
    public MasslessBodyEngine masslessEngine;

    public List<GravityParticles> gravityParticles;

    // Delegate for handling Maneuvers. Access through GE wrapper methods, but
    // separate implementation in a delegate.
    public ManeuverMgr maneuverMgr;

    //! All bodies are on rails (true until a body is added which is not on rails)
    private bool onRails = true; 

    private const double EPSILON = 1E-4; 	// minimum distance for gravitatonal force

    private IForceDelegate forceDelegate;

    //! A force may be selective. Selective force needs to track integrator internal index structure
    private SelectiveForceBase selectiveForce;

    private bool isCopy;

    // As KeplerSequences change segments, the Kepler depth can change (e.g. xfer to moon SOI)
    // This happens as fixedBodies is being iterated over so need to dump changes onto a list and do
    // after the iteration in MoveFixedBodies
    private List<GravityEngine.FixedBody> keplerDepthChanged;

    /// <summary>
    /// New Gravity state. Also need to call SetAlgorithmAndForce to fully configure. 
    /// </summary>
    /// <param name="size"></param>
    public GravityState(int size) {

        InitArrays(size);
        gravityParticles = new List<GravityParticles>();
        fixedBodies = new List<GravityEngine.FixedBody>();
        keplerDepthChanged = new List<GravityEngine.FixedBody>();

        maneuverMgr = new ManeuverMgr();
        onRails = true;

#pragma warning disable 162     // disable unreachable code warning
        if (GravityEngine.DEBUG)
            Debug.Log("Created new (empty) gravityState");
#pragma warning restore 162

    }

    /// <summary>
    /// Clone constructor
    /// 
    /// Creates a deep copy suitable for independent evolution as a trajectory or for maneuver iterations. 
    /// Maneuvers will be executed but the callback to motify the owner of the maneuver will be skipped (only
    /// the real evolution will notify).
    /// </summary>
    /// <param name="fromState"></param>
    public GravityState(GravityState fromState) {
        m = new double[fromState.arraySize];
        r = new double[fromState.arraySize, NDIM];
        info = new byte[fromState.arraySize];
        size2 = new double[fromState.arraySize];
        physicalTime = new double[System.Enum.GetNames(typeof(Evolvers)).Length];
        arraySize = fromState.arraySize;
        numBodies = fromState.numBodies;
        onRails = fromState.onRails;


        // omitting hasTrajectories
        integrator = fromState.integrator.DeepClone();

        // don't copy particles, but need to init list
        gravityParticles = new List<GravityParticles>();

        // DO copy the maneuvers
        maneuverMgr = new ManeuverMgr(fromState.maneuverMgr);

        fixedBodies = new List<GravityEngine.FixedBody>(fromState.fixedBodies);

        for (int i = 0; i < physicalTime.Length; i++) {
            physicalTime[i] = fromState.physicalTime[i];
        }
        time = fromState.time;
        forceDelegate = fromState.forceDelegate;
        selectiveForce = fromState.selectiveForce;
  
        keplerDepthChanged = new List<GravityEngine.FixedBody>(fromState.keplerDepthChanged);

        if (fromState.masslessEngine != null) {
            masslessEngine = fromState.masslessEngine.DeepClone();
            masslessEngine.ResetTrajectories((float)fromState.time);
        }

        for (int i = 0; i < arraySize; i++) {
            m[i] = fromState.m[i];
            info[i] = fromState.info[i];
            size2[i] = fromState.size2[i];
            r[i, 0] = fromState.r[i, 0];
            r[i, 1] = fromState.r[i, 1];
            r[i, 2] = fromState.r[i, 2];
        }

        // copies do not notify maneuver owners of maneuver completion. They are assumed to be "what if"
        // evolutions
        isCopy = true;

#pragma warning disable 162     // disable unreachable code warning
        if (GravityEngine.DEBUG)
            Debug.Log("Created new (copy) gravityState");
#pragma warning restore 162
    }

    /// <summary>
    /// Set the integrator required for the chosen algorithm
    /// </summary>
    /// <param name="algorithm"></param>
    public void SetAlgorithmAndForce(GravityEngine.Algorithm algorithm, IForceDelegate forceDelegate) {
        this.forceDelegate = forceDelegate;
        // cast may be null if no force selection
        if (forceDelegate is SelectiveForceBase) {
            selectiveForce = (SelectiveForceBase)forceDelegate;
        }
        switch (algorithm) {
            case GravityEngine.Algorithm.LEAPFROG:
                integrator = new LeapFrogIntegrator(forceDelegate);
                break;
            //case GravityEngine.Algorithm.LEAPFROG_JOB:
            //    integrator = new LeapFrogJob(forceDelegate);
            //    break;
            case GravityEngine.Algorithm.HERMITE8:
                integrator = new HermiteIntegrator(forceDelegate);
                break;
            case GravityEngine.Algorithm.AZTRIPLE:
                integrator = new AZTripleIntegrator();
                break;
            default:
                Debug.LogError("Unknown algortithm");
                break;
        }
    }

    public void InitArrays(int arraySize) {
        m = new double[arraySize];
        r = new double[arraySize, NDIM];
        info = new byte[arraySize];
        size2 = new double[arraySize];

        physicalTime = new double[System.Enum.GetNames(typeof(Evolvers)).Length];
        this.arraySize = arraySize;
        if (selectiveForce) {
            selectiveForce.Init(arraySize);
        }
    }

    public bool GrowArrays(int growBy) {

        integrator.GrowArrays(growBy);

        double[] m_copy = new double[arraySize];
        double[,] r_copy = new double[arraySize, NDIM];
        byte[] info_copy = new byte[arraySize];
        double[] size2_copy = new double[arraySize];

        for (int i = 0; i < arraySize; i++) {
            m_copy[i] = m[i];
            r_copy[i, 0] = r[i, 0];
            r_copy[i, 1] = r[i, 1];
            r_copy[i, 2] = r[i, 2];
            info_copy[i] = info[i];
            size2_copy[i] = size2[i];
        }

        int newSize = arraySize + growBy;
        m = new double[newSize];
        r = new double[newSize, NDIM];
        info = new byte[newSize];
        size2 = new double[newSize];

        for (int i = 0; i < arraySize; i++) {
            m[i] = m_copy[i];
            info[i] = info_copy[i];
            size2[i] = size2_copy[i];
            r[i, 0] = r_copy[i, 0];
            r[i, 1] = r_copy[i, 1];
            r[i, 2] = r_copy[i, 2];
        }
        arraySize += growBy;

        if (selectiveForce) {
            selectiveForce.IncreaseToSize(arraySize);
        }

        return true;
    }

    public void Clear() {
        numBodies = 0;
        integrator.Clear();
        masslessEngine = null;
        gravityParticles.Clear();
        fixedBodies.Clear();
    }


    public void ResetPhysicalTime() {
        for (int i = 0; i < physicalTime.Length; i++) {
            physicalTime[i] = 0.0;
        }
    }

    public void AddFixedBody(GravityEngine.FixedBody fixedBody) {
        // Need to maintain order by kepler depth
        int insertAt = fixedBodies.Count;
        for (int i = 0; i < fixedBodies.Count; i++) {
            if (fixedBody.kepler_depth < fixedBodies[i].kepler_depth) {
                insertAt = i;
                break;
            }
        }
        fixedBodies.Insert(insertAt, fixedBody);
    }

    public void RemoveFixedBody(NBody nbody) {
        // find object in FixedBodies list and remove
        GravityEngine.FixedBody fbRemove = null;
        foreach (GravityEngine.FixedBody fb in fixedBodies) {
            if (fb.nbody == nbody) {
                fbRemove = fb;
                break;
            }
        }
        if (fbRemove != null) {
            fixedBodies.Remove(fbRemove);
        }
    }

    /// <summary>
    /// Recompute and update the kepler depth of a fixed body.
    /// </summary>
    /// <param name="nbody"></param>
    public void UpdateKeplerDepth(NBody nbody, OrbitUniversal orbitU) {
        if (nbody.engineRef == null)
            return;
        GravityEngine.FixedBody fixedBody = nbody.engineRef.fixedBody;
        if (fixedBody == null)
            return;
        int depth = nbody.GetOrbitDepth();
        int newDepth = OrbitUtils.CalcKeplerDepth(orbitU);
        if (newDepth != depth) {
            fixedBody.kepler_depth = newDepth;
            keplerDepthChanged.Add(fixedBody);
        }
    }

    public void AddNBody(NBody nbody, Vector3 physicsPosition, Vector3 vel_phys, float massScale) {
        r[numBodies, 0] = physicsPosition.x;
        r[numBodies, 1] = physicsPosition.y;
        r[numBodies, 2] = physicsPosition.z;
        size2[numBodies] = nbody.size * nbody.size;
        // mass scale is applied to internal record BUT leave nbody mass as is
        m[numBodies] = nbody.mass * massScale;
        integrator.AddNBody(numBodies, nbody, physicsPosition, vel_phys);
        numBodies++;
        UpdateOnRails();
    }

    public void AddMasslessBody(GameObject gobject, float physToWorldFactor, double engineDt) {
        if (masslessEngine == null) {
            masslessEngine = new MasslessBodyEngine(engineDt);
            // worldState points to this also (simplifies Evolve code)
        }
        // massless on rails are not added to MBE
        onRails = false;
        masslessEngine.AddBody(gobject, physToWorldFactor);
    }

    public void RemoveMasslessBody(GameObject gobject) {
        masslessEngine.RemoveBody(gobject);
        UpdateOnRails();
    }

    /// <summary>
    /// Remove the body at index and shuffle up the rest. Ensure the integrator does the same to stay
    /// in alignment. 
    /// </summary>
    /// <param name="index"></param>
    public void RemoveNBody(NBody nbody) {
        integrator.RemoveBodyAtIndex(nbody.engineRef.index);
        // shuffle the rest down, update indices
        for (int j = nbody.engineRef.index; j < (numBodies - 1); j++) {
            info[j] = info[j + 1];
            m[j] = m[j + 1];
            r[j, 0] = r[j + 1, 0];
            r[j, 1] = r[j + 1, 1];
            r[j, 2] = r[j + 1, 2];
        }
        numBodies--;
        if (selectiveForce) {
            selectiveForce.RemoveBody(nbody.engineRef.index);
        }
        UpdateOnRails();
    }

    /// <summary>
    /// Check if all bodies are on rails. 
    /// 
    /// Called after a RemoveBody in GE. Need to move to a single state RemoveBody() but wait until do 
    /// masslessEngine refactor into integrator. 
    /// 
    /// internal use only. 
    /// </summary>
    public void UpdateOnRails() {
        onRails = false;
        if ((fixedBodies.Count == numBodies) &&
                ((masslessEngine == null) || (masslessEngine.NumBodies() == 0)) ) { 
                onRails = true;
        }
    }

    // Integrators need to use known positions to pre-determine accel. etc. to 
    // have valid starting values for evolution
    public void PreEvolve(GravityEngine ge) {
        // particles will pre-evolve when loading complete
        if (masslessEngine != null) {
            masslessEngine.PreEvolve(numBodies, this);
        }
        foreach (GravityEngine.FixedBody fixedBody in fixedBodies) {
            if (fixedBody.fixedOrbit != null) {
                fixedBody.fixedOrbit.PreEvolve(ge.physToWorldFactor, ge.massScale);
            }
        }
        integrator.PreEvolve(this, ref info);
    }

    /// <summary>
    /// Are all bodies in the world state "on-rails"?
    /// </summary>
    /// <returns></returns>
    public bool IsOnRails() {
        return onRails;
    }

    /*******************************************
    * Main Physics Loop
    ********************************************/

    /// <summary>
    /// Evolve the objects subject to gravity. 
    /// 
    /// Normal evolution is done by passing in worldState with a time interval corresponding to the 
    /// frame advance time multiplied by the time zoom. 
    /// 
    /// For trajectory updates in the case where the trajectory is up to date, this will be for the same interval
    /// but starting at a future time. 
    /// 
    /// In order for trajectory computation to "catch up", there are times when the interval may be longer (but limited
    /// by the re-compute factor to avoid a huge recomputation on a single frame). 
    /// </summary>
    ///
    /// <param name="ge">The Gravity engine</param>
    /// <param name="timeStep">The amount of physics DT to be evolved</param>
    /// 
    public bool Evolve(GravityEngine ge, double timeStep) {
        double gameDt = timeStep;
        bool trajectoryRestart = false;
        if (maneuverMgr.HaveManeuvers()) {
            List<Maneuver> maneuversInDt = maneuverMgr.ManeuversUntil((float)(time+timeStep));
            if (maneuversInDt.Count > 0) {
                foreach (Maneuver m in maneuversInDt) {
                    // evolve up to the time of the earliest maneuver
                    gameDt = System.Math.Max(m.worldTime -
                        (physicalTime[(int)GravityState.Evolvers.MASSIVE]), 0.0);
                    EvolveForTimestep(ge, gameDt);
                    m.Execute(this);
                    maneuverMgr.Executed(m, isCopy);
                }
                // recompute remaining time to evolve
                gameDt = System.Math.Max(time -
                        (physicalTime[(int)GravityState.Evolvers.MASSIVE]), 0.0);
                // if trajectories have made predictions, these need to be re-done since a manuever has
                // occured
                trajectoryRestart = true;
            }
        }
        EvolveForTimestep(ge, gameDt);
        return trajectoryRestart;
    }

    private void MoveFixedBodies(double time) {
        //==============================
        // Fixed Bodies
        //==============================
        // Update fixed update objects (if any)
        // Evolution is to a specific time - so use massive object physical time
        double[] r_new = new double[NDIM];
        foreach (GravityEngine.FixedBody fixedBody in fixedBodies) {
            // "if" needed for case where fixed body in process of going off-rails
            if (fixedBody.nbody.engineRef.bodyType == GravityEngine.BodyType.FIXED) {
                fixedBody.fixedOrbit.Evolve(time, ref r_new);
                int i = fixedBody.nbody.engineRef.index;
                r[i, 0] = r_new[0];
                r[i, 1] = r_new[1];
                r[i, 2] = r_new[2];
            }
        }
        foreach(GravityEngine.FixedBody fb in keplerDepthChanged) {
            // fixed bodies is ordered by orbit depth
            fixedBodies.Remove(fb);
            AddFixedBody(fb);
        }
        // MUST clear after, since OrbitU.SetNewCenter() may have added some things to update
        keplerDepthChanged.Clear();
    }

    private void EvolveForTimestep(GravityEngine ge, double physicsDt) {
        // if everything is on rails, can just jump to the end time
        if (onRails) {
            time += physicsDt;
            MoveFixedBodies(time);
            // Keep these up-to-date so can flip to off-rails if needed
            physicalTime[(int)Evolvers.MASSIVE] = time;
            physicalTime[(int)Evolvers.MASSLESS] = time;
            physicalTime[(int)Evolvers.PARTICLES] = time;
            return;
        }
        // Objective is to keep physical time proportional to game time 
        // Each integrator will run for at least as long as it is told but may overshoot
        // so correct time on next iteration. 
        // 
        // Keep the current physical time each integrator has reached in physicalTime[integrator_type]
        //
        double engineDt = ge.engineDt;
        if (physicsDt < engineDt)
            return;

        double timeEvolved = 0;

        // Need to move the integrators forward concurrently in steps matching the engineDt
        // - Hermite may be using a different timestep than this
        // - particles likely use a much longer timestep

        while (timeEvolved < physicsDt) {
            //==============================
            // Massive bodies
            //==============================
            // evolve all the massive game objects 
            double massiveDt = 0.0;
            if (numBodies > fixedBodies.Count) {
                // typical path - have massive bodies: use NBody integration
                massiveDt = integrator.Evolve(engineDt, this, ref info);
                physicalTime[(int)Evolvers.MASSIVE] += massiveDt;
                timeEvolved += massiveDt;
            } else {
                // all Kepler mode: skip integration
                physicalTime[(int)Evolvers.MASSIVE] += engineDt;
                timeEvolved += engineDt;
            }
 
            //==============================
            // Fixed Bodies
            //==============================
            // Update fixed update objects (if any)
            // Evolution is to a specific time - so use massive object physical time
            MoveFixedBodies(physicalTime[(int)Evolvers.MASSIVE]);

            // Debug.Log(string.Format("gameDt={0} integated={1} ptime={2} wtime={3}", gameDt, dt, physicalTime, worldTime));
            // LF is built in to particles and massless routines. They have their own DT built in
            // these run on a fixed timestep (if it is varied energy conservation is wrecked)
            // Track their evolution vs wall clock time seperately

            //==============================
            // Particles (should only be present in worldState)
            //==============================
            if (gravityParticles.Count > 0) {
                double particle_dt = ge.GetParticleDt();
                if (physicalTime[(int)GravityState.Evolvers.PARTICLES] <
                        physicalTime[(int)GravityState.Evolvers.MASSIVE]) {
                    double evolvedFor = 0.0;
                    if (forceDelegate != null) {
                        foreach (GravityParticles nbp in gravityParticles) {
                            evolvedFor = nbp.EvolveWithForce(particle_dt, numBodies, this,
                                                        ref size2, ref info, forceDelegate);
                        }
                    } else {
                        foreach (GravityParticles nbp in gravityParticles) {
                            evolvedFor = nbp.Evolve(particle_dt, numBodies, this, ref size2, ref info);
                        }
                    }
                    physicalTime[(int)Evolvers.PARTICLES] += evolvedFor;
                }
            }

            //==============================
            // Massless
            //==============================
            if (masslessEngine != null && masslessEngine.NumBodies() > 0) {
                // rockets need the time
                if (physicalTime[(int)Evolvers.MASSLESS] <
                        physicalTime[(int)Evolvers.MASSIVE]) {
                    if (forceDelegate != null) {
                        physicalTime[(int)Evolvers.MASSLESS] +=
                                masslessEngine.EvolveWithForce(engineDt, time, numBodies, this, ref info, forceDelegate);
                    } else {
                        physicalTime[(int)Evolvers.MASSLESS] +=
                                masslessEngine.Evolve(engineDt, time, numBodies, this, ref info);
                    }
                }
            }
            // must update time so trajectory times are up to date
            time = physicalTime[(int)Evolvers.MASSIVE];
            if (hasTrajectories) {
                ge.UpdateTrajectories();
            }

        } // while
    }

 
    /// <summary>
    /// Get the internal position used by the physics engine. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <returns></returns>
    public Vector3 GetPhysicsPosition(NBody nbody) {

        if (nbody == null || nbody.engineRef == null) {
            // may occur due to startup sequencing
            return Vector3.zero;
        }
        if (nbody.engineRef.bodyType == GravityEngine.BodyType.MASSLESS) {
            return masslessEngine.GetPosition(nbody);
        }
        if (nbody.IsFixedOrbit()) {
            // If Kepler evolution 
            return nbody.engineRef.fixedBody.fixedOrbit.GetPosition();
        }
        return new Vector3((float)r[nbody.engineRef.index, 0],
                           (float)r[nbody.engineRef.index, 1],
                           (float)r[nbody.engineRef.index, 2]);
    }

    /// <summary>
    /// Get the internal position used by the physics engine. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <returns></returns>
    public Vector3d GetPhysicsPositionDouble(NBody nbody) {

        if (nbody == null || nbody.engineRef == null) {
            // may occur due to startup sequencing
            return Vector3d.zero;
        }
        if (nbody.engineRef.bodyType == GravityEngine.BodyType.MASSLESS) {
            double[] pos = new double[3];
            masslessEngine.GetPositionDouble(nbody, ref pos);
            return new Vector3d(pos[0], pos[1], pos[2]);
        }
        if (nbody.IsFixedOrbit()) {
            // If Kepler evolution 
            Vector3 pos = nbody.engineRef.fixedBody.fixedOrbit.GetPosition();
            return new Vector3d(pos);
        }
        return new Vector3d(r[nbody.engineRef.index, 0],
                           r[nbody.engineRef.index, 1],
                           r[nbody.engineRef.index, 2]);
    }
    /// <summary>
    /// Get the physics velocity for an NBody as a double[]. 
    /// 
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="vel"></param>
    public void GetVelocityDouble(NBody nbody, ref double[] vel) {
        if (nbody.IsFixedOrbit()) {
            // If Kepler evolution 
            Vector3 v = nbody.engineRef.fixedBody.fixedOrbit.GetVelocity();
            vel[0] = v.x;
            vel[1] = v.y;
            vel[2] = v.z;

        } else if (nbody.engineRef.bodyType == GravityEngine.BodyType.MASSLESS) {
            masslessEngine.GetVelocityDouble(nbody, ref vel);
        } else {
            integrator.GetVelocityDoubleForIndex(nbody.engineRef.index, ref vel);
        }
    }

    /// <summary>
    /// Set the physics velocity from a double array. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="velocity"></param>
    /// 
    public void SetVelocityDouble(NBody nbody, ref double[] velocity) {
        if (nbody.engineRef == null) {
            Debug.LogError("nbody has not been added to engine " + nbody.gameObject.name);
        }
        if (nbody.engineRef.bodyType == GravityEngine.BodyType.MASSLESS) {
            masslessEngine.SetVelocityAtIndexDouble(nbody.engineRef.index, ref velocity);
        } else if (nbody.engineRef.bodyType == GravityEngine.BodyType.FIXED) {
            // only OrbitUniversal/FixedObject can set a velocity
            OrbitUniversal orbitU = nbody.GetComponent<OrbitUniversal>();
            if (orbitU != null) {
                orbitU.SetVelocityDouble(new Vector3d(velocity[0], velocity[1], velocity[2]));
                return;
            }
            Debug.LogWarning("Cannot change velocity of fixed body unless it is OrbitUniversal/FixedObject");
            

        } else {
            integrator.SetVelocityDoubleForIndex(nbody.engineRef.index, ref velocity);
        }
    }

    /// <summary>
    /// Set the physics velocity from a double array. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <param name="velocity"></param>
    public void SetPositionDouble(NBody nbody, ref double[] position) {
        if (nbody.engineRef == null) {
            Debug.LogError("nbody has not been added to engine " + nbody.gameObject.name);
        }
        if (nbody.engineRef.bodyType == GravityEngine.BodyType.MASSLESS) {
            masslessEngine.SetPositionAtIndexDouble(nbody.engineRef.index, ref position);
        } else {
            r[nbody.engineRef.index, 0] = position[0];
            r[nbody.engineRef.index, 1] = position[1];
            r[nbody.engineRef.index, 2] = position[2];
        }
    }

    /// <summary>
    /// Return the internal physics engine mass. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <returns></returns>
    public double GetMass(NBody nbody) {
        if (nbody.engineRef.bodyType == GravityEngine.BodyType.MASSLESS) {
            return 0;
        }
        return m[nbody.engineRef.index];
    }

    /// <summary>
    /// Get the internal physics time for massive bodies
    /// </summary>
    /// <returns>physics time for massive body integrator</returns>
    public double GetPhysicsTime() {
        return time;
    }

    /// <summary>
    /// @see GravityEngine#SetPhysicalTime for restrictions.
    /// </summary>
    /// <param name="newTime"></param>
    public void SetTime(double newTime) {
        if (!onRails) {
            // could try to FF?
            Debug.LogWarning("Not all bodies are fixed. Cannot proceed.");
            return;
        }
        time = newTime;
        physicalTime[(int)Evolvers.MASSIVE] = newTime;
        physicalTime[(int)Evolvers.MASSLESS] = newTime;
        physicalTime[(int)Evolvers.PARTICLES] = newTime;
        MoveFixedBodies(time);
    }

    /// <summary>
    /// Return the internal info field that records INACTIVE, FIXED. 
    /// </summary>
    /// <param name="nbody"></param>
    /// <returns></returns>
    public byte GetInfo(NBody nbody) {
        if (nbody.engineRef.bodyType == GravityEngine.BodyType.MASSLESS) {
            return masslessEngine.GetInfo(nbody);
        }
        return info[nbody.engineRef.index];
    }

    public string DumpAll(NBody[] gameNBodies, GravityEngine ge) {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("Massive Bodies:\n");
        for (int i = 0; i < numBodies; i++) {
            Vector3 vel = ge.GetVelocity(gameNBodies[i]);
            int engineIndex = gameNBodies[i].engineRef.index;
            sb.Append(string.Format("   n={0} {1} m={2} r={3} {4} {5} v={6} {7} {8} info={9} engineRef.index={10} extAcc={11}\n", 
                i, gameNBodies[i].name,
                m[i], r[i, 0], r[i, 1], r[i, 2],
                vel.x, vel.y, vel.z, info[i], engineIndex, integrator.GetExternalAccelForIndex(engineIndex)
            ));
        }
        // Title printed by MBE
        if (masslessEngine != null) {
            sb.Append(masslessEngine.DumpString());
        }
        sb.Append("Fixed Bodies:\n");
        for (int i = 0; i < fixedBodies.Count; i++) {
            int fb_index = fixedBodies[i].nbody.engineRef.index;
            sb.Append(string.Format("   n={0} {1} kepler_depth={2}\n",
                i,
                gameNBodies[fb_index].name,
                fixedBodies[i].kepler_depth));
            // add extra info if OrbitU or KeplerSeq
            sb.Append(fixedBodies[i].nbody.engineRef.fixedBody.fixedOrbit.DumpInfo());
        }
        // Any pending maneuvers - TODO
        // sb.Append(maneuverMgr.DumpAll());
        return sb.ToString();
    }
}
