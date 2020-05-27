using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(SpaceshipController), true)]
public class SpaceshipControllerEditor : Editor {

	private static string frameTip = "Reference frame to align dragable axes.";
    private static string trajListTip = "Object that listens for trajectory updates based on velocity changes "
                            + "made by this component. When a trajectory update computation due to a velocity "
                            + "change is completed the lsitener will be notified.";
    private static string centerTip = "Center object for determining ORBITAL reference frame.\n" +
                                       "This field is only required if orbital reference frame is selected.";

    private static string showMouseTip = "Use mouse drag to continuously increase velocity. If not set " +
                                    "then mouse position represents absolute velocity change.";
    private static string showVTip = "Show velocity vector as a line in the scene.";

    private static string axisEndTip = "Axis end prefab. This will be instantiated for the end of each axis with"
                        +" suitable rotations applied.";

    private static string axisOffsetTip = "Distance from spaceship to place each of the axis end markers.";

    private static string matTip = "Material to be applied to prefab for the axis end of the indictated axis.";

    private static string velLineTip = "Width of the velocity line.";
    private static string velMatTip = "Material to be applied to line indicating velocity change from user drag.";
    private static string vscaleTip = "Scale factor applied to mouse offset when determining the length of the " +
                            "velocity line in the scene.";
    private static string pscaleTip = "Scale factor applied to mouse offset when determining the magnitude of the "+
                                " velocity change to be applied to the Gravity Engine velocity update.";

    public override void OnInspectorGUI()
	{
		GUI.changed = false;

		SpaceshipController sctrl = (SpaceshipController) target;

        SpaceshipController.Frame frame;
        NBody orbitCenter;
        GameObject trajectoryListener;
        bool mouseVelocityIncrease;
        // axis
        GameObject axisEndPrefab = sctrl.axisEndPrefab;
        float axisOffset = sctrl.axisOffset;
        Material axis1Material = sctrl.axis1Material;
        Material axis2Material = sctrl.axis2Material;
        Material axis3Material = sctrl.axis3Material;

        // velocity
        bool showVelVector = sctrl.showVelocityVector; 
        float uiToPhysVelScale = sctrl.uiToPhysVelocityScale;
        float velScaleToScreen = sctrl.velocityScalePhysToScreen;
        Material velocityLineMaterial = sctrl.velocityLineMaterial;
        float velocityLineWidth = sctrl.velocityLineWidth;

        // foldouts
        bool showAxis = sctrl.editorShowAxis;
        bool showVelocity = sctrl.editorShowVelocity;

        frame = (SpaceshipController.Frame)EditorGUILayout.EnumPopup(new GUIContent("Ref. Frame", frameTip), sctrl.velocityFrame);

        trajectoryListener = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Trajectory Listener (optional)", trajListTip),
                sctrl.trajectoryListener,
                typeof(GameObject),
                 true);

        orbitCenter = (NBody)EditorGUILayout.ObjectField(
                new GUIContent("Center Object (optional)", centerTip),
                sctrl.orbitCenter,
                typeof(NBody),
                true);

        mouseVelocityIncrease = EditorGUILayout.Toggle(new GUIContent("Mouse Dynamic Vel.", showMouseTip),
                    sctrl.mouseVelocityIncrease);

        // AXIS Elements
        showAxis = EditorGUILayout.Foldout(showAxis, "Axis Configuration");
        if (showAxis) {

            axisEndPrefab = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Axis End Prefab", axisEndTip),
                    sctrl.axisEndPrefab,
                    typeof(GameObject),
                    true);
            axisOffset = EditorGUILayout.FloatField(new GUIContent("Axis Offset", axisOffsetTip), sctrl.axisOffset);

            axis1Material = (Material)EditorGUILayout.ObjectField(
                    new GUIContent("Axis 1 Material", matTip),
                    sctrl.axis1Material,
                    typeof(Material),
                     true);
            axis2Material = (Material)EditorGUILayout.ObjectField(
             new GUIContent("Axis 2 Material", matTip),
             sctrl.axis2Material,
             typeof(Material),
              true);
            axis3Material = (Material)EditorGUILayout.ObjectField(
                     new GUIContent("Axis 3 Material", matTip),
                     sctrl.axis3Material,
                     typeof(Material),
                      true);
        }

        // VELOCITY ELEMENTS
 
        showVelocity = EditorGUILayout.Foldout(showVelocity, "Velocity Line");
        if (showVelocity) {

            showVelVector = EditorGUILayout.Toggle(new GUIContent("Velocity Vector", showVTip),
                    sctrl.showVelocityVector);

            velScaleToScreen = EditorGUILayout.FloatField(new GUIContent("Screen Scale", vscaleTip), velScaleToScreen);
            uiToPhysVelScale = EditorGUILayout.FloatField(new GUIContent("Physics Scale",pscaleTip), uiToPhysVelScale);

            velocityLineMaterial = (Material)EditorGUILayout.ObjectField(
                   new GUIContent("Material", velMatTip),
                   velocityLineMaterial,
                   typeof(Material),
                    true);
            velocityLineWidth = EditorGUILayout.FloatField(new GUIContent("Line Width", velLineTip), velocityLineWidth);

        }

        if (GUI.changed) {
			Undo.RecordObject(sctrl, "SpaceshipConbtroller Change");
            sctrl.velocityFrame = frame;
            sctrl.trajectoryListener = trajectoryListener;
            sctrl.orbitCenter = orbitCenter;
            sctrl.mouseVelocityIncrease = mouseVelocityIncrease;

            sctrl.axisEndPrefab = axisEndPrefab;
            sctrl.axisOffset = axisOffset;
            sctrl.axis1Material = axis1Material;
            sctrl.axis2Material = axis2Material;
            sctrl.axis3Material = axis3Material;

            sctrl.showVelocityVector = showVelVector;
            sctrl.velocityScalePhysToScreen = velScaleToScreen;
            sctrl.velocityLineMaterial = velocityLineMaterial;
            sctrl.velocityLineWidth = velocityLineWidth;
            sctrl.uiToPhysVelocityScale = uiToPhysVelScale;

            sctrl.editorShowAxis = showAxis;
            sctrl.editorShowVelocity = showVelocity;

			EditorUtility.SetDirty(sctrl);
		}

	}
}
