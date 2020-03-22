using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Show the maneuvers as 3D on screen vectors. 
/// 
/// This script requires a prefab with a line renderer and a MeshRenderer (Cone for arrow end)
/// </summary>
public class ManeuverRenderer : MonoBehaviour {

    //! Prefab of a gameobject with a LineRender and Cone model as children
    public GameObject maneuverArrowPrefab;

    //! Visual scale of velocity in the scene
    public float lineLengthScale = 1f;
    public float lineWidthScale = 1f;
    public float coneScale = 1f;

    private List<ManeuverObject> maneuverObjects;

    private const int MAX_ARROWS = 4;

    private GravityEngine ge; 

    /// <summary>
    /// Inner class to hold the decomposed elements of the ManeuverArrow prefab so they 
    /// do not need to be looked up each time. 
    /// </summary>
    private class ManeuverObject
    {
        public LineRenderer line;
        public GameObject cone;
        public GameObject parent;

        public ManeuverObject(GameObject go) {
            parent = go;
            line = go.GetComponentInChildren<LineRenderer>();
            if (line == null) {
                Debug.LogError("Require a child with a LineRenderer component");
            }
            cone = go.GetComponentInChildren<MeshRenderer>().gameObject;
            if (cone == null) {
                Debug.LogError("Require a child with a MeshRenderer component (assumed to be the arrow head)");
            }
        }
    }

    void Start() {
        ge = GravityEngine.Instance();
        maneuverObjects = new List<ManeuverObject>();
        // create an object pool of maneuverArrows at the start
        for (int i = 0; i < MAX_ARROWS; i++) {
            GameObject go = Instantiate(maneuverArrowPrefab);
            go.SetActive(false);
            // make it a child of this object
            go.transform.SetParent(transform);
            ManeuverObject mo = new ManeuverObject(go);
            maneuverObjects.Add(mo);
        }
    }

    /// <summary>
    /// Show each maneuver in the list using an in-scene vector made from the maneuverArrowPreb. 
    /// Optionally update on-screen text. 
    /// 
    /// Position is determined from the maneuver Nbody (if present) or taken from the maneuver
    /// physical location, scaled and mapped to the scene.
    /// </summary>
    /// <param name="maneuvers"></param>
    public void ShowManeuvers(List<Maneuver> maneuvers) {
        int i = 0; 
        foreach( Maneuver m in maneuvers) {
            ManeuverObject mo = maneuverObjects[i];
            mo.parent.SetActive(true);
            Vector3 pos = m.physPosition.ToVector3();
            // TODO - need to scale and map position
            Vector3 vel = Vector3.zero;
            if (m.mtype == Maneuver.Mtype.vector) {
                vel = m.velChange;
            }
            ConfigManeuver(mo, pos, vel);
            i++;
        }
        // inactivate the rest of the cached maneuver objects
        for ( int j = i; j < MAX_ARROWS; j++) {
            maneuverObjects[j].parent.SetActive(false);
        }
    }

    public void Clear() {
        for (int j = 0; j < MAX_ARROWS; j++) {
            maneuverObjects[j].parent.SetActive(false);
        }
    }

    private void ConfigManeuver(ManeuverObject mo, Vector3 physPosition, Vector3 velocity) {
        mo.parent.transform.position = ge.MapPhyPosToWorld( physPosition);
        Vector3 endPoint = mo.parent.transform.position + lineLengthScale * velocity;
        // May need separate velocity scale??
        mo.line.SetPositions(new Vector3[] { mo.parent.transform.position, endPoint });
        mo.line.widthMultiplier = lineWidthScale;
        mo.line.positionCount = 2;
        mo.cone.transform.localPosition = lineLengthScale * velocity;
        mo.cone.transform.rotation = Quaternion.FromToRotation(Vector3.forward, velocity.normalized);
        mo.cone.transform.localScale = Vector3.one * coneScale * velocity.magnitude;
    }


}
