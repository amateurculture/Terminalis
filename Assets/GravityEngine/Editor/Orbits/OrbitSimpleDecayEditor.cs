using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(OrbitSimpleDecay), true)]
public class OrbitSimpleDecayEditor : Editor {

	private static string dTip = "Large numbers give slower decay. Decay factor is used to reduce velocity each frame by 10^(-factor).";

	public override void OnInspectorGUI()
	{
		GUI.changed = false;

		OrbitSimpleDecay osimple = (OrbitSimpleDecay) target;
		float decay = 100;

		decay = EditorGUILayout.FloatField(new GUIContent("Decay Factor (Log)", dTip), (float) osimple.logDecayFactor);

		// If there has been a change in the orbit parameters, then need to recalculate the positions and
		// assign them to the Line Renderer

		if (GUI.changed) {
			Undo.RecordObject(osimple, "OrbitSimpleDecay Change");
			osimple.logDecayFactor = decay;
			EditorUtility.SetDirty(osimple);
		}

	}

}
