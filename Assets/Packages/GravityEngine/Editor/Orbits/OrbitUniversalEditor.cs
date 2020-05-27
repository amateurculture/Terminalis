#define SOLAR_SYSTEM
using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(OrbitUniversal), true)]
public class OrbitUniversalEditor : Editor {

	private static string eTip = "Ellipse eccentricity (>= 0). (0=circle, 1=parabola, >1 hyperbola)";
	private static string aTip = "Distance from center to focus of ellipse";
	private static string pTip = "Distance from focus to closest approach";
	private static string paramTip = "Orbit size can be specified by closest approach(p) or ellipse semi-major axis (a)";
	private static string phaseTip = "Initial position specified by angle from focus to closest approach (true anomoly)";
	private static string centerTip = "Object at focus (center) of orbit";
	private static string omega_lcTip = "Rotation of pericenter from ascending node\nWhen inclination=0 will act in the same way as \u03a9";
	private static string omega_ucTip = "Rotation of ellipse from x-axis (degrees)\nWhen inclination=0 will act in the same way as \u03c9";
	
	private static string inclinationTip = "Inclination angle of ellipse to x-y plane (degrees)";
    public const string modeTip = "GRAVITY_ENGINE mode sets the initial velocity to acheive the orbit and then"
                                 + "evolves the body with gravity.\n"
                                 + "KEPLERS_EQN forces the body to move in the indicated orbit. Its mass is still used"
                                 + "by the gravity engine to influence other objects.";


    private void WarnAboutKeplerSeq(OrbitUniversal orbitU) {
        // when playing warn user if OrbitU attached to NBody is being used or not
        if (EditorApplication.isPlaying) {
            KeplerSequence ks = orbitU.GetComponent<KeplerSequence>();
            if (ks != null) {
                if (ks.GetCurrentOrbit() != orbitU) {
                    EditorGUILayout.LabelField("OrbitUniversal is not current orbit in Kepler sequence!",
                        EditorStyles.boldLabel);
                }
            }
        }
    }

    
    public override void OnInspectorGUI() {
        GUI.changed = false;
        OrbitUniversal orbitU = (OrbitUniversal)target;
        bool displayAndExit = false;

        // check there is an NBody, if not likely a synthesized Orbit as part of a predictor
        if (orbitU.GetComponent<NBody>() == null) {
            EditorGUILayout.LabelField("Ellipse parameters determined from position/velocity.");
            EditorGUILayout.LabelField("(Orbit Predictor).");
            displayAndExit = true;
        }

        // If there is a SolarBody, it is the one place data can be changed. The EB holds the
        // orbit scaled per SolarSystem scale. 
        SolarBody sbody = orbitU.GetComponent<SolarBody>();
        if (sbody != null) {
            EditorGUILayout.LabelField("Ellipse parameters controlled by SolarBody settings.");
            displayAndExit = true;
        }
        if (displayAndExit) { 
            EditorGUILayout.LabelField(string.Format("   {0,-25} ({1,1})\t  {2}",
                "Semi-Major Axis", "a", orbitU.GetMajorAxisInspector()), EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField(string.Format("   {0,-25} ({1,1})\t  {2}",
                "Eccentricity", "e", orbitU.eccentricity), EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField(string.Format("   {0,-25} ({1,1})\t  {2}",
                "Incliniation", "i", orbitU.inclination), EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField(string.Format("   {0,-25} ({1,1})\t  {2}",
                "Arg. of pericenter", "\u03c9", orbitU.omega_lc), EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField(string.Format("   {0,-25} ({1,1})\t  {2}",
                "Longitude of node", "\u03a9", orbitU.omega_uc), EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField(string.Format("   {0,-25} ({1,1})\t  {2}",
                "Phase", "M", orbitU.phase), EditorStyles.wordWrappedLabel);
            return;
        }

        // fields in class
        NBody centerNBody = null;
        OrbitUniversal.InputMode inputMode = orbitU.inputMode;
        double ecc = orbitU.eccentricity;
        double p_inspector = orbitU.p_inspector;
        double omega_uc = 0;
        double omega_lc = 0;
        double inclination = 0;
        double phase = 0;

        bool sizeUpdate = false;

        WarnAboutKeplerSeq(orbitU);

        centerNBody = (NBody)EditorGUILayout.ObjectField(
                new GUIContent("Center NBody", centerTip),
                orbitU.centerNbody,
                typeof(NBody),
                true);


        OrbitUniversal.EvolveMode evolveMode = orbitU.evolveMode;
        evolveMode = (OrbitUniversal.EvolveMode)EditorGUILayout.EnumPopup(new GUIContent("Evolve Mode", modeTip), evolveMode);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shape Parameters", EditorStyles.boldLabel);


        inputMode = (OrbitUniversal.InputMode)
        EditorGUILayout.EnumPopup(new GUIContent("Parameter Choice", paramTip), orbitU.inputMode);

        sizeUpdate = false;
        GravityScaler.Units units = GravityEngine.Instance().units;
        string promptp = string.Format("Semi-parameter (p) [{0}]", GravityScaler.LengthUnits(units));

        // The values for orbit size and shape can be entered in several ways. OrbitUniversal supports
        // these values as doubles (which is probably overkill in most case). Provide an explicit double mode
        // but also allow sliders (which reduce the value to a float). 

        // The editor script changes p_inspector. (p in OU is scaled for GE internal units)
        switch (inputMode) {

            case OrbitUniversal.InputMode.ELLIPSE_MAJOR_AXIS_A:
                EditorGUILayout.LabelField("Ellipse with float/sliders using semi-major axis.");
                ecc = EditorGUILayout.Slider(new GUIContent("Eccentricity", eTip), (float)orbitU.eccentricity, 0f, 0.99f);
                GetMajorAxis(orbitU, ref p_inspector, ref sizeUpdate, units);
                break;

            case OrbitUniversal.InputMode.ELLIPSE_APOGEE_PERIGEE:
                EditorGUILayout.LabelField("Ellipse with float/sliders using agogee/perigee.");
                EditorGUILayout.LabelField("MUST use <Return> after updating values!");
                double apogee_old = orbitU.GetApogeeInspector();
                double apogee = EditorGUILayout.DelayedDoubleField(new GUIContent("Apogee", eTip), apogee_old);
                double perigee_old = orbitU.GetPerigeeInspector();
                double perigee = EditorGUILayout.DelayedDoubleField(new GUIContent("Perigee", eTip), perigee_old);
                // enforce apogee > perigee
                if (apogee < perigee)
                    apogee = perigee;
             
                if (!EditorApplication.isPlaying && (apogee != apogee_old) || (perigee != perigee_old)) {
                    orbitU.SetSizeWithApogeePerigee(apogee, perigee);
                    sizeUpdate = true;
                    // Need to update ecc and p with new values
                    p_inspector = orbitU.p_inspector;
                    ecc = orbitU.eccentricity;
                }
                EditorGUILayout.LabelField(string.Format("Require Apogee > Perigee", ecc, p_inspector));
                EditorGUILayout.LabelField(string.Format("Apogee/Perigee result in: eccentricty={0:0.00}, p={1:0.00}", ecc, p_inspector));
                break;

            case OrbitUniversal.InputMode.ECC_PERIGEE:
                EditorGUILayout.LabelField("Orbit with double using eccentricity/perigee.");
                EditorGUILayout.LabelField("MUST use <Return> after updating values!");
                double old_ecc = orbitU.eccentricity;
                ecc = EditorGUILayout.DelayedDoubleField(new GUIContent("Eccentricity", eTip), orbitU.eccentricity);
                double hperigee_old = orbitU.GetPerigeeInspector();
                double hperigee = EditorGUILayout.DelayedDoubleField(new GUIContent("Perigee", eTip), hperigee_old);
                if (!EditorApplication.isPlaying &&  ((hperigee != hperigee_old) || (old_ecc != ecc))) {
                    orbitU.SetSizeWithEccPerigee(ecc, hperigee);
                    sizeUpdate = true;
                    // Need to update ecc and p with new values
                    p_inspector = orbitU.p_inspector;
                    ecc = orbitU.eccentricity;
                }
                EditorGUILayout.LabelField(string.Format("Apogee/Perigee result in: eccentricty={0:0.00}, p={1:0.00}", ecc, p_inspector));
                break;


            case OrbitUniversal.InputMode.DOUBLE:
                EditorGUILayout.LabelField("Specify values with double precision using semi-parameter");
                EditorGUILayout.LabelField("MUST use <Return> after updating values!");
                // no sliders (they do float)
                ecc = EditorGUILayout.DelayedDoubleField(new GUIContent("Eccentricity", eTip), orbitU.eccentricity);
                double old_p = orbitU.p_inspector;
                p_inspector = EditorGUILayout.DelayedDoubleField(new GUIContent(promptp, pTip), orbitU.p_inspector);
                if (old_p != p_inspector) {
                    sizeUpdate = true;
                }
                break;

            case OrbitUniversal.InputMode.DOUBLE_ELLIPSE:
                EditorGUILayout.LabelField("Specify values with double precision using semi-parameter");
                EditorGUILayout.LabelField("MUST use <Return> after updating values!");
                // no sliders (they do float)
                ecc = EditorGUILayout.DelayedDoubleField(new GUIContent("Eccentricity", eTip), orbitU.eccentricity);
                GetMajorAxis(orbitU, ref p_inspector, ref sizeUpdate, units);
                break;

            default:
                Debug.LogWarning("Unknown input mode - internal error");
                break;
        }
        if (!EditorApplication.isPlaying && (p_inspector != orbitU.p)) {
            sizeUpdate = true;
        }
        EditorGUILayout.LabelField("Scaled p (Unity units):   " + orbitU.p);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Orientation Parameters", EditorStyles.boldLabel);
        if ((inputMode != OrbitUniversal.InputMode.DOUBLE) && (inputMode != OrbitUniversal.InputMode.DOUBLE_ELLIPSE)) {
            // implementation uses AngleAxis, so degrees are more natural
            omega_uc = EditorGUILayout.Slider(new GUIContent("\u03a9 (Longitude of AN)", omega_ucTip), (float) orbitU.omega_uc, 0, 360f);
            omega_lc = EditorGUILayout.Slider(new GUIContent("\u03c9 (AN to Pericenter)", omega_lcTip), (float) orbitU.omega_lc, 0, 360f);
            inclination = EditorGUILayout.Slider(new GUIContent("Inclination", inclinationTip), (float) orbitU.inclination, 0f, 180f);
            // physics uses radians - but ask user for degrees to be consistent
            phase = EditorGUILayout.Slider(new GUIContent("Starting Phase", phaseTip), (float) orbitU.phase, 0, 360f);
        } else {
            // DOUBLE, so no sliders
            omega_uc = EditorGUILayout.DoubleField(new GUIContent("\u03a9 (Longitude of AN)", omega_ucTip), orbitU.omega_uc);
            omega_lc = EditorGUILayout.DoubleField(new GUIContent("\u03c9 (AN to Pericenter)", omega_lcTip), orbitU.omega_lc);
            inclination = EditorGUILayout.DoubleField(new GUIContent("Inclination", inclinationTip), orbitU.inclination);
            phase = EditorGUILayout.DoubleField(new GUIContent("Starting Phase", phaseTip), orbitU.phase);
        }

        if (GUI.changed) {
			Undo.RecordObject(orbitU, "EllipseBase Change");
			orbitU.p_inspector = p_inspector; 
			orbitU.eccentricity = ecc; 
			orbitU.centerNbody = centerNBody;
			orbitU.omega_lc = omega_lc;
			orbitU.omega_uc = omega_uc;
			orbitU.inclination = inclination;
			orbitU.phase = phase;
            orbitU.inputMode = inputMode;
            orbitU.evolveMode = evolveMode;
			EditorUtility.SetDirty(orbitU);
		}

        if (sizeUpdate) {
            orbitU.ApplyScale(GravityEngine.Instance().GetLengthScale());
        }

    }

    private static void GetMajorAxis(OrbitUniversal orbitU, ref double p_inspector, ref bool sizeUpdate, GravityScaler.Units units) {
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = oldLabelWidth + 50f;
        string prompt = string.Format("Semi-Major Axis (a) [{0}]", GravityScaler.LengthUnits(units));
        double old_a = orbitU.GetMajorAxisInspector();
        double a = EditorGUILayout.DelayedDoubleField(new GUIContent(prompt, aTip), old_a);
        if (!EditorApplication.isPlaying && (a != old_a)) {
            orbitU.SetMajorAxisInspector(a);
            sizeUpdate = true;
            // Need to update ecc and p with new values
            p_inspector = orbitU.p_inspector;
        }
        EditorGUILayout.LabelField(string.Format("Axis result in:  p={0:0.00}", p_inspector));
        EditorGUIUtility.labelWidth = oldLabelWidth;
    }
}
