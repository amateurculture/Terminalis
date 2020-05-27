using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(SolarBody), true)]
public class SolarBodyEditor : Editor {

	private static string eTip = "Ellipse eccentricity 0..1. (0=circle)";
	private static string aTip = "Distance from center to focus of ellipse";
	private static string pTip = "Distance from focus to closest approach";
	private static string phaseTip = "Initial position specified by angle from focus to closest approach (true anomoly)";
	private static string omega_lcTip = "Rotation of pericenter from ascending node\nWhen inclination=0 will act in the same way as \u03a9";
	private static string omega_ucTip = "Rotation of ellipse from x-axis (degrees)\nWhen inclination=0 will act in the same way as \u03c9";
	private static string inclinationTip = "Inclination angle of ellipse to x-y plane (degrees)";

	public override void OnInspectorGUI()
	{
		GUI.changed = false;

		SolarBody sbody = (SolarBody) target;
		float a; 
		float p;
		float ecc; 
		float inclination;
		float omega_lc; 
		float omega_uc;
		float longitude; 
		float radius_km; 

		EditorGUILayout.LabelField(target.name + " (" + sbody.bodyType + ")", EditorStyles.boldLabel);

		EditorGUILayout.LabelField("This object holds the UNSCALED data for the solar body");
		EditorGUILayout.LabelField("Epoch for data: " + sbody.epoch);

		GUILayout.Space(5);
		ecc = EditorGUILayout.Slider(new GUIContent("Eccentricity", eTip), sbody.ecc, 0f, 0.99f );
		float oldLabelWidth = EditorGUIUtility.labelWidth;
		EditorGUIUtility.labelWidth = oldLabelWidth + 50f;
		if (sbody.bodyType != SolarSystem.Type.COMET) {
			a = EditorGUILayout.FloatField(new GUIContent("Semi-Major Axis in AU", aTip), sbody.a);
			p = a*(1-ecc);
		} else {
			p = EditorGUILayout.FloatField(new GUIContent("Pericenter (in AU)", pTip), sbody.p);
			a = p/(1-ecc);
		}
		EditorGUIUtility.labelWidth = oldLabelWidth;

		// implementation uses AngleAxis, so degrees are more natural
		omega_uc = EditorGUILayout.Slider(new GUIContent("\u03a9 (Longitude of AN)", omega_ucTip), sbody.omega_uc, 0, 360f);
		omega_lc = EditorGUILayout.Slider(new GUIContent("\u03c9 (AN to Pericenter)", omega_lcTip), sbody.omega_lc, 0, 360f);
		inclination = EditorGUILayout.Slider(new GUIContent("Inclination", inclinationTip), sbody.inclination, 0f, 180f);
		// physics uses radians - but ask user for degrees to be consistent
		longitude = EditorGUILayout.Slider(new GUIContent("Starting Phase", phaseTip), sbody.longitude, 0, 360f);

		oldLabelWidth = EditorGUIUtility.labelWidth;
		EditorGUIUtility.labelWidth = oldLabelWidth + 50f;
		radius_km = EditorGUILayout.FloatField("Radius (km)", sbody.radiusKm);
		EditorGUIUtility.labelWidth = oldLabelWidth;

		EditorGUILayout.LabelField("Mass data is in NBody component in units of 1E24 kg");

		if (GUI.changed) {
			Undo.RecordObject(sbody, "Solar System Change");
			sbody.inclination = inclination;
			sbody.p = p;
            sbody.ecc = ecc;
			sbody.omega_lc = omega_lc;
			sbody.omega_uc = omega_uc;
			sbody.longitude = longitude;
			sbody.radiusKm = radius_km;
			// update ellipse base orbit in scene
			if (!float.IsNaN(a) && a != 0) {
				sbody.a = a; 
				sbody.UpdateOrbit();
			}
			EditorUtility.SetDirty(sbody);
		}

	}
}
