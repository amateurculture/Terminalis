using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(RandomPlanets), true)]
public class RandomPlanetEditor : Editor {

	public override void OnInspectorGUI()
	{
		 int numPlanets;

		 float a_min; 
		 float a_max;
		 float ecc_min; 
		 float ecc_max;
		 float incl_min;
		 float incl_max;
		 float omega_lc_min;
		 float omega_lc_max;
		 float omega_uc_min;
		 float omega_uc_max;
		 float phase_min;
		 float phase_max;
		 float scale_min;
		 float scale_max;

		GUI.changed = false;
		RandomPlanets rp = (RandomPlanets) target;

		numPlanets = EditorGUILayout.IntField("Number of planets", rp.numPlanets);

		EditorGUILayout.LabelField("Prefabs (instantiate randomly from list)", EditorStyles.boldLabel);
		SerializedProperty bodiesProp = serializedObject.FindProperty ("planetPrefabs");
     	EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(bodiesProp, true);
     	if(EditorGUI.EndChangeCheck())
         	serializedObject.ApplyModifiedProperties();

		float labelWidth = EditorGUIUtility.labelWidth;

		EditorGUILayout.LabelField("Orbit Parameter Ranges:", EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal();
		EditorGUIUtility.labelWidth = 70;
		EditorGUILayout.LabelField("Semi-Major (a)");
		EditorGUIUtility.labelWidth = 30;
		a_min = EditorGUILayout.FloatField("Min", rp.a_min );
		a_min = Mathf.Max(0.1f, a_min);
		a_max = EditorGUILayout.FloatField("Max", rp.a_max );
		a_max = Mathf.Max(0.1f, Mathf.Max(a_min, a_max));
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		EditorGUIUtility.labelWidth = 70;
		EditorGUILayout.LabelField("Eccentricity");
		EditorGUIUtility.labelWidth = 30;
		ecc_min = EditorGUILayout.FloatField("Min", rp.ecc_min );
		ecc_min = Mathf.Max(0, ecc_min);
		ecc_max = EditorGUILayout.FloatField("Max", rp.ecc_max );
		ecc_max = Mathf.Min(0.99f, Mathf.Max(ecc_min, ecc_max));
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		EditorGUIUtility.labelWidth = 70;
		EditorGUILayout.LabelField("Inlination");
		EditorGUIUtility.labelWidth = 30;
		incl_min = EditorGUILayout.FloatField("Min", rp.incl_min );
		incl_min = Mathf.Max(0, incl_min);
		incl_max = EditorGUILayout.FloatField("Max", rp.incl_max );
		incl_max = Mathf.Min(180f, Mathf.Max(incl_min, incl_max));
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		EditorGUIUtility.labelWidth = 70;
		EditorGUILayout.LabelField("\u03a9 (Longitude of AN)");
		EditorGUIUtility.labelWidth = 30;
		omega_uc_min = EditorGUILayout.FloatField("Min", rp.omega_uc_min );
		omega_uc_min = Mathf.Max(0, omega_uc_min);
		omega_uc_max = EditorGUILayout.FloatField("Max", rp.omega_uc_max );
		omega_uc_max = Mathf.Min(360f, Mathf.Max(omega_uc_min, omega_uc_max));
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		EditorGUIUtility.labelWidth = 70;
		EditorGUILayout.LabelField("\u03c9 (AN to Pericenter)");
		EditorGUIUtility.labelWidth = 30;
		omega_lc_min = EditorGUILayout.FloatField("Min", rp.omega_lc_min );
		omega_lc_min = Mathf.Max(0, omega_lc_min);
		omega_lc_max = EditorGUILayout.FloatField("Max", rp.omega_lc_max );
		omega_lc_max = Mathf.Min(360f, Mathf.Max(omega_lc_min, omega_lc_max));
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		EditorGUIUtility.labelWidth = 70;
		EditorGUILayout.LabelField("Phase (true anomoly)");
		EditorGUIUtility.labelWidth = 30;
		phase_min = EditorGUILayout.FloatField("Min", rp.phase_min );
		phase_min = Mathf.Max(0, phase_min);
		phase_max = EditorGUILayout.FloatField("Max", rp.phase_max );
		phase_max = Mathf.Min(360f, Mathf.Max(phase_min, phase_max));
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.LabelField("Renderer scale:", EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal();
		EditorGUIUtility.labelWidth = 70;
		EditorGUILayout.LabelField("Scale");
		EditorGUIUtility.labelWidth = 30;
		scale_min = EditorGUILayout.FloatField("Min", rp.scale_min );
		scale_min = Mathf.Max(0, scale_min);
		scale_max = EditorGUILayout.FloatField("Max", rp.scale_max );
		EditorGUILayout.EndHorizontal();

		EditorGUIUtility.labelWidth = labelWidth;

		if (GUI.changed) {
			Undo.RecordObject(rp, "RandomPlanets Change");
			rp.numPlanets = numPlanets;
			rp.a_min = a_min;
			rp.a_max = a_max;
			rp.ecc_min = ecc_min;
			rp.ecc_max = ecc_max;
			rp.incl_min = incl_min;
			rp.incl_max = incl_max;
			rp.omega_uc_min = omega_uc_min;
			rp.omega_uc_max = omega_uc_max;
			rp.omega_lc_min = omega_lc_min;
			rp.omega_lc_max = omega_lc_max;
			rp.phase_min = phase_min;
			rp.phase_max = phase_max;
			rp.scale_min = scale_min;
			rp.scale_max = scale_max;
			EditorUtility.SetDirty(rp);
		}

	}
}
