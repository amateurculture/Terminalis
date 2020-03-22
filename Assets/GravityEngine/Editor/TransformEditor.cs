// Alternative version, with redundant code removed
using UnityEngine;
using UnityEditor;
using System.Collections;

// Started life as:
// https://code.google.com/p/pixelplacement/source/browse/trunk/unity/com/pixelplacement/scripts/TransformInspector.cs?spec=svn149&r=149
//

[CustomEditor(typeof(Transform))]
/// <summary>
/// Transform editor.
///
/// Hide scale and rotation if this is an NBody object. Scale and rotation of an NBody collection of objects impacts
/// the physics/time evolution. To allow for this to be uniform for all NBodies physical and timescale are set
/// in the NBody engine.
/// 
/// </summary>
public class TransformEditor : Editor
{

	private string xformTip = "Show/control all transform attributes.\n" + 
							"If there are orbits or binaries this may have unintended and non-physical side effects.";

	private bool showXform; 

    public override void OnInspectorGUI()
    {
        Transform t = (Transform)target;
        Vector3 position = Vector3.zero;
		Vector3 scale = Vector3.one;
		Vector3 eulerAngles = Vector3.zero;
		bool showPosition = false; 

		// Only want to show Xform checkbox if there is a reason to supress normal view

		if ( (t.gameObject.GetComponent<BinaryPair>() != null) ||
			(t.gameObject.GetComponent<BinaryPair>() != null) ||
			(t.gameObject.GetComponent<EllipseBase>() != null) ||
			(t.gameObject.GetComponent<OrbitHyper>() != null) ||
            (t.gameObject.GetComponent<OrbitUniversal>() != null) ||
            (t.gameObject.GetComponent<DustRing>() != null) ||
			(t.gameObject.GetComponentsInChildren<NBody>().Length > 0)) {
			showXform = EditorGUILayout.Toggle(new GUIContent("Show Full transform", xformTip), showXform);
		}

        GravityEngine ge = t.gameObject.GetComponent<GravityEngine>();


        if (showXform) {
			// Replicate the standard transform inspector gui
	        EditorGUI.indentLevel = 0;
	        position = EditorGUILayout.Vector3Field("Position", t.localPosition);
	        eulerAngles = EditorGUILayout.Vector3Field("Rotation", t.localEulerAngles);
	        scale = EditorGUILayout.Vector3Field("Scale", t.localScale);
		} else if (ge != null) {
            if (ge.mapToScene) {
                // Replicate the standard transform inspector gui
                EditorGUI.indentLevel = 0;
                position = EditorGUILayout.Vector3Field("Position", t.localPosition);
                eulerAngles = EditorGUILayout.Vector3Field("Rotation", t.localEulerAngles);
                scale = EditorGUILayout.Vector3Field("Scale", t.localScale);
            } else {
                EditorGUILayout.LabelField("NbodyEngine scale handled by script controls below.");
            }

        } else if (t.gameObject.GetComponent<BinaryPair>() != null) {
			position = EditorGUILayout.Vector3Field("Position", t.localPosition);
			EditorGUILayout.LabelField("Nbody scale set by NBodyEngine parameters.");
			EditorGUILayout.LabelField("(To scale binary adjust the orbit parameters a or p.)");
			EditorGUILayout.LabelField("Orientation controlled by orbit parameters.");

		} else if (t.parent != null && t.parent.gameObject.GetComponent<BinaryPair>() != null) {
			EditorGUILayout.LabelField("Position/orientation are handled by BinaryPair parameters in the parent.");
			EditorGUILayout.LabelField("(To scale e.g. sphere, add it as a child.)");
			showPosition = true;

		} else if (t.gameObject.GetComponent<EllipseBase>() != null) {
			EditorGUILayout.LabelField("Position/orientation are handled by Elliptical Orbit parameters below.");
			EditorGUILayout.LabelField("(To scale e.g. sphere, add it as a child.)");
			showPosition = true;

		} else if (t.gameObject.GetComponent<OrbitHyper>() != null) {
			EditorGUILayout.LabelField("Position/orientation are handled by OrbitHyper parameters below.");
			EditorGUILayout.LabelField("(To scale e.g. sphere, add it as a child.)");
			showPosition = true;

        } else if (t.gameObject.GetComponent<OrbitUniversal>() != null) {
            EditorGUILayout.LabelField("Position/orientation are handled by OrbitUniversal parameters below.");
            EditorGUILayout.LabelField("(To scale e.g. sphere, add it as a child.)");
            showPosition = true;

        } else if (t.gameObject.GetComponent<DustRing>() != null) {
			EditorGUILayout.LabelField("Position taken from parent.\nOrientation is handled by DustRing parameters below.");
			showPosition = true;

		} else if (t.gameObject.GetComponentsInChildren<NBody>().Length > 0) {
			// Why is this about children?
			if (GravityEngine.Instance().units == GravityScaler.Units.DIMENSIONLESS) {
				// ideally all units would be the same - this is to preserve bkwd compat. 
				position = EditorGUILayout.Vector3Field("Position", t.localPosition);
			} else {
				EditorGUILayout.LabelField("Position is set from NBody initial position and");
				EditorGUILayout.LabelField("  GE units and length scale selection.");
				showPosition = true;
			}
			EditorGUILayout.LabelField("Nbody scale set by NBodyEngine parameters.");
			EditorGUILayout.LabelField("  (To scale e.g. sphere, add it as a child.)");
			EditorGUILayout.LabelField("Nbody orbital rotation controlled on a per orbit basis.");

        } else {          
	        // Replicate the standard transform inspector gui
	        EditorGUI.indentLevel = 0;
	        position = EditorGUILayout.Vector3Field("Position", t.localPosition);
	        eulerAngles = EditorGUILayout.Vector3Field("Rotation", t.localEulerAngles);
	        scale = EditorGUILayout.Vector3Field("Scale", t.localScale);
        }
        if (showPosition) {
        	EditorGUILayout.LabelField(string.Format("position= {0:F2} {1:F2} {2:F2}", t.position.x, t.position.y, t.position.z));
			EditorGUILayout.LabelField(string.Format("scale= {0:F2} {1:F2} {2:F2}", t.localScale.x, t.localScale.y, t.localScale.z));
        }
        if (GUI.changed)
        {
            Undo.RecordObject(t, "Transform Change");
            t.localPosition = FixNaN.FixIfNaN(position);
			t.localEulerAngles = FixNaN.FixIfNaN(eulerAngles);
			t.localScale = FixNaN.FixIfNaN(scale);
        }
    }

   

}