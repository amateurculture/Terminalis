using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ShipInfo : MonoBehaviour {

    public Text time; 
    public Text position;
    public Text velocity;
    public Text altitude;
    public Text fuelText;
    public Text attitude;
    public Text drag;
    public Text accel;
    public Text thrust; 

    public NBody ship;
    public RocketEngine rocketEngine;
    public EarthRocket earthRocket;
    public EarthAtmosphere atmosphere; 

    private float altitudeNow;

    private const string time_format     =  "         Time: {0:0.0} ( x {1})";
    private const string pos_format      =  "     Pos [km]:   ({0:0.0}, {1:0.0}, {2:0.0})";
    private const string vel_format      =  "  Vel [km/hr]: ({0:0.0}, {1:0.0}, {2:0.0})";
    private const string fuel_format     =  "    Fuel (kg): {0:0.0}";
    private const string attitude_format =  "Pitch: {0:0.0} Roll: {1:0.0} Yaw: {2:0.0}";
    private const string altitude_format =  "Altitude (km): {0:0.0}";
    private const string drag_format    =   " Drag (m/s^2): {0:0.000}";
    private const string accel_format   =   "Accel (m/s^2): {0:0.000}";
    private const string thrust_format  =   "   Thrust (%): {0:00.0}";
    private float initial_altitude; 

    // Use this for initialization
    void Start () {
        GravityEngine ge = GravityEngine.Instance();
        time.text = string.Format(time_format,ge.GetTimeWorldSeconds(), ge.GetTimeZoom() );
        Vector3 pos = ship.initialPos; 
        position.text = string.Format(pos_format, pos.x, pos.y, pos.z);
        velocity.text = string.Format(vel_format, 0f, 0f, 0f);
        if (rocketEngine == null)
        {
            Debug.LogError("Need a rocket component");
        }
        fuelText.text = string.Format(fuel_format, rocketEngine.GetFuel());
        attitude.text = string.Format(attitude_format, 0f, 0f, 0f);

        // assume earth at (0,0,0)
        initial_altitude = Vector3.Magnitude(ship.transform.position) - (float) atmosphere.heightEarthSurface;
        altitude.text = string.Format(altitude_format, initial_altitude);

        if (drag != null) {
            drag.text = string.Format(drag_format, atmosphere.GetAccelSI());
        }

        if (accel != null) {
            accel.text = string.Format(accel_format, earthRocket.GetAccel());
        }

        if (thrust != null) {
            thrust.text = string.Format(thrust_format, 100f);
        }
    }

    public void SetTextInfo(float fuel, Vector3 angles)
    {
        GravityEngine ge = GravityEngine.Instance();
        time.text = string.Format(time_format, ge.GetTimeWorldSeconds() , ge.GetTimeZoom() );
        double[] r = new double[3];
        double[] v = new double[3];
        GravityEngine.Instance().GetPositionVelocityScaled(ship, ref r, ref v);

        position.text = string.Format(pos_format, r[0], r[1], r[2]);
        velocity.text = string.Format(vel_format, v[0], v[1], v[2]);
        fuelText.text = string.Format(fuel_format, fuel);
        attitude.text = string.Format(attitude_format, angles.y, angles.x, angles.z);

        altitudeNow = Vector3.Magnitude(ship.transform.position) - (float) atmosphere.heightEarthSurface; 
        altitude.text = string.Format(altitude_format, altitudeNow);

        if (drag != null) {
            drag.text = string.Format(drag_format, atmosphere.GetAccelSI());
        }

        if (accel != null) {
            accel.text = string.Format(accel_format, earthRocket.GetAccel());
        }
        if (thrust != null) {
            thrust.text = string.Format(thrust_format, rocketEngine.GetThrottlePercent() );
        }
    }

    public float GetAltitude() {
        return altitudeNow;
    }


}
