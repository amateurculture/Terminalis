using UnityEngine;
using System.Collections;
using System; 	// Math fns
using System.Collections.Generic;	// Dictionary


/// <summary>
/// Evolve particles in the gravitation field computed by the GravityEngine. 
///
/// This script is attached to a Unity ParticleSystem. Initialization of the particles may be handled by a
/// script implementing IGravityParticleInit. Two common examples are DustBall and DustRing. If there is no init
/// delegate the Unity particle creation will be applied. 
///
/// Each instance of GravityParticles manages the gravitational evolution of it's particle system. Particles are
/// massless (they do not affect other particles) and are evolved with a built-in Leapfrog integrator. Particles which
/// move within the particle capture radius of an NBody are made inactive and will not be further evolved. Particles
/// that move outside the GravityEngine horizon are also made inactive. 
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class GravityParticles : MonoBehaviour {

	//! If the particle system has an NBody or NBody parent, add the velocity of the NBody to particles that are created.
	public bool addNBodyVelocity = false;
	//! Initial velocity to be added to particles (may be over-riden by an IGravityParticlesInit script, if present)
	public Vector3 initialVelocity = Vector3.zero;

	private ParticleSystem gravityParticles; 
	private ParticleSystem.Particle[] particles;
	private int particleCount;
	// Useful to know if all particles created at start - removes need to do lifecycle handling
	private bool oneTimeBurst = false; 
	
	private GravityEngine gravityEngine; 

	// summary info
	private int inactiveCount; 

	// eject count unused - not sure a generic Out of Bounds is a good idea. Disable for now.
	#pragma warning disable 0649
	private int ejectCount;  
	#pragma warning restore 0649

	// per-particle activity
	private bool[] inactive;
	private bool allInactive; 
	private bool playing; 

	private double dt = 0f;
	// physics space representation of the particles
	private double[,] r;		// position 
	private double[,] v;		// velocity
	private double[,] a;		// acceleration

	private int numBodies; 
//	private float outOfBoundsSquared; 
	private IGravityParticlesInit particlesInit; 
	private bool burstDone; 

	private GameObject nbodySource;

	private const double EPSILON = 1E-4; 	// minimum distance for gravitatonal force
		
	/*
	Some Notes about particles:
	If all are created as a burst at start and live forever - things are simple. 

	When emission and extinction are happening, things get complicated. 

	If particle lifetime expires a particle from end of the active range is shuffled down. 
	As particles fade and number decreases get more shuffling down until get to 0 active and isStopped is true.

	When burst of long-lived particles are inactive, keep them in particle system but move them out of view
	This enures r,v,a data stays in correspondence to the particles array and these
	arrays do not need to be shuffled when particles are inactivated.

	Particle Lifecycle Handling:
	Code needs to detect when shuffling has happened and shuffle the physical variables as well. 
	- Shuffling detection is done by keeping a copy of each particles random seed and detecting when it has changed. 
	- new seed might because a new particle overwrote, or because an existing particle was shuffled down
	- maintain a hashtable of seeds to physical array indices to determine if this was a shuffle (and copy physics info)
	*/

	private uint[] seed; 	// tracking array for random seed
	private Dictionary<uint,int> particleBySeed; 
	private int lastParticleCount; 

	private const float OUT_OF_VIEW = 10000f;

	// useful in debugging to stop particle removal 
	private const bool allowRemoval = true;
	private const bool debugLogs = GravityEngine.DEBUG;

	void Start () {
		gravityEngine = GravityEngine.instance;
		dt = gravityEngine.GetParticleDt();

//		outOfBoundsSquared = gravityEngine.outOfBounds * gravityEngine.outOfBounds; 

		gravityParticles = GetComponent<ParticleSystem>();
		InitParticleData();

		// Had Play() here - but after Unity 5.3 this broke. Now moved to the update loop. 

		// Get the Init Delegate (if there is one)
		particlesInit = GetComponent<IGravityParticlesInit>();

		if (particlesInit == null && addNBodyVelocity) {
			// Find the associated NBody 
			// Can either be directly attached (particleSystem is attached directly to object with NBody) OR
			// can be the parent (if ParticleSystem has its own Game Object and is a child)
			if (GetComponent<NBody>() != null) {
				nbodySource = transform.gameObject;
			} else if (transform.parent != null && transform.parent.gameObject.GetComponent<NBody>() != null) {
				nbodySource = transform.parent.gameObject;
			} 
		}

		particleBySeed = new Dictionary<uint,int>();

		// determine if this is a one-time burst scenario
		int burstCount = 0; 
		ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[gravityParticles.emission.burstCount]; 
		gravityParticles.emission.GetBursts(bursts);
		foreach (ParticleSystem.Burst burst in bursts) {
			burstCount += burst.maxCount;
		}
		if (burstCount == gravityParticles.main.maxParticles) {
			oneTimeBurst = true;
			burstDone = false;
		}
		#pragma warning disable 162		// disable unreachable code warning
		if (debugLogs) {
			Debug.Log(this.name + " start: oneTimeBurst=" + oneTimeBurst + " burstCount=" +
						 burstCount + " pc=" + gravityParticles.main.maxParticles);
		}
		#pragma warning restore 162
		// self-register with NBE
		gravityEngine.RegisterParticles(this);
		#pragma warning disable 162		// disable unreachable code warning
		if (debugLogs) {
			Debug.Log("Registered with GE: self=" + transform.gameObject.name);
		}
		#pragma warning restore 162
	}
	
	void OnDisable() {
		gravityEngine.DeregisterParticles(this);
	}

	private void InitParticleData() {
		if (gravityParticles == null) {
			Debug.LogError("Must be attached to a particle system object");
			return;
		}
		if (gravityParticles.main.simulationSpace == ParticleSystemSimulationSpace.Local) {
			Debug.LogError("Particle simulation space must be set to World.");
		}
		if (gravityParticles.main.duration > 9999) {
			Debug.LogWarning("Duration > 9999 has resulted in no particles visible in the past.");
		}
		// create array to hold particles
		particles = new ParticleSystem.Particle[gravityParticles.main.maxParticles]; 
		// get particles from the system (this fills in particles[])
		gravityParticles.GetParticles(particles);
		int maxParticles = gravityParticles.main.maxParticles;
		#pragma warning disable 162		// disable unreachable code warning
		if (debugLogs) {
			Debug.Log("Init numParticles=" + maxParticles);
		}
		#pragma warning restore 162
		r = new double[maxParticles,3];
		v = new double[maxParticles,3];
		a = new double[maxParticles,3];
		seed = new uint[maxParticles];
		inactive = new bool[maxParticles];
		allInactive = false;
	}


	// Called by Evolve after particles have been inited. 
	// - calculate the current acceleration on each particle so that on first LF step will evolve smoothly
	private void PreEvolve(int fromP, int toP, int numBodies, GravityState gravityState, ref byte[] info) {
		double[] m = gravityState.m;
		double[,] r_m = gravityState.r;
		// Precalc initial acceleration
		double[] rji = new double[GravityState.NDIM]; 
		double r2; 
		double r3; 
		for (int i=fromP; i < toP; i++) {
			for (int k=0; k < GravityState.NDIM; k++) {
				a[i,k] = 0.0;
			}
		}
		// evolve over all massive objects
		for (int i=0; i < numBodies; i++) {	
			if ((info[i] & GravityEngine.INACTIVE) == 0) {
				for (int j=fromP; j < toP; j++) {
					for (int k=0; k < GravityState.NDIM; k++) {
						rji[k] = r[j,k] - r_m[i,k];
					}
					r2 = 0; 
					for (int k=0; k < GravityState.NDIM; k++) {
						r2 += rji[k] * rji[k]; 
					}
					r3 = r2 * System.Math.Sqrt(r2) + EPSILON; 
					for (int k=0; k < GravityState.NDIM; k++) {
						a[j,k] += m[i] * rji[k]/r3; 
					}
					#pragma warning disable 162, 429		// disable unreachable code warning
					if (debugLogs && j== 0) {
						Debug.Log (string.Format ("PreEvolve : Initial a={0} {1} {2} rji={3} {4} {5} body={6} m={7}", 
							a[0,0], a[0,1], a[0,2], rji[0], rji[1], rji[2], i, m[i]));	
					}
					#pragma warning restore 162, 429
				}
			}
		}			
	}

	// If there is no init delegate use the initial particle position (scaled to physics space) and the
	// initial particle velocity to seed the particle physics data. 
	private void NoInitDelegateSetup(int fromP, int toP) {
		float scale = gravityEngine.physToWorldFactor;
		Vector3 sourceVelocity = Vector3.zero;
		if (addNBodyVelocity && nbodySource != null) {
			sourceVelocity = gravityEngine.GetVelocity(nbodySource);
		} 
		sourceVelocity += initialVelocity;
		for (int i=fromP; i < toP; i++) {
			// particles are in world space - just use their position
			r[i,0] = (particles[i].position.x)/scale;
			r[i,1] = (particles[i].position.y)/scale;
			r[i,2] = (particles[i].position.z)/scale;
			v[i,0] = sourceVelocity.x + particles[i].velocity.x;
			v[i,1] = sourceVelocity.y + particles[i].velocity.y;
			v[i,2] = sourceVelocity.z + particles[i].velocity.z;
			inactive[i] = false;
			particles[i].velocity = Vector3.zero;
		}
	}


	//
	// Emmisive particle management
	// - per cycle look for new particles or cases where particles expired and were shuffled
	//

	void ParticleLifeCycleHandler (int numBodies, GravityState gravityState, ref byte[] info)
	{
		// Particle life cycle management
		// - need GetParticles() call to get the correct number of active particle (p.particleCount did not work)
		// - IIUC this is a re-copy and it would be better to avoid this if possible
		particleCount = gravityParticles.GetParticles (particles);
		if (lastParticleCount < particleCount) {
			// there are new particles
			if (particlesInit != null) {
				particlesInit.InitNewParticles (lastParticleCount, gravityParticles.particleCount, ref r, ref v);
				// apply mass scale to particle velocities

			} else {
				NoInitDelegateSetup(lastParticleCount, gravityParticles.particleCount);
			}
			PreEvolve (lastParticleCount, gravityParticles.particleCount, numBodies, gravityState, ref info);
			for (int i = lastParticleCount; i < gravityParticles.particleCount; i++) {
				inactive[i] = false;
				seed[i] = particles[i].randomSeed;
				particleBySeed[particles[i].randomSeed] = i;
			}
			lastParticleCount = gravityParticles.particleCount;
		}
		if (oneTimeBurst) {
			// not doing life cycle for this particle system
			return;
		}
		// Check if any existing particles were replaced. 
		// As particles expire, Unity will move particles from the end down into their slot and reduce
		// the number of active particles. Need to detect this and move their physics data.
		// This makes emmisive particle systems more CPU intensive. 
		for (int i = 0; i < gravityParticles.particleCount; i++) {
			if (seed[i] != particles[i].randomSeed) {
				#pragma warning disable 162		// disable unreachable code warning
				if (debugLogs)
					Debug.Log("Seed changed was:" + seed[i] + " now:" + particles[i].randomSeed);
				#pragma warning restore 162
				// particle has been replaced
				particleBySeed.Remove (seed [i]);
				// remove old seed from hash
				if (particleBySeed.ContainsKey (particles[i].randomSeed)) {
					// particle was moved - copy physical data down
					int oldIndex = particleBySeed[particles[i].randomSeed];
					for (int k = 0; k < 3; k++) {
						r [i, k] = r [oldIndex, k];
						v [i, k] = v [oldIndex, k];
						a [i, k] = a [oldIndex, k];
					}
					particleBySeed [particles[i].randomSeed] = i;
					#pragma warning disable 162		// disable unreachable code warning
					if (debugLogs)
						Debug.Log("Shuffling particle from " + oldIndex + " to " + i + " vel=" + v[i,0] + " " + v[i,1]);
					#pragma warning restore 162
				}
				else {
					#pragma warning disable 162		// disable unreachable code warning
					if (debugLogs)
						Debug.Log("Reusing particle " + i + " vel=" + particles[i].velocity); 
					#pragma warning restore 162
					if (particlesInit != null) {
						particlesInit.InitNewParticles (i, i + 1, ref r, ref v);
					} else {
						NoInitDelegateSetup(i, i+1);
					}
					PreEvolve (i, i + 1, numBodies, gravityState, ref info);
					particleBySeed[particles[i].randomSeed] = i;
					#pragma warning disable 162		// disable unreachable code warning
					if (debugLogs)
						Debug.Log("Post-Setup Reusing particle " + i + " v[i,0]=" + v[i,0] + " v[i,1]=" + v[i,1]); 
					#pragma warning restore 162
				}
				seed[i] = particles[i].randomSeed;
				inactive[i] = false;
			}
		}
	}

	/// <summary>
	/// Evolve for the specified evolveTime using a dedicated Leapfrog integrator.
	///
	/// Evolve is called by GravityEngine on the fixed update loop. Do not call from scene scripts. 
	/// </summary>
	/// <param name="evolveTime">Evolve time.</param>
	/// <param name="numBodies">Number bodies.</param>
	/// <param name="m">M.</param>
	/// <param name="r_m">R m.</param>
	/// <param name="size2">Size2.</param>
	/// <param name="info">Info.</param>
	public double Evolve(double evolveTime, int numBodies, GravityState gravityState, ref double[] size2, ref byte[] info) {
		// do nothing if all inactive
		if (inactive == null || allInactive) {
			return evolveTime; 	// Particle system has not init-ed yet or is done
		}
		double[] m = gravityState.m;
		double[,] r_m = gravityState.r;
		//  (did not work in Start() -> Unity bug? Init sequencing?)
		if (!playing) {
			gravityParticles.Play();
			playing = true;
		}

		ParticleLifeCycleHandler (numBodies, gravityState, ref info);
		//
		// physics integration of particles in the field of the masses passed in by NBE
		//
		double time = 0; 
		while (time < evolveTime) {
			time += dt;
			// Update v and r
			for (int i=0; i < particleCount; i++) {
				if (!inactive[i]) {
					v[i,0] += a[i,0] * 0.5 * dt;
					r[i,0] += v[i,0] * dt;
					v[i,1] += a[i,1] * 0.5 * dt;
					r[i,1] += v[i,1] * dt;
					v[i,2] += a[i,2] * 0.5 * dt;
					r[i,2] += v[i,2] * dt;
				}			
			}
			// advance acceleration
			double[] rji = new double[GravityState.NDIM]; 
			double r2; 	// r squared
			double r3;  // r cubed
			
			// a = 0 
			for (int i=0; i < particleCount; i++) {
				a[i,0] = 0.0;
				a[i,1] = 0.0;
				a[i,2] = 0.0;
			}
			// calc a
			for (int i=0; i < numBodies; i++) {
				// check mass has inactive clear
				if ((info[i] & GravityEngine.INACTIVE) == 0) {
					for (int j=0; j < particleCount; j++) {
						// only evolve active particles
						if (!inactive[j]) {
							rji[0] = r[j,0] - r_m[i,0];
							rji[1] = r[j,1] - r_m[i,1];
							rji[2] = r[j,2] - r_m[i,2];

							r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
							// Check for incursion on massive bodies and inactivate particles that have collided
							// (Do not want to incur collider overhead per particle)
							if (allowRemoval && r2 < size2[i]) {
								inactive[j] = true; 
								inactiveCount++;
								#pragma warning disable 162		// disable unreachable code warning
								if (debugLogs) {
									Debug.Log (string.Format ("Inactivate particle {0} size2={1} due to object at {2} {3} {4} r2={5} count={6} p={7} {8}",
								                  j, size2[i],  r_m[i,0], r_m[i,1], r_m[i,2], r2, inactiveCount, r[j,0], r[j,1]));
								}
								#pragma warning restore 162
								if (oneTimeBurst) {
									r[j,0] = OUT_OF_VIEW;
								} else {
									particles[j].remainingLifetime = 0;
								}
								continue;
							}					
							r3 = r2 * System.Math.Sqrt(r2) + EPSILON; 
							a[j,0] -= m[i] * rji[0]/r3;
							a[j,1] -= m[i] * rji[1]/r3;
							a[j,2] -= m[i] * rji[2]/r3;
						} // for j
					} // info
				} // for i
			}
			// update velocity
			for (int i=0; i < particleCount; i++) {
				if (!inactive[i]) {
					v[i,0] += a[i,0] * 0.5 * dt;
					v[i,1] += a[i,1] * 0.5 * dt;
					v[i,2] += a[i,2] * 0.5 * dt;
				}				
			}
		} // while
		return time;
	}

	/// <summary>
	/// Evolves particles using massice bodies the with force delegate provided.
	/// </summary>
	/// <returns>The with force.</returns>
	/// <param name="evolveTime">Evolve time.</param>
	/// <param name="numBodies">Number bodies.</param>
	/// <param name="m">M.</param>
	/// <param name="r_m">R m.</param>
	/// <param name="size2">Size2.</param>
	/// <param name="info">Info.</param>
	/// <param name="force">Force.</param>
	public double EvolveWithForce(double evolveTime, 
									int numBodies, 
									GravityState gravityState,
									ref double[] size2, 
									ref byte[] info, 
									IForceDelegate force) {
		// do nothing if all inactive
		if (inactive == null || allInactive) {
			return evolveTime; 	// Particle system has not init-ed yet or is done
		}
		//  (did not work in Start() -> Unity bug? Init sequencing?)
		if (!playing) {
			gravityParticles.Play();
			playing = true;
		}
		double[] m = gravityState.m;
		double[,] r_m = gravityState.r;

		ParticleLifeCycleHandler (numBodies, gravityState, ref info);
		//
		// physics integration of particles in the field of the masses passed in by NBE
		//
		double time = 0; 
		while (time < evolveTime) {
			time += dt;
			// Update v and r
			for (int i=0; i < particleCount; i++) {
				if (!inactive[i]) {
					v[i,0] += a[i,0] * 0.5 * dt;
					r[i,0] += v[i,0] * dt;
					v[i,1] += a[i,1] * 0.5 * dt;
					r[i,1] += v[i,1] * dt;
					v[i,2] += a[i,2] * 0.5 * dt;
					r[i,2] += v[i,2] * dt;
				}			
			}
			// advance acceleration
			double[] rji = new double[GravityState.NDIM]; 
			double r2; 	// r squared
			double r_sep;  // r 
			double accel;
			// a = 0 
			for (int i=0; i < particleCount; i++) {
				a[i,0] = 0.0;
				a[i,1] = 0.0;
				a[i,2] = 0.0;
			}
			// calc a
			for (int i=0; i < numBodies; i++) {
				// check mass has inactive clear
				if ((info[i] & GravityEngine.INACTIVE) == 0) {
					for (int j=0; j < particleCount; j++) {
						// only evolve active particles
						if (!inactive[j]) {
							rji[0] = r[j,0] - r_m[i,0];
							rji[1] = r[j,1] - r_m[i,1];
							rji[2] = r[j,2] - r_m[i,2];
							// Particles that have moved outside of view are marked inactive

							r2 = rji[0] * rji[0] + rji[1] * rji[1] + rji[2] * rji[2];
							// Check for incursion on massive bodies and inactivate particles that have collided
							// (Do not want to incur collider overhead per particle)
							if (allowRemoval && r2 < size2[i]) {
								inactive[j] = true; 
								inactiveCount++;
								#pragma warning disable 162		// disable unreachable code warning
								if (debugLogs) {
									Debug.Log (string.Format ("Inactivate particle {0} size2={1} due to object at {2} {3} {4} r2={5} count={6} p={7} {8}",
								                  j, size2[i],  r_m[i,0], r_m[i,1], r_m[i,2], r2, inactiveCount, r[j,0], r[j,1]));
								}
								#pragma warning restore 162
								if (oneTimeBurst) {
									r[j,0] = OUT_OF_VIEW;
								} else {
									particles[j].remainingLifetime = 0;
								}
								continue;
							}					
							r_sep = System.Math.Sqrt(r2) + EPSILON; 
							accel = force.CalcPseudoForce(r_sep, i, j);
							a[j,0] -= m[i] * accel * (rji[0]/r_sep);
							a[j,1] -= m[i] * accel * (rji[1]/r_sep);
							a[j,2] -= m[i] * accel * (rji[2]/r_sep);
						} // for j
					} // info
				} // for i
			}
			// update velocity
			for (int i=0; i < particleCount; i++) {
				if (!inactive[i]) {
					v[i,0] += a[i,0] * 0.5 * dt;
					v[i,1] += a[i,1] * 0.5 * dt;
					v[i,2] += a[i,2] * 0.5 * dt;
				}				
			}
		} // while
		return time;
	}

	#pragma warning disable 414 // unused if debug const is false		
	private int debugCnt = 0; 
	#pragma warning restore 414		

	/// <summary>
	/// Updates the particles positions in world space.
	///
	/// UpdateParticles is called from the GravityEngine. Do not call from other scripts. 
	/// </summary>
	/// <param name="physicalScale">Physical scale.</param>
	public void UpdateParticles(float physicalScale, GravityEngine ge) {
		if (allInactive) {
			return;
		}
		for (int i=0; i < lastParticleCount; i++) {
			particles[i].position = ge.MapToScene(new Vector3((float) r[i,0] * physicalScale, 
												    (float) r[i,1] * physicalScale, 
												    (float) r[i,2] * physicalScale));
		}
		gravityParticles.SetParticles(particles, particleCount);
		// must be after display - so final inactivated particles are removed
		if (oneTimeBurst && burstDone && ((ejectCount + inactiveCount) >= particleCount)) {
			allInactive = true;
			#pragma warning disable 162		// disable unreachable code warning
			if (debugLogs)
				Debug.Log("All particles inactive! time = " + Time.time + " ejected=" + ejectCount + " inactive=" + inactiveCount + 
					" remaining=" + (particleCount - inactiveCount - ejectCount));
			#pragma warning restore 162
		}
		#pragma warning disable 162, 429		// disable unreachable code warning
		if (debugLogs && debugCnt++ > 30) {
			debugCnt = 0; 
			string log = "time = " + Time.time + " ejected=" + ejectCount + " inactive=" + inactiveCount + 
					" remaining=" + (particleCount - inactiveCount - ejectCount);
			log += " is Stopped " + gravityParticles.isStopped + " num=" + gravityParticles.particleCount + " pcount=" + particleCount + "\n";
			int logTo = (gravityParticles.main.maxParticles < 10) ? gravityParticles.main.maxParticles : 10;
			for (int i=0; i < logTo; i++) {
				log += string.Format("{0}  rand={1} life={2} inactive={3} ", i, particles[i].randomSeed, particles[i].remainingLifetime, inactive[i]);
				log += " pos=" + particles[i].position ;
				log += " phyPos= " + r[i,0] + " " + r[i,1] + " " + r[i,2];
				log += "\n";
			}
			Debug.Log(log);
		}
		#pragma warning restore 162, 429
	}

    public void MoveAll(ref double[] moveBy) {
        for (int i = 0; i < lastParticleCount; i++) {
            r[i, 0] += moveBy[0];
            r[i, 1] += moveBy[1];
            r[i, 2] += moveBy[2];
        }
    }

 }
