//
//  Hermite.m
//  zBody
//
//  The numerical core is adapted from the the code from GravityLab (hermite8.c). All the useful
//  descriptive comments are from that code. 
/*
Original Code subject to:
Copyright (c) 2004 -- present, Piet Hut & Jun Makino

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
IN THE SOFTWARE.
*/
//
//
using UnityEngine;
using System;

public sealed class HermiteIntegrator : INBodyIntegrator {

    private double[,] vel;
    private double[,] acc;
    private double[,] jerk;

    private double[,] old_pos;
    private double[,] old_vel;
    private double[,] old_acc;
    private double[,] old_jerk;
    private double[] size;
    private bool[] active;

    private GEExternalAcceleration[] externalAccel;

    private double dt;
    private double dtInitial;
    private double maxDt;
    private double minDt;
    
    int numBodies;       // number of bodies that have been added
    int maxBodies;

    // collision time is used for dynamic adjustment of timestep. It is normalized to the initial collision
    // time. This assumes objects are not very close to each other at the start.
    private double coll_time;  // collision time
    private double coll_time_initial;
    private double coll_time_avg;

    // Game wrapper to limit number of evolution loops

    private static double COLL_AVERAGE_FACTOR = 0.8;
    private int logCount = 0;
    private static int LOG_INTERVAL = 20000;

    // global so can be passed to GEExternalAccel w/o a lot of stack push/pop
    private double t_evolveStart;
    private double t_offset;
    private GravityState gravityState;
    		
	private double initialEnergy; 
        
	private const int NDIM = GravityState.NDIM;

	private const double EPSILON = 1E-3; 	// minimum distance for gravitatonal force

	private IForceDelegate forceDelegate;

    private static double MAX_LOOPS = 50;


    public HermiteIntegrator(IForceDelegate force) {
		#pragma warning disable 162		// disable unreachable code warning
		if (GravityEngine.DEBUG)
			Debug.Log("Instantiated with " + force);
		#pragma warning restore 162

		forceDelegate = force;
	}

	public INBodyIntegrator DeepClone() {
		HermiteIntegrator herm = new HermiteIntegrator(forceDelegate);
		herm.maxBodies = maxBodies;
		herm.Reset(maxBodies);
		// copy across
		herm.numBodies = numBodies;
		for( int j=0; j < numBodies; j++) {
			for (int k=0; k < NDIM; k++) {
				herm.vel[j,k] = vel[j, k]; 
				herm.acc[j,k] = acc[j, k]; 
				herm.jerk[j,k] = jerk[j, k]; 
				herm.old_pos[j,k] = old_pos[j, k]; 
				herm.old_vel[j,k] = old_vel[j, k]; 
				herm.old_acc[j,k] = old_acc[j, k]; 
				herm.old_jerk[j,k] = old_jerk[j, k];
			}
			herm.active[j] = active[j];
			herm.size[j] = size[j];
            herm.externalAccel[j] = externalAccel[j];
        }
        herm.dt = dt;
        herm.maxDt = maxDt;
        herm.minDt = minDt;
        herm.coll_time_initial = coll_time_initial;
        herm.coll_time_avg = coll_time_avg;
        herm.coll_time = coll_time;
        herm.dtInitial = dtInitial;
        return herm;
	}

    public void Setup(int maxBodies, double timeStep) {

		dt = timeStep; 
		dtInitial = dt;
		coll_time = double.MaxValue;
		numBodies = 0; 
		this.maxBodies = maxBodies;
		Reset(maxBodies);		

        maxDt = timeStep;    // minimum of 1 evolutions per update
        minDt = timeStep / MAX_LOOPS; // maximum of MAX_LOOPS evolutions per update
    }

    public void Clear() {
        numBodies = 0; 
    }

	public void AddNBody( int bodyNum, NBody nbody, Vector3 position, Vector3 velocity) {

		if (numBodies > maxBodies) {
			Debug.LogError("Added more than maximum allocated bodies! max=" + maxBodies);
			return;
		}
		if (bodyNum != numBodies) {
			Debug.LogError("Body numbers are out of sync integrator=" + numBodies + " GE=" + bodyNum);
			return;
		}
		active[numBodies] = true;
		// r,m already in GravityEngine
		vel[numBodies,0] = velocity.x; 
		vel[numBodies,1] = velocity.y; 
		vel[numBodies,2] = velocity.z;

        // check for engine
        GEExternalAcceleration ext_accel = nbody.GetComponent<GEExternalAcceleration>();
        if (ext_accel != null) {
            externalAccel[numBodies] = ext_accel;
#pragma warning disable 162     // disable unreachable code warning
            if (GravityEngine.DEBUG)
                Debug.Log("Added GEExternalAcceleration engine for " + nbody.gameObject);
#pragma warning restore 162
        }
        //		Debug.Log ("add object at r=" + gameObject.transform.position + " v=" + ubody.vel + " size2=" + size2[numBodies]
        //					+ " mass=" + ubody.mass);
        numBodies++;
	}	

	public void RemoveBodyAtIndex(int atIndex) {
		
		// shuffle the rest up + internal info
		for( int j=atIndex; j < (numBodies-1); j++) {
			for (int k=0; k < NDIM; k++) {
				vel[j,k] = vel[j+1, k]; 
				acc[j,k] = acc[j+1, k]; 
				jerk[j,k] = jerk[j+1, k]; 
				old_pos[j,k] = old_pos[j+1, k]; 
				old_vel[j,k] = old_vel[j+1, k]; 
				old_acc[j,k] = old_acc[j+1, k]; 
				old_jerk[j,k] = old_jerk[j+1, k]; 
			}
			active[j] = active[j+1];
            externalAccel[j] = externalAccel[j + 1];
		}
		numBodies--; 
		
	}

	public void GrowArrays(int growBy) {
		int newSize = maxBodies + growBy;
		double[,] vel_copy = new double[maxBodies, NDIM];
		double[,] acc_copy = new double[maxBodies, NDIM];
		double[,] jerk_copy = new double[maxBodies, NDIM];

		double[,] old_pos_copy = new double[maxBodies, NDIM];
		double[,] old_vel_copy = new double[maxBodies, NDIM];
		double[,] old_acc_copy = new double[maxBodies, NDIM];
		double[,] old_jerk_copy = new double[maxBodies, NDIM];
		double[] size_copy = new double[maxBodies]; 
		bool[] active_copy = new bool[maxBodies];
        GEExternalAcceleration[] externalAccelerations_copy = new GEExternalAcceleration[maxBodies];
		for( int j=0; j < maxBodies; j++) {
			for (int k=0; k < NDIM; k++) {
				vel_copy[j,k] = vel[j, k]; 
				acc_copy[j,k] = acc[j, k]; 
				jerk_copy[j,k] = jerk[j, k]; 
				old_pos_copy[j,k] = old_pos[j, k]; 
				old_vel_copy[j,k] = old_vel[j, k]; 
				old_acc_copy[j,k] = old_acc[j, k]; 
				old_jerk_copy[j,k] = old_jerk[j, k]; 	
			}
			active_copy[j] = active[j];
			size_copy[j] = size[j];
            externalAccelerations_copy[j] = externalAccel[j];
		}

		size = new double[newSize];
		vel = new double[newSize, NDIM];
		acc = new double[newSize, NDIM];
		jerk = new double[newSize, NDIM];
		
		old_pos = new double[newSize, NDIM];
		old_vel = new double[newSize, NDIM];
		old_acc = new double[newSize, NDIM];
		old_jerk = new double[newSize, NDIM];
			
		active = new bool[newSize];
		size = new double[newSize];
        externalAccel = new GEExternalAcceleration[newSize];

		for( int j=0; j < maxBodies; j++) {
			for (int k=0; k < NDIM; k++) {
				vel[j,k] = vel_copy[j, k]; 
				acc[j,k] = acc_copy[j, k]; 
				jerk[j,k] = jerk_copy[j, k]; 
				old_pos[j,k] = old_pos_copy[j, k]; 
				old_vel[j,k] = old_vel_copy[j, k]; 
				old_acc[j,k] = old_acc_copy[j, k]; 
				old_jerk[j,k] = old_jerk_copy[j, k]; 	
			}
			active[j] = active_copy[j];
			size[j] = size_copy[j];
            externalAccel[j] = externalAccelerations_copy[j];
		}
		maxBodies = newSize;
	}

	public Vector3 GetVelocityForIndex(int i) {
		return new Vector3( (float)vel[i,0], (float)vel[i,1], (float)vel[i,2]);
	}

	public void GetVelocityDoubleForIndex(int i, ref double[] v) {
		v[0] = vel[i, 0];
		v[1] = vel[i, 1];
		v[2] = vel[i, 2];
	}

	public void SetVelocityForIndex(int i, Vector3 velocity) {
		vel[i,0] = velocity.x;
		vel[i,1] = velocity.y;
		vel[i,2] = velocity.z;
	}

    public void SetVelocityDoubleForIndex(int i, ref double[] v) {
        vel[i, 0] = v[0];
        vel[i, 1] = v[1];
        vel[i, 2] = v[2];
    }

    public Vector3 GetAccelerationForIndex(int i) {
		return new Vector3( (float)acc[i,0], (float)acc[i,1], (float)acc[i,2]);
	}

	public float GetEnergy(GravityState gravityState) {
		return (float) NUtils.GetEnergy(numBodies, ref gravityState.m, ref gravityState.r, ref vel);
	}
	
	public float GetInitialEnergy(GravityState gravityState) {
		return (float) initialEnergy;
	}
	

	public void Reset(int numEntries)
	{
		size = new double[numEntries];
		vel = new double[numEntries, NDIM];
		acc = new double[numEntries, NDIM];
		jerk = new double[numEntries, NDIM];
		
		old_pos = new double[numEntries, NDIM];
		old_vel = new double[numEntries, NDIM];
		old_acc = new double[numEntries, NDIM];
		old_jerk = new double[numEntries, NDIM];
			
		active = new bool[numEntries];
        externalAccel = new GEExternalAcceleration[numEntries];
	    numBodies = 0;
	}



	/*-----------------------------------------------------------------------------
	 *  evolve_step  --  takes one integration step for an N-body system, using the
	 *                   Hermite algorithm.
	 * PM - modified to return  dt
	 *-----------------------------------------------------------------------------
	 */

	private double Evolve_step(ref double[] mass, ref double[,] pos, ref byte[] info)
	{
	    for (int i = 0; i < numBodies ; i++)
	        for (int k = 0; k < NDIM ; k++){
	            old_pos[i,k] = pos[i,k];
	            old_vel[i,k] = vel[i,k];
	            old_acc[i,k] = acc[i,k];
	            old_jerk[i,k] = jerk[i,k];
	        }
	    
	    Predict_step(ref pos, ref info);
		Get_acc_jerk_pot_coll(ref mass, ref pos, ref info);
		Correct_step(ref pos, ref info);
		
	    return dt; 
	}


	/*-----------------------------------------------------------------------------
	 *  predict_step  --  takes the first approximation of one Hermite integration
	 *                    step, advancing the positions and velocities through a
	 *                    Taylor series development up to the order of the jerks.
	 *-----------------------------------------------------------------------------
	 */

	private void Predict_step(ref double[,] pos, ref byte[] info)
	{
	    for (int i = 0; i < numBodies ; i++)
	    {
			if ((info[i] & (GravityEngine.INACTIVE + GravityEngine.FIXED_MOTION)) != 0) 					
				continue;
	    
	        for (int k = 0; k < NDIM ; k++){
	            pos[i,k] += vel[i,k]*dt + acc[i,k]*dt*dt/2 + jerk[i,k]*dt*dt*dt/6;
	            vel[i,k] += acc[i,k]*dt + jerk[i,k]*dt*dt/2;
	        }
	    }
	}

	/*-----------------------------------------------------------------------------
	 *  correct_step  --  takes one iteration to improve the new values of position
	 *                    and velocities, effectively by using a higher-order
	 *                    Taylor series constructed from the terms up to jerk at
	 *                    the beginning and the end of the time step.
	 *-----------------------------------------------------------------------------
	 */

	private void Correct_step(ref double[,] pos, ref byte[] info)
	{
	    for (int i = 0; i < numBodies ; i++)
	    {
			if ((info[i] & (GravityEngine.INACTIVE + GravityEngine.FIXED_MOTION)) != 0) 					
				continue;
			
	        for (int k = 0; k < NDIM ; k++){
	            vel[i,k] = old_vel[i,k] + (old_acc[i,k] + acc[i,k])*dt/2
	            + (old_jerk[i,k] - jerk[i,k])*dt*dt/12;
	            pos[i,k] = old_pos[i,k] + (old_vel[i,k] + vel[i,k])*dt/2
	            + (old_acc[i,k] - acc[i,k])*dt*dt/12;
	        }
	    }
	}


	/*-----------------------------------------------------------------------------
	 *  get_acc_jerk_pot_coll  --  calculates accelerations and jerks, and as side
	 *                             effects also calculates potential energy and
	 *                             the time scale coll_time for significant changes
	 *                             in local configurations to occur.
	 *                                                  __                     __
	 *                                                 |          -->  -->       |
	 *               M                           M     |           r  . v        |
	 *   -->          j    -->       -->          j    | -->        ji   ji -->  |
	 *    a   ==  --------  r    ;    j   ==  -------- |  v   - 3 ---------  r   |
	 *     ji     |-->  |3   ji        ji     |-->  |3 |   ji      |-->  |2   ji |
	 *            | r   |                     | r   |  |           | r   |       |
	 *            |  ji |                     |  ji |  |__         |  ji |     __|
	 *
	 *  note: it would be cleaner to calculate potential energy and collision time
	 *        in a separate function.  However, the current function is by far the
	 *        most time consuming part of the whole program, with a double loop
	 *        over all particles that is executed every time step.  Splitting off
	 *        some of the work to another function would significantly increase
	 *        the total computer time (by an amount close to a factor two).
	 *
	 *  We determine the values of all four quantities of interest by walking
	 *  through the system in a double {i,j} loop.  The first three, acceleration,
	 *  jerk, and potential energy, are calculated by adding successive terms;
	 *  the last, the estimate for the collision time, is found by determining the
	 *  minimum value over all particle pairs and over the two choices of collision
	 *  time, position/velocity and sqrt(position/acceleration), where position and
	 *  velocity indicate their relative values between the two particles, while
	 *  acceleration indicates their pairwise acceleration.  At the start, the
	 *  first three quantities are set to zero, to prepare for accumulation, while
	 *  the last one is set to a very large number, to prepare for minimization.
	 *       The integration loops only over half of the pairs, with j > i, since
	 *  the contributions to the acceleration and jerk of particle j on particle i
	 *  is the same as those of particle i on particle j, apart from a minus sign
	 *  and a different mass factor.
	 *-----------------------------------------------------------------------------
	 */

	double coll_time_q = double.MaxValue;   // collision time to 4th power
    double coll_est_q;                      // collision time scale estimate
    
    double[] rji = new double[NDIM];        // particle i to particle j
    double[] vji = new double[NDIM];        // vji[] = d rji[] / d t

    double[] da = new double[NDIM];         // main terms in pairwise
    double[] dj = new double[NDIM];         // acceleration and jerk

    double r, r2, r3, v2, rv_r2;

    private void Get_acc_jerk_pot_coll(ref double[] mass, ref double[,] pos, ref byte[] info)
	{
        // no dynamic assignments in inner loop
        double dummy = 0; // unused

        // reset coll_time_q each iteration
        coll_time_q = double.MaxValue;

        double timeNow = t_evolveStart + t_offset;
        for (int i=0; i < maxBodies; i++) {
            jerk[i, 0] = 0;
            jerk[i, 1] = 0;
            jerk[i, 2] = 0;
            if ((externalAccel[i] != null) && ((info[i] & GravityEngine.INACTIVE_OR_FIXED) == 0)) {
                double[] e_accel = externalAccel[i].acceleration(timeNow, gravityState, ref dummy);
                acc[i, 0] = e_accel[0];
                acc[i, 1] = e_accel[1];
                acc[i, 2] = e_accel[2];
            } else {
                acc[i, 0] = 0;
                acc[i, 1] = 0;
                acc[i, 2] = 0;
            }
		}

		// If we have a force delegate - use our "twin"
		if (forceDelegate != null) {
			Get_acc_jerk_pot_coll_delegate(ref mass, ref pos, ref info);
			return;
		}
	
	    // to 4th power (quartic)
	    for (int i = 0; i < numBodies ; i++)
	    {
			if ((info[i] & GravityEngine.INACTIVE) != 0) 					
				continue;
	        
	        for (int j = i+1; j < numBodies ; j++)
	        {            
	
				if ((info[j] & GravityEngine.INACTIVE) != 0) 					
					continue;
	            
	            for (int k = 0; k < NDIM ; k++){
	                rji[k] = pos[j,k] - pos[i,k];
	                vji[k] = vel[j,k] - vel[i,k];
	            }
	            r2 = 0;                           // | rji |^2
	            v2 = 0;                           // | vji |^2
	            rv_r2 = 0;                        // ( rij . vij ) / | rji |^2
	            for (int k = 0; k < NDIM ; k++){
	                r2 += rji[k] * rji[k];
	                v2 += vji[k] * vji[k];
	                rv_r2 += rji[k] * vji[k];
	            }
	            rv_r2 /= r2 + EPSILON;
	            r = Math.Sqrt(r2);                     // | rji |
	            r3 = r * r2 + EPSILON;                 // | rji |^3
	            	            
	            // add the {j (i)} contribution to the {i (j)} values of acceleration and jerk:
	            
	            for (int k = 0; k < NDIM ; k++){
	                da[k] = rji[k] / r3;                           // see equations
	                dj[k] = (vji[k] - 3 * rv_r2 * rji[k]) / r3;    // in the header
	                acc[i,k] += mass[j] * da[k];                 // using symmetry
	                acc[j,k] -= mass[i] * da[k];                 // find pairwise
	                jerk[i,k] += mass[j] * dj[k];                // acceleration
	                jerk[j,k] -= mass[i] * dj[k];                // and jerk
	            }
	            
	            // first collision time estimate, based on unaccelerated linear motion:
	            
	            coll_est_q = (r2*r2) / (v2*v2);
	            if (coll_time_q > coll_est_q)
	                coll_time_q = coll_est_q;
	            
	            //// second collision time estimate, based on free fall:
	            
	            //double da2 = 0;                                // da2 becomes the
	            //for (int k = 0; k < NDIM ; k++)                // square of the
	            //    da2 += da[k] * da[k];                      // pair-wise accel-
	            //double mij = mass[i] + mass[j];                // eration between
	            //da2 *= mij * mij;                              // particles i and j
	            
	            //coll_est_q = r2/da2;
	            //if (coll_time_q > coll_est_q)
	            //    coll_time_q = coll_est_q;
	        }
	    }                                               // from q for quartic back
	    if (numBodies > 1)
	    {
	        coll_time = Math.Sqrt(Math.Sqrt (coll_time_q));            // to linear collision time
	    }
	}

	/// <summary>
	/// Gets the acc jerk pot coll delegate using a force delegate. Near clone of above to avoid a conditional
	/// in the performance critical N^2 loop. 
	///
	/// Generalizing the above formula for an arbitrary force we get:
	///
	///         ( v_ij . r_ij ) f'(r)  - ( v_ij . r_ij ) f(r)  + v_ij f(r)
	/// j_ji =  ---------------------    --------------------    ---------
	///                   2                       3                
	///                 r                       r                    r
	///  
	/// </summary>
	/// <param name="mass">Mass.</param>
	/// <param name="pos">Position.</param>
	/// <param name="info">Info.</param>
	private void Get_acc_jerk_pot_coll_delegate(ref double[] mass, ref double[,] pos, ref byte[] info)
	{
		double f;
		double fdot;

        // external accels were pre-loaded prior to this call

        coll_time_q = double.MaxValue;
        double dummy = 0; // unused

        double timeNow = t_evolveStart + t_offset;
        for (int i = 0; i < maxBodies; i++) {
            jerk[i, 0] = 0;
            jerk[i, 1] = 0;
            jerk[i, 2] = 0;
            if ((externalAccel[i] != null) && ((info[i] & GravityEngine.INACTIVE_OR_FIXED) == 0)) {
                double[] e_accel = externalAccel[i].acceleration(timeNow, gravityState, ref dummy);
                acc[i, 0] = e_accel[0];
                acc[i, 1] = e_accel[1];
                acc[i, 2] = e_accel[2];
            } else {
                acc[i, 0] = 0;
                acc[i, 1] = 0;
                acc[i, 2] = 0;
            }
        }

        // to 4th power (quartic)
        for (int i = 0; i < numBodies ; i++)
	    {
			if ((info[i] & GravityEngine.INACTIVE) != 0) 					
				continue;
	        
	        for (int j = i+1; j < numBodies ; j++)
	        {            
	
				if ((info[j] & GravityEngine.INACTIVE) != 0) 					
					continue;
	            
	            for (int k = 0; k < NDIM ; k++){
	                rji[k] = pos[j,k] - pos[i,k];
	                vji[k] = vel[j,k] - vel[i,k];
	            }
	            r2 = 0;                           // | rji |^2
	            v2 = 0;                           // | vji |^2
	            rv_r2 = 0;                        // ( rij . vij ) / | rji |^2
	            for (int k = 0; k < NDIM ; k++){
	                r2 += rji[k] * rji[k];
	                v2 += vji[k] * vji[k];
	                rv_r2 += rji[k] * vji[k];
	            }
	            rv_r2 /= r2 + EPSILON;
	            r = Math.Sqrt(r2);                     // | rji |
	            	            
	            // add the {j (i)} contribution to the {i (j)} values of acceleration and jerk:
	            f = forceDelegate.CalcPseudoForce(r, i, j);
	            fdot = forceDelegate.CalcPseudoForceDot(r, i, j);
	            for (int k = 0; k < NDIM ; k++){
	                da[k] = f * rji[k] / r;                   // see equations
	                dj[k] = (rv_r2 * fdot)*rji[k] - (rv_r2 * f)*rji[k]/r + (vji[k] * f)/r;
	                acc[i,k] += mass[j] * da[k];                 // using symmetry
	                acc[j,k] -= mass[i] * da[k];                 // find pairwise
	                jerk[i,k] += mass[j] * dj[k];                // acceleration
	                jerk[j,k] -= mass[i] * dj[k];                // and jerk
	            }

                // first collision time estimate, based on unaccelerated linear motion:
                coll_est_q = (r2*r2) / (v2*v2);
	            if (coll_time_q > coll_est_q)
	                coll_time_q = coll_est_q;
	            
	            // second collision time estimate, based on free fall:	            
	        //    double da2 = 0;                                // da2 becomes the
	        //    for (int k = 0; k < NDIM ; k++)                // square of the
	        //        da2 += da[k] * da[k];                      // pair-wise accel-
	        //    double mij = mass[i] + mass[j];                // eration between
	        //    da2 *= mij * mij;                              // particles i and j
	            
	        //    coll_est_q = r2/da2;
	        //    if (coll_time_q > coll_est_q)
	        //        coll_time_q = coll_est_q;
	        }
	    }                                               // from q for quartic back
	    if (numBodies > 1)
	    {
	        coll_time = Math.Sqrt(Math.Sqrt (coll_time_q));            // to linear collision time
	    }
	}


    public void PreEvolve(GravityState gravityState, ref byte[] info)
	{
		double[] mass = gravityState.m;
		double[,] pos = gravityState.r;
        this.gravityState = gravityState;
	    Get_acc_jerk_pot_coll(ref mass, ref pos, ref info);
        coll_time_initial = coll_time;
        coll_time_avg = coll_time;
		initialEnergy = NUtils.GetEnergy(numBodies, ref mass, ref pos, ref vel);
	}

 
	public double Evolve(double time, GravityState gravityState, ref byte[] info)
	{
		double[] mass = gravityState.m;
		double[,] pos = gravityState.r;
        this.gravityState = gravityState;
	    t_offset = 0;
        t_evolveStart = gravityState.GetPhysicsTime();

        int nloops = 0; 
        if (dt > time) {
            dt = time;
        }
	        
	    while (t_offset < time)
	    {
	        
	        // If only one body collision time will be MAXFLOAT
	        t_offset += dt; 
	        Evolve_step(ref mass, ref pos, ref info);
            // timestep adjustement, relative to initial collision time
            coll_time_avg = COLL_AVERAGE_FACTOR * coll_time_avg + (1 - COLL_AVERAGE_FACTOR) * coll_time;
	        dt = dtInitial * coll_time_avg/coll_time_initial;
            if (dt > maxDt) {
                dt = maxDt;
            } else if (dt < minDt) {
	            dt = minDt;
	        }	
	        nloops++;
	    }
#pragma warning disable 162        // disable unreachable code warning
        if (GravityEngine.DEBUG) {
            if (logCount++ > LOG_INTERVAL) {
                logCount = 0;
                Debug.LogFormat("time={0} dt={1} coll_time={2} coll_factor={3} coll_time_initial={4} nloops={5} numbodies={6}",
                        time, dt, coll_time, coll_time_avg / coll_time_initial, coll_time_initial, nloops, numBodies);
            }
        }
#pragma warning restore 162
        return t_offset;
	}

	private void LogAccel() {
		for (int i=0; i < numBodies; i++) {
			Debug.Log(string.Format("i={0} acc={1} {2} {3} jerk={4} {5} {6}", i, 
						acc[i,0], acc[i,1], acc[i,2],
						jerk[i,0], jerk[i,1], jerk[i,2]));
		}

	}

    public string GetExternalAccelForIndex(int i) {
        string s = "none";
        if (externalAccel[i] != null)
            s = externalAccel.ToString();
        return s;
    }
}
