using UnityEngine;
using System.Collections;

public class ForceR : IForceDelegate {

	public double CalcPseudoForce(double r_sep, int i, int j) {

		return r_sep;
	}

    public double CalcPseudoForceMassless(double r_sep, int i, int j) {

        return r_sep;
    }

    public double CalcPseudoForceDot(double r_sep, int i, int j) {
		return 1.0;
	}
}
