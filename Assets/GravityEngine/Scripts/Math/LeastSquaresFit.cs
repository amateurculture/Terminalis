using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Routines to do a linear least-squares fit. 
/// 
/// Code from: http://csharphelper.com/blog/2014/10/find-a-linear-least-squares-fit-for-a-set-of-points-in-c/
/// </summary>
public class LeastSquaresFit {

    public class LSPoint
    {
        public double x;
        public double y; 

        public LSPoint(double x, double y) {
            this.x = x;
            this.y = y;
        }
    }

    // Return the error squared.
    public static double ErrorSquared(List<LSPoint> points,
        double m, double b) {
        double total = 0;
        foreach (LSPoint pt in points) {
            double dy = pt.x - (m * pt.y + b);
            total += dy * dy;
        }
        return total;
    }

    // Find the least squares linear fit.
    // Return the total error.
    public static double FindLinearLeastSquaresFit(
        List<LSPoint> points, out double m, out double b) {
        // Perform the calculation.
        // Find the values S1, Sx, Sy, Sxx, and Sxy.
        double S1 = points.Count;
        double Sx = 0;
        double Sy = 0;
        double Sxx = 0;
        double Sxy = 0;
        foreach (LSPoint pt in points) {
            Sx += pt.x;
            Sy += pt.y;
            Sxx += pt.x * pt.x;
            Sxy += pt.x * pt.y;
        }

        // Solve for m and b.
        m = (Sxy * S1 - Sx * Sy) / (Sxx * S1 - Sx * Sx);
        b = (Sxy * Sx - Sy * Sxx) / (Sx * Sx - S1 * Sxx);

        return System.Math.Sqrt(ErrorSquared(points, m, b));
    }


}
