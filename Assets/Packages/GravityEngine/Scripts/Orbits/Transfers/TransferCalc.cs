using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Calculate the transfer options from one body to a target body given
/// the gravitational field of a central mass. 
/// 
/// The options can be retrieved as a list of string or of OrbitTransfer
/// objects. (Some orbit transfers have tunable parameters, e.g. bi-elliptic
/// xfer)
/// </summary>
public class TransferCalc  {

	private NBody ship; 
	private NBody target; 
	private NBody centralMass; 

	// Limits to decide when orbits are effectivly co-planar or circular
	public const float DELTA_INCL = 0.01f;
	private const float DELTA_ECC = 0.01f;
    private const float DELTA_RADIUS = 0.01f;


	public TransferCalc(NBody ship, NBody target, NBody centralMass) {
		this.ship = ship;
		this.target = target;
		this.centralMass = centralMass;
	}
    
    public OrbitTransfer Circularize() {
        OrbitData shipOrbit = new OrbitData();
        shipOrbit.SetOrbitForVelocity(ship, centralMass);
        return new CircularizeXfer(shipOrbit);
    }

    public List<OrbitTransfer> FindTransfers(bool rendezvous) {
		// find orbit parameters for each body. 
		OrbitData shipOrbit = new OrbitData();
        shipOrbit.SetOrbit(ship, centralMass);
        //shipOrbit.SetOrbitForVelocity(ship, centralMass);
		OrbitData targetOrbit = new OrbitData();
        targetOrbit.SetOrbit(target, centralMass);
        //targetOrbit.SetOrbitForVelocity(target, centralMass);
        //Debug.Log("ship:" + shipOrbit.LogString());
        //Debug.Log("target:" + targetOrbit.LogString());
        List<OrbitTransfer> transfers = new List<OrbitTransfer>();

        if ((shipOrbit.ecc < 1f) && (targetOrbit.ecc < 1f)) {
            // both ellipses
            if (shipOrbit.ecc >= DELTA_ECC)
            {
                // add option to circularize our orbit (independent of target)
                // TODO: need more general ellipse tuning
                transfers.Add(new CircularizeXfer(shipOrbit));
            } 

			if ((shipOrbit.ecc < DELTA_ECC) && (targetOrbit.ecc < DELTA_ECC)) {
                // both circular - can use Hohmann
                if (Mathf.Abs(shipOrbit.inclination - targetOrbit.inclination) > DELTA_INCL)
                {
                    // Change in inclination
                    // If same radius can use CircularInclinationAndAN()
                    if (Mathf.Abs(shipOrbit.a - targetOrbit.a) < DELTA_RADIUS) {
                        transfers.Add(new CircularInclinationAndAN(shipOrbit, targetOrbit));
                    } else {
                        Debug.Log("TODO: transfer between non-coplaner circles");
                    }
                    
                } else { 
                    // Same inclination
                    transfers.Add(new HohmannXfer(shipOrbit, targetOrbit, true));
                    // If the target is a moon (mass > 0) then consider a patched conic transfer
                    if (target.mass > 0) {
                        // Need to let user pick the angle of arrival
                        transfers.Add(new PatchedConicXfer(shipOrbit, targetOrbit, PatchedConicXfer.LAMBDA1_DEFAULT));
                    }
                    if (BiellipticXfer.HasLowerDv(shipOrbit, targetOrbit))
                    {
                        // Pick 50% excess for orbit (UI can re-adjust as needed)
                        transfers.Add(new BiellipticXfer(shipOrbit, targetOrbit, 50f));
                    }
                }
			} else {
				Debug.Log("TODO: Transfer between co-planar ellipses.");
			}
		} else {
			Debug.Log("Not transfer between ellipses.");
		}
        return transfers;
	}

}
