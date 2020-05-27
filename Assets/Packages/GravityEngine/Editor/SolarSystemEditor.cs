using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(SolarSystem), true)]
public class SolarSystemEditor : Editor {

	// When first attached, check the mass of the sun is correct. If not, fix it. 
	[SerializeField]
	private bool sunMassOk = false; 

	private void CheckSunMass(SolarSystem solar) {
		// NBody is a required component
		NBody nbody = solar.gameObject.GetComponent<NBody>();
		if (nbody != null) {
			if (Mathf.Abs(nbody.mass - SolarSystem.mass_sun) > 1E-3 ) {
				nbody.mass = SolarSystem.mass_sun;
				Debug.Log("Setting mass of sun");
				sunMassOk = true;
			}
		}
	}


	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		GUI.changed = false;

		SolarSystem solar = (SolarSystem) target;

		if (!sunMassOk) {
			CheckSunMass(solar);
		}

		float planetScale = 0; 
		float epochTime = solar.epochTime;
        bool epochFoldout;
	
		GameObject planetPrefab = null; 
		GameObject asteroidPrefab = null; 
		GameObject cometPrefab = null; 

		EditorGUIUtility.labelWidth = 200f; 
		planetScale = EditorGUILayout.FloatField(new GUIContent("Planet size per 10000km"), solar.planetScale);

		EditorGUIUtility.labelWidth = 0f; // reset 

		System.DateTime datetime = SolarUtils.DateForEpoch(solar.epochTime);
		epochFoldout = EditorGUILayout.Foldout( solar.epochFoldout, "Start date: " + datetime.ToString("yyyy-MM-dd"));
		if (epochFoldout) {
            EditorGUILayout.LabelField("Press ENTER to update changes in start date.");
			int year = EditorGUILayout.DelayedIntField("YYYY", datetime.Year);
			int month = EditorGUILayout.DelayedIntField("MM", datetime.Month);
			int day = EditorGUILayout.DelayedIntField("DD", datetime.Day); 
			System.DateTime newTime = new System.DateTime(year, month, day);
            Debug.LogFormat("newTime={0}", newTime.Ticks);
			if (!newTime.Equals(datetime)) {
				epochTime = SolarUtils.DateTimeToEpoch(newTime);
			}
		}

		EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
		planetPrefab = (GameObject) EditorGUILayout.ObjectField(
			new GUIContent("Planet Prefab", "Game object with NBody"), solar.planetPrefab, typeof(GameObject), true);
		asteroidPrefab = (GameObject) EditorGUILayout.ObjectField(
			new GUIContent("Asteroid Prefab", "Game object with NBody"), solar.asteroidPrefab, typeof(GameObject), true);
		cometPrefab = (GameObject) EditorGUILayout.ObjectField(
			new GUIContent("Comet Prefab", "Game object with NBody"), solar.cometPrefab, typeof(GameObject), true);

		if (GUILayout.Button("Add Body")) {
			SolarBodyCreatorWindow.Init();
        }

		 // Apply changes to the serializedProperty 
         //  - always do this in the end of OnInspectorGUI.
         // Checking the Event type lets us update after Undo and Redo commands.
         if (Event.current.type == EventType.ExecuteCommand &&
             Event.current.commandName == "UndoRedoPerformed") {
            // explicitly re-set so setter code will run
			solar.planetScale = planetScale; 
			solar.epochTime = epochTime;
         }

		if (GUI.changed) {
			Undo.RecordObject(solar, "Solar System Change");
			solar.planetScale = planetScale;
            solar.epochFoldout = epochFoldout;
            // epochTime runs a setter that updates the objects in the scene
			solar.epochTime = epochTime;
			solar.planetPrefab = planetPrefab;
			solar.asteroidPrefab = asteroidPrefab;
			solar.cometPrefab = cometPrefab;
			EditorUtility.SetDirty(solar);
		}


	}


}
