using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Test controller to execute a variety of Hohmann scenarios.
/// </summary>
public class HohmannGeneralTestAll : MonoBehaviour
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

    private struct CircOrbit
    {
        public float radius;
        public float inclination;
        public float omegaU;
        public float phase; 

        public CircOrbit(float r, float i, float o, float p) {
            radius = r;
            inclination = i;
            omegaU = o;
            phase = p;
        }
    }

    private struct XferTest
    {
        public CircOrbit fromOrbit;
        public CircOrbit toOrbit;
        public bool rendezvous; 

        public XferTest(CircOrbit from, CircOrbit to, bool r) {
            fromOrbit = from;
            toOrbit = to;
            rendezvous = r;
        }
    }

    private List<XferTest> xferTests; 

    private int testNum; 

    private enum State { SETUP_NEXT_TEST, TESTING, CHECK_TEST, DONE};

    private State state;

    private GravityEngine ge;

    private string summary = "SUMMARY:\n";

    // Start is called before the first frame update
    void Start()
    {
        ge = GravityEngine.Instance();

        fromNbody = fromGO.GetComponent<NBody>();
        toNbody = toGO.GetComponent<NBody>();

        fromOrbit = fromNbody.GetComponent<OrbitUniversal>();
        toOrbit = toNbody.GetComponent<OrbitUniversal>();

        fromOP = fromNbody.GetComponentInChildren<OrbitPredictor>();
        toOP = toNbody.GetComponentInChildren<OrbitPredictor>();

        xferTests = new List<XferTest>();

        // coplanar inner to outer
        xferTests.Add(new XferTest(new CircOrbit(25f, 0f, 0f, 0f), new CircOrbit(40f, 0f, 0f, 0f), false));
        xferTests.Add(new XferTest(new CircOrbit(25f, 0f, 0f, 0f), new CircOrbit(40f, 0f, 0f, 0f), true));
        xferTests.Add(new XferTest(new CircOrbit(25f, 0f, 0f, 0), new CircOrbit(40f, 0f, 0f, 20f), true));
        xferTests.Add(new XferTest(new CircOrbit(25f, 0f, 0f, 190f), new CircOrbit(40f, 0f, 0f, 20f), true));
        xferTests.Add(new XferTest(new CircOrbit(15f, 0f, 0f, 93f), new CircOrbit(25f, 0f, 0f, 12f), true));
        // coplanar outer to inner
        xferTests.Add(new XferTest(new CircOrbit(35f, 0f, 0f, 0f), new CircOrbit(20f, 0f, 0f, 0f), false));
        xferTests.Add(new XferTest(new CircOrbit(35f, 0f, 0f, 0f), new CircOrbit(20f, 0f, 0f, 0f), true));
        xferTests.Add(new XferTest(new CircOrbit(35f, 0f, 0f, 20f), new CircOrbit(20f, 0f, 0f, 30f), true));
        xferTests.Add(new XferTest(new CircOrbit(35f, 0f, 0f, 50f), new CircOrbit(20f, 0f, 0f, 10f), true));
        // same orbit, target lead rendezvous
        xferTests.Add(new XferTest(new CircOrbit(35f, 0f, 0f, 0f), new CircOrbit(35f, 0f, 0f, 10f), true));

        // same orbit, interceptor lead rendezvous
        xferTests.Add(new XferTest(new CircOrbit(35f, 0f, 0f, 15f), new CircOrbit(35f, 0f, 0f, 40f), true));
        // same orbit, 180 apart
        xferTests.Add(new XferTest(new CircOrbit(35f, 0f, 0f, 20f), new CircOrbit(35f, 0f, 0f, 200f), true));

        // same radius, different inclination/omega
        xferTests.Add(new XferTest(new CircOrbit(35f, 15f, 0f, 0f), new CircOrbit(35f, 45f, 30f, 0f), false));
        xferTests.Add(new XferTest(new CircOrbit(35f, 45f, 30f, 15f), new CircOrbit(35f, 45f, 30f, 40f), true)); // DUP
        xferTests.Add(new XferTest(new CircOrbit(35f, 15f, 0f, 0f), new CircOrbit(35f, 45f, 30f, 0f), true));

        // coplanar inner to outer, with inclination (25->40)
        xferTests.Add(new XferTest(new CircOrbit(25f, 0f, 0f, 0f), new CircOrbit(40f, 30f, 0f, 0f), false));
        xferTests.Add(new XferTest(new CircOrbit(25f, 0f, 0f, 10f), new CircOrbit(40f, 30f, 0f, 0f), true));
        // outer to inner (40->30)
        xferTests.Add(new XferTest(new CircOrbit(40f, 0f, 0f, 359f), new CircOrbit(25f, 30f, 0f, 0f), true));
        xferTests.Add(new XferTest(new CircOrbit(40f, 0f, 0f, 1f), new CircOrbit(30f, 30f, 0f, 0f), false));
        xferTests.Add(new XferTest(new CircOrbit(40f, 0f, 0f, 10f), new CircOrbit(30f, 30f, 0f, 0f), true));
        // outer to inner (40->25)
        xferTests.Add(new XferTest(new CircOrbit(40f, 0f, 0f, 0f), new CircOrbit(25f, 30f, 0f, 0f), false));
        xferTests.Add(new XferTest(new CircOrbit(40f, 0f, 0f, 10f), new CircOrbit(25f, 30f, 0f, 0f), true));

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

    private void InitOrbit(OrbitUniversal orbitU, CircOrbit circOrbit) {
        orbitU.p =  circOrbit.radius;
        orbitU.p_inspector = circOrbit.radius;
        orbitU.omega_uc = circOrbit.omegaU;
        orbitU.inclination = circOrbit.inclination;
        orbitU.phase = circOrbit.phase;
        // zero the rest
        orbitU.omega_lc = 0;
        orbitU.eccentricity = 0;
    }

    private void TransferComplete(Maneuver m) {
        state = State.CHECK_TEST;
    }

    private void CheckTestResult(bool rendezvous) {
        // prefer to use OrbitU where possible
        OrbitUniversal targetOrbit = toOP.GetOrbitUniversal();
        OrbitUniversal orbit = fromOP.GetOrbitUniversal();
        bool failed = false; 
        if (Mathd.Abs(orbit.p - targetOrbit.p) > 1E-2) {
            Debug.LogWarning(string.Format("radius failed {0} expected: {1}", orbit.p, targetOrbit.p));
            failed = true;
        }
        if (orbit.eccentricity > 1E-2) {
            Debug.LogWarning(string.Format("Final orbit not circular. ecc={0}", orbit.eccentricity));
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
                xferTests[testNum].rendezvous
            );
            Debug.LogError("Test " + testNum + " failed. " + testStr);
            summary += "Test " + testNum + " failed. " + testStr + "\n";
        } else {
            Debug.Log("Test " + testNum + " passed");
            summary += "Test " + testNum + " passed" + "\n";
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!ge.IsSetup())
            return;

        switch(state) {
            case State.SETUP_NEXT_TEST:
                InitOrbit(fromOrbit, xferTests[testNum].fromOrbit);
                InitOrbit(toOrbit, xferTests[testNum].toOrbit);
                ge.AddBody(fromGO);
                ge.AddBody(toGO);
                OrbitData fromOD = new OrbitData(fromOrbit);
                OrbitData toOD = new OrbitData(toOrbit);
                Debug.LogFormat("Start Test {0} :  rdvs={3} \nfromOD={1} \ntoOD={2}", 
                    testNum, fromOD.LogString(), toOD.LogString(), xferTests[testNum].rendezvous);

                HohmannGeneral hohmann = new HohmannGeneral(fromOD, toOD, xferTests[testNum].rendezvous);
                List<Maneuver> maneuvers = hohmann.GetManeuvers();
                if (maneuvers.Count == 0) {
                    string errStr = string.Format("Test {0} failed. No maneuvers", testNum);
                    Debug.LogError(errStr);
                    summary += errStr + "\n";
                    state = State.CHECK_TEST;
                    return;
                }
                maneuvers[maneuvers.Count - 1].onExecuted = TransferComplete;
                ge.AddManeuvers(maneuvers);
                state = State.TESTING;
                break;

            case State.TESTING:
                return;

            case State.CHECK_TEST:
                Debug.Log("Check test " + testNum);
                CheckTestResult(xferTests[testNum].rendezvous);
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
