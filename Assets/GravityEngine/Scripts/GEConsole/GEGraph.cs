using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility singleton to allow scripts to update a quantity and see it displayed as a line graph
/// via a secondary camera. 
/// 
/// Trail prefab needs to be a game object with a trail renderer attached. 
/// 
/// This object moves in the +x direction as time goes on. It is assumed there is a Camera as a child of this
/// object to create a "strip chart" like behaviour. Typically this camera will have a new view window. 
/// 
/// Concept: This object will move in +x as time goes on, and the camera as a child will follow along. 
/// Each graph line will move up and down based on the latest value. A trail renderer on the trailPrefab
/// will leave a strip-chart like line behind. 
/// </summary>
public class GEGraph : MonoBehaviour {

    [SerializeField]
    public GameObject trailPrefab;

    [SerializeField]
    public float timeScale = 1.0f;

    protected class GraphLine
    {
        public double scale;
        public double offset;
        public double value; 
        public Color color;
        public GameObject gameObject;
    }

    private List<GraphLine> graphLines;
    private static GEGraph instance;

    private Camera graphCamera;

    /// <summary>
    /// Singleton indexer. 
    /// (Eventually want to generalize to more than one, perhaps with tags to identify them)
    /// </summary>
    /// <returns></returns>
    public static GEGraph Instance() {
        if (instance == null)
            instance = (GEGraph)FindObjectOfType<GEGraph>();
        return instance;
    }

	// Use this for initialization
	void Start () {
        graphLines = new List<GraphLine>();

        graphCamera = GetComponentInChildren<Camera>();
        if (graphCamera == null) {
            Debug.LogError("Script assumes a Camera attach to a child game object.");
        }
	}

    /// <summary>
    /// Define a new graph line
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public int NewGraphLine(Color color, double scale, double offset) {
        GraphLine line = new GraphLine();
        line.color = color;
        line.scale = scale;
        line.offset = offset;
        line.gameObject = Instantiate(trailPrefab);
        line.gameObject.transform.SetParent(this.transform);
        graphLines.Add(line);
        return graphLines.Count - 1;
    }

    public void SetValue(int index, double value) {
        graphLines[index].value = value;
    }
	
	// Update is called once per frame
	void Update () {
        float time = Time.time * timeScale;
        Vector3 myPos = transform.position;
        myPos.x = time;
        transform.position = myPos;
        foreach(GraphLine line in graphLines) {
            Vector3 pos = new Vector3(0, (float)(line.value * line.scale + line.offset), 0);
            line.gameObject.transform.localPosition = pos;
        }
	}
}
