using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(GravityParticles), true)]
public class GravityParticlesEditor : Editor {

	private static string velTip = "Add NBody velocity to particles"; 
	private static string iTip = "Add velocity to all particles created."; 

	public override void OnInspectorGUI()
	{
		GUI.changed = false;

		GravityParticles nbp = (GravityParticles) target;
		Vector3 initialV = nbp.initialVelocity;
		bool addNBodyVelocity = nbp.addNBodyVelocity;

		IGravityParticlesInit particlesInit = nbp.GetComponent<IGravityParticlesInit>();

		GravityScaler.Units units = GravityEngine.Instance().units;
		string prompt = string.Format("Initial Vel. ({0})", GravityScaler.VelocityUnits(units));

		if (particlesInit == null) {
			EditorGUILayout.LabelField("No init script" , EditorStyles.boldLabel);
			bool hasNBody = false; 
			if (nbp.GetComponent<NBody>() != null) {
				hasNBody = true;
			} else if (nbp.transform.parent != null && nbp.transform.parent.gameObject.GetComponent<NBody>() != null) {
				hasNBody = true;
			} 
			if (hasNBody) {
				addNBodyVelocity = EditorGUILayout.Toggle(new GUIContent("Use velocity from NBody", velTip), nbp.addNBodyVelocity);
				if (addNBodyVelocity) {
					EditorGUILayout.LabelField("Initial Velocity: (from NBody)" , EditorStyles.boldLabel);

				} else {
					initialV = EditorGUILayout.Vector3Field(new GUIContent(prompt, iTip), nbp.initialVelocity);
				}
			} else {
				initialV = EditorGUILayout.Vector3Field(new GUIContent(prompt, iTip), nbp.initialVelocity);
			}
		} else {
			EditorGUILayout.LabelField("Initalize with: " + particlesInit.GetType().ToString() , EditorStyles.boldLabel);

		}

		if (GUI.changed) {
			Undo.RecordObject(nbp, "GravityParticles Change");
			nbp.initialVelocity = initialV;
			nbp.addNBodyVelocity = addNBodyVelocity;
			EditorUtility.SetDirty(nbp);
		}
	}
}

