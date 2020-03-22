using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectiveForceTester : MonoBehaviour {

    public NBody a;
    public NBody b;

    private bool setup; 
	
	// Update is called once per frame
	void Update () {
		if (!setup){
            setup = true;
            IForceDelegate force = GravityEngine.Instance().GetForceDelegate();
            if (force != null) {
                SelectiveForceBase selectiveForce = (SelectiveForceBase) force;
                if (selectiveForce != null) {
                     selectiveForce.ForceSelection(a, b, false);
                }
            }
        }
	}
}
