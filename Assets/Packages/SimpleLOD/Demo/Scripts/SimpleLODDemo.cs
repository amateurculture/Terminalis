using UnityEngine;
using System.Collections;
using OrbCreationExtensions;

public class SimpleLODDemo : MonoBehaviour {

	public GameObject[] scenes;

	private string[] sceneStrings = new string[] {"Original", "Submeshes merged per material", "LOD levels generated"};
	private int currentScene;
	private int lodSetting = 0;

	void Start () {
		QualitySettings.SetQualityLevel(QualitySettings.names.Length-1, true);  // set best quality as default;
		for(int i=1;i<scenes.Length;i++) scenes[i].SetActive(false);
		SetScene(0);
	}
	
	private void SetScene(int aScene) {
		scenes[currentScene].SetActive(false);
		scenes[aScene].SetActive(true);
		currentScene = aScene;
		Camera.main.gameObject.GetComponent<SimpleLODDemoCamera>().SetCurrentScene(scenes[currentScene]);
	}

	private void SetLOD(int aLod) {
		lodSetting = aLod;
		aLod--; // first option was automatic instead of lod 0
		LODSwitcher[] switchers = scenes[currentScene].GetComponentsInChildren<LODSwitcher>();
		foreach(LODSwitcher switcher in switchers) {
			if(aLod < 0) switcher.ReleaseFixedLODLevel();
			else switcher.SetFixedLODLevel(aLod);
		}
	}

	void OnGUI() {
		GUI.skin.label.normal.textColor = Color.black;
		GUI.Label(new Rect(2,70,200,100), "Switch scene to:");
        int newScene = GUI.SelectionGrid(new Rect(2, 90, 250, 80), currentScene, sceneStrings, 1);
        if(newScene != currentScene) SetScene(newScene);
		if(currentScene == 2) {
			GUI.Label(new Rect(Screen.width - 102,2,100,24), "Set LOD to:");
        	int newLOD = GUI.SelectionGrid(new Rect(Screen.width - 102, 24, 100, 110), lodSetting, new string[5] {"Automatic", "LOD 0", "LOD 1", "LOD 2", "LOD 3"}, 1);
        	if(newLOD != lodSetting) SetLOD(newLOD);
        }
	}

}
