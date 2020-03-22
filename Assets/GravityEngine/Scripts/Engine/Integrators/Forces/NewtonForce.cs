using UnityEngine;
using System.Collections;

/// <summary>
/// Newton force.
/// This is not generally used - since Netwonian gravity is the more efficient default
/// force built in to the integrators. 
///
/// This code is used to double check the force delegate code
/// </summary>
public class NewtonForce : MonoBehaviour, IForceDelegate {

	public double CalcPseudoForce(double r_sep, int i, int j) {

		return 1.0/(r_sep*r_sep);
	}

    public double CalcPseudoForceMassless(double r_sep, int i, int j) {

        return 1.0 / (r_sep * r_sep);
    }

    public double CalcPseudoForceDot(double r_sep, int i, int j) {
		return -2.0/(r_sep*r_sep*r_sep);
	}

}
