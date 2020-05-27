#define SOLAR_SYSTEM
using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(EllipseBase), true)]
public class EllipseBaseEditor : Editor {

	private static string eTip = "Ellipse eccentricity 0..1. (0=circle)";
	private static string aTip = "Distance from center to focus of ellipse";
	private static string pTip = "Distance from focus to closest approach";
	private static string paramTip = "Orbit size can be specified by closest approach(p) or ellipse semi-major axis (a)";
	private static string phaseTip = "Initial position specified by angle from focus to closest approach (true anomoly)";
	private static string centerTip = "Object at focus (center) of orbit";
	private static string omega_lcTip = "Rotation of pericenter from ascending node\nWhen inclination=0 will act in the same way as \u03a9";
	private static string omega_ucTip = "Rotation of ellipse from x-axis (degrees)\nWhen inclination=0 will act in the same way as \u03c9";
	
	private static string inclinationTip = "Inclination angle of ellipse to x-y plane (degrees)";

	// Used by BinaryPairEditor, OrbitEllipseEditor and DustRingEditor extensions
	protected bool axisUpdated = false; 
		
	public override void OnInspectorGUI()
	{
		GUI.changed = false;
		EllipseBase ellipseBase = (EllipseBase) target;
		// fields in class
		GameObject centerObject = null;
		EllipseBase.ParamBy paramBy = EllipseBase.ParamBy.AXIS_A;
		float ecc = 0; 
		float a = 0; 
		float p = 0; 
		float omega_uc = 0; 
		float omega_lc = 0; 
		float inclination = 0; 
		float phase = 0; 

		if (!(target is BinaryPair)) {
			centerObject = (GameObject) EditorGUILayout.ObjectField(
				new GUIContent("CenterObject", centerTip), 
				ellipseBase.centerObject,
				typeof(GameObject), 
				true);
		}

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Ellipse Parameters", EditorStyles.boldLabel);

		// If there is a SolarBody, it is the one place data can be changed. The EB holds the
		// orbit scaled per SolarSystem scale. 
		SolarBody sbody = ellipseBase.GetComponent<SolarBody>();
		if (sbody != null) {
			EditorGUILayout.LabelField("\tEllipse parameters controlled by SolarBody settings.");
			EditorGUILayout.LabelField(string.Format("   {0,-25}\t ({1,1})\t  {2}",
				"Semi-Major Axis", "a", ellipseBase.a), EditorStyles.wordWrappedLabel);
			EditorGUILayout.LabelField(string.Format("   {0,-25}\t ({1,1})\t  {2}",
				"Eccentricity","e", ellipseBase.ecc), EditorStyles.wordWrappedLabel);
			EditorGUILayout.LabelField(string.Format("   {0,-25}\t ({1,1})\t  {2}",
				"Incliniation", "i", ellipseBase.inclination), EditorStyles.wordWrappedLabel);
			EditorGUILayout.LabelField(string.Format("   {0,-25}\t ({1,1})\t  {2}",
				"Arg. of pericenter", "\u03c9", ellipseBase.omega_lc), EditorStyles.wordWrappedLabel);
			EditorGUILayout.LabelField(string.Format("   {0,-25}\t ({1,1})\t  {2}",
				"Longitude of node","\u03a9", ellipseBase.omega_uc), EditorStyles.wordWrappedLabel);
			EditorGUILayout.LabelField(string.Format("   {0,-25}\t ({1,1})\t  {2}",
				"Phase", "M", ellipseBase.phase), EditorStyles.wordWrappedLabel);
			return;
		}

		paramBy =
			(EllipseBase.ParamBy)EditorGUILayout.EnumPopup(new GUIContent("Parameter Choice", paramTip), ellipseBase.paramBy);

		ecc = EditorGUILayout.Slider(new GUIContent("Eccentricity", eTip), ellipseBase.ecc, 0f, 0.99f );


		axisUpdated = false;
		// backwards compatibility when loading scenes before unit scaling:
		if (ellipseBase.a_scaled < 0) {
			axisUpdated = true;
		}
		GravityScaler.Units units = GravityEngine.Instance().units;
		if (ellipseBase.paramBy == EllipseBase.ParamBy.AXIS_A) {
			float oldLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = oldLabelWidth + 30f;
			string prompt = string.Format("Semi-Major Axis (a) [{0}]", GravityScaler.LengthUnits(units));
			a = EditorGUILayout.FloatField(new GUIContent(prompt, aTip), ellipseBase.a);
			if (a != ellipseBase.a) {
				axisUpdated = true;
			}
			EditorGUILayout.LabelField("Scaled a (Unity units):   " + ellipseBase.a_scaled);
			EditorGUIUtility.labelWidth = oldLabelWidth;
			p = a*(1-ecc);
		} else {
			string prompt = string.Format("Pericenter [{0}]", GravityScaler.LengthUnits(units));
			p = EditorGUILayout.FloatField(new GUIContent(prompt, pTip), ellipseBase.p);
			if (p != ellipseBase.p) {
				axisUpdated = true;
			}
			EditorGUILayout.LabelField("Scaled p (Unity units) " + ellipseBase.p_scaled);
			a = p/(1-ecc);
		}
		// implementation uses AngleAxis, so degrees are more natural
		omega_uc = EditorGUILayout.Slider(new GUIContent("\u03a9 (Longitude of AN)", omega_ucTip), ellipseBase.omega_uc, 0, 360f);
		omega_lc = EditorGUILayout.Slider(new GUIContent("\u03c9 (AN to Pericenter)", omega_lcTip), ellipseBase.omega_lc, 0, 360f);
		inclination = EditorGUILayout.Slider(new GUIContent("Inclination", inclinationTip), ellipseBase.inclination, 0f, 180f);
		// physics uses radians - but ask user for degrees to be consistent
		phase = EditorGUILayout.Slider(new GUIContent("Starting Phase", phaseTip), ellipseBase.phase, 0, 360f);

		// Checking the Event type lets us update after Undo and Redo commands.
		// A redo updates _lengthScale but does not run the setter
//        if (Event.current.type == EventType.ExecuteCommand &&
//            Event.current.commandName == "UndoRedoPerformed") {
//			ellipseBase.ApplyScale(GravityEngine.Instance().GetLengthScale());
//        }

		if (GUI.changed) {
			Undo.RecordObject(ellipseBase, "EllipseBase Change");
			ellipseBase.a = a; 
			ellipseBase.p = p; 
			ellipseBase.ecc = ecc; 
			ellipseBase.centerObject = centerObject;
			ellipseBase.omega_lc = omega_lc;
			ellipseBase.omega_uc = omega_uc;
			ellipseBase.inclination = inclination;
			ellipseBase.phase = phase;
			ellipseBase.paramBy= paramBy;
			EditorUtility.SetDirty(ellipseBase);
		}		

	}
}
