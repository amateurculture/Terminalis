using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(OrbitSegment), true)]
public class OrbitSegmentEditor : Editor {

	private const string centerTip = "NBody that the body is in orbit around.";
	private const string bodyTip = "NBody that indicates the start of the orbit segment.";
	private const string rTip = "Number of points to use in line renderering of orbit.";
    private const string vTip = "Velocity will be set explicitly by a script (do not ask GE for velocity every frame)";
    private const string shortTip = "Draw the short path between the Nbody and the destination";
    private const string dirTip = "(optional) Game object for destination of ellipse segment.\n"
        + "Code will take angle from ellipse focus to this object to determine segment end point.\n" 
        + "If not used, must be set from a script using SetDestination()";
    public override void OnInspectorGUI()
	{
		GUI.changed = false;
        OrbitSegment orbit = (OrbitSegment) target;

		GameObject body; 
		GameObject centerObject = orbit.centerBody;
        GameObject destObject;
        bool vFromScript = orbit.velocityFromScript;
        bool shortPath = orbit.shortPath;
		int numPoints;

        centerObject = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("CenterObject", centerTip),
                orbit.centerBody,
                typeof(GameObject),
                true);
 
        body = (GameObject) EditorGUILayout.ObjectField(
				new GUIContent("Body", bodyTip), 
				orbit.body,
				typeof(GameObject), 
				true);

        vFromScript = EditorGUILayout.Toggle(new GUIContent("Velocity From Script", vTip),  vFromScript);

		numPoints = EditorGUILayout.IntField(new GUIContent("Number of Points", rTip), orbit.numPoints);

        shortPath = EditorGUILayout.Toggle(new GUIContent("Short Path", shortTip), orbit.shortPath);

        destObject = (GameObject)EditorGUILayout.ObjectField(
        new GUIContent("Destination Object", dirTip),
        orbit.destination,
        typeof(GameObject),
        true);

        if (GUI.changed) {
			Undo.RecordObject(orbit, "OrbitEllipse Change");
			orbit.centerBody = centerObject;
			orbit.body = body;
			orbit.numPoints = numPoints;
            orbit.velocityFromScript = vFromScript;
            orbit.destination = destObject;
            orbit.shortPath = shortPath;
			EditorUtility.SetDirty(orbit);
		}	


	}
}
