using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quick and dirty component to draw a 2D radius for sphere of influence around a moon
/// for the FreeReturn mini-game. Assumes the moon has an orbit universal to get inclination.
/// 
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class DrawSOI : MonoBehaviour {

    [SerializeField]
    private NBody moonBody = null;

    [SerializeField]
    private NBody planetBody = null;

    private float soiRadius;

    private float inclination = 0.0f; 

    private LineRenderer soiRenderer;

    // Use this for initialization
    void Start () {
        soiRenderer = GetComponent<LineRenderer>();
        soiRadius = OrbitUtils.SoiRadius(planetBody, moonBody);

        OrbitUniversal orbitU = moonBody.GetComponent<OrbitUniversal>();
        if (orbitU != null) {
            inclination = (float) orbitU.inclination;
        }
    }

    // Update is called once per frame
    void Update () {
        Draw(soiRadius);
	}

    /// <summary>
    ///  Draw a circle at SOI radius around the moon
    /// </summary>
    /// <param name="physRadius">radius in physics units</param>
    private void Draw(float physRadius) {
        const int numPoints = 200;
        Vector3[] points = new Vector3[numPoints];

        float radius = GravityScaler.ScaleDistancePhyToScene(physRadius);

        float dtheta = 2f * Mathf.PI / (float)numPoints;
        float theta = 0;

        // add a fudge factor to ensure we go all the way around the circle
        for (int i = 0; i < numPoints; i++) {
            points[i] = new Vector3(radius * Mathf.Cos(theta), radius * Mathf.Sin(theta), 0);
            points[i] = Quaternion.AngleAxis(inclination, Vector3.right) * points[i];
            points[i] += moonBody.transform.position;
            theta += dtheta;
        }
        // close the path (credit for fix to R. Vincent)
        points[numPoints - 1] = points[0];
        soiRenderer.positionCount = numPoints;
        soiRenderer.SetPositions(points);

    }

}
