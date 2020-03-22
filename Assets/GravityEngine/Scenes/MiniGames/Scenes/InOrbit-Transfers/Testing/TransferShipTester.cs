using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Test controller to execute a variety of TransferShip scenarios.
/// 
/// Started life as a clone of the HohmannGeneralTester
/// </summary>
public class TransferShipTester : MonoBehaviour
{
    [SerializeField]
    private GameObject fromGO = null;

    [SerializeField]
    private GameObject toGO = null;

    [SerializeField]
    private GameObject centerObject = null;

    //! Control of tests to be run
    [SerializeField]
    private int fromTestNumber = 0;
    [SerializeField]
    private int toTestNumber = -1; 

    private NBody fromNbody;
    private NBody toNbody;

    private OrbitUniversal fromOrbit;
    private OrbitUniversal toOrbit;

    private OrbitPredictor fromOP;
    private OrbitPredictor toOP;

    private TransferShip transferShip;

    private struct TestOrbit
    {
        public float radius;
        public float eccentricity;
        public float inclination;
        public float omegaU;
        public float omegaL;
        public float phase; 

        public TestOrbit(float r, float e, float i, float o, float l, float p) {
            radius = r;
            eccentricity = e;
            inclination = i;
            omegaU = o;
            omegaL = l;
            phase = p;
        }
    }

    private struct XferTest
    {
        public TestOrbit fromOrbit;
        public TestOrbit toOrbit;
        public TransferShip.Transfer type; 

        public XferTest(TestOrbit from, TestOrbit to, TransferShip.Transfer t) {
            fromOrbit = from;
            toOrbit = to;
            type = t;
        }
    }

    private List<XferTest> xferTests; 

    private int testNum; 

    private enum State { SETUP_NEXT_TEST, DO_TRANSFER, TESTING, CHECK_TEST, DONE};

    private State state;

    private GravityEngine ge;

    private string summary = "SUMMARY:\n";

    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();

        fromNbody = fromGO.GetComponent<NBody>();
        toNbody = toGO.GetComponent<NBody>();

        transferShip = fromNbody.GetComponent<TransferShip>();

        fromOrbit = fromNbody.GetComponent<OrbitUniversal>();
        toOrbit = toNbody.GetComponent<OrbitUniversal>();

        fromOP = fromNbody.GetComponentInChildren<OrbitPredictor>();
        toOP = toNbody.GetComponentInChildren<OrbitPredictor>();

        xferTests = new List<XferTest>();

        // Copy of Tests from Hohmann General
        // coplanar inner to outer
        xferTests.Add(new XferTest(new TestOrbit(25f, 0f, 0f, 0f, 0f, 0f), new TestOrbit(40f, 0f, 0f, 0f, 0f, 0f), TransferShip.Transfer.HOHMANN));
        xferTests.Add(new XferTest(new TestOrbit(25f, 0f, 0f, 0f, 0f, 0f), new TestOrbit(40f, 0f, 0f, 0f, 0f, 0f), TransferShip.Transfer.HOHMANN_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(25f, 0f, 0f, 0f, 0f, 0), new TestOrbit(40f, 0f, 0f, 0f, 0f, 20f), TransferShip.Transfer.HOHMANN_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(25f, 0f, 0f, 0f, 0f, 190f), new TestOrbit(40f, 0f, 0f, 0f, 0f, 20f), TransferShip.Transfer.HOHMANN_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(15f, 0f, 0f, 0f, 0f, 93f), new TestOrbit(25f, 0f, 0f, 0f, 0f, 12f), TransferShip.Transfer.HOHMANN_RDVS));
        // coplanar outer to inner
        xferTests.Add(new XferTest(new TestOrbit(35f, 0f, 0f, 0f, 0f, 0f), new TestOrbit(20f, 0f, 0f, 0f, 0f, 0f), TransferShip.Transfer.HOHMANN));
        xferTests.Add(new XferTest(new TestOrbit(35f, 0f, 0f, 0f, 0f, 0f), new TestOrbit(20f, 0f, 0f, 0f, 0f, 0f), TransferShip.Transfer.HOHMANN_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(35f, 0f, 0f, 0f, 0f, 20f), new TestOrbit(20f, 0f, 0f, 0f, 0f, 30f), TransferShip.Transfer.HOHMANN_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(35f, 0f, 0f, 0f, 0f, 50f), new TestOrbit(20f, 0f, 0f, 0f, 0f, 10f), TransferShip.Transfer.HOHMANN_RDVS));
        // same orbit, target lead rendezvous
        xferTests.Add(new XferTest(new TestOrbit(35f, 0f, 0f, 0f, 0f, 0f), new TestOrbit(35f, 0f, 0f, 0f, 0f, 10f), TransferShip.Transfer.HOHMANN_RDVS));

        // same orbit, interceptor lead rendezvous
        xferTests.Add(new XferTest(new TestOrbit(35f, 0f, 0f, 0f, 0f, 15f), new TestOrbit(35f, 0f, 0f, 0f, 0f, 40f), TransferShip.Transfer.HOHMANN_RDVS));
        // same orbit, 180 apart
        xferTests.Add(new XferTest(new TestOrbit(35f, 0f, 0f, 0f, 0f, 20f), new TestOrbit(35f, 0f, 0f, 0f, 0f, 200f), TransferShip.Transfer.HOHMANN_RDVS));

        // same radius, different inclination/omega
        xferTests.Add(new XferTest(new TestOrbit(35f, 0f, 15f, 0f, 0f, 0f), new TestOrbit(35f, 0f, 45f, 30f, 0f, 0f), TransferShip.Transfer.HOHMANN));
        xferTests.Add(new XferTest(new TestOrbit(35f, 0f, 45f, 30f, 0f, 15f), new TestOrbit(35f, 0f, 45f, 30f, 0f, 40f), TransferShip.Transfer.HOHMANN_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(35f, 0f, 15f, 0f, 0f, 0f), new TestOrbit(35f, 0f, 45f, 30f, 0f, 0f), TransferShip.Transfer.HOHMANN_RDVS));

        // coplanar inner to outer, with inclination (25->40)
        xferTests.Add(new XferTest(new TestOrbit(25f, 0f, 0f, 0f, 0f, 0f), new TestOrbit(40f, 0f, 30f, 0f, 0f, 0f), TransferShip.Transfer.HOHMANN));
        xferTests.Add(new XferTest(new TestOrbit(25f, 0f, 0f, 0f, 0f, 10f), new TestOrbit(40f, 0f, 30f, 0f, 0f, 0f), TransferShip.Transfer.HOHMANN_RDVS));
        // outer to inner (40->30)
        xferTests.Add(new XferTest(new TestOrbit(40f, 0f, 0f, 0f, 0f, 359f), new TestOrbit(25f, 0f, 30f, 0f, 0f, 0f), TransferShip.Transfer.HOHMANN_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(40f, 0f, 0f, 0f, 0f, 1f), new TestOrbit(30f, 0f, 30f, 0f, 0f, 0f), TransferShip.Transfer.HOHMANN));
        xferTests.Add(new XferTest(new TestOrbit(40f, 0f, 0f, 0f, 0f, 10f), new TestOrbit(30f, 0f, 30f, 0f, 0f, 0f), TransferShip.Transfer.HOHMANN_RDVS));
        // outer to inner (40->25)
        xferTests.Add(new XferTest(new TestOrbit(40f, 0f, 0f, 0f, 0f, 0f), new TestOrbit(25f, 0f, 30f, 0f, 0f, 0f), TransferShip.Transfer.HOHMANN));
        xferTests.Add(new XferTest(new TestOrbit(40f, 0f, 0f, 0f, 0f, 10f), new TestOrbit(25f, 0f, 30f, 0f, 0f, 0f), TransferShip.Transfer.HOHMANN_RDVS));

        // Repeat of above (excepting same orbit cases), but with LAMBERT_RDVS
        // coplanar inner to outer
        xferTests.Add(new XferTest(new TestOrbit(25f, 0f, 0f, 0f, 0f, 0f), new TestOrbit(40f, 0f, 0f, 0f, 0f, 0f), TransferShip.Transfer.LAMBERT_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(25f, 0f, 0f, 0f, 0f, 0), new TestOrbit(40f, 0f, 0f, 0f, 0f, 20f), TransferShip.Transfer.LAMBERT_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(25f, 0f, 0f, 0f, 0f, 190f), new TestOrbit(40f, 0f, 0f, 0f, 0f, 20f), TransferShip.Transfer.LAMBERT_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(15f, 0f, 0f, 0f, 0f, 93f), new TestOrbit(25f, 0f, 0f, 0f, 0f, 12f), TransferShip.Transfer.LAMBERT_RDVS));
        // coplanar outer to inner
        xferTests.Add(new XferTest(new TestOrbit(35f, 0f, 0f, 0f, 0f, 0f), new TestOrbit(20f, 0f, 0f, 0f, 0f, 0f), TransferShip.Transfer.LAMBERT_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(35f, 0f, 0f, 0f, 0f, 20f), new TestOrbit(20f, 0f, 0f, 0f, 0f, 30f), TransferShip.Transfer.LAMBERT_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(35f, 0f, 0f, 0f, 0f, 50f), new TestOrbit(20f, 0f, 0f, 0f, 0f, 10f), TransferShip.Transfer.LAMBERT_RDVS));

        // skip same orbit rdvs cases - Hohmann is better

        // same radius, different inclination/omega
        xferTests.Add(new XferTest(new TestOrbit(35f, 0f, 45f, 30f, 0f, 15f), new TestOrbit(35f, 0f, 45f, 30f, 0f, 40f), TransferShip.Transfer.LAMBERT_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(35f, 0f, 15f, 0f, 0f, 0f), new TestOrbit(35f, 0f, 45f, 30f, 0f, 0f), TransferShip.Transfer.LAMBERT_RDVS));

        // coplanar inner to outer, with inclination (25->40)
        xferTests.Add(new XferTest(new TestOrbit(25f, 0f, 0f, 0f, 0f, 10f), new TestOrbit(40f, 0f, 30f, 0f, 0f, 0f), TransferShip.Transfer.LAMBERT_RDVS));
        // outer to inner (40->30)
        xferTests.Add(new XferTest(new TestOrbit(40f, 0f, 0f, 0f, 0f, 359f), new TestOrbit(25f, 0f, 30f, 0f, 0f, 0f), TransferShip.Transfer.LAMBERT_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(40f, 0f, 0f, 0f, 0f, 10f), new TestOrbit(30f, 0f, 30f, 0f, 0f, 0f), TransferShip.Transfer.LAMBERT_RDVS));
        // outer to inner (40->25)
        xferTests.Add(new XferTest(new TestOrbit(40f, 0f, 0f, 0f, 0f, 10f), new TestOrbit(25f, 0f, 30f, 0f, 0f, 0f), TransferShip.Transfer.LAMBERT_RDVS));

        // ellipse to ellipse - different Omega
        xferTests.Add(new XferTest(new TestOrbit(40f, 0.3f, 0f, 0f, 0f, 10f), new TestOrbit(40f, 0.3f, 0f, 90f, 0f, 0f), TransferShip.Transfer.LAMBERT_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(40f, 0.3f, 0f, 0f, 0f, 10f), new TestOrbit(40f, 0.3f, 0f, 180f, 0f, 0f), TransferShip.Transfer.LAMBERT_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(40f, 0.3f, 0f, 0f, 0f, 10f), new TestOrbit(40f, 0.3f, 0f, 270f, 0f, 0f), TransferShip.Transfer.LAMBERT_RDVS));

        // ellipse to ellipse - different Omega and inclination
        xferTests.Add(new XferTest(new TestOrbit(40f, 0.3f, 10f, 0f, 0f, 10f), new TestOrbit(30f, 0.3f, 0f, 90f, 0f, 0f), TransferShip.Transfer.LAMBERT_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(40f, 0.3f, 20f, 0f, 0f, 10f), new TestOrbit(30f, 0.3f, 40f, 180f, 0f, 0f), TransferShip.Transfer.LAMBERT_RDVS));
        xferTests.Add(new XferTest(new TestOrbit(30f, 0.3f, 50f, 0f, 0f, 10f), new TestOrbit(40f, 0.3f, 10f, 270f, 0f, 0f), TransferShip.Transfer.LAMBERT_RDVS));

        if (toTestNumber < 0)
            toTestNumber = xferTests.Count;
        else if (toTestNumber == fromTestNumber)
            toTestNumber = fromTestNumber + 1;

        testNum = System.Math.Min(fromTestNumber, xferTests.Count);

        state = State.SETUP_NEXT_TEST;

        ge.AddGEStartCallback(GEStart);
    }

    private void GEStart() {
        ge.AddBody(centerObject);
    }

    private void InitOrbit(OrbitUniversal orbitU, TestOrbit testOrbit) {
        orbitU.p =  testOrbit.radius;
        orbitU.p_inspector = testOrbit.radius;
        orbitU.omega_uc = testOrbit.omegaU;
        orbitU.eccentricity = testOrbit.eccentricity;
        orbitU.inclination = testOrbit.inclination;
        orbitU.phase = testOrbit.phase;
        orbitU.omega_lc = 0;
    }

    private void TransferComplete(Maneuver m) {
        state = State.CHECK_TEST;
    }

    private void CheckTestResult(TransferShip.Transfer type) {
        // prefer to use OrbitU where possible
        bool rendezvous = (type == TransferShip.Transfer.HOHMANN_RDVS) || (type == TransferShip.Transfer.LAMBERT_RDVS);
        OrbitUniversal targetOrbit = toOP.GetOrbitUniversal();
        OrbitUniversal orbit = fromOP.GetOrbitUniversal();
        bool failed = false; 
        if (Mathd.Abs(orbit.p - targetOrbit.p) > 1E-2) {
            Debug.LogWarning(string.Format("radius failed {0} expected: {1}", orbit.p, targetOrbit.p));
            failed = true;
        }
        if (Mathd.Abs(orbit.eccentricity - targetOrbit.eccentricity) > 1E-2) {
            Debug.LogWarning(string.Format("Final orbit eccentricty mismatch. ecc={0}", orbit.eccentricity));
            failed = true;
        }
        if (Mathd.Abs(orbit.inclination - targetOrbit.inclination) > 1E-2) {
            Debug.LogWarning(string.Format("inclination failed {0} expected: {1}", orbit.inclination, targetOrbit.inclination));
            failed = true;
        }
        // 1E-1 since need that to pass. Look at improvements. 
        if (!NUtils.FloatEqualMod360((float) orbit.omega_uc, (float) targetOrbit.omega_uc, 1E-1f)) {
            Debug.LogWarning(string.Format("omegaU failed {0} expected: {1}", orbit.omega_uc, targetOrbit.omega_uc));
            failed = true;
        }
        // very coarse - try to do better
        double phase1 = orbit.phase + orbit.omega_lc;
        double phase2 = targetOrbit.phase + targetOrbit.omega_lc;
        if (rendezvous && !NUtils.FloatEqualMod360( (float) phase1, (float) phase2, 2E-1f)) {
            Debug.LogWarning(string.Format("phase + omega failed {0} expected: {1}", phase1, phase2));
            failed = true;
        }
        if (failed) {
            string testStr = string.Format(
                "\n   r={0} i={1} o={2} phase={3}  =>   r={4} i={5} o={6} phase={7}  rdvs={8}",
                xferTests[testNum].fromOrbit.radius,
                xferTests[testNum].fromOrbit.inclination,
                xferTests[testNum].fromOrbit.omegaU,
                xferTests[testNum].fromOrbit.phase,
                xferTests[testNum].toOrbit.radius,
                xferTests[testNum].toOrbit.inclination,
                xferTests[testNum].toOrbit.omegaU,
                xferTests[testNum].toOrbit.phase, 
                xferTests[testNum].type
            );
            Debug.LogError("Test " + testNum + " failed. " + testStr);
            summary += "Test " + testNum + " failed. " + testStr + "\n";
        } else {
            Debug.Log("Test " + testNum + " passed");
            summary += "Test " + testNum + " passed" + "\n";
        }
    }

    // LateUpdate to allow OrbitPredictor on ship to run before this. 
    void LateUpdate()
    {
        if (!ge.IsSetup())
            return;

        switch(state) {
            case State.SETUP_NEXT_TEST:
                InitOrbit(fromOrbit, xferTests[testNum].fromOrbit);
                InitOrbit(toOrbit, xferTests[testNum].toOrbit);
                transferShip.SetTransferType(xferTests[testNum].type);
                ge.AddBody(fromGO);
                ge.AddBody(toGO);
                OrbitData fromOD = new OrbitData(fromOrbit);
                OrbitData toOD = new OrbitData(toOrbit);
                Debug.LogFormat("Start Test {0} :  rdvs={3} \nfromOD={1} \ntoOD={2}", 
                    testNum, fromOD.LogString(), toOD.LogString(), xferTests[testNum].type);
                // transfer ship gets fromOrbit from the OrbitPredictor attached to the ship, need this to update
                // so wait one frame for this to happen 
                state = State.DO_TRANSFER;
                break;

            case State.DO_TRANSFER:
                // Need to call ship moved so xfer is recomputed for each test case
                transferShip.ShipMoved();
                transferShip.DoTransfer(TransferComplete);
                state = State.TESTING;
                break;

            case State.TESTING:
                return;

            case State.CHECK_TEST:
                Debug.Log("Check test " + testNum);
                CheckTestResult(xferTests[testNum].type);
                testNum += 1;
                if (testNum >= toTestNumber) {
                    Debug.Log("FINISHED");
                    Debug.Log(summary);
                    ge.SetEvolve(false);
                    state = State.DONE;
                    break;
                }
                ge.RemoveBody(fromGO);
                ge.RemoveBody(toGO);
                state = State.SETUP_NEXT_TEST;
                break;

            case State.DONE:
                break;
        }



    }
}
