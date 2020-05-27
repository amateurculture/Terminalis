using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Q&D controller to test TOF
/// 
/// AS - change phase of from marger
/// QW - change phase of to marker
/// 
/// Display the time between the two points. 
/// 
/// Run a counter when spaceship passed each marker to report actual time in GE to 
/// compare to TOF algorithm.
/// 
/// </summary>
public class FindTOFController: MonoBehaviour {

    public NBody spaceshipNBody;

    public float fromPhase;
    public float toPhase;

    //! Prefab for symbol to be used for manuevering
    public GameObject fromMarker;
    public GameObject toMarker;

    public Text tofText;

    private OrbitUniversal shipOrbit;
    private OrbitData shipOrbitData;

    private const float PHASE_PER_KEY = 0.1f;

    // Use this for initialization
    void Start () {

        shipOrbit = spaceshipNBody.GetComponent<OrbitUniversal>();
        if (shipOrbit == null) {
            Debug.LogError("spaceship needs an OrbitUniversal");
        }
     }

    private void SetMarkers() {

        if (shipOrbitData == null) {
            // Cannot rely on controller start after GE start, so instead of forcing
            // start order, do a lazy init here
            shipOrbitData = new OrbitData();
            shipOrbitData.SetOrbit(spaceshipNBody, shipOrbit.centerNbody);
        }

        // skip scaling since we are in dimensionless units
        Vector3 pos = shipOrbitData.GetPhysicsPositionforEllipse(fromPhase);
        fromMarker.transform.position = pos;

        pos = shipOrbitData.GetPhysicsPositionforEllipse(toPhase);
        toMarker.transform.position = pos;

    }

  
     // Update is called once per frame
    void Update() {
        if (Input.GetKey(KeyCode.A)) {
            fromPhase += PHASE_PER_KEY;
        } else if (Input.GetKey(KeyCode.S)) {
            fromPhase -= PHASE_PER_KEY;
        } else if (Input.GetKey(KeyCode.Q)) {
            toPhase += PHASE_PER_KEY;
        } else if (Input.GetKey(KeyCode.W)) {
            toPhase -= PHASE_PER_KEY;
        }
        fromPhase = NUtils.DegreesMod360(fromPhase);
        toPhase = NUtils.DegreesMod360(toPhase);
        SetMarkers();

        shipOrbitData.SetOrbitForVelocity(spaceshipNBody, shipOrbit.centerNbody);

        // Determine TOF 
        Vector3d from = new Vector3d(shipOrbitData.GetPhysicsPositionforEllipse(fromPhase));
        Vector3d to = new Vector3d(shipOrbitData.GetPhysicsPositionforEllipse(toPhase));

        Vector3d shipPos = GravityEngine.instance.GetPositionDoubleV3(spaceshipNBody);
        double tPeri = shipOrbit.TimeOfFlight(shipPos, new Vector3d(shipOrbitData.GetPhysicsPositionforEllipse(0f)));
        double tApo = shipOrbit.TimeOfFlight(shipPos, new Vector3d(shipOrbitData.GetPhysicsPositionforEllipse(180f)));
        double tof = shipOrbit.TimeOfFlight(from, to);
        // Scale to game time
        //tApo = GravityScaler.ScaleToGameSeconds((float) tApo);
        //tPeri = GravityScaler.ScaleToGameSeconds((float)tPeri);
        //tof = GravityScaler.ScaleToGameSeconds((float)tof);
        //tofText.text = string.Format("Time of Flight = {0:#.#}\nTime to Apoapsis = {1:#.#}\nTime to Periapsis = {2:#.#}\ntau = {3}", 
        //    tof, tApo, tPeri, shipOrbitData.tau);
        GravityScaler.Units units = GravityEngine.instance.units;
        tofText.text = string.Format("Time of Flight = {0}\nTime to Apoapsis = {1}\nTime to Periapsis = {2}\ntau = {3}",
             GravityScaler.GetWorldTimeFormatted(tof, units),
             GravityScaler.GetWorldTimeFormatted(tApo, units),
             GravityScaler.GetWorldTimeFormatted(tPeri, units),
             GravityScaler.GetWorldTimeFormatted(shipOrbitData.tau, units));
    }



}
