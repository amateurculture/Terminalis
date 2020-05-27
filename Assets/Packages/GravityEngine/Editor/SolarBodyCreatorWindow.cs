using UnityEngine;
using UnityEditor;
using System.Collections;

public class SolarBodyCreatorWindow : EditorWindow {

	// TODO Need to get this from SolarSystem
	public static float physicalScale = 10.0f; 

	//	private static string planetTip = "Select the planet name from the chooser. ";

	// combined list of names of built in asteroids and comets
	private int bodyNum = 0; 
	private int lastBodyNum = 0; 

	private SolarSystem solarSystem;

	private const string typeTip = "";

	private enum CreateType {PLANET, ASTEROID, COMET, JPL_ASTEROID, JPL_COMET};
	private CreateType createType = CreateType.PLANET;
	private CreateType lastCreateType = CreateType.COMET; // Make different from createType for first time
	private string[] planetNames;
	private string[] asteroidNames;
	private string[] cometNames;

	private string jplData; 
	private string lastJplData; 
	private OrbitData orbitData; 

	// Only ever want one. DestroyImmediate was causing errors...
	private static GameObject tempGameobject; 
	private SolarBody sbody; 

	public static void Init()
    {
		SolarBodyCreatorWindow window = ScriptableObject.CreateInstance<SolarBodyCreatorWindow>();
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 350);
        window.ShowPopup();
    }

	public SolarBodyCreatorWindow() {
		planetNames = PlanetData.GetNames();
		asteroidNames = AsteroidData.GetNames();
		cometNames = CometData.GetNames();
	}

    void OnGUI()
    {
    	// check that there is a SolarSystem in the world
		solarSystem = GameObject.FindObjectOfType<SolarSystem>();
		if (solarSystem == null) {
			EditorGUILayout.LabelField("Please create a GameObject with a Solar System compenent first.",EditorStyles.wordWrappedLabel);
			if (GUILayout.Button("Close")) {
				this.Close();
			}
    	} else {
    		// Create a ghost game object to hold the solar body to allow a pretty print of
    		// orbit params prior to adding. 
    		if (tempGameobject == null && solarSystem.planetPrefab != null) {
    			tempGameobject = Instantiate(solarSystem.planetPrefab) as GameObject;
    			tempGameobject.hideFlags = HideFlags.HideAndDontSave;
    			sbody = tempGameobject.GetComponent<SolarBody>();
       		}

			createType = (CreateType)EditorGUILayout.EnumPopup(new GUIContent("Select Solar body type", typeTip), createType );

    		// Select type of object
	        SolarSystem.Type solarType = SolarSystem.Type.PLANET;
	        switch(createType) {
	        case CreateType.PLANET:
				EditorGUILayout.LabelField("Select body",EditorStyles.wordWrappedLabel);
				bodyNum = EditorGUILayout.Popup(bodyNum, planetNames);
	        	break;
	        case CreateType.ASTEROID:
				EditorGUILayout.LabelField("Select body",EditorStyles.wordWrappedLabel);
				bodyNum = EditorGUILayout.Popup(bodyNum, asteroidNames);
				solarType = SolarSystem.Type.ASTEROID;
	        	break;
			case CreateType.COMET:
				EditorGUILayout.LabelField("Select body",EditorStyles.wordWrappedLabel);
				bodyNum = EditorGUILayout.Popup(bodyNum, cometNames);
				solarType = SolarSystem.Type.COMET;
	        	break;
			case CreateType.JPL_ASTEROID:
				EditorGUILayout.LabelField("Enter asteroid from JPL database",EditorStyles.wordWrappedLabel);
				EditorGUILayout.LabelField("Paste a line from JPL data",EditorStyles.wordWrappedLabel);
				if (GUILayout.Button("Open Asteroid Database")) {
					Application.OpenURL("http://ssd.jpl.nasa.gov/dat/ELEMENTS.NUMBR");
				}
				jplData = EditorGUILayout.TextField("JPL DATA", jplData);
				solarType = SolarSystem.Type.COMET;
	        	break;
			case CreateType.JPL_COMET:
				EditorGUILayout.LabelField("Enter comet from JPL database",EditorStyles.wordWrappedLabel);
				EditorGUILayout.LabelField("Paste a line from JPL data",EditorStyles.wordWrappedLabel);
				if (GUILayout.Button("Open Comet Database")) {
					Application.OpenURL("http://ssd.jpl.nasa.gov/dat/ELEMENTS.COMET");
				}
				jplData = EditorGUILayout.TextField("JPL DATA", jplData);
				solarType = SolarSystem.Type.COMET;
	        	break;
	        default:
	        	break;
	        }

	        // If bodyType or bodyNum has changed - get new orbit data
			if (jplData == null && (createType == CreateType.JPL_ASTEROID || createType == CreateType.JPL_COMET)) {
				EditorGUILayout.LabelField("Paste a complete line from the orbit data base into");
				EditorGUILayout.LabelField("the JPL data box.");
				EditorGUILayout.LabelField("");
				EditorGUILayout.LabelField("");
				EditorGUILayout.LabelField("");
			} else if (sbody != null) { 
				if ((createType != lastCreateType) || (bodyNum != lastBodyNum) || (jplData != lastJplData)) {
					switch(createType) {
					case CreateType.PLANET:
						PlanetData.SetSolarBody(sbody, bodyNum);
						break;
					case CreateType.ASTEROID:
						AsteroidData.SetSolarBody(sbody, bodyNum);
						break;
					case CreateType.COMET:
						CometData.SetSolarBody(sbody, bodyNum);
						break;
					case CreateType.JPL_ASTEROID:
						if (jplData != null) {
							AsteroidData.SetSolarBody(sbody, jplData);
						} 
						break;
					case CreateType.JPL_COMET:
						if (jplData != null) {
							CometData.SetSolarBody(sbody, jplData);
						} 
						break;
					default:
						break;
					}
				}
				// Display orbit info 
				EditorGUILayout.LabelField("Body Parameters:",EditorStyles.boldLabel);
				EditorGUILayout.LabelField(sbody.name, EditorStyles.boldLabel);
				EditorGUILayout.LabelField(string.Format("   {0,-25}\t ({1,1})\t  {2}\t AU",
					"Semi-Major Axis", "a", sbody.a), EditorStyles.wordWrappedLabel);
				EditorGUILayout.LabelField(string.Format("   {0,-25}\t ({1,1})\t  {2}\t Unity m",
					"\t(scaled)", "a", sbody.a * GravityEngine.Instance().GetLengthScale()), EditorStyles.wordWrappedLabel);
				EditorGUILayout.LabelField(string.Format("   {0,-25}\t ({1,1})\t  {2}",
					"Eccentricity","e", sbody.ecc), EditorStyles.wordWrappedLabel);
				EditorGUILayout.LabelField(string.Format("   {0,-25}\t ({1,1})\t  {2}\t degrees",
					"Incliniation", "i", sbody.inclination), EditorStyles.wordWrappedLabel);
				EditorGUILayout.LabelField(string.Format("   {0,-25}\t ({1,1})\t  {2}\t degress",
					"Arg. of pericenter", "\u03c9", sbody.omega_lc), EditorStyles.wordWrappedLabel);
				EditorGUILayout.LabelField(string.Format("   {0,-25}\t ({1,1})\t  {2}\t degress",
					"Longitude of node","\u03a9", sbody.omega_uc), EditorStyles.wordWrappedLabel);
				EditorGUILayout.LabelField(string.Format("   {0,-25}\t ({1,1})\t  {2}\t degress",
					"Longitude", "L", sbody.longitude), EditorStyles.wordWrappedLabel);

			} 
			lastCreateType = createType;
			lastBodyNum = bodyNum;
			lastJplData = jplData;

	        GUILayout.Space(20);
	        if (GUILayout.Button("Create")) {
				GameObject go = solarSystem.AddObject(solarType, sbody);
				if (go != null) {
					Undo.RegisterCreatedObjectUndo(go, sbody.name);
				}
				DestroyImmediate(tempGameobject);
				this.Close();
			}

			if (GUILayout.Button("Cancel")) {
				DestroyImmediate(tempGameobject);
				this.Close();
			}
		}
    }


}
