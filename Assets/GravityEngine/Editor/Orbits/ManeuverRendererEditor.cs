using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(ManeuverRenderer), true)]
public class ManeuverRendererEditor : Editor {

	private const string pfTip = "Maneuver direction prefab containing a LineRenderer and MeshRenderer (typically attached to child objects.";
	private const string lenTip = "Velocity dependent scale factor for line length";
	private const string widthTip = "Velocity dependent scale factor for line width";
    private const string coneTip = "Velocity dependent scale factor for arrow head (mesh scale)";

    public override void OnInspectorGUI()
	{
		GUI.changed = false;
        ManeuverRenderer mr = (ManeuverRenderer) target;

		GameObject prefab = mr.maneuverArrowPrefab;
        float lineLen = mr.lineLengthScale;
        float lineWidth = mr.lineWidthScale;
        float coneScale = mr.coneScale;

        prefab = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Maneuver Arrow Prefab", pfTip),
                prefab,
                typeof(GameObject),
                true);
 
       

		lineLen = EditorGUILayout.FloatField(new GUIContent("Line Length scale", lenTip), lineLen);
        lineWidth = EditorGUILayout.FloatField(new GUIContent("Line Width scale", widthTip), lineWidth);
        coneScale = EditorGUILayout.FloatField(new GUIContent("Arrow head scale", coneTip), coneScale);

        if (GUI.changed) {
			Undo.RecordObject(mr, "OrbitEllipse Change");
			mr.maneuverArrowPrefab = prefab;
			mr.lineLengthScale = lineLen;
			mr.lineWidthScale = lineWidth;
            mr.coneScale = coneScale;
			EditorUtility.SetDirty(mr);
		}	


	}
}
