using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Script to clear all trails after objects have been placed by GE. 
/// 
/// Kinda hacky. 
/// 
/// </summary>
public class TrailClearAtStart : MonoBehaviour {

    private TrailRenderer[] trails;

    private int frameCount = 0; 

	// Use this for initialization
	void Start () {
        trails = (TrailRenderer[])Object.FindObjectsOfType(typeof(TrailRenderer));
    }

    // Update is called once per frame
    void Update () {
		if (frameCount++ > 5) {
            foreach (TrailRenderer t in trails) {
                t.Clear();
            }
            this.gameObject.SetActive(false);
        }
	}
}
