using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orbit transfer.
/// Base class for all orbit transfers
/// </summary>
public class OrbitTransfer  {

	//! Name of the transfer (will be over-riden by implementing class
	protected string name = "base (error)";

	//! Maneuvers required to execute the transfer
	protected List<Maneuver> maneuvers;

	//! total cost of the manuevers
	protected float deltaV;
	protected float deltaT;

    protected OrbitData fromOrbit;
    protected OrbitData toOrbit;
    protected NBody centerBody; 

	public OrbitTransfer(OrbitData fromOrbit, OrbitData toOrbit) {
		maneuvers = new List<Maneuver>();
        this.fromOrbit = fromOrbit;
        this.toOrbit = toOrbit;

        // find the center body
        if (fromOrbit.centralMass != null) {
            centerBody = fromOrbit.centralMass;
        } else if (toOrbit.centralMass != null) {
            centerBody = toOrbit.centralMass;
        } else {
            Debug.LogError("Neither orbit has a central mass.");
        }

	}

    public OrbitTransfer(OrbitData fromOrbit)
    {
        if (fromOrbit.centralMass != null) {
            centerBody = fromOrbit.centralMass;
        } else {
            Debug.LogError("fromOrbit does not have a central mass.");
        }
        maneuvers = new List<Maneuver>();
    }

    public OrbitTransfer() {
        maneuvers = new List<Maneuver>();
    }

    public float GetDeltaV() {
		return deltaV;
	}

	public float GetDeltaT() {
		return deltaT;
	}

	public List<Maneuver> GetManeuvers() {
		return maneuvers;
	}

	public override string ToString() {
	
		return "forgot to override";
	}

}
