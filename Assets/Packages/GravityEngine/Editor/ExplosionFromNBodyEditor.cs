using UnityEngine;
using UnityEditor;
using System.Collections;


[CustomEditor(typeof(ExplosionFromNBody), true)]
public class ExplosionFromNBodyEditor : Editor {

	private static string esizeTip = "Maximum size of explosion.\nThis is the goal, but due to numerical integration issues it"
									+" may be necessary to use Velocity Adjust to reduce the initial velocity. (Particles may jump too"
									+" far on the first step if ejecting from a heavy object.)"; 
	private static string coneTip = "Cone half-angle (degrees) for particle ejection."; 
	private static string velTip = "Variation of the particles speed. The number provided is the standard deviation of the distribution."
								+"\nLarger values create wider range in velocities."; 
	private static string softenTip = "Factor to adjust velocity. In general values in the range 0.8-1.0 are practical.\n"
								+ "This commonly requires some experimentation to find a value that looks good in the scene."; 

	public override void OnInspectorGUI()
	{
		GUI.changed = false;

		ExplosionFromNBody efn = (ExplosionFromNBody) target;
		float explosionSize = 10f;
		float soften = 1f;
		float coneWidth = 30f;
		float velocitySpread = 0; 

		explosionSize = EditorGUILayout.FloatField(new GUIContent("Explosion Size", esizeTip), efn.explosionSize);
		coneWidth = EditorGUILayout.FloatField(new GUIContent("Cone Width (deg.)", coneTip), efn.coneWidth);
		velocitySpread = EditorGUILayout.FloatField(new GUIContent("Velocity Spread", velTip), efn.velocitySpread);
		soften = EditorGUILayout.FloatField(new GUIContent("Velocity Adjust", softenTip), efn.soften);

		if (GUI.changed) {
			Undo.RecordObject(efn, "ExplosionFromNBody Change");
			efn.explosionSize = explosionSize;
			efn.coneWidth = coneWidth; 
			efn.soften = soften;
			efn.velocitySpread = velocitySpread;
			EditorUtility.SetDirty(efn);
		}
	}
}
