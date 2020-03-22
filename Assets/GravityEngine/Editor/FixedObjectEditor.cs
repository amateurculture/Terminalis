using UnityEditor;
using System.Collections;

[CustomEditor(typeof(FixedObject), true)]
public class FixedObjectEditor : Editor {

	public override void OnInspectorGUI()
	{
		EditorGUILayout.LabelField("This object will not be affected by gravity.");
		EditorGUILayout.LabelField("It's mass will affect others.");
	}
}
