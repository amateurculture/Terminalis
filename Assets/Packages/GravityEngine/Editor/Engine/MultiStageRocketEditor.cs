using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MultiStageEngine), true)]
public class MultiStageRocketEditor : Editor
{

    private const string stageTip = "Number of detachable stages on this rocket.\n" +
        "(Need to press enter or change focus for update)";
    private const string massEmptyTip = "Mass of empty stage in kg.";
    private const string thrustAxisTip = "Direction of engine thrust.";
    private const string engineOnTip = "Start engine at beginning of scene.";
    private const string effectTip = "Game object activated when stage is on (e.g. particle system for flame)";

    public override void OnInspectorGUI() {

        MultiStageEngine mstage = (MultiStageEngine)target;
        bool editorInited = mstage.editorInited;
        if (!editorInited) {
            mstage.massFuel = new double[MultiStageEngine.MAX_STAGES];
            mstage.massStageEmpty = new double[MultiStageEngine.MAX_STAGES];
            mstage.burnRate = new double[MultiStageEngine.MAX_STAGES];
            mstage.thrust = new double[MultiStageEngine.MAX_STAGES];
            mstage.effectObject = new GameObject[MultiStageEngine.MAX_STAGES];
            mstage.coeefDrag = new double[MultiStageEngine.MAX_STAGES];
            mstage.crossSectionalArea = new double[MultiStageEngine.MAX_STAGES];
            // editor state tracking
            mstage.editorInited = true;
            mstage.editorStageFoldout = new bool[MultiStageEngine.MAX_STAGES];
        }
 
        GUI.changed = false;


        int numStages = mstage.numStages;

        float payloadMass = EditorGUILayout.FloatField("Payload Mass (kg)", (float)mstage.payloadMass);

        // wait for user to press enter in this field
        int stages = EditorGUILayout.DelayedIntField(new GUIContent("Number of Stages", stageTip), numStages);

        // need to display/collect the information for existing stages
        double[] massStageEmpty = new double[stages];
        double[] massFuel = new double[stages];
        double[] thrust = new double[stages];
        double[] burnRate = new double[stages];
        double[] crossSectionalArea = new double[stages];
        double[] coeefDrag = new double[stages];
        GameObject[] effectObject = new GameObject[stages];

        // could have increased/decreased number of stages
        for (int i = 0; i < stages; i++) {
            massStageEmpty[i] = mstage.massStageEmpty[i];
            massFuel[i] = mstage.massFuel[i];
            burnRate[i] = mstage.burnRate[i];
            thrust[i] = mstage.thrust[i];
            if (mstage.effectObject[i] != null)
                effectObject[i] = mstage.effectObject[i];
            else
                effectObject[i] = null;
        }

        // Create a foldout for each stage 
        for (int s = 0; s < stages; s++) {
            mstage.editorStageFoldout[s] = EditorGUILayout.Foldout(mstage.editorStageFoldout[s], "Stage " + (s + 1));
            if (mstage.editorStageFoldout[s]) {
                massStageEmpty[s] = EditorGUILayout.FloatField("Mass Empty (kg)", (float)mstage.massStageEmpty[s]);
                massFuel[s] = EditorGUILayout.FloatField("Mass Fuel (kg)", (float)mstage.massFuel[s]);
                burnRate[s] = EditorGUILayout.FloatField("Burn Rate (kg/s)", (float)mstage.burnRate[s]);
                thrust[s] = EditorGUILayout.FloatField("Thrust (N)", (float)mstage.thrust[s]);
                crossSectionalArea[s] = EditorGUILayout.FloatField("Cross Sectional Area (m^2)", 
                        (float) mstage.crossSectionalArea[s]);
                coeefDrag[s] = EditorGUILayout.FloatField("Coeef Drag", (float)mstage.coeefDrag[s]);
                effectObject[s] = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Effect Object (optional)", effectTip),
                        mstage.effectObject[s], typeof(GameObject), true);
            }
        }

        Vector3 thrustAxis = EditorGUILayout.Vector3Field(new GUIContent("Thrust Axis", thrustAxisTip), mstage.thrustAxis);

        bool engineOn = EditorGUILayout.Toggle(new GUIContent("Engine On", engineOnTip), mstage.engineOn);

        if (GUI.changed) {
            Undo.RecordObject(mstage, "MultiStageEngine Change");
            mstage.numStages = stages;
            mstage.payloadMass = payloadMass;
            mstage.thrustAxis = thrustAxis;
            mstage.engineOn = engineOn;
            for (int i = 0; i < stages; i++) {
                if (mstage.editorStageFoldout[i]) {
                    mstage.massStageEmpty[i] = massStageEmpty[i];
                    mstage.massFuel[i] = massFuel[i];
                    mstage.thrust[i] = thrust[i];
                    mstage.burnRate[i] = burnRate[i];
                    mstage.effectObject[i] = effectObject[i];
                    mstage.crossSectionalArea[i] = crossSectionalArea[i];
                    mstage.coeefDrag[i] = coeefDrag[i];
                }
            }
            EditorUtility.SetDirty(mstage);
        }

    }
}
