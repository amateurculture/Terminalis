using UnityEngine;
using System.Collections;
using OrbCreationExtensions;

public class SimpleLODShowInfo : MonoBehaviour {

	public LODSwitcher lodSwitcher;
	public float offsetY = 2f;
	void OnGUI() {
		if(lodSwitcher != null) {
			Vector3 screenCenter = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0,offsetY,0));
			if(screenCenter.z > 0f) {
				GUIStyle style = new GUIStyle(GUI.skin.label);
				style.normal.textColor = Color.black;
				style.alignment = TextAnchor.LowerCenter;
				GUI.Label(new Rect(screenCenter.x - 50, Screen.height - screenCenter.y - 20, 100, 20), "LOD "+lodSwitcher.GetLODLevel(), style);
			}
		}
	}
	
}
