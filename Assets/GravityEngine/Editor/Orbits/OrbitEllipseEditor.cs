using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(OrbitEllipse), true)]
public class OrbitEllipseEditor : EllipseBaseEditor {

	public const string modeTip = "GRAVITY_ENGINE mode sets the initial velocity to acheive the orbit and then"
	                             + "evolves the body with gravity.\n"
	                             + "KEPLERS_EQN forces the body to move in the indicated orbit. Its mass is still used"
	                             + "by the gravity engine to influence other objects.";

	private const string hillTip = "The Hill Radius indicates the distance within which this objects gravity " +
								   "dominates the gravity of the central object.";

	private const string periodTip = "Orbit period in units of ?";
								
	public override void OnInspectorGUI()
	{
		GUI.changed = false;
		OrbitEllipse orbit = (OrbitEllipse) target;
		OrbitEllipse.evolveType evolveMode = orbit.evolveMode;

		evolveMode = (OrbitEllipse.evolveType) EditorGUILayout.EnumPopup(new GUIContent("Evolve Mode", modeTip), orbit.evolveMode);

		if (GUI.changed) {
			Undo.RecordObject(orbit, "OrbitEllipse Change");
			orbit.evolveMode = evolveMode;
			EditorUtility.SetDirty(orbit);
		}	
		base.OnInspectorGUI();

		// Display the Hill Radius as a guide for where to place moons...
		float r_hill = 0; 
		if (orbit.GetCenterObject() != null) {
			r_hill = OrbitUtils.HillRadius( orbit.GetCenterObject(), orbit.transform.gameObject);
		}
		EditorGUILayout.LabelField(new GUIContent(string.Format("Hill Radius:  {0}", r_hill), hillTip));
		// EditorGUILayout.LabelField(new GUIContent(string.Format("Orbit Period: {0}", orbit.GetPeriod()), periodTip));

		if (axisUpdated) {
			orbit.ApplyScale(GravityEngine.Instance().GetLengthScale());
		}

	}
}
