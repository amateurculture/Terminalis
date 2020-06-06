/*
using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof (CarController))]
    public class CarUserControl : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use


        private void Awake()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
        }


        private void FixedUpdate()
        {
            // pass the input to the car!
            float h = CrossPlatformInputManager.GetAxis("Horizontal");
            float v = CrossPlatformInputManager.GetAxis("Vertical");
#if !MOBILE_INPUT
            float handbrake = CrossPlatformInputManager.GetAxis("Jump");
            m_Car.Move(h, v, v, handbrake);
#else
            m_Car.Move(h, v, v, 0f);
#endif
        }
    }
}
*/

using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof (CarController))]
    public class CarUserControl : MonoBehaviour
    {
        private CarController car;
        public bool usingHandbrake;
        private float handbrakeForce;
        public bool isDisabled;

        private void Awake()
        {
            car = GetComponent<CarController>();
            usingHandbrake = true;
        }

        private void FixedUpdate()
        {
            float h = 0;
            float v = 0;

            if (!isDisabled)
            {
                h = CrossPlatformInputManager.GetAxis("Horizontal");
                v = Input.GetAxis("Fire1") - Input.GetAxis("Fire2");
                //float b = Input.GetAxis("Fire2");

                if (Input.GetKeyDown(KeyCode.W)) v = 1;
                if (Input.GetKeyDown(KeyCode.S)) v = -1;

                if (Input.GetKeyDown(KeyCode.X) || Input.GetButtonDown("Submit")) 
                    usingHandbrake = !usingHandbrake;

                handbrakeForce = (usingHandbrake) ? 1 : 0;
            }

#if !MOBILE_INPUT
            car.Move(h, v, v, handbrakeForce);
#else
            car.Move(h, v, b, 0f);
#endif
        }
    }
}
