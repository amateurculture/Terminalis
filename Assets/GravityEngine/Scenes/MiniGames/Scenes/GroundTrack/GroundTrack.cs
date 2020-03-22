using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Create a line renderer that shows the ground track of a satelite on a planet that is being rotated by a 
/// PlanetRotation script. This requires that a list of points be recorded, since the planet rotates underneath
/// the orbit. 
/// 
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class GroundTrack : MonoBehaviour
{
    [SerializeField]
    private NBody ship = null;

    [SerializeField]
    private NBody planet = null;

    [SerializeField]
    private PlanetRotation planetRotation = null;

    [SerializeField]
    private float radius = 1f;

    [SerializeField]
    private float trailTime= 60f; 

    private LineRenderer lineR;

    private GravityEngine ge; 

    private struct TrackPoint
    {
        public Vector3 pos;
        public float time;
    }

    private List<TrackPoint> trailPoints;

    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();
        lineR = GetComponent<LineRenderer>();
        trailPoints = new List<TrackPoint>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 toShip = ge.GetPhysicsPosition(ship) - ge.GetPhysicsPosition(planet);
        Vector3 point = planet.transform.position + radius * toShip.normalized;
        TrackPoint tp = new TrackPoint();
        tp.pos = point;
        tp.time = ge.GetPhysicalTime();
        trailPoints.Add(tp);

        if (Time.time > trailTime) {
            trailPoints.RemoveAt(0);
        }

        // Need to adjust points each time through. A bit greedy
        Vector3[] points = new Vector3[trailPoints.Count];
        float timeNow = ge.GetPhysicalTime();
        for (int i = 0; i < points.Length; i++) {
            points[i] = planetRotation.RotatePoint(trailPoints[i].pos, trailPoints[i].time - timeNow);
        }
        lineR.positionCount = trailPoints.Count;
        lineR.SetPositions(points);

    }
}
