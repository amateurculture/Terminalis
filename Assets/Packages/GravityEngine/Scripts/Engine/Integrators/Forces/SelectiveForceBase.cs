using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for a selective (per-object) force that allows the force to be changed depending
/// on which two objects are interacting. 
/// 
/// Can be used e.g. to exclude some objects from the influnce of others or to allow some objects to have
/// a different force than others. 
/// 
/// Selective forces only operate on massive forces. If massless objects are present the GE must
/// be configured with "Optimize Massless" set to false. 
/// 
/// </summary>
public abstract class SelectiveForceBase : MonoBehaviour, IForceDelegate {

    // boolean array indicating if there should be a force between [i] and [j]
    // array size must track the internals of the integrator that calls it (grow, and 
    // pack down as required)
    protected bool[,] excludeForce;

    // Child class must override these methods!
    public abstract double CalcPseudoForce(double r_sep, int i, int j);

    public abstract double CalcPseudoForceDot(double r_sep, int i, int j);

    public abstract double CalcPseudoForceMassless(double r_sep, int i, int j);

    /// <summary>
    /// Configure force selection between body1 and body2. If body 2 is NULL this indicates
    /// that the useForce value should be applied to body1's interaction with all other bodies
    /// (massive and massless). 
    /// 
    /// Body1 must be massive.
    /// 
    /// </summary>
    /// <param name="body1"></param>
    /// <param name="body2"></param>
    /// <param name="useForce"></param>
    public void ForceSelection(NBody body1, NBody body2, bool useForce) {
        if (body1.engineRef == null) {
            Debug.LogError("Body must have been added to GE: " + body1.gameObject.name);
            return;
        }
        if (body1.engineRef.bodyType != GravityEngine.BodyType.MASSIVE) {
            Debug.LogError("Body must be massive: " + body1.gameObject.name);
            return;
        }
        if ((body2 != null) && (body2.engineRef != null)) {
            excludeForce[body1.engineRef.index, body2.engineRef.index] = !useForce;
            excludeForce[body2.engineRef.index, body1.engineRef.index] = !useForce;          
        } else {
            // change state for all pairings, massive and massless
            for (int i=0; i <= excludeForce.GetUpperBound(1); i++) {
                excludeForce[body1.engineRef.index, i] = !useForce;
                excludeForce[i, body1.engineRef.index] = !useForce;
            }
        }
    }

    //******************************************************
    // Size management
    //******************************************************

    public void Init(int numMassive) {
        // by default all force pairings are used
        excludeForce = new bool[numMassive, numMassive];
        InitCommands();
    }


    public void RemoveBody(int index) {
        // shuffle arrays down from above index
    }

    public void IncreaseToSize(int newSize) {
        // create a new array and copy across
    }

    //******************************************************
    //Console commands
    //******************************************************

    private void InitCommands() {
        GEConsole.RegisterCommandIfConsole(new ShowForceCommand(this));
    }

    private string ShowForce() {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append( "Selective Force State:\n");
        int numBodies = GravityEngine.Instance().GetWorldState().numBodies;
        for (int i=0; i < numBodies; i++) {
            for (int j=i+1; j < numBodies; j++) {
                sb.Append(string.Format("  [{0}][{1}] = {2}\n", i, j, 
                    (excludeForce[i,j] ? "Exclude" : "Use Force")));
            }
        }
        return sb.ToString();
    }

    public class ShowForceCommand : GEConsole.GEConsoleCommand
    {
        private SelectiveForceBase forceBase; 

        public ShowForceCommand(SelectiveForceBase forceBase) {
            this.forceBase = forceBase;
            names = new string[] { "selectiveForce", "sf" };
            help = "Show selective force status of all bodies";
        }

        override
        public string Run(string[] args) {
            return forceBase.ShowForce();
        }

    }

}
