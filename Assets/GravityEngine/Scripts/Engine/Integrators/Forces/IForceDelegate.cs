using UnityEngine;
using System.Collections;

public interface IForceDelegate  {

    /// <summary>
    /// Calculate the distance dependent pseudo-force between two bodies 
    /// seperated by distance r_sep.
    /// (Note: This has taken e.g.  F1 = m1 a1 = (G m1 m2)/r^2
    ///  m1 cancels and we exclude m[2] so the integrator can use the same
    ///  quantity in two places)
    ///
    /// e.g. for Newtonian (1/R^2) gravity this would be:
    ///			m2/(r_sep*r_sep);
    ///
    /// </summary>
    /// <param name="r_sep">R sep. The distance between the bodies</param>
    /// <param name="i">The engine/integrator index of the first mass (i < j)</param>
    /// <param name="j">The engine/integrator index of the second mass</param>
    /// <returns>The accel.</returns>
    double CalcPseudoForce(double r_sep, int i, int j);

    /// <summary>
    /// Calculate the distance dependent pseudo-force between two bodies 
    /// seperated by distance r_sep.
    /// (Note: This has taken e.g.  F1 = m1 a1 = (G m1 m2)/r^2
    ///  m1 cancels and we exclude m[2] so the integrator can use the same
    ///  quantity in two places)
    ///
    /// e.g. for Newtonian (1/R^2) gravity this would be:
    ///			m2/(r_sep*r_sep);
    /// </summary>
    /// <param name="r_sep"></param>
    /// <param name="i">index of massive body</param>
    /// <param name="j">index of massless body </param>
    /// <returns></returns>
    double CalcPseudoForceMassless(double r_sep, int i, int j);

    /// <summary>
    /// Calculates the time derivitive of the (pseudo) force law
    /// This function is required only by the Hermite algorithm. If Leapfrog
    /// is used it can be stubbed out. 
    ///
    /// e.g. for Newtonian (1/R^2) gravity: -2.0 /r^3
    /// 
    /// </summary>
    /// <returns>The jerk.</returns>
    /// <param name="r_sep">R sep.</param>
    double CalcPseudoForceDot(double r_sep, int i, int j); 

}
