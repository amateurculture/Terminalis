using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Standard Leapfrog algorithm 
// This is vastly better than the standard Euler approach ( x = x_0 + v dt ) because it is energy conserving
// (in the lingo "symplectic"). 
//
// Mark as sealed - may improve performance depending on compiler...

public sealed class LeapFrogIntegrator : INBodyIntegrator {

	private double dt; 
	private int numBodies; 
	private int maxBodies; 
	
	// per body physical parameters. Second index is the dimension. 
	// These are for massive bodies with interactions and NOT particles
	private double[,] v; 
	private double[,] a; 
	
	private double initialEnergy; 
	
	private const double EPSILON = 1E-4; 	// minimum distance for gravitatonal force

	// working variable for Evolve - allocate once
	private double[] rji;	

	private IForceDelegate forceDelegate;

	private GEExternalAcceleration[] externalAccel;

	/// <summary>
	/// Initializes a new instance of the <see cref="LeapFrogIntegrator"/> class.
	/// An optional force delegate can be provided if non-Newtonian gravity is 
	/// desired.
	/// </summary>
	/// <param name="force">Force.</param>
	public LeapFrogIntegrator(IForceDelegate force) {

		forceDelegate = force;
	}

	/// <summary>
	/// Setup the specified maxBodies and timeStep.
	/// </summary>
	/// <param name="maxBodies">Max bodies.</param>
	/// <param name="timeStep">Time step.</param>
	public void Setup(int maxBodies, double timeStep) {
		dt = timeStep; 
		numBodies = 0; 
		this.maxBodies = maxBodies;
		
		v = new double[maxBodies,GravityState.NDIM];
		a = new double[maxBodies,GravityState.NDIM];

		rji = new double[GravityState.NDIM];

        externalAccel = new GEExternalAcceleration[maxBodies];

    }

    public void Clear() {
        numBodies = 0; 
    }

	// Clone this integrator and copy across internal state
	public INBodyIntegrator DeepClone() {
		LeapFrogIntegrator clone = new LeapFrogIntegrator(forceDelegate);
		clone.Setup(maxBodies, dt);
		for (int i=0; i < maxBodies; i++) {
			for (int j=0; j < GravityState.NDIM; j++) {
				clone.v[i,j] = v[i,j];
				clone.a[i,j] = a[i,j];
			}
			clone.externalAccel[i] = externalAccel[i];
		}
		clone.numBodies = numBodies;
		return clone;
	}

	// TODO - If add during sim - would be useful to do a PreEvolve()
	public void AddNBody( int bodyNum, NBody nbody, Vector3 position, Vector3 velocity) {

		if (numBodies > maxBodies) {
			Debug.LogError("Added more than maximum allocated bodies! max=" + maxBodies);
			return;
		}
		if (bodyNum != numBodies) {
			Debug.LogError("Body numbers are out of sync in integrator=" + numBodies + " GE=" + bodyNum);
			return;
		}
		// r,m already in GravityEngine
		v[numBodies,0] = velocity.x; 
		v[numBodies,1] = velocity.y; 
		v[numBodies,2] = velocity.z; 

		// check for engine
        GEExternalAcceleration ext_accel = nbody.GetComponent<GEExternalAcceleration>();
        if (ext_accel != null) {
            externalAccel[numBodies] = ext_accel;
#pragma warning disable 162     // disable unreachable code warning
            if (GravityEngine.DEBUG)
                Debug.Log("Added GEExternalAcceleration engine for " + nbody.gameObject);
#pragma warning restore 162
        }		
		numBodies++;		
	}
	
	public void RemoveBodyAtIndex(int atIndex) {
	
		// shuffle the rest up + internal info
		for( int j=atIndex; j < (numBodies-1); j++) {
			for (int k=0; k < GravityState.NDIM; k++) {
				v[j,k] = v[j+1, k]; 
			}	
			externalAccel[j] = externalAccel[j+1];
		}
		numBodies--; 
	
	}

	public void GrowArrays(int growBy) {
		double[,] v_copy = new double[maxBodies, GravityState.NDIM]; 
		double[,] a_copy = new double[maxBodies, GravityState.NDIM];  
		GEExternalAcceleration[] externalAccelerations_copy = new GEExternalAcceleration[maxBodies];

		for( int j=0; j < numBodies; j++) {
			for (int k=0; k < GravityState.NDIM; k++) {
				v_copy[j,k] = v[j, k]; 
				a_copy[j,k] = a[j, k]; 
			}
			externalAccelerations_copy[j] = externalAccel[j];
		}
		v = new double[maxBodies+growBy, GravityState.NDIM];
		a = new double[maxBodies+growBy, GravityState.NDIM];
		externalAccel = new GEExternalAcceleration[maxBodies+growBy];

		for( int j=0; j < numBodies; j++) {
			for (int k=0; k < GravityState.NDIM; k++) {
				v[j,k] = v_copy[j, k]; 
			}
			externalAccel[j] = externalAccelerations_copy[j];
		}
		maxBodies += growBy;
	}

	public Vector3 GetVelocityForIndex(int i) {
		return new Vector3( (float)v[i,0], (float)v[i,1], (float)v[i,2]);
	}

	public void GetVelocityDoubleForIndex(int i, ref double[] vel) {
		vel[0] = v[i, 0];
		vel[1] = v[i, 1];
		vel[2] = v[i, 2];
	}

	public void SetVelocityForIndex(int i, Vector3 vel) {
		v[i,0] = vel.x;
		v[i,1] = vel.y;
		v[i,2] = vel.z;
	}

    public void SetVelocityDoubleForIndex(int i, ref double[] vel) {
        v[i, 0] = vel[0];
        v[i, 1] = vel[1];
        v[i, 2] = vel[2];
    }

    public Vector3 GetAccelerationForIndex(int i) {
		return new Vector3( (float)a[i,0], (float)a[i,1], (float)a[i,2]);
	}

    public string GetExternalAccelForIndex(int i) {
        string s = "none";
        if (externalAccel[i] != null)
            s = externalAccel.ToString();
        return s;
    }

	public void PreEvolve(GravityState gravityState, ref byte[] info) {
		double[] m = gravityState.m;
		double[,] r = gravityState.r;
		// Precalc initial acceleration
		double[] rji = new double[GravityState.NDIM]; 
		double r2; 
		double r3; 
		double r_sep;
		double accel;

		for (int i=0; i < numBodies; i++) {
			a[i,0] = 0.0;
			a[i,1] = 0.0;
			a[i,2] = 0.0;
		}
		for (int i=0; i < numBodies; i++) {
			for (int j=i+1; j < numBodies; j++) {
				r2 = 0; 
				for (int k=0; k < GravityState.NDIM; k++) {
					rji[k] = r[j,k] - r[i,k];
					r2 += rji[k] * rji[k]; 
				}
				if (forceDelegate == null) {
				r3 = r2 * System.Math.Sqrt(r2) + EPSILON; 
					for (int k=0; k < GravityState.NDIM; k++) {
						a[i,k] += m[j] * rji[k]/r3; 
						a[j,k] -= m[i] * rji[k]/r3;
					}
				} else {
					r_sep = System.Math.Sqrt(r2) + EPSILON; 
					accel = forceDelegate.CalcPseudoForce(r_sep, i, j);
					for (int k=0; k < GravityState.NDIM; k++) {
						a[i,k] += m[j] * accel * (rji[k]/r_sep);
						a[j,k] -= m[i] * accel * (rji[k]/r_sep);
					}
				}
			}
		}	
		
		initialEnergy = NUtils.GetEnergy(numBodies, ref m, ref r, ref v);
	}
			
	public float GetEnergy(GravityState gravityState) {
		return (float) NUtils.GetEnergy(numBodies, ref gravityState.m, ref gravityState.r, ref v);
	}

	public float GetInitialEnergy(GravityState gravityState) {
		return (float) initialEnergy;
	}


	public double Evolve(double time, GravityState gs, ref byte[] info) {

		if (forceDelegate != null) {
			return EvolveForceDelegate(time, gs, ref info);
		}
		int numSteps = 0;

        double timeNow = gs.GetPhysicsTime();

		// If objects are fixed want to use their mass but not update their position
		// Better to calc their acceleration and ignore than add an if statement to core loop. 
		for (double t = 0; t < time; t += dt) {
			numSteps++;
			// Update v and r
			for (int i=0; i < numBodies; i++) {
				if ((info[i] & GravityEngine.INACTIVE_OR_FIXED) == 0) {
					v[i,0] += a[i,0] * 0.5 * dt;
					gs.r[i,0] += v[i,0] * dt;
					v[i,1] += a[i,1] * 0.5 * dt;
					gs.r[i,1] += v[i,1] * dt;
					v[i,2] += a[i,2] * 0.5 * dt;
					gs.r[i,2] += v[i,2] * dt;
				}				
			}
			// advance acceleration
			double r2; 
			double r3;
            double dummy = 0; 

			// a = 0 or init with eternal value
			for (int i=0; i < numBodies; i++) {
	            if ((externalAccel[i] != null) && ((info[i] & GravityEngine.INACTIVE_OR_FIXED) == 0)) {
	                double[] e_accel = externalAccel[i].acceleration(timeNow + t, gs, ref dummy);
	                a[i, 0] = e_accel[0];
	                a[i, 1] = e_accel[1];
	                a[i, 2] = e_accel[2];
	            } else {
					a[i,0] = 0.0;
					a[i,1] = 0.0;
					a[i,2] = 0.0;
				}
			}
			// calc a
			for (int i=0; i < numBodies; i++) {
			   if ((info[i] & GravityEngine.INACTIVE) == 0) {					
			      for (int j=i+1; j < numBodies; j++) {
					 if ((info[j] & GravityEngine.INACTIVE) == 0) {	
					 	// O(N^2) in here, unpack loops to optimize				
						r2 = 0; 
						rji[0] = gs.r[j,0] - gs.r[i,0];
						r2 += rji[0] * rji[0]; 
						rji[1] = gs.r[j,1] - gs.r[i,1];
						r2 += rji[1] * rji[1]; 
						rji[2] = gs.r[j,2] - gs.r[i,2];
						r2 += rji[2] * rji[2]; 
						r3 = r2 * System.Math.Sqrt(r2) + EPSILON; 
						a[i,0] += gs.m[j] * rji[0]/r3; 
						a[j,0] -= gs.m[i] * rji[0]/r3;
						a[i,1] += gs.m[j] * rji[1]/r3; 
						a[j,1] -= gs.m[i] * rji[1]/r3;
						a[i,2] += gs.m[j] * rji[2]/r3; 
						a[j,2] -= gs.m[i] * rji[2]/r3;
					 }
			      }
			   }
			}
			// update velocity
			for (int i=0; i < numBodies; i++) {
				if ((info[i] & GravityEngine.FIXED_MOTION) == 0) {
					v[i,0] += a[i,0] * 0.5 * dt;
					v[i,1] += a[i,1] * 0.5 * dt;
					v[i,2] += a[i,2] * 0.5 * dt;
					//DebugBody(i, ref m, ref r, "evolve");			
				}	
			}
			// coll_time code
		}
		return (numSteps * dt);

		
	}
	
	private void DebugBody(int i, ref double[] m, ref double[,] r, string log) {
		Debug.Log (string.Format("{0} x=({1},{2},{3}) v=({4},{5},{6}) a=({7},{8},{9}) m={10}", log + i ,
		                         r[i,0], r[i,1], r[i,2], v[i,0], v[i,1], v[i,2], a[i,0], a[i,1], a[i,2], m[i]));
	}

	/// <summary>
	/// Evolves using the force delegate. Internals differ slightly and for effeciency do not want
	/// a conditional on forceDelegate in the inner loop. 
	///
	/// </summary>
	/// <returns>The force delegate.</returns>
	/// <param name="time">Time.</param>
	/// <param name="m">M.</param>
	/// <param name="r">The red component.</param>
	/// <param name="info">Info.</param>
	private double EvolveForceDelegate(double time, GravityState gravityState, ref byte[] info) {
	
		int numSteps = 0;
		double[] m = gravityState.m;
		double[,] r = gravityState.r;
		// advance acceleration
		double r2; 
		double r_sep; 
		double f;

        double timeNow = gravityState.GetPhysicsTime();
        double dummy = 0; 

        // If objects are fixed want to use their mass but not update their position
        // Better to calc their acceleration and ignore than add an if statement to core loop. 
        for (double t = 0; t < time; t += dt) {
			numSteps++;
			// Update v and r
			for (int i=0; i < numBodies; i++) {
				if ((info[i] & GravityEngine.INACTIVE_OR_FIXED) == 0) {
					v[i,0] += a[i,0] * 0.5 * dt;
					r[i,0] += v[i,0] * dt;
					v[i,1] += a[i,1] * 0.5 * dt;
					r[i,1] += v[i,1] * dt;
					v[i,2] += a[i,2] * 0.5 * dt;
					r[i,2] += v[i,2] * dt;
				}				
			}

            // a = 0 
            // a = 0 or init with eternal value
            for (int i = 0; i < numBodies; i++) {
                if (externalAccel[i] != null) {
                    double[] e_accel = externalAccel[i].acceleration(timeNow + t, gravityState, ref dummy);
                    a[i, 0] = e_accel[0];
                    a[i, 1] = e_accel[1];
                    a[i, 2] = e_accel[2];
                } else {
                    a[i, 0] = 0.0;
                    a[i, 1] = 0.0;
                    a[i, 2] = 0.0;
                }
            }
            // calc a
            for (int i=0; i < numBodies; i++) {
			   if ((info[i] & GravityEngine.INACTIVE) == 0) {					
			      for (int j=i+1; j < numBodies; j++) {
					 if ((info[j] & GravityEngine.INACTIVE) == 0) {	
					 	// O(N^2) in here, unpack loops to optimize				
						r2 = 0; 
						rji[0] = r[j,0] - r[i,0];
						r2 += rji[0] * rji[0]; 
						rji[1] = r[j,1] - r[i,1];
						r2 += rji[1] * rji[1]; 
						rji[2] = r[j,2] - r[i,2];
						r2 += rji[2] * rji[2]; 
						r_sep = System.Math.Sqrt(r2) + EPSILON; 
						f = forceDelegate.CalcPseudoForce(r_sep, i, j);
						a[i,0] += m[j] * f*(rji[0]/r_sep);
						a[i,1] += m[j] * f*(rji[1]/r_sep);
						a[i,2] += m[j] * f*(rji[2]/r_sep);

						a[j,0] -= m[i] * f* (rji[0]/r_sep);
						a[j,1] -= m[i] * f* (rji[1]/r_sep);
						a[j,2] -= m[i] * f* (rji[2]/r_sep);
					 }
			      }
			   }
			}
			// update velocity
			for (int i=0; i < numBodies; i++) {
				if ((info[i] & GravityEngine.FIXED_MOTION) == 0) {
					v[i,0] += a[i,0] * 0.5 * dt;
					v[i,1] += a[i,1] * 0.5 * dt;
					v[i,2] += a[i,2] * 0.5 * dt;
					//DebugBody(i, ref m, ref r, "evolve");			
				}	
			}
			// coll_time code
		}
		return (numSteps * dt);
	}

}
