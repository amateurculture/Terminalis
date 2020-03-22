using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(ThreeBodySolution), true)]
public class ThreeBodyEditor : Editor {

	private string[] solutionNames; 
	private string[] setNames; 

	private int setIndex; 
	private int solutionIndex; 
	private ThreeBodySolution threeBodySoln;

	void OnEnable() {

		setNames = SolutionServer.Instance().GetSetNames();

	    threeBodySoln = (ThreeBodySolution) target;
	    // find index of existing soln
		setIndex = 0;
		for (int i=0; i < setNames.Length; i++) {
			if (setNames[i] == threeBodySoln.solutionSet) {
				setIndex = i; 
				break;
			}
		}
		solutionNames = SolutionServer.Instance().GetSolutionNamesForSet(setNames[setIndex]);
		solutionIndex = 0;
		for (int i=0; i < solutionNames.Length; i++) {
			if (solutionNames[i] == threeBodySoln.solutionName) {
				solutionIndex = i; 
				break;
			}
		}
	}

	public override void OnInspectorGUI() {

		GUI.changed = false;
		int oldSetIndex = setIndex;
		setIndex = EditorGUILayout.Popup("Solution Set", setIndex, setNames);
		if (setIndex != oldSetIndex) {
			// need to reset on set change to ensure index is in bounds
			solutionIndex = 0; 
		}
		string solutionSet = null;
		string solutionName = null; 
		GameObject body1 = null; 
		GameObject body2 = null; 
		GameObject body3 = null; 


		solutionNames = SolutionServer.Instance().GetSolutionNamesForSet(setNames[setIndex]);
		solutionIndex = EditorGUILayout.Popup("Solution", solutionIndex, solutionNames);

		solutionSet = setNames[setIndex];
		solutionName = solutionNames[solutionIndex];
		// bodies
		body1 = (GameObject) EditorGUILayout.ObjectField(
			new GUIContent("Body 1", "Game object with NBody"), threeBodySoln.body1, typeof(GameObject), true);
		body2 = (GameObject) EditorGUILayout.ObjectField(
			new GUIContent("Body 2", "Game object with NBody"), threeBodySoln.body2, typeof(GameObject), true);
		body3 = (GameObject) EditorGUILayout.ObjectField(
			new GUIContent("Body 3", "Game object with NBody"), threeBodySoln.body3, typeof(GameObject), true);
		EditorGUILayout.LabelField("Solutions are in positions [-1..1].");
		EditorGUILayout.LabelField("To enlarge use GravityEngine Phy To World scaling.");
		if (GUI.changed) {
			Undo.RecordObject(threeBodySoln, "ThreeBodySolution Change");
			threeBodySoln.solutionSet = solutionSet;
			threeBodySoln.solutionName = solutionName;
			threeBodySoln.body1 = body1;
			threeBodySoln.body2 = body2;
			threeBodySoln.body3 = body3;
			EditorUtility.SetDirty(threeBodySoln);
		}
	}
}
