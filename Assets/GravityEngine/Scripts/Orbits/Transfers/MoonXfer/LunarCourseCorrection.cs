using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System;

public class LunarCourseCorrection
{

    private NBody spaceship;
    private NBody moon;

    public double targetDistance;
    public double targetAccuracy;

    public CorrectionData correctionFinal; 

    // Timeslice to checkfor closest approach
    private const double searchDt_coarse = 1f;
    private const double searchDt_fine = 0.01f;

    private double searchDt = searchDt_coarse;

    public LunarCourseCorrection(NBody spaceship, NBody moon) {
        this.spaceship = spaceship;
        this.moon = moon;
        threadCountMutex = new Mutex();
    }

    /// <summary>
    /// Perform Lunar closest approach and course correction calculations. 
    /// 
    /// ClosestApproach calculations can be done on the main thread or asynch with callback. 
    /// 
    /// Course correction calculations can only be done async with a callback. 
    /// 
    /// Note that the threading APIs assume that a specific Async call will complete before another one
    /// is started. If multiple parallel computations are required, instantiate additional copies of this
    /// class. 
    /// 
    /// </summary>
    /// <param name="targetDistance"></param>
    /// <returns></returns>
    public double ClosestApproach(CorrectionData calcData) {

        long start_ms = System.DateTimeOffset.Now.Millisecond;

        Vector3 shipPos = calcData.gravityState.GetPhysicsPosition(spaceship);
        Vector3 moonPos = calcData.gravityState.GetPhysicsPosition(moon);
        float lastDistance = Vector3.Distance(shipPos, moonPos);
        float distance = 0f;

        double end_phys_time = calcData.gravityState.time + calcData.maxPhysTime;

        searchDt = searchDt_coarse;

        // correction is applied to the ship velocity purely in the direction the ship is travelling
        // VERY simplistic approach for first attempt
        double[] shipVel = new double[] { 0, 0, 0 };
        calcData.gravityState.GetVelocityDouble(spaceship, ref shipVel);
        shipVel[0] *= (1 + calcData.correction);
        shipVel[1] *= (1 + calcData.correction);
        shipVel[2] *= (1 + calcData.correction);
        calcData.gravityState.SetVelocityDouble(spaceship, ref shipVel);

        // @TODO: dumb implementation: needs refinement
        // HACK: add a time limit on simulation run time
        const float TIME_LIMIT_SEC = 20f;
        float timeEnd_ms = start_ms + TIME_LIMIT_SEC * 1000;

        while ((System.DateTimeOffset.Now.Millisecond < timeEnd_ms) &&
              (calcData.gravityState.time < end_phys_time)) {
            calcData.gravityState.Evolve(GravityEngine.Instance(), searchDt);
            // check if we have passed through min distance
            shipPos = calcData.gravityState.GetPhysicsPosition(spaceship);
            moonPos = calcData.gravityState.GetPhysicsPosition(moon);
            distance = Vector3.Distance(shipPos, moonPos);
            float delta = distance - lastDistance;
            // Need to be within approach distance to care (screens out cases where ship is in orbit around
            // planet and sign change in delta is triggered)
            if (distance < calcData.approachDistance) {
                searchDt = searchDt_fine;
                if (delta > 0f) {
                    break;
                }
            }
            lastDistance = distance;
        }
        calcData.distance = distance;
        calcData.timeAtApproach = calcData.gravityState.time;
        calcData.execTimeMs = System.DateTimeOffset.Now.Millisecond - start_ms;
        Debug.Log(string.Format("Closest approach d={0} @ t={1} maxTime={2}", 
            calcData.distance, 
            calcData.timeAtApproach, 
            calcData.maxPhysTime));
        if (calcData.gravityState.time > end_phys_time) {
            calcData.distance = -1;
            Debug.LogWarning("Physics evolution time exceeded");
        } else if (System.DateTimeOffset.Now.Millisecond > timeEnd_ms) {
            calcData.distance = -2;
            Debug.LogWarning("Run-time limit exceeded");
        }
        return calcData.distance;
    }

    public void ClosestApproachAsync(CorrectionData correctionData, CalcCallback calcCallback) {
        correctionData.gravityState.isAsync = true;
        this.calcCallback = calcCallback;
        calcState = CalcState.CLOSEST_APPROACH;
        // setup Mutex
        threadCountMutex.WaitOne();
        threadsPending++;
        threadCountMutex.ReleaseMutex();
        System.Threading.ThreadPool.QueueUserWorkItem(
            new System.Threading.WaitCallback(CalcCorrectionThread),
                new object[] { correctionData, CreateUnityAdapter(), (JobResultHandler)CalcResultHandler });
    }


    private void CalcResultHandler(double distance, CorrectionData threadArgs) {
        Debug.LogFormat("thread done: correction={0}  distance={1} @ t={2} exec time={3} (ms)",
            threadArgs.correction, distance, threadArgs.timeAtApproach, threadArgs.execTimeMs);
        threadCountMutex.WaitOne();
        threadsPending--;
        threadCountMutex.ReleaseMutex();
        CalculationUpdate();
    }

    public class CorrectionData {
        // args
        //! Gravity state to evolve [input]
        public GravityState gravityState;
        //! Closest approach checking only starts once objects are this close [input]
        public double approachDistance;
        //! time limit (in physics time) for evolution [input]
        public double maxPhysTime;
        //! factor applied to spaceship velocity at start of calculation [input]
        public double correction;
        // results
        //! run-time used by the calculation in ms [output]
        public long execTimeMs;
        //! physics time at point of closest approach [output]
        public double timeAtApproach;
        //! distance in physics units at closest approach [output]
        public double distance;

        //! Clear the results
        public void Reset() {
            execTimeMs = 0;
            timeAtApproach = -1;
            distance = -1;
        }

    };

    public delegate void CalcCallback(LunarCourseCorrection lcc);

    private CalcCallback calcCallback;

    private delegate void JobResultHandler(double distance, CorrectionData threadArgs);

    private Mutex threadCountMutex;
    private int threadsPending;
    private CorrectionData[] correctionData;
    private enum CalcState { NOT_STARTED, INITIAL_THREE, CLOSEST_APPROACH, REFINING, DO_CALLBACK, DONE };
    private CalcState calcState;

    public string CorrectionCalcAsync(double targetDistance, 
                    double targetAccuracy, 
                    double approachDistance, 
                    double maxTime, 
                    CalcCallback calcCallback)
    {
        this.calcCallback = calcCallback; 
        double[] corrections = { -0.001, 0, 0.001 };
        this.targetDistance = targetDistance;
        this.targetAccuracy = targetAccuracy;

        threadsPending = 0;
        calcState = CalcState.NOT_STARTED;

        string s = "";
        GravityEngine ge = GravityEngine.Instance();

        correctionData = new CorrectionData[corrections.Length];
        int i = 0; 
        foreach (double correction in corrections) {
            // Run each computation as a dedicated thread
            correctionData[i] = new CorrectionData();
            correctionData[i].gravityState = ge.GetGravityStateCopy();
            correctionData[i].approachDistance = approachDistance;
            correctionData[i].maxPhysTime = maxTime;
            correctionData[i].correction = correction;
            threadCountMutex.WaitOne();
            threadsPending++;
            threadCountMutex.ReleaseMutex();
            System.Threading.ThreadPool.QueueUserWorkItem(
                new System.Threading.WaitCallback(CalcCorrectionThread), 
                    new object[] { correctionData[i], CreateUnityAdapter(), (JobResultHandler) CalcResultHandler }); 
            i++;
        }
        calcState = CalcState.INITIAL_THREE;
        return s;
    }


    /// <summary>
    /// Course correction calculations are Async on worker threads. In order to allow the rest of the game to 
    /// run, the update runs as a state machine and should be polled periodically to check when the full
    /// correction calculation is done. 
    /// 
    /// A calculation is started with CalculationStart() and then this routine is polled until it returns true.
    /// 
    /// Results are retrieved with GetCorrection(); 
    /// 
    /// </summary>
    /// <returns></returns>
    public bool CalculationUpdate() {

        switch(calcState) {
            case CalcState.INITIAL_THREE:
                if (threadsPending == 0) {
                    Debug.Log(CorrectionsLog());
                    // results are in, figure out which direction is working and start a new thread to 
                    // see if we are not within the desired accuracy
                    if (CorrectionInitialEstimate()) {
                        calcState = CalcState.REFINING;
                    } else {
                        // Correction estimation failed. Use the 0 correction path
                        correctionFinal = correctionData[1];
                        calcState = CalcState.DONE;
                    }
                
                }
                break;

            case CalcState.REFINING:
                if (threadsPending == 0) {
                    Debug.Log(CorrectionsLog());
                    // TODO: check the result, refine if necessary
                    calcState = CalcState.DO_CALLBACK;
                    Debug.LogFormat("correction={0} gives distance={1} for target={2}", 
                        correctionData[0].correction, correctionData[0].distance, targetDistance);
                    correctionFinal = correctionData[0];
                }
                break;

            case CalcState.CLOSEST_APPROACH:
                if (threadsPending == 0) {
                    calcState = CalcState.DO_CALLBACK;
                }
                break;
            case CalcState.NOT_STARTED:
            case CalcState.DONE:
            case CalcState.DO_CALLBACK:
                break;

            default:
                Debug.LogError("Unsupported state");
                break;
        }

        if (calcState == CalcState.DO_CALLBACK) {
            calcCallback(this);
            calcState = CalcState.DONE;
        }

        return (calcState == CalcState.DONE);

    }

    /// <summary>
    /// Get the corrected velocity for the lunar course correction. 
    /// 
    /// This routine should only be called from the correction done callback to ensure
    /// the computation has completed. 
    /// </summary>
    /// <param name="v"></param>
    public void GetCorrectionVelocity(ref double[] v) {
        GravityEngine.Instance().GetVelocityDouble(spaceship, ref v);
        v[0] *= 1 + correctionFinal.correction;
        v[1] *= 1 + correctionFinal.correction;
        v[2] *= 1 + correctionFinal.correction;
    }

    private bool CorrectionInitialEstimate() {

        // Check that none of the reponses had an issue (returned -1)
        foreach (CorrectionData cd in correctionData) {
            if (cd.distance < 0) {
                Debug.LogWarning("Course correction failed to determine a result");
                return false;
            }
        }

        // Have three results and know the target min approach. 
        List<LeastSquaresFit.LSPoint> points = new List<LeastSquaresFit.LSPoint>();
        foreach (CorrectionData cd in correctionData) {
            points.Add(new LeastSquaresFit.LSPoint(cd.correction, cd.distance));
        }
        double m = 0;
        double b = 0;
        double error = LeastSquaresFit.FindLinearLeastSquaresFit(points, out m, out b);

        // determine correction with a linear fit
        // have y = mx + b with correction as x, want to find correction for target distance
        // x = (y-b)/m
        double correction = (targetDistance - b) / m;
        Debug.LogFormat("LS Fit m={0} b={1} er={2} => correction={3} for target={4}", 
            m, b, error, correction, targetDistance);

        // Determine the actual distance with another sim
        // Re-use correctionData[0]
        correctionData[0].Reset();
        correctionData[0].correction = correction;
        correctionData[0].gravityState = GravityEngine.Instance().GetGravityStateCopy();
        threadCountMutex.WaitOne();
        threadsPending++;
        threadCountMutex.ReleaseMutex();
        System.Threading.ThreadPool.QueueUserWorkItem(
            new System.Threading.WaitCallback(CalcCorrectionThread),
                new object[] { correctionData[0], CreateUnityAdapter(), (JobResultHandler)CalcResultHandler });
        return true;
    }

    private string CorrectionsLog() {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("Calculation Summary:\n");
        foreach( CorrectionData data in correctionData) {
           sb.Append(string.Format("correction={0}  distance={1} time={2},  exec time={3} (ms)",
                data.correction, data.distance, data.timeAtApproach, data.execTimeMs));
        }
        return sb.ToString();
    }

    //---------------------------------------------------------------------------------------
    // Code from: http://blog.yamanyar.com/2015/05/unity-creating-c-thread-with-callback.html
    //---------------------------------------------------------------------------------------

    public void CalcCorrectionThread(object state) {

        object[] array = state as object[];
        ThreadAdapter adapter = array[1] as ThreadAdapter;
        JobResultHandler callback = array[2] as JobResultHandler;

        CorrectionData calcData = array[0] as CorrectionData;
        double distance = ClosestApproach(calcData);

        //if adapter is not null; callback is also not null.
        if (adapter != null) {
            adapter.ExecuteOnUi(delegate {
                callback(distance, calcData);
            });
        }

    }

    /// <summary>
    /// Must be called from an ui thread
    /// </summary>
    /// <returns>The unity adapter.</returns>
    internal static ThreadAdapter CreateUnityAdapter() {
        GameObject gameObject = new GameObject();
        return gameObject.AddComponent<ThreadAdapter>();
    }

    internal class ThreadAdapter : MonoBehaviour
    {

        private volatile bool waitCall = true;

        public static int x = 0;

        //this will hold the reference to delegate which will be
        //executed on ui thread
        private volatile Action theAction;

        public void Awake() {
            DontDestroyOnLoad(gameObject);
            this.name = "ThreadAdapter-" + (x++);
        }

        public IEnumerator Start() {
            while (waitCall) {
                yield return new WaitForSeconds(.05f);
            }
            theAction();
            Destroy(gameObject);
        }

        public void ExecuteOnUi(System.Action action) {
            this.theAction = action;
            waitCall = false;
        }
    }

}
