using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Launch to Orbit Mini-Game control logic
/// 
/// Provides user interface and game control for the launch and staging of a two stage rocket.
/// 
/// WASD orientation control for the ship
/// 
/// SPACE to launch/stage
/// 
/// F1/F2 for camera selection
/// 
/// Some UI is delegated:
/// - TimeZoom using 1-5 handled by the TmeZoom script
///-  Camera controls handled by CameraSpin script
///
/// </summary>
[RequireComponent(typeof(LineScaler))]
public class ReentryUI : MonoBehaviour
{

    public GameObject mainCameraBoom;
    public GameObject shipCameraBoom;

    public GameObject shipModel;

    //! Ship GO includes stages at launch
    public GameObject ship;

    public EarthAtmosphere atmosphere; 

    // orbit parameter HUD - enabled at altitude HUD_ENABLE_ALTITUTE
    public GameObject orbitHUD;
    private const float HUD_ENABLE_ALTITUTE = 50f; // km

    public ShipInfo shipInfo;

    private float launchTime;

    private const float LAUNCH_LOCKOUT = 5f;

    private LineScaler lineScaler;

    // Start scene with ship and record initial scale
    private Vector3 initialShipScale; 

    private const float SHIP_AT_EARTH_SCALE = 60;
    private const float EARTH_LINE_SCALE = 300f;
    private const float SHIP_LINE_SCALE = 1f;

    private bool orbitPredictorOn = false;

    // awkward - flag to set line scale one frame after orbit predictor is turned on. Ick.
    private bool doLineScale = false; 
 
    // Use this for initialization
    void Start() {
        mainCameraBoom.SetActive(false);
        shipCameraBoom.SetActive(true);
        lineScaler = GetComponent<LineScaler>();
        initialShipScale = shipModel.transform.localScale;

    }


    private void SelectCamera() {
        if (Input.GetKeyUp(KeyCode.F1)) {
            // Earth cam
            mainCameraBoom.SetActive(true);
            shipCameraBoom.SetActive(false);
            lineScaler.SetZoom(EARTH_LINE_SCALE);
            shipModel.transform.localScale = SHIP_AT_EARTH_SCALE * Vector3.one;
            // Need to move forward so not hidden under the earth?

        } else if (Input.GetKeyUp(KeyCode.F2)) {
            // ship cam
            mainCameraBoom.SetActive(false);
            shipCameraBoom.SetActive(true);
            lineScaler.SetZoom(SHIP_LINE_SCALE);
            shipModel.transform.localScale = initialShipScale;
        }
    }


    // Update is called once per frame
    void Update() {

        SelectCamera();
        if (Input.GetKeyUp(KeyCode.P)) {
            orbitHUD.SetActive(true);
        }

        // toggle trajectory prediction
        if (Input.GetKeyUp(KeyCode.T)) {
            bool pred = GravityEngine.Instance().trajectoryPrediction; 
            GravityEngine.Instance().SetTrajectoryPrediction(!pred);
        }

        shipInfo.SetTextInfo(0, Vector3.zero);
        float altitude = shipInfo.GetAltitude();

        // Awkward
        if(doLineScale) {
            lineScaler.FindAll();
            if (mainCameraBoom.activeInHierarchy) {
                lineScaler.SetZoom(EARTH_LINE_SCALE);
            } else {
                lineScaler.SetZoom(SHIP_LINE_SCALE);
            }

        }

        if ((altitude < HUD_ENABLE_ALTITUTE) && orbitPredictorOn) {
            orbitHUD.SetActive(false);
            // have added an orbit predictor, need to apply correct scale, but LineScaler will not be there until next frame
            doLineScale = true; 
            orbitPredictorOn = false;
        }
    }
}
