using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Computes the acceleration due to drag in the Earth's atmosphere for a given height and
/// velocity. 
/// 
/// This component is attached to the spaceship and called via the GEExternalAcceleration interface
/// from within the physics integrator as a callback. It will potentially get called a LOT so efforts
/// are made to make the acceleration computation effecient where possible. 
/// </summary>
[RequireComponent(typeof(NBody))]   // Must be attached to the spaceship, which must have an NBody
public class EarthAtmosphere : MonoBehaviour, GEExternalAcceleration {

    [Tooltip("NBody object for Earth")]
    [SerializeField]
    private NBody earth;

    [Tooltip("Optional trigger on Impact with Earth Surface")]
    [SerializeField]
    private ImpactTrigger impactTrigger = null;

    [SerializeField]
    [Tooltip("Height of the Earth's surface (km)")]
    public double heightEarthSurface = 6371;

    [SerializeField]
    [Tooltip("Spaceship NBody (if null will use attached Nbody")]
    private NBody spaceship;

    // Cannot add { get; set; } to attribute with a tooltip - so public. (Ick)

    [SerializeField]
    [Tooltip("Inertial mass of the spaceship in kg")]
    public double inertialMassKg = 100;

    [SerializeField]
    [Tooltip("Cross sectional area in m^2")]
    public double crossSectionalArea;

    [SerializeField]
    [Tooltip("Drag Co-efficient. 2.0-2.1 for a sphere. 2.2 is a typical value")]
    public double coeefDrag = 2.1;

    private double geDistanceToKm;

    private double[] densityTablePer10km;

    private double[] v_ship;
    private double[] v_earth;

    // conversion factors
    private double velocityScaleInternalToSI;
    private double accelSItoGE;

    private GravityState liveState;
    private double accel_last;

    private bool impact; 

    /// <summary>
    /// Validation:
    /// - confirmed velocity in SI units matches expected orbital velocity of ISS
    /// - checked that SI acceleration = -g at terminal velocity (9.73 m/s^2 at 28 km)
    /// </summary>

    // Use this for initialization
    void Start () {
		if (inertialMassKg == 0) {
            Debug.LogError("Mass is zero. Drag calculation will fail.");
        }
        if (spaceship == null)
            spaceship = GetComponent<NBody>();

        geDistanceToKm = 1;
        v_ship = new double[] { 0, 0, 0 };
        v_earth = new double[] { 0, 0, 0 };

        velocityScaleInternalToSI = GravityScaler.VelocityScaletoSIUnits()/GravityScaler.GetVelocityScale();
        accelSItoGE = GravityScaler.AccelSItoGEUnits()/ GravityScaler.AccelerationScaleInternalToGEUnits();

        LoadDensityProfile();

        liveState = GravityEngine.Instance().GetWorldState();
	}

    public void InitFrom(EarthAtmosphere atm, NBody nbody) {
        this.earth = atm.earth;
        this.spaceship = nbody;
        this.heightEarthSurface = atm.heightEarthSurface;
        this.inertialMassKg = atm.inertialMassKg;
        this.crossSectionalArea = atm.crossSectionalArea;
        this.coeefDrag = atm.coeefDrag;
    }

    /// <summary>
    /// Calculate the acceleration due to drag in the Earths atmosphere. 
    /// - first determines the density at the current heigh
    /// - then computes the accel change due to drag (not the drag force!)
    /// </summary>
    /// <param name="time"></param>
    /// <param name="gravityState"></param>
    /// <returns></returns>
    /// 

    // local but avoid allocing each time
    private double[] accel = new double[] { 0, 0, 0 };
    private double[] v_rel = new double[] { 0, 0, 0 };

    //private int logCount = 0;
    //private const int LOG_INTERVAL = 1;

    /// <summary>
    /// Get the last "in flight" value for atmosphere deceleration. 
    /// </summary>
    /// <returns>Magnitude of the acceleration in m/s^2</returns>
    public double GetAccelSI() {
        return accel_last;
    }

    /// <summary>
    /// Determine the acceleration due to atmospheric resistance. 
    /// </summary>
    /// <param name="time">(not used)</param>
    /// <param name="gravityState"></param>
    /// <param name="massKg">Updated mass (0 to use the inertial mass from inspector)</param>
    /// <returns></returns>
    public double[] acceleration(double time, GravityState gravityState, ref double massKg) {

        bool isLiveState = (gravityState == liveState);
        accel[0] = 0;
        accel[1] = 0;
        accel[2] = 0;

        if (impact)
            return accel; 

        if (massKg < 1E-9) {
            massKg = inertialMassKg;
        }

        // determine height above Earth's surface in km
        // due to dynamic add/delete cannot in general keep a cached value of the index for ship or Earth
        // @TODO: Pass in this objects position to save a lookup
        Vector3d earthPos = gravityState.GetPhysicsPositionDouble(earth);
        Vector3d shipPos = gravityState.GetPhysicsPositionDouble(spaceship);
        double h = Vector3d.Distance(earthPos, shipPos) * geDistanceToKm - heightEarthSurface;
        if (h < 0) {
            if (isLiveState) {
                impact = true;
                Debug.LogFormat("Impact of {0} at h={1} shipPos={2}", gameObject.name, h, shipPos);
                if (impactTrigger != null) {
                    impactTrigger.Impact(spaceship, gravityState);
                }
                return accel;
            } else {
                return accel;
            }
        }

        // retrieve the density from the pre-calculated table
        double hdiv10 = h / 10.0;
        int densityIndex = (int) hdiv10;
        if (densityIndex > densityTablePer10km.Length-2) {
            // too far away, no drag
            return accel;
        }
        // interpolate.
        double density = densityTablePer10km[densityIndex] +
                        (hdiv10 - (double) densityIndex) *
                        (densityTablePer10km[densityIndex+1] - densityTablePer10km[densityIndex]);

        // determine accel due to drag
        gravityState.GetVelocityDouble(spaceship, ref v_ship);
        gravityState.GetVelocityDouble(earth, ref v_earth);
        // convert to SI
        v_rel[0] = (v_ship[0] - v_earth[0]) * velocityScaleInternalToSI;
        v_rel[1] = (v_ship[1] - v_earth[1]) * velocityScaleInternalToSI;
        v_rel[2] = (v_ship[2] - v_earth[2]) * velocityScaleInternalToSI;
        double v_rel_sq = (v_rel[0] * v_rel[0]) + (v_rel[1] * v_rel[1]) + (v_rel[2] * v_rel[2]);
        // guard against divide by zero for norm
        if (v_rel_sq < 1E-9) {
            return accel;
        }
        double v_rel_mag = System.Math.Sqrt(v_rel_sq);

        double a_mag = 0.5 * coeefDrag * crossSectionalArea * density * v_rel_sq/inertialMassKg;

        // save the last live value for shipInfo etc.
        if (isLiveState) {
            accel_last = a_mag;
        }

        // convert to GE scale
        a_mag *= accelSItoGE;

        // in the -ve direction of v_rel
        v_rel[0] = -v_rel[0] / v_rel_mag;
        v_rel[1] = -v_rel[1] / v_rel_mag;
        v_rel[2] = -v_rel[2] / v_rel_mag;

        accel[0] = a_mag * v_rel[0];
        accel[1] = a_mag * v_rel[1];
        accel[2] = a_mag * v_rel[2];

        //if (logCount++ > LOG_INTERVAL) {
        //    logCount = 0;
        //    Debug.LogFormat("h={0} accel = ({1}, {2}, {3}) |a|={4} (m/s^2) -|v_rel|=({5},{6},{7}) v_rel = {8} m/s", 
        //        h, accel[0], accel[1], accel[2], a_mag/accelSItoGE, 
        //        v_rel[0], v_rel[1], v_rel[2], v_rel_mag);
        //}

        return accel;
    }

    /// <summary>
    /// Determining the density for each call to acceleration is quite expensive. Instead create a 
    /// table indexed by integer km number/10 and then interpolate the density from there. Instead of doing this
    /// at run time, take from a table of values computed from the DensityTable method below. 
    /// </summary>
    private void LoadDensityProfile() {

        densityTablePer10km = new double[101];

        densityTablePer10km[0] = 1.225;
        densityTablePer10km[1] = 0.308337666482878;
        densityTablePer10km[2] = 0.0776098910792704;
        densityTablePer10km[3] = 0.01774;
        densityTablePer10km[4] = 0.003972;
        densityTablePer10km[5] = 0.001057;
        densityTablePer10km[6] = 0.0003206;
        densityTablePer10km[7] = 8.77E-05;
        densityTablePer10km[8] = 1.905E-05;
        densityTablePer10km[9] = 3.396E-06;
        densityTablePer10km[10] = 5.297E-07;
        densityTablePer10km[11] = 9.661E-08;
        densityTablePer10km[12] = 2.438E-08;
        densityTablePer10km[13] = 8.484E-09;
        densityTablePer10km[14] = 3.845E-09;
        densityTablePer10km[15] = 2.07E-09;
        densityTablePer10km[16] = 1.32784591953683E-09;
        densityTablePer10km[17] = 8.51775258951991E-10;
        densityTablePer10km[18] = 5.464E-10;
        densityTablePer10km[19] = 3.90373444167217E-10;
        densityTablePer10km[20] = 2.789E-10;
        densityTablePer10km[21] = 2.13011858346484E-10;
        densityTablePer10km[22] = 1.62689321607108E-10;
        densityTablePer10km[23] = 1.24255126312868E-10;
        densityTablePer10km[24] = 9.49007363391221E-11;
        densityTablePer10km[25] = 7.248E-11;
        densityTablePer10km[26] = 5.81922633011631E-11;
        densityTablePer10km[27] = 4.67210197035306E-11;
        densityTablePer10km[28] = 3.7511063469739E-11;
        densityTablePer10km[29] = 3.01166346873302E-11;
        densityTablePer10km[30] = 2.418E-11;
        densityTablePer10km[31] = 2.00665869406376E-11;
        densityTablePer10km[32] = 1.66529326487248E-11;
        densityTablePer10km[33] = 1.3819996725071E-11;
        densityTablePer10km[34] = 1.14689894873021E-11;
        densityTablePer10km[35] = 9.518E-12;
        densityTablePer10km[36] = 7.88971838521758E-12;
        densityTablePer10km[37] = 6.53999329670523E-12;
        densityTablePer10km[38] = 5.4211709762781E-12;
        densityTablePer10km[39] = 4.4937499811882E-12;
        densityTablePer10km[40] = 3.725E-12;
        densityTablePer10km[41] = 3.13983578401641E-12;
        densityTablePer10km[42] = 2.64659563774227E-12;
        densityTablePer10km[43] = 2.23083911119595E-12;
        densityTablePer10km[44] = 1.8803942200581E-12;
        densityTablePer10km[45] = 1.585E-12;
        densityTablePer10km[46] = 1.34472083341642E-12;
        densityTablePer10km[47] = 1.14086695257044E-12;
        densityTablePer10km[48] = 9.67916441184711E-13;
        densityTablePer10km[49] = 8.21184481682875E-13;
        densityTablePer10km[50] = 6.967E-13;
        densityTablePer10km[51] = 5.95659455100653E-13;
        densityTablePer10km[52] = 5.09272551242725E-13;
        densityTablePer10km[53] = 4.35414109905211E-13;
        densityTablePer10km[54] = 3.72267161546252E-13;
        densityTablePer10km[55] = 3.18278246875997E-13;
        densityTablePer10km[56] = 2.72119200666783E-13;
        densityTablePer10km[57] = 2.32654477955506E-13;
        densityTablePer10km[58] = 1.98913218839821E-13;
        densityTablePer10km[59] = 1.70065364642521E-13;
        densityTablePer10km[60] = 1.454E-13;
        densityTablePer10km[61] = 1.26504851358249E-13;
        densityTablePer10km[62] = 1.10065181686194E-13;
        densityTablePer10km[63] = 9.57618944218064E-14;
        densityTablePer10km[64] = 8.33173605200477E-14;
        densityTablePer10km[65] = 7.24900296296442E-14;
        densityTablePer10km[66] = 6.30697415629518E-14;
        densityTablePer10km[67] = 5.48736470538128E-14;
        densityTablePer10km[68] = 4.7742658624674E-14;
        densityTablePer10km[69] = 4.15383626737414E-14;
        densityTablePer10km[70] = 3.614E-14;
        densityTablePer10km[71] = 3.22855174742338E-14;
        densityTablePer10km[72] = 2.88421316706989E-14;
        densityTablePer10km[73] = 2.5765997400346E-14;
        densityTablePer10km[74] = 2.30179457473695E-14;
        densityTablePer10km[75] = 2.05629853250599E-14;
        densityTablePer10km[76] = 1.836985672481E-14;
        densityTablePer10km[77] = 1.64106344850035E-14;
        densityTablePer10km[78] = 1.46603715115895E-14;
        densityTablePer10km[79] = 1.30967814226946E-14;
        densityTablePer10km[80] = 1.17E-14;
        densityTablePer10km[81] = 1.07979659273866E-14;
        densityTablePer10km[82] = 9.96547591188049E-15;
        densityTablePer10km[83] = 9.19716832022883E-15;
        densityTablePer10km[84] = 8.48809488463849E-15;
        densityTablePer10km[85] = 7.83368883356844E-15;
        densityTablePer10km[86] = 7.22973547954024E-15;
        densityTablePer10km[87] = 6.6723450745379E-15;
        densityTablePer10km[88] = 6.15792775817317E-15;
        densityTablePer10km[89] = 5.68317043727025E-15;
        densityTablePer10km[90] = 5.245E-15;
        densityTablePer10km[91] = 4.96315625900571E-15;
        densityTablePer10km[92] = 4.69645758842852E-15;
        densityTablePer10km[93] = 4.44409015732391E-15;
        densityTablePer10km[94] = 4.20528386652199E-15;
        densityTablePer10km[95] = 3.97930999867005E-15;
        densityTablePer10km[96] = 3.76547899455162E-15;
        densityTablePer10km[97] = 3.56313834889675E-15;
        densityTablePer10km[98] = 3.37167061926219E-15;
        densityTablePer10km[99] = 3.19049154190597E-15;
        densityTablePer10km[100] = 3.019E-15;

    }
#if UNITY_EDITOR
    // Code to to do a one-time generation of the density table used above
    // Cheezy, but generate the text and dump it in a log stmt so it can be cut and pasted above.
    // This at least keeps the generation code in the same file and accesible to anyone who wants to modify
    // or check it. 
    private class AtmosphereData
    {
        public double h0;
        public double rho0;
        public double H;

        public AtmosphereData(double h0, double rho0, double H) {
            this.h0 = h0;
            this.rho0 = rho0;
            this.H = H;
        }
    };
 
    private void DensityTable() {
        // Data from Table 8-4 in Vallado p567
        AtmosphereData[] atmoData = {
            new AtmosphereData(0, 1.225, 7.249 ),
            new AtmosphereData(25, 3.899E-2, 6.349 ),
            new AtmosphereData( 30, 1.774E-2, 6.682  ),
            new AtmosphereData( 40, 3.972E-3, 7.554  ),
            new AtmosphereData( 50, 1.057E-3, 8.382  ),
            new AtmosphereData( 60, 3.206E-4, 7.714   ),
            new AtmosphereData( 70, 8.770E-5, 6.549  ),
            new AtmosphereData( 80, 1.905E-5, 5.799   ),
            new AtmosphereData( 90,  3.396E-6, 5.382 ),
            new AtmosphereData( 100, 5.297E-7, 5.877  ),
            new AtmosphereData( 110, 9.661E-8, 7.263  ),
            new AtmosphereData( 120, 2.438E-8, 9.473  ),
            new AtmosphereData( 130, 8.484E-9, 12.636  ),
            new AtmosphereData( 140, 3.845E-9, 16.149  ),
            new AtmosphereData( 150, 2.070E-9, 22.523  ),
            new AtmosphereData( 180, 5.464E-10, 29.740  ),
            new AtmosphereData( 200, 2.789E-10, 37.105  ),
            new AtmosphereData( 250, 7.248E-11, 45.546  ),
            new AtmosphereData( 300, 2.418E-11, 53.628  ),
            new AtmosphereData( 350, 9.518E-12, 53.298  ),
            new AtmosphereData( 400, 3.725E-12, 58.515  ),
            new AtmosphereData( 450, 1.585E-12, 60.828  ),
            new AtmosphereData( 500, 6.967E-13, 63.822  ),
            new AtmosphereData( 600, 1.454E-13, 71.835  ),
            new AtmosphereData( 700, 3.614E-14, 88.667  ),
            new AtmosphereData( 800, 1.170E-14, 124.64  ),
            new AtmosphereData( 900, 5.245E-15, 181.05  ),
            new AtmosphereData( 1000, 3.019E-15, 268.0  ),
            new AtmosphereData( 10000, double.NaN, double.NaN  ) // terminating value
        };

        // Generate a string with C# code to assign a density for every 10km interval from 0km to 1000km
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i=0; i <= 100; i++) {
            double h = 10 * i;
            // find entry in data table
            int e = 0;
            while (atmoData[e].h0 <= h)
                e++;
            // want the entry before the one found
            e -= 1;
            double density = atmoData[e].rho0 * Mathd.Exp(-(h - atmoData[e].h0) / atmoData[e].H);
            sb.Append(string.Format("densityTablePer10km[{0}] = {1};\n", i, density));
        }

        Debug.Log(sb.ToString());
    }

#endif

}
