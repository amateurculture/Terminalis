using UnityEngine;
using System.Collections;

/// <summary>
/// Force chooser. A Factory pattern to select a force for 
/// integrators that support force delegates. 
///
/// </summary>
public class ForceChooser  {

	public enum Forces { Gravity, InverseR, InverseR3, ForceR, ForceR2, Custom};

	public static IForceDelegate InstantiateForce(Forces force, GameObject host) {

		IForceDelegate forceD = null;
		switch(force) {
		case Forces.Gravity:
			// leave as null - GE has gravity built-in
			break;
		case Forces.InverseR:
			forceD = new InverseR();
			break;
		case Forces.InverseR3:
			forceD = new InverseR3();
			break;
		case Forces.ForceR:
			forceD = new ForceR();
			break;
		case Forces.ForceR2:
			forceD = new ForceR2();
			break;
		case Forces.Custom:
			forceD = host.GetComponent<IForceDelegate>();
			if (forceD == null) {
				Debug.LogError("Custom IForceDelegate is not attached to " + host.name);
			}
			break;
		default:
			Debug.LogError("Unknown force (was it added to ForceChooser? => " + force);
			break;
		}

		return forceD;
	}

}
