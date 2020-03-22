using System; // Math (double)
using UnityEngine;
using System.IO;


public sealed class AZTripleIntegrator : INBodyIntegrator {

// This integrator is adapted from the code by Aarseth and Zare (originally in Fortran, from 1974)
// The original code does not contain any Copyright notice or license and is available
// from http://www.ast.cam.ac.uk/~sverre/web/pages/nbody.htm

/*
*             T R I P L E
*             ***********
*
*
*       Three-body regularization program.
*       ----------------------------------
*
*       Method of Aarseth & Zare, Celestial Mechanics 10, 185.
*       ......................................................
*
*       Developed by Sverre Aarseth, IOA, Cambridge.
*       ............................................
*
      PROGRAM TRIPLE
*
      IMPLICIT  REAL*8  (A-H,M,O-Z)
*
*
*       COMMON variables
*       ****************
*
*       ------------------------------------------------------------------
*       C11     Inverse mass factor for DERQP (also C12,C19,C20,C24,C25).
*       ENERGY  Twice the initial total energy.
*       ERROR   Relative energy error at the end (ignore with T' = 1/U).
*       ICALL   Indicator for pericentre check (first function call only).
*       M       Particle mass.
*       NAME    Particle identity (initialized to 1,2,3).
*       NSTEPS  Number of DIFSY calls.
*       P       Regularized momenta.
*       Q       Regularized coordinates.
*       R       Distance between M(1) and M(2) (not regularized).
*       R1      Distance between M(1) and M(3).
*       R2      Distance between M(2) and M(3).
*       RCOLL   Minimum two-body separation (osculating pericentre).
*       RIJ     Minimum pairwise separations (osculating pericentre).
*       X       Particle coordinates (X(3,I) is Z-component).
*       XDOT    Velocity components (XDOT(3,I) is Z-component).
*       ------------------------------------------------------------------
*
      COMMON/AZREG/  Q(8),p[8),R,R1,R2,ENERGY,M(3),X(3,3),XDOT(3,3),
     &               RCOLL,ERROR,C11,C12,C19,C20,C24,C25,NSTEPS,NAME(3)
*/

		private int numBodies; 
		private int maxBodies = 3; 
	
		const double ZERO = 1E-15;
	
		double[] q; 
		double[] p;
		double[] m; 
		// Note x[k,i] is dimension k, body i
		double[,] x; 
		double[,] xdot; 
		double r, r1, r2, energy, c11, c12, c19, c20, c24, c25; 
		int nsteps; 
		int[] name; 
		
		double[,] rij; 
		// int icall;
		double[] ep;
		double tfac;
		int itfac = 0; 
//		int jc = -1; 
		int nhalf2 = 16; 
		int nfn; 
		double[] y; 
		double[] order; 
		double[] rk; 
		
		double tol0; // tcrit; 
		
		// AZ method REQUIRES a frame in which CM and VCM are zero. Transfer to this frame and then 
		// use these arrays to map back into world frame of reference. 
		double[] centerOfMass; 
		double[] velCM; 
		double m_total;		// total mass	

	public void Setup(int maxBodies, double timeStep) {
		// to align with Fortran index from 1 and use an extra entry
		q = new double[9];
		p = new double[9];
		m = new double[4];
		x = new double[4,4];
		xdot = new double[4,4];
		rij = new double[4,4];
		ep = new double[5];		// inited in default data
		y = new double[18];
		order = new double[17];
		rk = new double[4];
	
		name = new int[4]; 
		numBodies = 0; 
		
		// default from input file
		tol0 = 1E-12; 
//		tcrit = 80.0; 
		
		//logw = new StreamWriter("triple.log");

		ep = new double[]{0, 0.04E0,0.0016E0,0.64E-4,0.256E-5};
		itfac = 0;
//		jc = -1;
		nhalf2 = 16;
		
	}

    public void Clear() {
        numBodies = 0; 
    }

	public INBodyIntegrator DeepClone() {
		// crazy how much state is in here!
		AZTripleIntegrator azt = new AZTripleIntegrator();
		azt.Setup(maxBodies, 0);
		azt.numBodies = numBodies;
		azt.maxBodies = maxBodies;
		azt.centerOfMass = new double[4];
		azt.velCM = new double[4];

		for (int i=0; i < 9; i++) {
			azt.q[i] = q[i];
			azt.p[i] = p[i];
		}
		for (int i=0; i < 4; i++) {
			azt.m[i] = m[i];
			azt.rk[i] = rk[i];
			azt.name[i] = name[i];
			azt.centerOfMass[i] = centerOfMass[i];
			azt.velCM[i] = velCM[i];
			for (int j=0; j < 4; j++) {
				azt.x[i,j] = x[i,j];
				azt.xdot[i,j] = xdot[i,j];
				azt.rij[i,j] = rij[i,j];
			}
		}
		for (int i=0; i < 5; i++) {
			azt.ep[i] = ep[i];
		}
		for (int i=0; i < 18; i++) {
			azt.y[i] = y[i];
		}
		for (int i=0; i < 17; i++) {
			azt.order[i] = order[i];
		}
		azt.r = r;
		azt.r1 = r1;
		azt.r2 = r2;
		azt.energy = energy;
		azt.c11 = c11;
		azt.c12 = c12;
		azt.c19 = c19;
		azt.c20 = c20;
		azt.c24 = c24;
		azt.c25 = c25;
		azt.nsteps = nsteps;
		azt.tfac = tfac;
		azt.itfac = itfac;
		azt.nhalf2 = nhalf2;
		azt.nfn = nfn;
		azt.tol0 = tol0;
		azt.m_total = m_total;
		azt.n = n;
		azt.nreg = nreg;
		azt.tau = tau;
		azt.eps = eps;
		azt.dtau = dtau;
		azt.time = time;
		azt.tnext = tnext;
		azt.torb = torb;
		azt.rmin = rmin;
		azt.rm = rm;
		azt.r12min = r12min;
		azt.rmax = rmax;
		azt.initialEnergy = initialEnergy;
		// Integrator stuff
		for (int i=0; i < nmx; i++) {
			azt.ya[i] = ya[i];
			azt.yl[i] = yl[i];
			azt.ym[i] = ym[i];
			azt.dy[i] = dy[i];
			azt.dz[i] = dz[i];
			for (int j=0; j < 8; j++ ){
				azt.dt[i,j] = dt[i,j];
			}
		}
		for (int j=0; j < 8; j++ ){
			azt.d[j] = d[j];
		}
		azt.xn = xn;
		azt.g = g;
		azt.b = b;
		azt.b1 = b1;
		azt.u = u;
		azt.v = v;
		azt.c = c;
		azt.ta = ta;
		azt.w = w;
		azt.konv = konv;
		azt.b0 = b0;
		azt.kl = kl;
		azt.gr = gr;
		azt.fybad = fybad;
	
		return azt;
	}

	public void AddNBody( int bodyNum, NBody nbody, Vector3 position, Vector3 velocity) {

		numBodies++;
		if (numBodies > maxBodies) {
			Debug.LogError("Added more than maximum allocated bodies! max=" + maxBodies);
			return;
		}
		// x is coord, body starting at 1
		x[1,numBodies] = position.x;
		x[2,numBodies] = position.y;
		x[3,numBodies] = position.z;
		xdot[1,numBodies] = velocity.x;
		xdot[2,numBodies] = velocity.y;
		xdot[3,numBodies] = velocity.z;
		name[numBodies] = numBodies;
		m[numBodies] = nbody.mass;
		
	}

	public void RemoveBodyAtIndex(int atIndex) {
	
	}

	public void GrowArrays(int growBy) {
		Debug.LogError("AZT cannot grow arrays. Size fixed at 3.");
	}

	
	public Vector3 GetVelocityForIndex(int i) {
        // Silence this - since it may not get used much and spams demo
		// Debug.LogWarning("Not supported in this integrator");
		return Vector3.zero;
	}

	public void GetVelocityDoubleForIndex(int i, ref double[] vel) {
		Debug.LogWarning("Not supported in this integrator");
	}

	public 	void SetVelocityForIndex(int i, Vector3 vel) {
		Debug.LogError("Not supported in this integrator");
	}

    public void SetVelocityDoubleForIndex(int i, ref double[] v) {
        Debug.LogError("Not supported in this integrator");
    }

    public Vector3 GetAccelerationForIndex(int i) {
		return Vector3.zero;
	}

	public float GetEnergy(GravityState gravityState) {
		return (float) (0.5*Calculate2XEnergy());
	}
	
	public float GetInitialEnergy(GravityState gravityState) {
		return (float) initialEnergy;
	}
	

	private void AdjustToCMFrame() {
		// find the CM and it's velocity (frame with total Momtm=0)
		centerOfMass = new double[4];
		velCM = new double[4];
		m_total = 0;
		for (int i=1; i <= 3; i++) {
			m_total += m[i];
			for (int k=1; k <=3; k++) {
				centerOfMass[k] += m[i]*x[k,i];
				velCM[k] += m[i]*xdot[k,i];
			}
		}
		// redefine X, xdot w.r.t. CM
		for (int i=1; i <= 3; i++) {
			for (int k=1; k <=3; k++) {
				x[k,i] = x[k,i] - centerOfMass[k]/m_total;
				xdot[k,i] = xdot[k,i] - velCM[k]/m_total;
			}
		}	
	}
			
	int n=17; 
	int nreg;
	double tau, eps, dtau, time, tnext, torb, rmin, rm, r12min, rmax; // rstar;
	double initialEnergy; 
	
	public void PreEvolve(GravityState gravityState, ref byte[] info) {
	
		double[,] r_arg = gravityState.r;
		AdjustToCMFrame();
		initialEnergy = 0.5*Calculate2XEnergy();
		
		// This integrator does not support flags. 
		foreach (byte b in info) {
			if (b != 0) {
				Debug.LogError ("AZTIntegrator does not support fixed motion or inactive flags");
				return;
			}
		}
		eps = tol0;
//*
//*       Initialize diagnostic variables & counters.
		r12min = 100.0; 
		rmin = 100.0; 
		for (int j=1; j<=3; j++) {
			for (int k=1; k <= 3; k++) {
				rij[j,k] = 100.0;
			}
		}
		nsteps = 0; 
		nreg = 0; 
		// icall = 0; 
		nfn = 0; 
//		jc = -1; 
//*
//*       Initialize local time & regularized time.
		time = 0.0; 
		tau = 0.0; 
		for (int k=1; k <= 17; k++) {
			y[k] = 0.0; 
		}

//*
//*       Obtain initial energy and transform to regularized variables.
		Transf(1); 
//*
//*       Define gravitational radius and pericentre check distance.
//        double rgrav = (m[1]*m[2] + m[1]*m[3] + m[2]*m[3])/(0.5*Math.Abs(energy));
//        rstar = 0.5*rgrav;
//*
//*       Form the two smallest distances (assume sensible reference body).
		// PM - note R1 describes body 0 in C# notation...
        double r1 = q[1]*q[1] + q[2]*q[2] + q[3]*q[3] + q[4]*q[4];
		double r2 = q[5]*q[5] + q[6]*q[6] + q[7]*q[7] + q[8]*q[8];
//*
//*       Specify the crossing time (also meaningful if ENERGY > 0).
		double tcr = Math.Pow((m[1] + m[2] + m[3]), 2.5)/Math.Pow(Math.Abs(energy),1.5);
     	tnext = 0.0; 
//*
//*       Define a nominal crossing time for nearly parabolic systems.
	    rmax = Math.Max(r1,r2);
	    double tstar = rmax*Math.Sqrt(rmax/(m[1]+m[2]+m[3]));
//*
//*       Determine the smallest two-body time-scale from parabolic orbit.
	    int im = 1;
	    rm = r1; 
	    if (r2 < r1) {
	         im = 2;
	         rm = r2;
	     }
	     double vp2 = 2.0*(m[im] + m[3])/rm;
	     double tp = rm/Math.Sqrt(vp2);
	     tstar = Math.Min(tp,tstar);
	     torb = tp;
//	     tcrit = torb*tcrit;
//*
//*       Set TPR and initial step (ignore M1*M2/R if DT/DTAU = 1/U).
     	double tpr = r1*r2/Math.Sqrt(r1 + r2);
     	dtau = Math.Min(tcr,tstar)*Math.Pow(eps,0.1)/tpr;
		//*
//*       Try alternative expression for initial step (Seppo Mikkola).
//*       Initialize time constant & input array for the integrator.
	    for (int k=1; k <= 8; k++) {
	         y[k] = q[k];
	         y[k+8] = p[k];
	  	}
//*
//*       Produce initial output.
		Transf(2);
		//*
//*       Specify reference values for each vector (Mikkola's method).
	  	for (int k=1; k <= 4; k++) {
	         int k1 = 4 * (k-1);	
	         double sn1 = 0.25*(Math.Abs(y[k1+1]) + Math.Abs(y[k1+2]) + 
	         	Math.Abs(y[k1+3]) + Math.Abs(y[k1+4]));
			 // Initialize the reference vector.
	         if (nsteps == 0) {
	         	order[k1+1] = sn1;
	         }
			 // Do not permit any small values because of relative tolerance.
	         sn1 = 0.1*sn1 + 0.9*order[k1+1]; 
	         for (int l=1; l <= 4; l++) {
	         	order[k1+l] = sn1;
	         }
		}
		
		Transf(3); //XMOVIE did this 
		
		// May have swapped bodies
		int[] index = new int[3];
		for (int i=1; i <=3; i++) {
			if (name[i] == 1)
				index[0] = i;
			if (name[i] == 2)
				index[1] = i;
			if (name[i] == 3)
				index[2] = i;		
		}
		// Copy positions back into GravityEngine array
		for (int i=1; i <= 3; i++) {
			for (int k=1; k <= 3; k++) {
				r_arg[i-1,k-1] = x[k,index[i-1]];
				// adjust to CM position
//				r_arg[i-1,k-1] += centerOfMass[k];
//				// include time evolution of CM
//				r_arg[i-1,k-1] += velCM[k] * time;				
			}
		}
		
	 } // setup
	 
	/// <summary>
	/// Evolve the specified evolveTime, m_arg and r_arg.
	///
	/// AZT is focused on accuracy and the time it evolves for can vary over several orders of magnitude. 
	/// </summary>
	/// <param name="evolveTime">Evolve time.</param>
	/// <param name="m_arg">M_arg. NOT USED</param>
	/// <param name="r_arg">R_arg. passes results back</param>
		
	public double Evolve(double evolveTime, GravityState gravityState, ref byte[] info) {

		double[,] r_arg = gravityState.r;

		double oldTime = time;
label_30:
		dtau = dtau * 0.05;	// XMOVIE
//*
//*       Advance the equations of motion by Bulirsch-Stoer integrator.
		Difsy1(n, eps, ref dtau, ref tau, ref y); 
//*
//*       Copy regularized coordinates & momenta and obtain physical time.
//*       Note that the momentum includes factor of 2 in AZ formulation.
	     for (int k=1; k <= 8; k++) {
	         q[k] = y[k];
	         p[k] = y[k+8];
	     }
//*       Note that the momentum includes factor of 2 in AZ formulation.

//*       Set explicit time (Baumgarte & Stiefel, 1974 & Aarseth, 1976).
		// BS integrator returns new time value
		time = y[17];
//*
//*       Update relative distances (NB! not quite latest value).
		r1 = q[1]*q[1] + q[2]*q[2] + q[3]*q[3] + q[4]*q[4];
		r2 = q[5]*q[5] + q[6]*q[6] + q[7]*q[7] + q[8]*q[8];
///*
//*       Check minimum two-body separations and increase step counter.
	    rmin = Math.Min(rmin,r);
	    rm = Math.Min(r1,r2);
	    r12min = Math.Min(r12min,rm); 
	    rmax = Math.Max(r1, Math.Max(r2,r));
	    nsteps++;

//*
//*       Check minimum two-body separations.
		rk[1] = r1; 
		rk[2] = r2; 
		rk[3] = r; 
//*       Consider pairs 1-2, 1-3 & 2-3 with identified names.
	    for (int k=1; k <= 3; k++) {
	        for (int l=k+1; l <= 3; l++) {
	            int i = name[k];
	            int j = name[l];
//	*       Use cyclic loop index (3,1,2) for distances R, R1 & R2.
	            int kk  = k - 1;
	            if (kk == 0)
	            	kk = 3; 
	            rij[i,j] = Math.Min(rij[i,j],rk[kk]);
	            rij[j,i] = Math.Min(rij[j,i],rk[kk]); 
	  		}
	    }
//*
//*       Switch on search indicator inside RSTAR (reset in DERQP).
//		if (rm < rstar)
//			icall = 1; 
//*
//*       Use actual two-body force to decide on switching.
	    double f12 = (m[1] + m[2])/(r*r);
	    double f13 = (m[1] + m[3])/(r1*r1);
	    double f23 = (m[2] + m[3])/(r2*r2);
	    if (f12 < Math.Max(f13, f23))
	     	goto label_70;
//*
		int imin = 1; 
//*       Use a simple distance test to determine new reference body IMIN.
		if (r2 < Math.Pow(1.00001, r1))
			imin = 2;
//*
//*       Transform to physical variables and rename the exchanged particles.
		Transf(3);
//*
		// exchange either 1 or 2 with 3
		double temp1; 
	    for (int k=1; k <= 3; k++) {
	         temp1 = x[k,3];
	         double temp2 = xdot[k,3];
	         x[k,3] = x[k,imin];
	         xdot[k,3] = xdot[k,imin];
	         x[k,imin] = temp1;
	         xdot[k,imin] = temp2;
	    }
//*
		// exchange mass and name label
		 temp1 = m[3];
	     m[3] = m[imin];
	     m[imin] = temp1; 
	     int name3 = name[3];
	     name[3] = name[imin];
	     name[imin] = name3;
//*
//*       Transform back to regularized variables and initialize input array.
		Transf(4);
		for (int k=1; k <= 8; k++) {
	        y[k] = q[k];
	        y[k+8] = p[k];
	    }
//*
//*       Update regularization counter at the end of switching procedure.
//      NREG = NREG + 1
		nreg = nreg + 1; 
//*
//*       Check termination criteria (TIME > TCRIT or RMAX > RMAX0).
label_70:
		tnext = tnext + torb; 
//*
//*       Obtain final output after transforming to physical variables.
//      CALL TRANSF(2)
		Transf(2);
//*
//*       See whether the final configuration is well resolved.
//      RATIO = R1/R2
// PM - defeat ratio check
//*       Set index of second binary component & escaper.
		imin = 1; 
		if (r2 < r1)
			imin = 2; 
		int iesc = 3 - imin; 

//*
//*       Evaluate orbital elements.
	     double rdot = 0.00;
	     double vrel2 = 0.00;
	     double vesc = 0.00;
	     for (int k=1; k <= 3; k++) {
	         rdot = rdot + (x[k,3] - x[k,imin])*(xdot[k,3] - xdot[k,imin]);
	         vrel2 = vrel2 + (xdot[k,3] - xdot[k,imin])*(xdot[k,3] - xdot[k,imin]); 
	         vesc = vesc + (xdot[k,3] - xdot[k,iesc])*(xdot[k,3] - xdot[k,iesc]);
	     }
//*
		double rb = rm;
		double mb = m[3] + m[imin];
		double semi = 2.0/rb - vrel2/mb;
		vesc = Math.Sqrt(vesc)/Math.Sqrt(mb*Math.Abs(semi));
//*       Velocity of escaper w.r. to M(3) (scaled by binary velocity).
		semi = 1.0/semi; 

		Transf(3); //XMOVIE did this 
		// May have swapped bodies
		int[] index = new int[3];
		for (int i=1; i <=3; i++) {
		 	if (name[i] == 1)
		 		index[0] = i;
			if (name[i] == 2)
				index[1] = i;
		 	if (name[i] == 3)
				index[2] = i;		
		}
		// Copy positions back into GravityEngine array and use CM offset and drift
		for (int i=1; i <= 3; i++) {
			for (int k=1; k <= 3; k++) {
				r_arg[i-1,k-1] = x[k,index[i-1]];
				r_arg[i-1,k-1] += centerOfMass[k]/m_total;
				r_arg[i-1,k-1] += time * velCM[k]/m_total;
			}
		}

		if ((time - oldTime) < evolveTime)	
			goto label_30;
				      
		return (time - oldTime);
//*
		
}
	// Fortran CALLS DERQp[Y(1),Y(9),DZ(1),DZ(9),DZ(17))
	// where e.g. Y(9) allows Y[9..16] to be referenced as P
	private void Derqp_wrapper(ref double[] y, ref double[] yp) {
		double[] p = new double[9]; 
		double[] pr = new double[9]; 
		double tpr = yp[17];
		for (int i=1; i <= 8; i++) {
			p[i] = y[i+8];
			pr[i] = yp[i+8];
		}
		Derqp ( ref y, ref p, ref yp, ref pr, ref tpr);
		for (int i=1; i <= 8; i++) {
			y[i+8] = p[i];
			yp[i+8] = pr[i];
		}
		yp[17] = tpr;
		return;
	}

//      SUBROUTINE DERQp[Q,P,QPR,PPR,TPR)
	private void Derqp(ref double[] q, ref double[] p, ref double[] qpr, ref double[] ppr, ref double tpr) {
//*
//*
//*       Equations of motion.
//*       --------------------
// PM - NFN counts number of integration steps
		nfn++;
//*       Form scalar distances & coefficients.
		//      R1 = Q(1)*Q(1) + Q(2)*Q(2) + Q(3)*Q(3) + Q(4)*Q(4)
		r1 = q[1]*q[1] + q[2]*q[2] + q[3]*q[3] + q[4]*q[4];
		//      R2 = Q(5)*Q(5) + Q(6)*Q(6) + Q(7)*Q(7) + Q(8)*Q(8)
		r2 = q[5]*q[5] + q[6]*q[6] + q[7]*q[7] + q[8]*q[8];
		//      C3 = Q(1)*p[1) - Q(2)*p[2) - Q(3)*p[3) + Q(4)*p[4)
		double c3 = q[1]*p[1] - q[2]*p[2] - q[3]*p[3] + q[4]*p[4];
		//      C4 = Q(5)*p[5) - Q(6)*p[6) - Q(7)*p[7) + Q(8)*p[8)
		double c4 = q[5]*p[5] - q[6]*p[6] - q[7]*p[7] + q[8]*p[8];
		//      C5 = Q(2)*p[1) + Q(1)*p[2) - Q(4)*p[3) - Q(3)*p[4)
		double c5 = q[2]*p[1] + q[1]*p[2] - q[4]*p[3] - q[3]*p[4];
		//      C6 = Q(6)*p[5) + Q(5)*p[6) - Q(8)*p[7) - Q(7)*p[8)
		double c6 = q[6]*p[5] + q[5]*p[6] - q[8]*p[7] - q[7]*p[8];
		//      C7 = Q(3)*p[1) + Q(4)*p[2) + Q(1)*p[3) + Q(2)*p[4)
		double c7 = q[3]*p[1] + q[4]*p[2] + q[1]*p[3] + q[2]*p[4];
		//      C8 = Q(7)*p[5) + Q(8)*p[6) + Q(5)*p[7) + Q(6)*p[8)
		double c8 = q[7]*p[5] + q[8]*p[6] + q[5]*p[7] + q[6]*p[8];
		//      C9 = p[1)*p[1) + p[2)*p[2) + p[3)*p[3) + p[4)*p[4)
		double c9 = p[1]*p[1] + p[2]*p[2] + p[3]*p[3] + p[4]*p[4];
		//      C10 = p[5)*p[5) + p[6)*p[6) + p[7)*p[7) + p[8)*p[8)
		double c10 = p[5]*p[5] + p[6]*p[6] + p[7]*p[7] + p[8]*p[8];
		//      C13 = C11*R2
		//      C14 = C12*R1
		//      C15 = C12*C10
		//      C16 = C11*C9
		//      C17 = R2*ENERGY
		//      C18 = R1*ENERGY
		double c13 = c11*r2;
		double c14 = c12*r1;
		double c15 = c12*c10;
		double c16 = c11*c9;
		double c17 = r2*energy;
		double c18 = r1*energy;
//*       Note that twice the energy is saved in COMMON.
		//      C21 = Q(1)*Q(1) - Q(2)*Q(2) - Q(3)*Q(3) + Q(4)*Q(4)
		//     &    - Q(5)*Q(5) + Q(6)*Q(6) + Q(7)*Q(7) - Q(8)*Q(8)
		double c21 = q[1]*q[1] - q[2]*q[2] - q[3]*q[3] + q[4]*q[4]
	         - q[5]*q[5] + q[6]*q[6] + q[7]*q[7] - q[8]*q[8];
		//      C22 = Q(1)*Q(2) - Q(3)*Q(4) - Q(5)*Q(6) + Q(7)*Q(8)
		double c22 = q[1]*q[2] - q[3]*q[4] - q[5]*q[6] + q[7]*q[8];
		//      C23 = Q(1)*Q(3) + Q(2)*Q(4) - Q(5)*Q(7) - Q(6)*Q(8)
		double c23 = q[1]*q[3] + q[2]*q[4] - q[5]*q[7] - q[6]*q[8];
		//      C22 = C22 + C22
		//      C23 = C23 + C23
		//      RR = C21*C21 + C22*C22 + C23*C23
		//      R = SQRT(RR)
		//      A = C25/R
		c22 = c22 + c22;
		c23 = c23 + c23;
		double rr = c21*c21 + c22*c22 + c23*c23;
	    r = Math.Sqrt(rr);
		double a = c25/r;
//*
//*       Set first derivative of the physical time (standard transformation).
//      TPR = R1*R2
//      B = A*TPR/RR
		tpr = r1 * r2; 
		double b = a*tpr/rr;
//*
//*       Combine vectorial components with relevant coefficients.
		double[] s2 = new double[9];
	    s2[1] = q[1]*c4 + q[2]*c6 + q[3]*c8;
	    s2[2] =-q[2]*c4 + q[1]*c6 + q[4]*c8;
	    s2[3] =-q[3]*c4 - q[4]*c6 + q[1]*c8;
	    s2[4] = q[4]*c4 - q[3]*c6 + q[2]*c8;
	    s2[5] = q[5]*c3 + q[6]*c5 + q[7]*c7;
	    s2[6] =-q[6]*c3 + q[5]*c5 + q[8]*c7;
	    s2[7] =-q[7]*c3 - q[8]*c5 + q[5]*c7;
	    s2[8] = q[8]*c3 - q[7]*c5 + q[6]*c7;

		double[] s5 = new double[9];
		s5[1] = p[1]*c4 + p[2]*c6 + p[3]*c8;
	    s5[2] =-p[2]*c4 + p[1]*c6 + p[4]*c8;
	    s5[3] =-p[3]*c4 - p[4]*c6 + p[1]*c8;
	    s5[4] = p[4]*c4 - p[3]*c6 + p[2]*c8;
	    s5[5] = p[5]*c3 + p[6]*c5 + p[7]*c7;
	    s5[6] =-p[6]*c3 + p[5]*c5 + p[8]*c7;
	    s5[7] =-p[7]*c3 - p[8]*c5 + p[5]*c7;
	    s5[8] = p[8]*c3 - p[7]*c5 + p[6]*c7;

		double[] s8 = new double[9];
		s8[1] = q[1]*c21 + q[2]*c22 + q[3]*c23;
	    s8[2] =-q[2]*c21 + q[1]*c22 + q[4]*c23;
	    s8[3] =-q[3]*c21 - q[4]*c22 + q[1]*c23;
	    s8[4] = q[4]*c21 - q[3]*c22 + q[2]*c23;
	    s8[5] =-q[5]*c21 - q[6]*c22 - q[7]*c23;
	    s8[6] = q[6]*c21 - q[5]*c22 - q[8]*c23;
	    s8[7] = q[7]*c21 + q[8]*c22 - q[5]*c23;
	    s8[8] =-q[8]*c21 + q[7]*c22 - q[6]*c23;

		double c1 = c17 - c15 + c19 + a*r2;
	    double c2 = c18 - c16 + c20 + a*r1;
//*
//*       Form derivatives for standard equations of motion.
	     for (int i=1; i <= 4; i++) {
	        int k = i + 4;
	        qpr[i] = c13*p[i] + c24*s2[i];
	        qpr[k] = c14*p[k] + c24*s2[k];
	        ppr[i] = c1*q[i] - c24*s5[i] - b*s8[i];
	        ppr[k] = c2*q[k] - c24*s5[k] - b*s8[k];
	     }
//*
//*       Check tolerance scaling TFAC = TPR*U (ITFAC > 1).
		if (itfac > 0) {
			tfac = m[3] *(m[1]*r2 + m[2]*r1);
			itfac = -1; 
		}
}
//      SUBROUTINE DIFSY1(N,EPS,H,X,Y)
	// Since locals are SAVEed need to declare here
	const int nmx = 18; // PM - index from 1
	double[] ya = new double[nmx]; 
	double[] yl = new double[nmx]; 
	double[] ym = new double[nmx]; 
	double[] dy = new double[nmx]; 
	double[] dz = new double[nmx]; 
	double[,] dt = new double[nmx,8];
	double[] d = new double[8]; 
	// dabs is a function. 
	double xn, g, b, b1, u, v, c, ta, w;
	bool konv, b0, kl, gr, fybad; 
	
	private void Difsy1(int n, double eps, ref double h, ref double x, ref double[] y) {
//*
//*
//*       Bulirsch-Stoer integrator.
//*       --------------------------
//*
//*       Works if Gamma = (H - E)/L. For other time transformations 'eps'
//*       must be scaled appropriately such that the test is esentially
//*       of the form (H - E)/L < EPS. This is achieved by using TFAC.
//*       Convergence test: ABS(Q'*DP) < EPS*TFAC & ABS(P'*DQ) < EPS*TFAC.
//*       Reference: Mikkola (1987). In 'The Few Body Problem' p. 311.
//*       This works only if eqs. are canonical and we have P:s & Q:s.
//*       One additional eq. is allowed (e.g. for t'=...??) but not checked.
//*
//*

		w = 0; 
//*
		int l;
//*
	    int jti=0;
	    double fy=1.0;
	    for (int i=1; i <= n; i++)
	    	ya[i]=y[i];
		Derqp_wrapper(ref y, ref dz);  
label_10:
		xn = x + h; 
		b0 = false; 	// b0 can change in the j loop. 
		int m = 1; 
		int jr = 2; 
		int js = 3; 

		for (int j=1; j <= 10; j++) {
	     	if (b0) {
	     		d[2] = 16.0/9.0;
	     		d[4] = 64.0/9.0;
	     		d[6] = 256.0/9.0;
	     	} else {
	     		d[2] = 2.25; 
	     		d[4] = 9.0; 
	     		d[6] = 3.6E1;
	     	}
	    
		     if ( j > 7) { 
		     	l = 7; 
		     	d[7] = 6.4E1; 
		     } else {
		     	l = j;
		     	d[l] = m*m;
		     }
		     konv = l > 3;
		     m = m + m; 
		     g = h/(double)m;
		     b = g + g; 
		     m = m -1; 
		     for (int i=1; i <= n; i++) {
		     	yl[i] = ya[i];
		     	ym[i] = ya[i] + g * dz[i];
		     }
			 for (int k=1; k <= m; k++) {
			 	Derqp_wrapper(ref ym, ref dy);
		     	for (int i=1; i <= n; i++) {
		     		u = yl[i] + b * dy[i];
		     		yl[i] = ym[i]; 
		     		ym[i] = u; 
		     	} // I
	     	 } // K

			itfac = 1; 
			Derqp_wrapper(ref ym, ref dy);
			kl = l < 2; 
			gr = l > 5; 
			double fs = 0.0; 
			for (int i=1; i <= n; i++) {
				v = dt[i,1];	
				c = (ym[i]+yl[i]+g*dy[i])* 0.5;
				dt[i,1] = c; 
				ta = c; 
				if (!kl) {
					for (int k=2; k <= l; k++) {  
						b1 = d[k]*v;
						b = b1 - c; 
						w = c - v; 
						u = v; 
						if (b != 0.0) { // b != 0 
//							if (Math.Abs (b) > ZERO) { // b != 0 
							b = w/b; 
							u = c*b; 
							c = b1 * b; 
						}
						v = dt[i,k];
						dt[i,k] = u; 
						ta = u + ta; 
					} // for k
					int is_ = i + n/2;   
					if (is_ > nhalf2)
						is_ = i - (n/2); 
					double dyis = Math.Abs( dy[is_])/tfac;
					if (i > nhalf2)
						dyis = 0.0; 
					if (konv) {
						double test = Math.Abs( (y[i]-ta) * dyis);
						if (test > eps)
							konv = false;
					}
					if (!gr) {
						double fv = Math.Abs(w) * dyis; 
						if (fs < fv)
							fs = fv; 
					}
				} // if !kl
				y[i] = ta;
			} // for i 
			if (fs != 0.0) {	// PM - small value check?
				double fa = fy; 
				int k = l - 1;
				fy= Math.Pow(ep[k]/fs, 1.0/(double)(l+k));
				double fa7 = 0.7 * fa; 
				if (l == 2)	{
					fa7 = 0.0;  
				}
				fybad = !((fa7 > fy) || (fy > 0.7)); 
				if (fybad) {
					h = h * fy; 
					jti = jti + 1; 
					if (jti > 5) { 
						h = 0.0; 
						for (int i=0; i < n; i++) {
							y[i] = ya[i]; 
						}
						return;
					}
					goto label_10;	// this jumps out of a loop. ?
				}
			} // if fs
			if (konv) {
				x = xn; 
				h = h * fy; 
				return; 
			}
			d[3] = 4.0;
			d[5] = 1.6E1; 
			b0 = !b0; 
			m = jr; 
			jr = js; 
			js = m + m; 
		} // for j
		h = 0.5 * h; 
	goto label_10;  
}

	private double Calculate2XEnergy() {
		//*       Obtain total kinetic & potential energy.
		double energy_;
		double zke = 0.0; 
		double pot = 0.0; 
		for (int i=1; i <= 3; i++) {
			// this is really twice the kinetic energy (no factor 0.5)
			zke = zke + m[i]*(xdot[1,i]*xdot[1,i] + 
			                  xdot[2,i]*xdot[2,i] + xdot[3,i]*xdot[3,i]);
			for (int j=i+1; j <= 3; j++) {
				pot = pot - m[i]*m[j]/Math.Sqrt(
					(x[1,i] - x[1,j])*(x[1,i] - x[1,j]) + 
					(x[2,i] - x[2,j])*(x[2,i] - x[2,j]) + 
					(x[3,i] - x[3,j])*(x[3,i] - x[3,j]) ); 
			}
			
		}
		//*
		//*       Save twice the initial energy for routine DERQP.
		energy_ = zke + 2.0 * pot; 
		return energy_;
	}

//      SUBROUTINE TRANSF(KDUM)
	private void Transf(int kdum) { 
		// kdum = 1 "init" calculate regularized variables (q,p)
		// kdum = 2 "output" ? transform (q,p) to physical variables (x,xdot) AND calculate energy, error
		// kdum = 3 "KS->phy" transform KS (q,p) to physical variables (x,xdot)
		// kdum = 4 "switching" ?
		
/*
*
*       Transformation of physical & KS variables.
*       ------------------------------------------
*
     IMPLICIT  REAL*8  (A-H,M,O-Z)
     COMMON/AZREG/  Q(8),p[8),R,R1,R2,ENERGY,M(3),X(3,3),XDOT(3,3),
    &               RCOLL,ERROR,C11,C12,C19,C20,C24,C25,NSTEPS,NAME(3)
     REAL*8  Q1(9),P1(9),Q2(9),P2(9)
*
*/
		double[] q1 = new double[10]; 
		double[] p1 = new double[10]; 
		double[] q2 = new double[10]; 
		double[] p2 = new double[10]; 

//*       Decide path (initialization, output, KS -> phys, switching).
		if (kdum == 4) goto label_4;
		if (kdum > 1) goto label_20;
//*
//*       Obtain total kinetic & potential energy.

		energy = Calculate2XEnergy(); 
//*
//*       Introduce physical coordinates & momenta.
label_4:
		for (int i=1; i <= 3; i++) {
			for (int k=1; k <= 3; k++) {
				int i1 = 3*i + k - 3;
				q1[i1] = x[k,i];
				p1[i1] = m[i] * xdot[k,i];
			}
		}
		
//*
//*       Set mass factors for routine DERQP.
	    c11 = 0.250/m[1] + 0.25/m[3];
	    c12 = 0.250/m[2] + 0.25/m[3];
	    c19 = 2.00*m[2]*m[3];
	    c20 = 2.00*m[1]*m[3];
	    c24 = 0.250/m[3];
	    c25 = 2.00*m[1]*m[2];
//*
//*       Define relative coordinates & absolute momenta (eqn (45)).
		for (int k=1; k <= 3; k++) {
	         q2[k] = q1[k] - q1[k+6];
	         q2[k+3] = q1[k+3] - q1[k+6];
	         p2[k+3] = p1[k+3];
		}
		//*
//*       Expand the variables by relabelling (eqn (46)).
		for (int k=1; k <= 3; k++) {
	         q1[k] = q2[k];
	         q1[k+4] = q2[k+3];
	         p1[k+4] = p2[k+3];
		}
//*
//*       Initialize the redundant variables (eqn (47)).
	    q1[4] = 0.00;
	    q1[8] = 0.00;
	    p1[4] = 0.00;
	    p1[8] = 0.00;
		
//*
//*       Introduce regularized variables for each KS pair.
		for (int kcomp=1; kcomp <= 2; kcomp++) {
			int k = 4*(kcomp - 1); 
//*
//*       Form scalar distance from relative separation vector.
            double rk_ = Math.Sqrt(q1[k+1]*q1[k+1] + q1[k+2]*q1[k+2] + q1[k+3]*q1[k+3]);

//*
//*       Perform KS coordinate transformation (eqn (48) or (49)).
	        if (q1[k+1] > 0) {
	             q[k+1] = Math.Sqrt(0.50*(rk_ + q1[k+1]));
	             q[k+2] = 0.50*q1[k+2]/q[k+1];
	             q[k+3] = 0.50*q1[k+3]/q[k+1];
	             q[k+4] = 0.00;
	        } else {
	             q[k+2] = Math.Sqrt(0.50*(rk_ - q1[k+1]));
	             q[k+1] = 0.50*q1[k+2]/q[k+2];
	             q[k+4] = 0.50*q1[k+3]/q[k+2];
	             q[k+3] = 0.00;
	        }

//*
//*       Set regularized momenta (eqn (50)).
        	p[k+1] = 2.00*(+q[k+1]*p1[k+1] + q[k+2]*p1[k+2] +
                                          q[k+3]*p1[k+3]);
			p[k+2] = 2.00*(-q[k+2]*p1[k+1] + q[k+1]*p1[k+2] +
                                          q[k+4]*p1[k+3]);
			p[k+3] = 2.00*(-q[k+3]*p1[k+1] - q[k+4]*p1[k+2] +
                                          q[k+1]*p1[k+3]);
			p[k+4] = 2.00*(+q[k+4]*p1[k+1] - q[k+3]*p1[k+2] +
                                          q[k+2]*p1[k+3]);
		}
//*
//      GO TO 40
	return; // 40 is a return stmt
//*
//*       Transform each KS pair from regularized to physical variables.
label_20:
		for (int kcomp=1; kcomp <= 2; kcomp++) {
			int k = 4*(kcomp-1); 
//*
//*       Obtain relative coordinates (eqn (52)).
	        q1[k+1] = q[k+1]*q[k+1] - q[k+2]*q[k+2] - q[k+3]*q[k+3] + q[k+4]*q[k+4];
	        q1[k+2] = 2.00*(q[k+1]*q[k+2] - q[k+3]*q[k+4]);
	        q1[k+3] = 2.00*(q[k+1]*q[k+3] + q[k+2]*q[k+4]);
//*
//*       Form product of half Levi-Civita matrix & regularized momenta.
	        p1[k+1] = q[k+1]*p[k+1] - q[k+2]*p[k+2] - q[k+3]*p[k+3] +
	                                                  q[k+4]*p[k+4];
			p1[k+2] = q[k+2]*p[k+1] + q[k+1]*p[k+2] - q[k+4]*p[k+3] -
	                                                  q[k+3]*p[k+4];
			p1[k+3] = q[k+3]*p[k+1] + q[k+4]*p[k+2] + q[k+1]*p[k+3] +
	                                                  q[k+2]*p[k+4];
//*
//*       Evaluate scalar distance.
			double rk_ = q[k+1]*q[k+1] + q[k+2]*q[k+2] + q[k+3]*q[k+3] + q[k+4]*q[k+4];
			//*
//*       Set absolute momenta (eqn (53)).
	        p1[k+1] = 0.50*p1[k+1]/rk_;
	        p1[k+2] = 0.50*p1[k+2]/rk_;
	        p1[k+3] = 0.50*p1[k+3]/rk_;
//   22 CONTINUE
		}
//*
//*       Re-define variables in six dimensions (eqn (54)).
		for (int k=1; k <= 3; k++) {
			q1[k+3] = q1[k+4];
			p1[k+3] = p1[k+4];
		}
//*
//*       Obtain physical coordinates & momenta in c.m. frame.
//      DO 26 K = 1,3
//*       Absolute momentum of reference body (last eqn (55)).
		for (int k=1; k <= 3; k++) {
			q2[k+6] = -(m[1]*q1[k] + m[2]*q1[k+3])/(m[1] + m[2] + m[3]);
//*       Physical coordinates of reference body M(3) (first eqn (55)).
			q2[k] = q1[k] + q2[k+6];
         	q2[k+3] = q1[k+3] + q2[k+6];
//*       Physical coordinates of body M(1) & M(2) (second eqn (55)).
			p2[k] = p1[k];
         	p2[k+3] = p1[k+3];
//*       Physical momenta of body M(1) & M(2) (third eqn (55)).
			p2[k+6] = -(p2[k] + p2[k+3]);
//       Absolute momentum of reference body (last eqn (55)).
		}
//*
//*       Specify coordinates & velocities in c.m. frame.
		for (int i=1; i <= 3; i++) {
			for (int k=1; k <= 3; k++) {
             	int i1 = 3*i + k - 3; 
             	x[k,i] = q2[i1];
             	xdot[k,i] = p2[i1]/m[i];
			}
		}
//*
//      IF (KDUM.EQ.3) GO TO 40
// PM - 40 just returns
		if (kdum == 3) 
			return;
//*
//*       Evaluate total energy & relative energy error.
//		double s1 = p2[1]*p2[1] + p2[2]*p2[2] + p2[3]*p2[3];
//	    double s2 = p2[4]*p2[4] + p2[5]*p2[5] + p2[6]*p2[6];
//	    double s3 = p2[7]*p2[7] + p2[8]*p2[8] + p2[9]*p2[9];
//		double zke = 0.50*(s1/m[1] + s2/m[2] + s3/m[3]);
//		s1 = m[1]*m[3]/Math.Sqrt((q2[7] - q2[1])*(q2[7] - q2[1])
//		     	+ (q2[8] - q2[2])*(q2[8] - q2[2]) 
//	            + (q2[9] - q2[3])*(q2[9] - q2[3]));
//		s2 = m[2]*m[3]/Math.Sqrt((q2[7] - q2[4])*(q2[7] - q2[4])
//	     	+ (q2[8] - q2[5])*(q2[8] - q2[5]) 
//            + (q2[9] - q2[6])*(q2[9] - q2[6]));
//		s3 = m[1]*m[2]/Math.Sqrt((q2[4] - q2[1])*(q2[4] - q2[1])
//	     	+ (q2[5] - q2[2])*(q2[5] - q2[2]) 
//            + (q2[6] - q2[3])*(q2[6] - q2[3]));
//		double ht = zke - s1 - s2 - s3;
/*     Current total energy computed from physical variables.
       Relative energy error with respect to initial value.
       PM = if initial energy is zero (some choreographies) this does not work
*/
	 //error = (ht - 0.5* energy)/(0.5*energy);
	}

    public void SetPositionDoubleForIndex(int i, ref double[] v) {
        throw new NotImplementedException();
    }

    public string GetExternalAccelForIndex(int i) {
        return "none";
    }
}
