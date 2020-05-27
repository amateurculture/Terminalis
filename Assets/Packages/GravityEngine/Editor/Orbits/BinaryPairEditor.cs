using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(BinaryPair), true)]
public class BinaryPairEditor : EllipseBaseEditor {

	public override void OnInspectorGUI()
	{
		GUI.changed = false;
		BinaryPair bPair = (BinaryPair) target;
		Vector3 velocity = Vector3.zero;

		GravityScaler.Units units = GravityEngine.Instance().units;
		string prompt = string.Format("Velocity ({0})", GravityScaler.VelocityUnits(units));
		velocity = EditorGUILayout.Vector3Field(new GUIContent(prompt, "velocity of binary center of mass"), bPair.velocity);


		if (GUI.changed) {
			Undo.RecordObject(bPair, "EllipseBase Change");
			bPair.velocity = velocity;
			EditorUtility.SetDirty(bPair);
		}	
		base.OnInspectorGUI();

		if (axisUpdated) {
			bPair.ApplyScale(GravityEngine.Instance().GetLengthScale());
		}

	}
}
