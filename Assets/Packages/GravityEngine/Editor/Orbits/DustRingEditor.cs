using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(DustRing), true)]
public class DustRingEditor : EllipseBaseEditor {


	private static string rPercentTip = "Ring width as a percent of the radius.";

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		GUI.changed = false;

		DustRing dustRing = (DustRing) target;

		float ringWidthPercent = 0; 
		ringWidthPercent = EditorGUILayout.Slider(new GUIContent("Width (% radius)", rPercentTip), 100f*dustRing.ringWidthPercent, 0, 100)/100f;
		if (GUI.changed) {
			Undo.RecordObject(dustRing, "DustRing Change");
			dustRing.ringWidthPercent = FixNaN.FixIfNaN(ringWidthPercent);
			EditorUtility.SetDirty(dustRing);
		}

		if (axisUpdated) {
			dustRing.ApplyScale(GravityEngine.Instance().GetLengthScale());
		}

	}
}
