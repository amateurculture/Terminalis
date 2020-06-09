using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarUserControl : MonoBehaviour
    {
        private CarController car;
        public bool usingHandbrake;
        public bool isDisabled;
        public int gearShift;

        private void Awake()
        {
            car = GetComponent<CarController>();
            usingHandbrake = true;
        }

        private void LateUpdate()
        {
            float h = 0;
            float v = 0;
            float v2 = 0;
            float unbiased = 0;

            if (!isDisabled)
            {
                h = CrossPlatformInputManager.GetAxis("Horizontal");

                unbiased = Input.GetAxis("Fire1") - Input.GetAxis("Fire2");
                v = (unbiased > 0) ? unbiased : 0;
                v2 = (unbiased < 0) ? unbiased : 0;


                v = Input.GetAxis("Fire1");
                v2 = Input.GetAxis("Fire2");

                if (Input.GetKey(KeyCode.W)) v = 1;
                if (Input.GetKey(KeyCode.S)) v = -1;

                if (Input.GetKeyDown(KeyCode.X) || Input.GetButtonDown("Submit"))
                    usingHandbrake = !usingHandbrake;

                gearShift = 0;
                if (Input.GetButtonDown("Equip Previous Item")) gearShift = -1;
                if (Input.GetButtonDown("Equip Next Item")) gearShift = 1;

#if !MOBILE_INPUT
                car.Move(h, v, v2, (usingHandbrake) ? 1f : 0f, gearShift);
#else
                car.Move(h, v, b, 0f);
#endif
            }
        }
    }
}
