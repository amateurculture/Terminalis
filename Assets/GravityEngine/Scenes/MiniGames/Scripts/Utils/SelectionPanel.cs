using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Attach to a Panel with a Layout group. Allows for programatic addition of buttons in the
/// layout group. 
/// </summary>
public class SelectionPanel : MonoBehaviour {

    //! button prefab  - need to contain a UI Button and a Layout element
    public GameObject buttonPrefab;

    private List<GameObject> buttonList;

    public void Clear() {
        if (buttonList != null) {
            foreach (GameObject b in buttonList) {
                Destroy(b);
            }
        }
    }

    public void AddButton(string label, UnityAction action) {

        // can get called as part of other awake(), so defend
        if (buttonList == null) {
            buttonList = new List<GameObject>();
        }

        GameObject buttonGo = Instantiate(buttonPrefab) as GameObject;
        buttonGo.transform.SetParent(transform);

        Button button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(action);
        Text text = buttonGo.GetComponentInChildren<Text>();
        text.text = label;

        buttonList.Add(buttonGo);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
