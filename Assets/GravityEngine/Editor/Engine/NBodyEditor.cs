using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(NBody), true)]
public class NBodyEditor : Editor {


	private static string mTip = "Mass. Unit selection controlled by Gravity Engine. DL=dimensionless (arbitrary units).";
	private static string velTip = "Velocity component of the object.";
	private static string autoTip = "Determine particle capture radius using size of mesh filter child (typically a sphere)";
	private static string sizeTip = "Radius within which particles will be captured and removed from the scene.";
	private static string iposTip = "Initial position (in the units chosen in the Gravity Engine). ";
    private static string rotateTip = "Auto-rotate frame as object moves in gravity field.";

    public override void OnInspectorGUI()
	{
		GUI.changed = false;

		NBody nbody = (NBody) target;
		float mass = 0f;
		float size = 0.1f;
		bool autoSize = true;
		Vector3 velocity = Vector3.zero; 
		Vector3 initialPos = Vector3.zero;
        bool rotateFrame = false; 

		if (GravityEngine.Instance() == null) {
			EditorGUILayout.LabelField("Require a GravityEngine in the scene to", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("display NBody component.", EditorStyles.boldLabel);
		}

        GravityEngine ge = GravityEngine.Instance();

		GravityScaler.Units units = ge.units;
		string mass_prompt = string.Format("Mass ({0})", GravityScaler.MassUnits(units));
		mass = EditorGUILayout.FloatField(new GUIContent(mass_prompt, mTip), (float) nbody.mass);

		// If the velocity is controlled by an EllipseBase, or this NBody is the direct child of
		// BinaryPair or ThreeeBodySolution then don't allowit to be controlled. 
		string controlledBy = null;
		if (nbody.transform.gameObject.GetComponent<OrbitEllipse>() != null) {
			controlledBy = "Initial position/velocity is set by ellipse parameters.";
		} else if (nbody.transform.gameObject.GetComponent<OrbitHyper>() != null) {
			controlledBy = "Initial position/velocity is set by hyperbola parameters.";
        } else if (nbody.transform.gameObject.GetComponent<OrbitUniversal>() != null) {
            controlledBy = "Initial position/velocity is set by OrbitUniversal parameters.";
        } else if (nbody.transform.parent != null) {
			if (nbody.transform.parent.gameObject.GetComponent<BinaryPair>() != null) {
				controlledBy = "Initial position/velocity is set by BinaryPair parent.";
			} else if (nbody.transform.parent.gameObject.GetComponent<ThreeBodySolution>() != null) {
				controlledBy = "Initial position/velocity is set by ThreeBodySolution parent.";
			}
		}
		if (controlledBy == null) {
			switch(units) {
				case GravityScaler.Units.DIMENSIONLESS:
					EditorGUILayout.LabelField("Initial position set via transform");
					velocity = EditorGUILayout.Vector3Field(new GUIContent("Velocity", velTip), nbody.vel);
					initialPos = nbody.transform.position;
					break;
				default:
					string prompt = string.Format("Initial Pos ({0})", GravityScaler.LengthUnits(units));
					initialPos = EditorGUILayout.Vector3Field(new GUIContent(prompt, iposTip), nbody.initialPos);

					prompt = string.Format("Velocity ({0})", GravityScaler.VelocityUnits(units));
					velocity = EditorGUILayout.Vector3Field(new GUIContent(prompt, velTip), nbody.vel);
					break;
			}
		} else {
			EditorGUILayout.LabelField(controlledBy, EditorStyles.boldLabel);
			//EditorGUILayout.LabelField(string.Format("vel= {0:F2} {1:F2} {2:F2}", nbody.vel.x, nbody.vel.y, nbody.vel.z));
		}


		// particle capture size
		EditorGUIUtility.labelWidth = 200f;
		EditorGUIUtility.fieldWidth = 20f;
        rotateFrame = EditorGUILayout.Toggle(new GUIContent("Maintain prograde orientation", rotateTip), nbody.rotateFrame);
        autoSize = EditorGUILayout.Toggle(new GUIContent("Automatic particle capture size", autoTip), nbody.automaticParticleCapture);
		EditorGUIUtility.labelWidth = 0;
		EditorGUIUtility.fieldWidth = 0;
		if (!autoSize) {
			EditorGUIUtility.labelWidth = 200f;
			EditorGUIUtility.fieldWidth = 40f;
			size = EditorGUILayout.FloatField(new GUIContent("Particle capture radius", sizeTip), (float) nbody.size);
			EditorGUIUtility.labelWidth = 0;
			EditorGUIUtility.fieldWidth = 0;
		} else {
			float detectedSize = nbody.CalculateSize();
			if (detectedSize < 0) {
				EditorGUILayout.LabelField("Did not detect a child with a MeshFilter.", EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Using size=" + size);
			} else {
				EditorGUILayout.LabelField("Particle Capture radius=" + detectedSize);
				size = detectedSize;
			}
		}
		//if (nbody.transform.hasChanged) {
		//	// User has dragged the object and the transform has changed, need
		//	// to change the initial Pos to correspond to this position in the correct units
		//	if (units != GravityScaler.Units.DIMENSIONLESS) {
		//		initialPos = nbody.transform.position/ge.GetLengthScale();
		//	}
		//	nbody.initialPos = initialPos;
		//	nbody.transform.hasChanged = false;
		//}

		if (GUI.changed) {
			Undo.RecordObject(nbody, "NBody Change");
			nbody.mass = FixNaN.FixIfNaN(mass);
			nbody.vel = FixNaN.FixIfNaN(velocity);
			nbody.size = size;
            nbody.rotateFrame = rotateFrame;
			nbody.automaticParticleCapture = autoSize;
			nbody.initialPos = initialPos;
			Debug.Log("new v=" + velocity);
			// must be after initialPos is updated
			nbody.ApplyScale(ge.GetLengthScale(), 
							 ge.GetVelocityScale() );
            nbody.EditorUpdate(GravityEngine.Instance());
			EditorUtility.SetDirty(nbody);
		}



	}
}
