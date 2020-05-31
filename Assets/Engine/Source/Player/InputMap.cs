using UnityEngine;
using UnityEngine.XR;

namespace Controllers
{
    public class InputMap : MonoBehaviour
    {
        public static bool hasController()
        {
            string[] names = Input.GetJoystickNames();

            for (int x = 0; x < names.Length; x++)
            {
                if (names[x].Length > 0) //names[x].Length == 22 || names[x].Length == 33) // XBox Controller
                {
                    return true;
                }
            }
            return false;
        }

        public static string VRName()
        {
            return XRSettings.loadedDeviceName;
        }

        public static bool hasOVR()
        {
            if (System.IO.File.Exists("Assets/LICENSE.txt"))
            {
                return true;
            }
            return false;
        }

        public static bool isVrEnabled()
        {
            return XRSettings.enabled; 
        }

        // todo implement isHeadSet on...somehow
        public static bool _isHeadsetOn = false;
        public static bool IsHeadsetOn()
        {
            return _isHeadsetOn;

            //OVRInput?.GetDown(OVRInput.Button b);

        }

        public static void RecenterPose()
        {
#if OVRInput
        OVRManager.display.RecenterPose();
#else
            // Do nothing since we don't have a VR headset to reset
#endif
        }

        public static bool StartButton()
        {
#if OVRInput
        return OVRInput.GetDown(OVRInput.RawButton.Start, OVRInput.Controller.All);
#else
            return false;
#endif
        }

        public static bool BackButton()
        {
#if OVRInput
        return OVRInput.GetDown(OVRInput.RawButton.Back, OVRInput.Controller.All);
#else
            return false;
#endif
        }

        public static bool LThumbstickUp()
        {
            if (Input.GetKey(KeyCode.W) || Input.GetAxis("Vertical") > 0.5)
                return true;
            else
                return false;
        }

        public static bool LThumbstickDown()
        {
            if (Input.GetKey(KeyCode.S) || Input.GetAxis("Vertical") < -0.5)
                return true;
            else
                return false;
        }

        public static bool LThumbstickLeft()
        {
            if (Input.GetKey(KeyCode.A) || Input.GetAxis("Horizontal") < -0.5)
                return true;
            else
                return false;
        }

        public static bool LThumbstickRight()
        {
            if (Input.GetKey(KeyCode.D) || Input.GetAxis("Horizontal") > 0.5)
                return true;
            else
                return false;
        }

        public static bool RThumbstickUp()
        {
            if (Input.GetAxis("Oculus_GearVR_DpadX") < -0.5)
                return true;
            else
                return false;
        }

        public static bool RThumbstickDown()
        {
            if (Input.GetAxis("Oculus_GearVR_DpadX") > 0.5)
                return true;
            else
                return false;
        }

        public static bool RThumbstickRight()
        {
            if (Input.GetAxis("Oculus_GearVR_RThumbstickY") < -0.5)
                return true;
            else
                return false;
        }

        public static bool RThumbstickLeft()
        {
            if (Input.GetAxis("Oculus_GearVR_RThumbstickY") > 0.5)
                return true;
            else
                return false;
        }

        public static bool ButtonA()
        {
            bool buttonPress = false;

            if (hasController())
                buttonPress = Input.GetKeyDown("joystick button 0");

            if (!buttonPress)
                return Input.GetKeyDown("space");

            return buttonPress;
        }

        public static bool ButtonB()
        {
            bool buttonPress = false;

            if (hasController())
                buttonPress = Input.GetKeyDown("joystick button 1");

            if (!buttonPress)
                return Input.GetKeyDown("space");

            return buttonPress;
        }

        public static bool ButtonX()
        {
            bool buttonPress = false;

            if (hasController())
                buttonPress = Input.GetKeyDown("joystick button 2");

            if (!buttonPress)
                return Input.GetKeyDown("space");

            return buttonPress;
        }

        public static bool ButtonY()
        {
            bool buttonPress = false;

            if (hasController())
                buttonPress = Input.GetKeyDown("joystick button 3");

            if (!buttonPress)
                return Input.GetKeyDown("space");

            return buttonPress;
        }

        public static bool LShoulder()
        {
            bool buttonPress = false;

            if (hasController()) // TODO Create mapping file for linux and mac
                buttonPress = Input.GetKeyDown("joystick button 4");

            //if (!buttonPress) return Input.GetKeyDown(KeyCode.Q);

            return buttonPress;
        }

        public static bool RShoulder()
        {
            bool buttonPress = false;

            if (hasController()) // TODO Create mapping file for linux and mac
                buttonPress = Input.GetKeyDown("joystick button 5");

            //if (!buttonPress) return Input.GetKeyDown(KeyCode.E);

            return buttonPress;
        }

        public static double LTrigger()
        {
            //return Input.GetAxis("Oculus_GearVR_RThumbstickX");
            return 0;
        }

        public static double RTrigger()
        {
            //return Input.GetAxis("Oculus_GearVR_RThumbstickX");
            return 0;
        }

        // Trigger
        public static Vector2 Axis1DPrimaryThumbstick()
        {
#if OVRInput
        return OVRInput.Get (OVRInput.Axis1D.PrimaryThumbstick);
#else
            return Vector2.zero;
#endif
        }

        // Left Stick
        public static Vector2 LeftThumbstick()
        {
            return new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
        }

        // Right Stick
        public static Vector2 RightThumbstick()
        {
            //return new Vector2(Input.GetAxis("Oculus_GearVR_DpadX"), Input.GetAxis("Oculus_GearVR_RThumbstickY"));
            // TODO need a better way to handle the xbox controller right thumbstick
            return Vector2.zero;
        }

        public static Vector2 Axis2DLThumbstick()
        {
#if OVRInput
        return OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
#else
            return Vector2.zero;
#endif
        }

        public static double Axis1DPrimaryIndexTrigger()
        {
#if OVRInput
        return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
#else
            return 0f;
#endif
        }
    }
}
