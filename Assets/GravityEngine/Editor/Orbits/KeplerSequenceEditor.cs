using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(KeplerSequence), true)]
public class KeplerSequenceEditor : Editor {

    public override void OnInspectorGUI() {

        KeplerSequence keplerSeq = (KeplerSequence) target;

        OrbitUniversal orbitU = keplerSeq.GetComponent<OrbitUniversal>();
        if (orbitU.evolveMode == OrbitUniversal.EvolveMode.GRAVITY_ENGINE) {
            EditorGUILayout.LabelField("Base orbit is in GRAVITY MODE. Kepler sequence will be ignored!");
            return;
        }
        if (EditorApplication.isPlaying) {
            EditorGUILayout.LabelField("Dump of Kepler Sequence Elements");
            EditorGUILayout.LabelField(string.Format("time={0} current={1}",
                GravityEngine.Instance().GetPhysicalTime(), keplerSeq.GetCurrentOrbitIndex()));
            string[] info = keplerSeq.DumpInfo().Split('\n');
            foreach(string s in info)
                EditorGUILayout.LabelField(s);
            EditorGUILayout.LabelField("Tip: GetCurrentOrbit() returns the active element.");

        } else {
            EditorGUILayout.LabelField("Inspector will show active elements when playing");
        }

    }


}
