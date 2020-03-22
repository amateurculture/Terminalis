using UnityEngine;
using System.Collections;

public class ForceR2 : IForceDelegate {

	public double CalcPseudoForce(double r_sep, int i, int j) {

		return r_sep*r_sep;
	}

    public double CalcPseudoForceMassless(double r_sep, int i, int j) {

        return r_sep * r_sep;
    }

    public double CalcPseudoForceDot(double r_sep, int i, int j) {
		return 2.0 * r_sep;
	}
}
