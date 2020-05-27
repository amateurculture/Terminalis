using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectiveNewton : SelectiveForceBase {


    public override double CalcPseudoForce(double r_sep, int i, int j) {
        if (excludeForce[i, j])
            return 0.0;

        return 1.0 / (r_sep * r_sep);
    }

    public override double CalcPseudoForceMassless(double r_sep, int i, int j) {

        return 1.0 / (r_sep * r_sep);
    }

    public override double CalcPseudoForceDot(double r_sep, int i, int j) {
        if (excludeForce[i, j])
            return 0.0;
        return -2.0 / (r_sep * r_sep * r_sep);
    }
}
