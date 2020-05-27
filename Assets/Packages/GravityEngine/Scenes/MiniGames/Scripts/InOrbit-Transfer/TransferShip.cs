using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 
/// TransferShip computes the transfer to a new orbit around the same central body. The script is attached to 
/// the NBody that is to perform the transfer. The transfer can be executed by the DoTransfer method. Alternatly the
/// computed transfer can be accessed and the maneuvers retrieved from there. 
/// 
/// The attached NBody object is assumed to have an OrbitPredictor child object. This is used to determine the
/// initial orbit for the transfer. 
/// 
/// The target orbit may be designated in several different ways, and this depends on the transfer type selected. 
/// 
///     targetNBody: an NBody that is in the desired final orbit. In the case of a rendezvous type maneuver
///                  the transfer will ensure a transfer that results in rendezvous. This may result in a
///                  delay before the transfer and an intermediate phasing orbit if the circumstances require it. 
///                  
///     targetPoint: the point the NBody should intercept when using the LAMBERT_POINT transfer
///     
///     targetPhase: specifies position in target orbit (only used when LAMBERT_ORBIT is selected)
///     
/// In the case of Hohmann transfers the transfer time is fixed by the orbit geometries. 
/// 
/// Lambert transfers require that the time of flight be specified by transferTimeFactor. 
/// In order to maintain scale independence this time is expressed as a multiplier of the minimum energy 
/// Lambert transfer (1.0 will result in the minimum energy transfer). 
/// 
/// TODO: Direction of Lambert transfer
/// 
/// Transfer Types:
/// 
/// LAMBERT_POINT: Transfer to the point specified by targetPoint. 
/// </summary>
public class TransferShip : MonoBehaviour
{

    public enum Transfer {  HOHMANN, HOHMANN_RDVS, LAMBERT_POINT, LAMBERT_ORBIT, LAMBERT_INTERCEPT, LAMBERT_RDVS};

    [SerializeField]
    private Transfer transferType = Transfer.HOHMANN;

    [SerializeField]
    [Tooltip("Used when HOHMANN or LAMBERT_ORBIT selected to indicate target orbit. (Lambert also needs targetPhase)")]
    private OrbitUniversal targetOrbit = null;

    [SerializeField]
    [Tooltip("Phase in target orbit for the Lambert transfer")]
    private float targetPhase = 0.0f; 

    [SerializeField]
    [Tooltip("Target for rendezvous when using HOHMANN_RDVS, LAMBERT_TARGET or LAMBERT_RDVS modes")]
    private NBody targetNbody = null; 

    [SerializeField]
    [Tooltip("Used when LAMBERT_POINT is selected to designate destination point")]
    private Vector3 targetPoint = Vector3.zero;

    private Vector3d targetPoint3d = Vector3d.zero;

    [SerializeField]
    [Tooltip("Used when LAMBERT is selected to designate time factor for transfer wrt time for min. energy xfer")]
    private float transferTimeFactor = 1.0f;

    private NBody shipNbody = null;
    private OrbitUniversal shipOrbit = null; 

    private GravityEngine ge = null;

    private OrbitTransfer orbitTransfer;

    // Not Awake(): OrbitPredictor will not be setup until Awake phase has completed
    void Start() {
        Init();
    }

    public void Init() {
       
        shipNbody = GetComponent<NBody>();
        if (shipNbody == null) {
            Debug.LogError("Did not find attached NBody");
        }

        OrbitPredictor orbitPredictor = GetComponentInChildren<OrbitPredictor>();
        if (orbitPredictor == null) {
            Debug.LogError("Script requires an attached OrbitPredictor component");
        }
        shipOrbit = orbitPredictor.GetOrbitUniversal();
        ge = GravityEngine.Instance();

        targetPoint3d = new Vector3d(targetPoint);
    }

    public void SetTargetPoint(Vector3d point) {
        targetPoint3d = point;
    }

    public void SetTargetPhase(float phase) {
        targetPhase = phase;
    }

    public void SetTransferTimeFactor(float time) {
         transferTimeFactor = time;
    }

    public void SetTransferType(Transfer transferType) {
        this.transferType = transferType;
    }

    private OrbitData GetTargetOrbitData() {
        OrbitData orbitData = null; 
        if (targetNbody != null) {
            orbitData = new OrbitData();
            orbitData.SetOrbit(targetNbody, shipOrbit.centerNbody);
        } else if (targetOrbit != null) {
            orbitData = new OrbitData(targetOrbit);
        } else {
            Debug.LogError("No target orbit specified");
        }
        return orbitData;
    }

    public OrbitUniversal GetTargetOrbit() {
        return targetOrbit;
    }

    /// <summary>
    /// A Hohmann tranfer requires a circular source and destination orbit. 
    /// (Eventually could support co-axial ellipses)
    /// 
    /// </summary>
    /// <param name="rendezvous"></param>
    private void ComputeHohmann(bool rendezvous) {
        // check a Hohmann is possible (both are circular)
        if( !shipOrbit.IsCircular() || !targetOrbit.IsCircular()) {
            Debug.LogWarning("Hohmann xfer requires start and end orbits be circular.");
            return;
        }
        OrbitData shipOrbitData = new OrbitData(shipOrbit);
        OrbitData targetOrbitData = GetTargetOrbitData();
        orbitTransfer = new HohmannGeneral(shipOrbitData, targetOrbitData, rendezvous);
    }

    private void ComputeLambert() {
        OrbitData shipOrbitData = new OrbitData(shipOrbit);
        shipOrbitData.SetOrbitForVelocity(shipNbody, shipOrbit.centerNbody);
        bool shortPath = true;
        LambertUniversal lambertU = null;
        switch(transferType) {
            case Transfer.LAMBERT_POINT:
                Vector3d r_from = GravityEngine.Instance().GetPositionDoubleV3(shipNbody);
                Vector3d r_to = targetPoint3d;
                // compute the min energy path (this will be in the short path direction)
                lambertU = new LambertUniversal(shipOrbitData, r_from, r_to, shortPath);
                break;

            case Transfer.LAMBERT_ORBIT:
                // maneuvers will include change to target orbit at the target point
                OrbitData targetOrbitData = GetTargetOrbitData();
                // sneaky: need to remove NBody from OrbitData so it uses phase and not the NBody
                targetOrbitData.nbody = null;
                targetOrbitData.phase = targetPhase;
                lambertU = new LambertUniversal(shipOrbitData, targetOrbitData, shortPath);
                break;

            case Transfer.LAMBERT_RDVS:
            case Transfer.LAMBERT_INTERCEPT:
                // maneuvers will include change to target orbit at the target point
                OrbitData targetOrbitData2 = GetTargetOrbitData();
                lambertU = new LambertUniversal(shipOrbitData, targetOrbitData2, shortPath);
                break;
        }

        orbitTransfer = lambertU;

        // apply any time of flight change
        double t_flight = transferTimeFactor * lambertU.GetTMin();
        bool reverse = !shortPath;

        ComputeLambertForTflight(lambertU, t_flight, reverse);
        // only accept a transfer if in same direction ship is currently orbiting
        Vector3 shipVel = ge.GetVelocity(shipNbody);
        // Lambert maneuver is additive velocity
        Vector3 newVel = shipVel + lambertU.GetManeuvers()[0].velChange;
        if (Vector3.Dot(shipVel, newVel) < 0) {
            ComputeLambertForTflight(lambertU, t_flight, !reverse);
        }
    }

    private void ComputeLambertForTflight(LambertUniversal lambertU, double t_flight, bool reverse) {
        const bool df = false;
        const int nrev = 0;
        int error = 0;
        if ((transferType == Transfer.LAMBERT_INTERCEPT) || (transferType == Transfer.LAMBERT_RDVS)) {
            bool rdvs = (transferType == Transfer.LAMBERT_RDVS);
            error = lambertU.ComputeXferWithPhasing(reverse, df, nrev, t_flight, rdvs);
        } else {
            error = lambertU.ComputeXfer(reverse, df, nrev, t_flight);
        }
        if (error != 0) {
            Debug.LogWarning("Lambert failed to find solution. time=" + t_flight );
            return;
        }
    }


    public void ShipMoved() {
        ComputeTransfer();
    }

    public OrbitTransfer GetTransfer() {
        return orbitTransfer;
    }

    public Transfer GetTransferType() {
        return transferType;
    }


    public void DoTransfer(Maneuver.OnExecuted maneuverCallback) {
        if (orbitTransfer == null) {
            ComputeTransfer();
        }
        List<Maneuver> maneuvers = orbitTransfer.GetManeuvers();
        maneuvers[maneuvers.Count - 1].onExecuted = maneuverCallback;
        ge.AddManeuvers(orbitTransfer.GetManeuvers());
    }

    private void ComputeTransfer() {
        switch(transferType) {
            case Transfer.HOHMANN:
                ComputeHohmann(false);
                break;

            case Transfer.HOHMANN_RDVS:
                ComputeHohmann(true);
                break;

            case Transfer.LAMBERT_POINT:
            case Transfer.LAMBERT_ORBIT:
            case Transfer.LAMBERT_INTERCEPT:
            case Transfer.LAMBERT_RDVS:
                ComputeLambert();
                break;

            default:
                Debug.LogError("Unsupported type: " + transferType);
                break;
       
        }
    }

    

    // Update is called once per frame
    void Update()
    {
        
    }
}
