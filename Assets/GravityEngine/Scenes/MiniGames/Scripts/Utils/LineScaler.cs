using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Find all line renderers in the scene and allow their width to be globally changed. 
/// 
/// This is useful when zooming across larger scales with orbit predictor and/or trajectory
/// paths in use. 
/// 
/// </summary>
public class LineScaler : MonoBehaviour {

    //! scale of width w.r.t. zoom value
    public float zoomSlope = 0.2f;

    private LineRenderer[] lineRenderers;

    private float[] initialWidth; 

	void Start () {
        FindAll();
	}

    /// <summary>
    /// Re-detect all line renderers in a scene
    /// </summary>
    public void FindAll() {
        lineRenderers = (LineRenderer[])Object.FindObjectsOfType(typeof(LineRenderer));
        initialWidth = new float[lineRenderers.Length];
        for (int i = 0; i < lineRenderers.Length; i++) {
            initialWidth[i] = lineRenderers[i].startWidth;
        }
    }

    // Update is called once per frame
    public void SetZoom (float zoom) {

        // turn zoom into a start/end width
        // this will be scene dependent

        for (int i = 0; i < lineRenderers.Length; i++) {
            lineRenderers[i].widthMultiplier = zoom * zoomSlope;
        }
    }
}
