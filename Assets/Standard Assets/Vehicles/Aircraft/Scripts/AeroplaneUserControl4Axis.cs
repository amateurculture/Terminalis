using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

namespace UnityStandardAssets.Vehicles.Aeroplane
{
    [RequireComponent(typeof (AeroplaneController))]
    public class AeroplaneUserControl4Axis : MonoBehaviour
    {
        public float maxRollAngle = 80;
        public float maxPitchAngle = 80;
        public AeroplaneController m_Aeroplane;
        public bool m_AirBrakes;
        public CarController car;

        private float m_Yaw;

        private void Awake()
        {
            m_Aeroplane = GetComponent<AeroplaneController>();
            m_AirBrakes = true;
            m_Aeroplane.Throttle = 0f;
            car = GetComponent<CarController>();
        }

        private void LateUpdate()
        {
            float roll = Input.GetAxis("Horizontal");
            float pitch = Input.GetAxis("Vertical");

            if (Input.GetButtonDown("Jump"))  
                m_AirBrakes = !m_AirBrakes;

            if (Input.GetButtonDown("Equip Next Item"))
                m_Aeroplane.Throttle += .2f;
            
            if (Input.GetButtonDown("Equip Previous Item"))
                m_Aeroplane.Throttle -= .2f;

            m_Aeroplane.Throttle = Mathf.Clamp(m_Aeroplane.Throttle, -.2f, 1f);
            float throttle = m_Aeroplane.Throttle;

            if (Mathf.Abs(m_Aeroplane.Throttle) < .001f){
                m_Aeroplane.Throttle = throttle = 0f;
            }

            if (m_Aeroplane.Throttle <= -.2f)
            {
                car.Move(roll, -.2f, -.2f, m_AirBrakes ? 1f : 0f, 0);
            }
            else
            {
                car.Move(0f, 0f, 0f, m_AirBrakes ? 1f : 0f, 0);

                m_Yaw = Input.GetAxis("Fire1") - Input.GetAxis("Fire2");

#if MOBILE_INPUT
        AdjustInputForMobileControls(ref roll, ref pitch, ref m_Throttle);
#endif
                // Clamp rolling to prevent wobble at start
                roll = (Mathf.Abs(roll) < .1f) ? 0f : roll;

                // Pass the input to the aeroplane
                if (roll != 0f || pitch != 0f || m_Yaw != 0f || throttle > 0f)
                    m_Aeroplane.Move(roll, pitch, m_Yaw, throttle, m_AirBrakes);

                /*
                else
                {
                    // Kill the propellors if stopped
                    m_Aeroplane.Throttle -= Time.deltaTime * .01f;
                    if (m_Aeroplane.Throttle < 0f) 
                        m_Aeroplane.Throttle = 0f;
                    car.Move(0f, 0f, 0f, m_AirBrakes ? 1f : 0f);
                }
                */
            }
        }

        private void AdjustInputForMobileControls(ref float roll, ref float pitch, ref float throttle)
        {
            // because mobile tilt is used for roll and pitch, we help out by
            // assuming that a centered level device means the user
            // wants to fly straight and level!

            // this means on mobile, the input represents the *desired* roll angle of the aeroplane,
            // and the roll input is calculated to achieve that.
            // whereas on non-mobile, the input directly controls the roll of the aeroplane.

            float intendedRollAngle = roll*maxRollAngle*Mathf.Deg2Rad;
            float intendedPitchAngle = pitch*maxPitchAngle*Mathf.Deg2Rad;
            roll = Mathf.Clamp((intendedRollAngle - m_Aeroplane.RollAngle), -1, 1);
            pitch = Mathf.Clamp((intendedPitchAngle - m_Aeroplane.PitchAngle), -1, 1);
        }
    }
}
