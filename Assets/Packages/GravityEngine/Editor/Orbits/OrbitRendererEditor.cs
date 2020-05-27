using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(OrbitRenderer), true)]
public class NewBehaviourScript : Editor {

	private const string nTip = "Number of points in orbit path for renderer"; 

	public override void OnInspectorGUI()
	{
		GUI.changed = false;

		OrbitRenderer orbitRenderer = (OrbitRenderer) target;
		int numPoints = 100;

		numPoints = EditorGUILayout.IntField(new GUIContent("Number of Points", nTip), (int) orbitRenderer.numPoints);

		// If there has been a change in the orbit parameters, then need to recalculate the positions and
		// assign them to the Line Renderer

		if (GUI.changed) {
			Undo.RecordObject(orbitRenderer, "OrbitRenderer Change");
			orbitRenderer.numPoints = numPoints;
			EditorUtility.SetDirty(orbitRenderer);
		}

	}
}
