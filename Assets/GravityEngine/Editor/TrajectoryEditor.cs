using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Trajectory), true)]
public class TrajectoryEditor : Editor {

	// Nothing fancy - just needed to support tool tips

	private const string minV_tip = "New points added to line only if seperated by this much from last point.";
    private const string maxP_tip = "Maximum number of points to be used in line renderer.";
    private const string timeMarker_tip = "If set, then this object will be instantiated at the interval specified" +
			" by the time mark interval. Used to show how time evolves along a trajectory. Time markers will" +
			" be aligned so their z-axis points along the trajectory.";
	private const string timeInterval_tip = "Time between time markers on the trajectory. Requires a prefab object" +
			" be specified.";
	private const string textPF_tip = "Prefab text (UI GUI) object to hold the time value along the trajectory." +
			" Use of this object requires that a Trajectory canvas be assigned on the Gravity Engine.";
	private const string textRot_tip = "If set (and a time marker prefab is present) will rotate the text to" +
			" align with the trajectory direction. Otherwise text will be aligned in world space.";
	private const string record_tip = "Record trajectory points, velocity and time for use in " +
			" a TrajectoryMatch calculation";

	public override void OnInspectorGUI() {

		GUI.changed = false;
		Trajectory traj = (Trajectory) target;

		float minVertexDistance =traj.minVertexDistance;
        int maxPoints = traj.maxPoints;

        GameObject timeMarkerPrefab = traj.timeMarkerPrefab;
		float timeMarkInterval = traj.timeMarkInterval;
		GameObject textPrefab = traj.textPrefab;
		bool rotateText = traj.rotateText;
		bool recordData = traj.recordData;

		minVertexDistance = EditorGUILayout.FloatField(new GUIContent("Min. Vertex Distance", minV_tip), minVertexDistance);

        maxPoints = EditorGUILayout.IntField(new GUIContent("Max. Points to Render", maxP_tip), maxPoints);

        timeMarkerPrefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Time Marker Prefab", timeMarker_tip),
						timeMarkerPrefab, typeof(GameObject), true);
		timeMarkInterval = EditorGUILayout.FloatField(new GUIContent("Time Mark Interval", timeInterval_tip), timeMarkInterval);

		textPrefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Time Text Prefab", textPF_tip),
						textPrefab, typeof(GameObject), true);
		rotateText = EditorGUILayout.Toggle(new GUIContent("Align Text to Traj.", textRot_tip), rotateText);

		recordData = EditorGUILayout.Toggle(new GUIContent("RecordData", record_tip), recordData);

		if (GUI.changed) {
			Undo.RecordObject(traj, "Trajectory Change");
			traj.minVertexDistance = minVertexDistance;
            traj.maxPoints = maxPoints;
			traj.timeMarkerPrefab = timeMarkerPrefab;
			traj.timeMarkInterval = timeMarkInterval;
			traj.textPrefab = textPrefab;
			traj.rotateText = rotateText;
			traj.recordData = recordData;
			EditorUtility.SetDirty(traj);
		}		


	}
}
