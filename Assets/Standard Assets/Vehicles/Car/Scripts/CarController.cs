using UnityEngine;

#pragma warning disable 649
namespace UnityStandardAssets.Vehicles.Car
{
    internal enum CarDriveType
    {
        FrontWheelDrive,
        RearWheelDrive,
        FourWheelDrive
    }

    internal enum SpeedType
    {
        MPH,
        KPH
    }

    public class CarController : MonoBehaviour
    {
        [SerializeField] private CarDriveType m_CarDriveType = CarDriveType.FourWheelDrive;
        [SerializeField] private WheelCollider[] m_WheelColliders = new WheelCollider[4];
        [SerializeField] private GameObject[] m_WheelMeshes = new GameObject[4];
        [SerializeField] private WheelEffects[] m_WheelEffects = new WheelEffects[4];
        [SerializeField] private Vector3 m_CentreOfMassOffset;
        [SerializeField] private float m_MaximumSteerAngle;
        [Range(0, 1)] [SerializeField] public float m_SteerHelper; // 0 is raw physics , 1 the car will grip in the direction it is facing
        [Range(0, 1)] [SerializeField] public float m_TractionControl; // 0 is no traction control, 1 is full interference
        [SerializeField] public float m_WheelTorque;
        [SerializeField] public float m_BrakeTorque;
        [SerializeField] private float m_HandbrakeTorque;
        [SerializeField] public float m_Downforce = 100f;
        [SerializeField] private SpeedType m_SpeedType;
        [SerializeField] private float m_Topspeed = 200;
        [SerializeField] private int NoOfGears = 5;
        [SerializeField] private float m_RevRangeBoundary = 1f;
        [SerializeField] public float m_SlipLimit;
        //[SerializeField] public float m_BrakeTorque;

        [HideInInspector] public int gearboxSetting = 0;
        private int m_GearNum;
        private Quaternion[] m_WheelMeshLocalRotations;
        private Vector3 m_Prevpos, m_Pos;
        private float m_SteerAngle;
        private float m_GearFactor;
        private float m_OldRotation;
        private float m_CurrentTorque;
        private Rigidbody m_Rigidbody;
        private const float k_ReversingThreshold = 0.01f;

        public bool Skidding { get; private set; }
        public float BrakeInput { get; private set; }
        public float CurrentSteerAngle { get { return m_SteerAngle; } }
        public float CurrentSpeed
        {
            get
            {
                if (m_Rigidbody != null)
                    return m_Rigidbody.velocity.magnitude * 2.23693629f;
                else
                    return 0f;
            }
        }
        public float MaxSpeed { get { return m_Topspeed; } }
        public float Revs { get; private set; }
        public float AccelInput { get; private set; }

        private void Start()
        {
            m_WheelMeshLocalRotations = new Quaternion[4];
            for (int i = 0; i < 4; i++)
            {
                m_WheelMeshLocalRotations[i] = m_WheelMeshes[i].transform.localRotation;
            }
            m_WheelColliders[0].attachedRigidbody.centerOfMass = m_CentreOfMassOffset;

            //m_HandbrakeTorque = float.MaxValue;

            m_Rigidbody = GetComponent<Rigidbody>();
            m_CurrentTorque = (m_WheelTorque * 4) - (m_TractionControl * (m_WheelTorque * 4));
        }

        // todo base torque off gear conversion...
        void GearChange(float gearShift)
        {
            if (gearShift < 0) m_GearNum--;
            if (gearShift > 0) m_GearNum++;

            if (m_GearNum < -1)
                m_GearNum = -1;
            else if (m_GearNum > NoOfGears)
                m_GearNum = NoOfGears;
        }

        void GearShift(int gearShift)
        {
            gearboxSetting += gearShift;
            if (gearboxSetting < 0) gearboxSetting = 0;
            if (gearboxSetting > 3) gearboxSetting = 3;
        }

        // todo make automatic gear changing logarithmic instead of assuming an even distribution of gears
        private void GearChanging()
        {
            float f = Mathf.Abs(CurrentSpeed / MaxSpeed);
            float upgearlimit = (1 / (float)NoOfGears) * (m_GearNum + 1);
            float downgearlimit = (1 / (float)NoOfGears) * m_GearNum;

            if (m_GearNum > 0 && f < downgearlimit)
                m_GearNum--;

            if (f > upgearlimit && (m_GearNum < (NoOfGears - 1)))
                m_GearNum++;
        }

        // simple function to add a curved bias towards 1 for a value in the 0-1 range
        private static float CurveFactor(float factor)
        {
            return 1 - (1 - factor) * (1 - factor);
        }

        // unclamped version of Lerp, to allow value to exceed the from-to range
        private static float ULerp(float from, float to, float value)
        {
            return (1.0f - value) * from + value * to;
        }

        private void CalculateGearFactor()
        {
            float f = (1 / (float)NoOfGears);
            // gear factor is a normalised representation of the current speed within the current gear's range of speeds.
            // We smooth towards the 'target' gear factor, so that revs don't instantly snap up or down when changing gear.
            var targetGearFactor = Mathf.InverseLerp(f * m_GearNum, f * (m_GearNum + 1), Mathf.Abs(CurrentSpeed / MaxSpeed));
            m_GearFactor = Mathf.Lerp(m_GearFactor, targetGearFactor, Time.deltaTime * 5f);
        }

        private void CalculateRevs()
        {
            // calculate engine revs (for display / sound)
            // (this is done in retrospect - revs are not used in force/power calculations)
            CalculateGearFactor();
            var gearNumFactor = m_GearNum / (float)NoOfGears;
            var revsRangeMin = ULerp(0f, m_RevRangeBoundary, CurveFactor(gearNumFactor));
            var revsRangeMax = ULerp(m_RevRangeBoundary, 1f, gearNumFactor);
            Revs = ULerp(revsRangeMin, revsRangeMax, m_GearFactor);
        }

        public void Move(float steering, float accel, float footbrake, float handbrake, int gearShift)
        {
            GearShift(gearShift);

            for (int i = 0; i < 4; i++)
            {
                Quaternion quat;
                Vector3 position;
                m_WheelColliders[i].GetWorldPose(out position, out quat);
                m_WheelMeshes[i].transform.position = position;
                m_WheelMeshes[i].transform.rotation = quat;
            }

            //clamp input values
            steering = Mathf.Clamp(steering, -1, 1);
            AccelInput = accel = Mathf.Clamp(accel, -1, 1);
            BrakeInput = footbrake = Mathf.Clamp(footbrake, 0, 1);
            handbrake = Mathf.Clamp(handbrake, 0, 1);
            //gearShift = Mathf.Clamp(gearShift, -1, 1);

            //Set the steer on the front wheels; assuming that wheels 0 and 1 are the front wheels
            m_SteerAngle = steering * m_MaximumSteerAngle;
            m_WheelColliders[0].steerAngle = m_SteerAngle;
            m_WheelColliders[1].steerAngle = m_SteerAngle;

            SteerHelper();

            switch (gearboxSetting)
            {
                case 0: ApplyDrive(0, 1, handbrake); break; // Park
                case 1: ApplyDrive(-accel, footbrake, handbrake); break; // Reverse
                case 2: ApplyDrive(0, footbrake, handbrake); break; // Neutral
                case 3: ApplyDrive(accel, footbrake, handbrake); break; // Drive
                default: break;
            }

            CapSpeed();
            CalculateRevs();
            GearChanging();
            AddDownForce();
            CheckForWheelSpin();
            TractionControl();
        }

        private void CapSpeed()
        {
            if (m_Rigidbody == null) return;

            float speed = m_Rigidbody.velocity.magnitude;
            switch (m_SpeedType)
            {
                case SpeedType.MPH:

                    speed *= 2.23693629f;
                    if (speed > m_Topspeed)
                        m_Rigidbody.velocity = (m_Topspeed / 2.23693629f) * m_Rigidbody.velocity.normalized;
                    break;

                case SpeedType.KPH:
                    speed *= 3.6f;
                    if (speed > m_Topspeed)
                        m_Rigidbody.velocity = (m_Topspeed / 3.6f) * m_Rigidbody.velocity.normalized;
                    break;
            }
        }

        bool hasKilledTorque = false;

        private void ApplyDrive(float accel, float footbrake, float handbrake)
        {
            float thrustTorque;
            thrustTorque = accel * m_CurrentTorque;

            if (handbrake > 0.1f)
            {
                var hbTorque = handbrake * m_HandbrakeTorque;
                for (int i = 0; i < 4; i++)
                {
                    m_WheelColliders[i].motorTorque = 0;
                    m_WheelColliders[i].brakeTorque = 0;
                }
                m_WheelColliders[2].brakeTorque = hbTorque;
                m_WheelColliders[3].brakeTorque = hbTorque;
                hasKilledTorque = true;
                //Debug.Log("HAND BRAKE -- Torque: " + thrustTorque + " Brake Torque: " + m_BrakeTorque * footbrake + " Handbrake Torque: " + hbTorque);
            }
            else if (footbrake > 0.1f)
            {
                //Debug.Log("BRAKING -- Torque: " + thrustTorque + " Brake Torque: " + m_BrakeTorque * footbrake);
                hasKilledTorque = true;
                for (int i = 0; i < 4; i++)
                {
                    m_WheelColliders[i].brakeTorque = m_BrakeTorque * footbrake;
                    m_WheelColliders[i].motorTorque = 0;
                }
            }
            else
            {
                // kill all torque as soon as player accelerates, but not again until the player brakes
                if (hasKilledTorque)
                {
                    //Debug.Log("KILL TORQUE -- Torque: " + thrustTorque + " Brake Torque: " + m_BrakeTorque * footbrake);
                    for (int i = 0; i < 4; i++)
                    {
                        m_WheelColliders[i].brakeTorque = 0;
                        m_WheelColliders[i].motorTorque = 0;
                    }
                    hasKilledTorque = false;
                }

                switch (m_CarDriveType)
                {
                    case CarDriveType.FourWheelDrive:

                        for (int i = 0; i < 4; i++)
                            m_WheelColliders[i].motorTorque = thrustTorque;
                        break;

                    case CarDriveType.FrontWheelDrive:
                        m_WheelColliders[0].motorTorque = m_WheelColliders[1].motorTorque = thrustTorque;
                        break;

                    case CarDriveType.RearWheelDrive:
                        //Debug.Log("DRIVING -- Torque: " + thrustTorque + " Brake Torque: " + m_BrakeTorque * footbrake);
                        m_WheelColliders[2].motorTorque = m_WheelColliders[3].motorTorque = thrustTorque;
                        break;
                }
            }
        }

        private void SteerHelper()
        {
            for (int i = 0; i < 4; i++)
            {
                WheelHit wheelhit;
                m_WheelColliders[i].GetGroundHit(out wheelhit);
                if (wheelhit.normal == Vector3.zero)
                    return; // wheels arent on the ground so dont realign the rigidbody velocity
            }

            // this if is needed to avoid gimbal lock problems that will make the car suddenly shift direction
            if (Mathf.Abs(m_OldRotation - transform.eulerAngles.y) < 10f)
            {
                var turnadjust = (transform.eulerAngles.y - m_OldRotation) * m_SteerHelper;
                Quaternion velRotation = Quaternion.AngleAxis(turnadjust, Vector3.up);

                if (m_Rigidbody != null)
                    m_Rigidbody.velocity = velRotation * m_Rigidbody.velocity;
            }
            m_OldRotation = transform.eulerAngles.y;
        }

        private void AddDownForce()
        {
            m_WheelColliders[0].attachedRigidbody.AddForce(-transform.up * m_Downforce *
                                                         m_WheelColliders[0].attachedRigidbody.velocity.magnitude);
        }

        private void CheckForWheelSpin()
        {
            for (int i = 0; i < 4; i++)
            {
                WheelHit wheelHit;
                m_WheelColliders[i].GetGroundHit(out wheelHit);

                // is the tire slipping above the given threshhold
                if (Mathf.Abs(wheelHit.forwardSlip) >= m_SlipLimit || Mathf.Abs(wheelHit.sidewaysSlip) >= m_SlipLimit)
                {
                    m_WheelEffects[i].EmitTyreSmoke();

                    // avoiding all four tires screeching at the same time; if they do it can lead to some strange audio artifacts
                    if (!AnySkidSoundPlaying())
                    {
                        m_WheelEffects[i].PlayAudio();
                    }
                    continue;
                }

                // if it wasnt slipping stop all the audio
                if (m_WheelEffects[i].PlayingAudio)
                    m_WheelEffects[i].StopAudio();

                // end the trail generation
                m_WheelEffects[i].EndSkidTrail();
            }
        }

        // crude traction control that reduces the power to wheel if the car is wheel spinning too much
        private void TractionControl()
        {
            WheelHit wheelHit;
            switch (m_CarDriveType)
            {
                case CarDriveType.FourWheelDrive:
                    for (int i = 0; i < 4; i++)
                    {
                        m_WheelColliders[i].GetGroundHit(out wheelHit);
                        AdjustTorque(wheelHit.forwardSlip);
                    }
                    break;

                case CarDriveType.RearWheelDrive:
                    m_WheelColliders[2].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);

                    m_WheelColliders[3].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);
                    break;

                case CarDriveType.FrontWheelDrive:
                    m_WheelColliders[0].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);

                    m_WheelColliders[1].GetGroundHit(out wheelHit);
                    AdjustTorque(wheelHit.forwardSlip);
                    break;
            }
        }

        private void AdjustTorque(float forwardSlip)
        {
            if (forwardSlip >= m_SlipLimit && m_CurrentTorque >= 0)
            {
                m_CurrentTorque -= 10 * m_TractionControl;
            }
            else
            {
                m_CurrentTorque += 10 * m_TractionControl;
                if (m_CurrentTorque > m_WheelTorque)
                    m_CurrentTorque = m_WheelTorque;
            }
        }

        private bool AnySkidSoundPlaying()
        {
            for (int i = 0; i < 4; i++)
                if (m_WheelEffects[i].PlayingAudio)
                    return true;
            return false;
        }
    }
}
