using UnityEngine;
using System;
using Controllers;
using UnityEngine.Rendering.PostProcessing;

namespace AC_System
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(ThirdPersonPlayer))]

    public class PlayerController : MonoBehaviour
    {
        #region Variables

        //[SerializeField] private MouseLook m_MouseLook;
        //[SerializeField] private AudioClip[] m_FootstepSounds;
        //private AudioClip m_JumpSound = null;
        //private AudioClip m_LandSound = null;

        public bool invertHorizontal = false;
        public bool invertVertical = false;
        public bool canFly = false;
        public bool canSwim = true;

        public enum ControllerMode
        {
            Walking,
            Swimming,
            Flying
        }
        ;

        public ControllerMode controllerMode;

        // Second POV is a bit of a joke, the only way it would make sense is if the user
        // had a camera setup that was pointing at them and the "controller" just showed the
        // person what they were doing right now (i.e. a mirror...)
        // OR if the game were told from the perspective of a party member, but not the main 
        // character. But either way, neither is going to require a specific camera POV for this
        // that either makes sense or woulnd't require any camera change.
        // What about a camera that points at the front of the character instead of behind?
        //
        // Examples of 2nd person camera typically require moving the perspective in a way that is difficult, but not necessarily impossible.
        //
        // An easily abstractable example is Laiku in Super Mario 64, so to abstract this for all games, you could:
        //
        //1. Move the 2nd person controller camera to a position just outside the mesh camera location to show you looking at the camera, looking at the player. This would obviously be a really pointless joke.
        //
        //2. Be able to move the camera perspective to your target and see yourself moving around as that person. Perhaps if you hold down a key you can control the character OR the camera, similar to in Mario 64 where you could control the camera with a seperate stick. Some very obvious problems with this, you can't control both at the same time so it's going to look... um huh. Or, it's obsevational only from the perspective of the monster whom you don't control, but whose mind you can switch to! AH HAHAHAHAH *cough*
        //
        //3. 

        public enum ControllerPOV
        {
            First,
            Second,
            Third
        }
        public ControllerPOV controllerPOV;

        [Tooltip("(Optional) This is required for some programs like Gaia that put the water level at something other than y = 0.")]
        public float waterLevel = 0;

        [HideInInspector]
        private bool m_IsWalking;

        private bool isSwimming = false;
        //private bool isRunning = false;
        private bool m_Jump;
        private bool m_PreviouslyGrounded;
        private bool m_Jumping;
        private bool animStarted = false;
        private bool moving = false;
        private bool ButtonAPressed = false;

        private float m_WalkSpeed = 1.5f;
        private float m_RunSpeed = 4f;
        private float m_JumpSpeed = 6f;
        private float m_SwimSpeed = 2f;
        private float m_FlySpeed = 15f;

        private float m_StickToGroundForce = 15;
        // 1
        private float m_GravityMultiplier = 2;
        private float m_StepInterval = 1;
        // 5
        private float m_YRotation;
        private float m_StepCycle;
        private float m_NextStep;
        //private float turnRate = 22.5f;
        //private float turnTime = .2f;
        private float initialSpeed = 3f;
        private float currentSpeed = 0f;
        private float RotationRatchet = 45.0f;
        [Range(0f, 1f)] private float m_RunstepLenghten = 0.7f;

        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private Animation anim;
        private ThirdPersonCamera smoothFollow;
        private Rigidbody rigidBody;

        public bool lockCursor = false; //true;
        private bool m_cursorIsLocked;

        bool leftPaddlePressed;
        bool rightPaddlePressed;
        public bool autoRun;
        private ThirdPersonPlayer thirdPersonPlayer;
        private float eyeLevel = 1.4f; // Non VR

        public GameObject cameraTarget;
        public GameObject corpse;
        ControllerPOV previousPOV;

        float burndownTime;
        bool pressedAlpha;
        public float rotationXClamp = 90f;

        CapsuleCollider capsuleCollider;

        float previousMouseX;
        float previousMouseY;
        PostProcessVolume ppv;
        DepthOfField dof;
        private Vector3 m_CamForward;
        private Vector3 m_Move;

#if HYDROFORM
        HydroformComponent Water;
#endif

        #endregion

        GameObject OVR_Integration()
        {
            GameObject camera;
            GameObject cameraPrefab = (GameObject)Resources.Load("OVRCameraRig", typeof(GameObject));

            if (InputMap.hasOVR() && cameraPrefab != null)
            {
                camera = Instantiate(cameraPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                return camera;
            }

            return null;
        }
        
        private void Awake()
        {
            if (!gameObject.GetComponent<PlayerController>().enabled)
                return;

            m_CharacterController = GetComponent<CharacterController>();
            rigidBody = transform.GetComponent<Rigidbody>();
            thirdPersonPlayer = transform.GetComponent<ThirdPersonPlayer>();
            capsuleCollider = transform.GetComponent<CapsuleCollider>();

            GameObject camera;

            if (Camera.main == null)
            { 
                if ((camera = OVR_Integration()) == null)
                {
                    camera = new GameObject("MainCamera");
                    camera.tag = "MainCamera";
                    camera.AddComponent<Camera>();
                    camera.AddComponent<AudioListener>();
                    camera.AddComponent<ThirdPersonCamera>();
                    Camera.main.transform.position = new Vector3(0, transform.position.y, 0);
                }
            }

            if (cameraTarget == null)
            {
                GameObject thingy = new GameObject();
                cameraTarget = Instantiate(thingy, transform);
                cameraTarget.name = "CameraTarget";
                cameraTarget.transform.localPosition = new Vector3(0, 1.5f, -.25f); // 0, 1.35, 0 todo camera target is weird...
                cameraTarget.transform.parent = transform;
                Destroy(thingy, 0);
            }
            
            smoothFollow = Camera.main.GetComponent<ThirdPersonCamera>();

            if (smoothFollow == null)
                smoothFollow = Camera.main.gameObject.AddComponent<ThirdPersonCamera>();
            
            smoothFollow.target = cameraTarget.transform;
            smoothFollow.distance = 3f; //1

            m_CharacterController.center = new Vector3(0, .85f, 0);
            m_CharacterController.radius = .25f;
            m_CharacterController.height = 1.7f;

            capsuleCollider.center = new Vector3(0, .85f, 0);
            capsuleCollider.radius = .25f;
            capsuleCollider.height = 1.7f;
            
            ppv = Camera.main.GetComponent<PostProcessVolume>();

            if (ppv != null)
                ppv.profile.TryGetSettings<DepthOfField>(out dof);

            updatePOV();
        }
        
        private void Start()
        {
            if (controllerMode == ControllerMode.Flying && !canFly)
                controllerMode = ControllerMode.Walking;

            m_StepCycle = 0f;
            m_NextStep = m_StepCycle / 2f;
            m_Jumping = false;

            anim = GetComponent<Animation>();

            if (anim != null) anim.Play("idle", PlayMode.StopAll);

            if (controllerPOV == ControllerPOV.First) previousPOV = ControllerPOV.Third;
            else previousPOV = ControllerPOV.First;

            autoRun = false;

#if HYDROFORM
            HydroformComponent[] compList = FindObjectsOfType(typeof(HydroformComponent)) as HydroformComponent[];
            if (compList != null && compList.Length > 0 && compList[0] != null)
                Water = compList[0];
#endif
        }

        private bool isWalking()
        {
            if (controllerMode == ControllerMode.Flying) // && transform.position.y > 0)
                return false;

            float waterLine = -1f;
            float shoreCheck = -.925f;

            if (InputMap.VRName() == "Oculus")
            {
                waterLine = 0f;
                shoreCheck = .1f;
            }

            float height = 0;

#if HYDROFORM
            if (Water != null)
                height = Water.GetHeightAtPoint(transform.position);
#else
            height = waterLevel;
#endif
            waterLine += height; // waterLevel;
            shoreCheck += height; // waterLevel;

            if (controllerMode != ControllerMode.Swimming && canSwim && transform.position.y <= waterLine)
            { // Underwater
                controllerMode = ControllerMode.Swimming;
            }
            else if (controllerMode != ControllerMode.Walking && !canSwim && transform.position.y <= waterLine)
            {
                controllerMode = ControllerMode.Walking;
            }
            else if (controllerMode != ControllerMode.Walking && transform.position.y > shoreCheck)
            { // On shore (-.9)
                controllerMode = ControllerMode.Walking;
            }
            else if (canSwim && controllerMode == ControllerMode.Swimming && transform.position.y > waterLine)
            { // Swimming
                Vector3 v = transform.position;
                v.y = waterLine;
                transform.position = v;

                //if (Water != null) {
                //    transform.position = new Vector3(transform.position.x, height, transform.position.z);
                //}
            }

            if (controllerMode != ControllerMode.Walking)
                return false;

            return true;
        }

        private void updatePOV()
        {
            if (previousPOV != controllerPOV)
            {
                previousPOV = controllerPOV;
                switch (controllerPOV)
                {
                    case ControllerPOV.First:

                        Camera.main.transform.parent = gameObject.transform;
                        Vector3 camPosition = Vector3.zero;
                        if (!InputMap.isVrEnabled())
                            camPosition.y = eyeLevel;

                        camPosition.z = .35f;
                        Camera.main.transform.localPosition = camPosition;

                        if (smoothFollow != null)
                            smoothFollow.enabled = false;

                        if (rigidBody != null)
                        {
                            rigidBody.useGravity = false;
                            rigidBody.isKinematic = true;
                        }

                        m_CharacterController.enabled = true;
                        thirdPersonPlayer.enabled = false;
                        Camera.main.transform.parent = transform;

                        if (corpse != null)
                            corpse.SetActive(false);

                        break;

                    case ControllerPOV.Second:
                    // For now, second POV is the same as third

                    case ControllerPOV.Third:
                        if (smoothFollow != null)
                            smoothFollow.enabled = true;

                        if (rigidBody != null)
                        {
                            rigidBody.useGravity = true;
                            rigidBody.isKinematic = false;
                        }

                        m_CharacterController.enabled = false;
                        thirdPersonPlayer.enabled = true;
                        Camera.main.transform.parent = transform.parent;

                        if (corpse != null)
                            corpse.SetActive(true);

                        break;

                    default:
                        break;
                }
            }
            rotationXClamp = (rotationXClamp > 90f) ? 90f : rotationXClamp;
            rotationXClamp = (rotationXClamp < 0) ? 0 : rotationXClamp;
        }

        // todo update autorun when the third person controller is integrated
        private void updateAutorun()
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W))
                autoRun = (autoRun) ? false : true;
        }

        float timeToNextAlpha;
        
        // update rotation and jumping
        private void Update()
        {
            updateRotation();
            updatePOV();
            updateAutorun();

            if (InputMap.LShoulder())
                leftPaddlePressed = true;
            if (InputMap.RShoulder())
                rightPaddlePressed = true;

            if ((!canFly && controllerMode == ControllerMode.Flying) || (controllerMode == ControllerMode.Flying && InputMap.ButtonA() && !ButtonAPressed))
            {
                controllerMode = ControllerMode.Walking;
                ButtonAPressed = true;
            }
            if (!InputMap.ButtonA())
                ButtonAPressed = false;

            if (isWalking())
            {
                // the jump state needs to read here to make sure it is not missed
                if (!m_Jump && !m_Jumping && !isSwimming && !ButtonAPressed)
                {
                    m_Jump = InputMap.ButtonA();
                }
                // double jump triggers flying if available
                if (canFly && m_Jumping && InputMap.ButtonA() && !ButtonAPressed)
                {
                    controllerMode = ControllerMode.Flying;
                    return;
                }
                if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
                {
                    PlayLandingSound();
                    m_MoveDir.y = 0f;
                    m_Jumping = false;
                }
                if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
                {
                    m_MoveDir.y = 0f;
                }
                m_PreviouslyGrounded = m_CharacterController.isGrounded;
            }

            UpdateCursorLock();

            if (smoothFollow != null)
                controllerPOV = smoothFollow.UpdateCameraForScrollwheel(controllerPOV, corpse);

            isWalking();
            RatchetView();
            fixedUpdateRotation();

            Vector3 deltaPosition = Vector3.zero;
            bool lastMoving = moving;

            if ((canSwim && controllerMode == ControllerMode.Swimming) || (canFly && controllerMode == ControllerMode.Flying))
            {
                m_IsWalking = !Input.GetKey(KeyCode.LeftShift);

                if (m_IsWalking == true && InputMap.hasController())
                    m_IsWalking = (InputMap.LTrigger() > .5) ? false : true;

                if (moving)
                {
                    float speed1 = initialSpeed;
                    if (controllerMode == ControllerMode.Flying)
                        speed1 = m_FlySpeed;
                    if (controllerMode == ControllerMode.Swimming)
                        speed1 = m_SwimSpeed;

                    Vector3 tempVector;
                    if (InputMap.isVrEnabled())
                        tempVector = new Vector3(InputMap.LeftThumbstick().x, InputMap.LeftThumbstick().y, InputMap.RightThumbstick().x);
                    else
                        tempVector = new Vector3(InputMap.LeftThumbstick().x, InputMap.LeftThumbstick().y, 0);

                    if (m_IsWalking)
                        currentSpeed = speed1 * tempVector.magnitude;
                    else
                        currentSpeed = speed1 * 2 * tempVector.magnitude;
                }

                moving = false;

                if (Input.GetKey(KeyCode.W))
                {
                    moving = true;
                    deltaPosition += Camera.main.transform.forward;
                }
                else if (Input.GetKey(KeyCode.S))
                {
                    moving = true;
                    deltaPosition -= Camera.main.transform.forward;
                }
                if (Input.GetKey(KeyCode.A))
                {
                    moving = true;
                    deltaPosition -= Camera.main.transform.right * .5f;
                }
                else if (Input.GetKey(KeyCode.D))
                {
                    moving = true;
                    deltaPosition += Camera.main.transform.right * .5f;
                }
                if (Input.GetKey(KeyCode.W))
                {
                    moving = true;
                    deltaPosition += Camera.main.transform.up * .5f;
                }
                else if (Input.GetKey(KeyCode.S))
                {
                    moving = true;
                    deltaPosition -= Camera.main.transform.up * .5f;
                }

                if (!moving)
                {
                    deltaPosition += Camera.main.transform.forward * InputMap.LeftThumbstick().x;
                    deltaPosition += Camera.main.transform.right * InputMap.LeftThumbstick().y;
                    deltaPosition -= Camera.main.transform.up * InputMap.RightThumbstick().x;

                    if (deltaPosition.magnitude > 0f)
                        moving = true;
                }

                if (moving)
                {
                    if (moving != lastMoving) currentSpeed = initialSpeed;

                    if (m_CharacterController.enabled)
                        m_CharacterController.Move(deltaPosition * currentSpeed * Time.fixedDeltaTime);
                }
                else
                    currentSpeed = 0f;
            }

            float distanceCheck = 10f; //1.35f;
            
            if (controllerPOV == ControllerPOV.First)
            {
                if (ppv != null && dof != null)
                {
                    RaycastHit hit = new RaycastHit();
                    LayerMask layerMask = new LayerMask();
                    layerMask = ~(1 << LayerMask.NameToLayer("Player")) | ~(1 << LayerMask.NameToLayer("Agent"));

                    if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, distanceCheck, layerMask))
                        dof.focusDistance.value = hit.distance + .1f;
                    else
                        dof.focusDistance.value = distanceCheck;
                }
            }
            else
            {
                if (dof != null && smoothFollow != null)
                    dof.focusDistance.value = Vector3.Distance(cameraTarget.transform.position, Camera.main.transform.position);// smoothFollow.distance  * 1.25f;

                if (dof != null)
                {
                    dof.focusDistance.value = (dof.focusDistance.value > distanceCheck) ? distanceCheck : dof.focusDistance.value;
                    dof.focusDistance.value = (dof.focusDistance.value < 0) ? 0 : dof.focusDistance.value;
                }
            }

            if (!isWalking())
                return;

            float speed;
            GetInput(out speed);

            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = Camera.main.transform.forward * m_Input.y + Camera.main.transform.right * m_Input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(Camera.main.transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            m_MoveDir.z = desiredMove.z * speed;
            m_MoveDir.x = desiredMove.x * speed;

            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;
                }
            }
            else
            {
                m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
            }

            if (m_CharacterController.enabled)
                m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);

            ProgressStepCycle(speed);

            // read inputs
            float h = CrossPlatformInputManager.GetAxis("Horizontal");
            float v = CrossPlatformInputManager.GetAxis("Vertical");
            bool crouch = Input.GetKey(KeyCode.C);

            // calculate move direction to pass to character
            if (Camera.main.transform != null)
            {
                // calculate camera relative direction to move:
                m_CamForward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
                m_Move = v * m_CamForward + h * Camera.main.transform.right;
            }
            else
            {
                // we use world-relative directions in the case of no main camera
                m_Move = v * Vector3.forward + h * Vector3.right;
            }
#if !MOBILE_INPUT
            // walk speed multiplier
            if (Input.GetKey(KeyCode.LeftShift)) m_Move *= 0.5f;
#endif

            // pass all parameters to the character control script
            thirdPersonPlayer.Move(m_Move, crouch, m_Jump);
            m_Jump = false;
        }
        
        // updates movement
        private void FixedUpdate()
        {
            
        }

        public void UpdateCursorLock()
        {
            if (lockCursor)
                InternalLockUpdate();
        }

        private void InternalLockUpdate()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            /*

            if (Input.GetKeyUp(KeyCode.Escape)) m_cursorIsLocked = false;
            else if (Input.GetMouseButtonUp(0)) m_cursorIsLocked = true;

            if (m_cursorIsLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (!m_cursorIsLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            */
        }

        private void fixedUpdateRotation()
        {
            if (InputMap.hasController())
            {
                if (InputMap.isVrEnabled())
                {
                    float invV = (invertVertical) ? -1 : 1;
                    float invH = (invertHorizontal) ? -1 : 1;
                    Vector3 euler = transform.localEulerAngles;

                    // STANDARD MODE *****************************************************************
                    float turnAngleX = invV * (InputMap.RightThumbstick().x) * Time.fixedDeltaTime * 100f;
                    float turnAngleY = invH * (InputMap.RightThumbstick().y) * Time.fixedDeltaTime * 100f;

                    // Add dead zone to prevent controller drift
                    if (Math.Abs(turnAngleX) < 0.25)
                        turnAngleX = 0f;
                    if (Math.Abs(turnAngleY) < 0.25)
                        turnAngleY = 0f;

                    // Rotate view using XBox Controller

                    // Turn off up/down controller motion when in VR
                    if (!InputMap.isVrEnabled())
                        euler.x = (euler.x - turnAngleX) % 360; // UP / DOWN

                    euler.y = (euler.y - turnAngleY) % 360; // Left / Right
                    transform.localEulerAngles = euler;
                }
                else
                {
                    float invV = (invertVertical) ? -1 : 1;
                    float invH = (invertHorizontal) ? -1 : 1;
                    Vector3 euler = Camera.main.transform.localEulerAngles;

                    // STANDARD MODE *****************************************************************
                    float turnAngleX = invV * (InputMap.RightThumbstick().x);// * Time.fixedDeltaTime * 100f;
                    float turnAngleY = invH * (InputMap.RightThumbstick().y);// * Time.fixedDeltaTime * 100f;

                    // Add dead zone to prevent controller drift
                    if (Math.Abs(turnAngleX) < 0.25)
                        turnAngleX = 0f;
                    if (Math.Abs(turnAngleY) < 0.25)
                        turnAngleY = 0f;

                    // Rotate view using XBox Controller

                    // Turn off up/down controller motion when in VR
                    if (!InputMap.isVrEnabled())
                        euler.x = (euler.x - turnAngleX) % 360; // UP / DOWN

                    euler.y = (euler.y - turnAngleY) % 360; // Left / Right
                    Camera.main.transform.localEulerAngles = euler;
                }
            }

            if (InputMap.isVrEnabled())
            {
                Vector3 euler = transform.localEulerAngles;
                float invV = (invertVertical) ? -1 : 1;
                //float invH = (invertHorizontal) ? -1 : 1;
                float axisX = CrossPlatformInputManager.GetAxis("Mouse X") * invV; // * Time.fixedDeltaTime * 100f;
                //float axisY = CrossPlatformInputManager.GetAxis ("Mouse Y") * invH * Time.fixedDeltaTime * 100f;
                euler.y = (euler.y + axisX) % 360;
                transform.localEulerAngles = euler;
            }
        }

        private void updateRotation()
        {
            if (!InputMap.isVrEnabled())
            {
                //float currentMouseX = CrossPlatformInputManager.GetAxis("Mouse X");
                //float currentMouseY = CrossPlatformInputManager.GetAxis("Mouse Y");

                //float testMouseX = currentMouseX + previousMouseX;
                //float testMouseY = currentMouseY + previousMouseY;

                //previousMouseX = CrossPlatformInputManager.GetAxis("Mouse X");
                //previousMouseY = CrossPlatformInputManager.GetAxis("Mouse Y");

                //if (testMouseX > -15f && testMouseX < 15f && testMouseY > -15f && testMouseY < 15f)
                //{
                Vector3 euler = Camera.main.transform.localEulerAngles;
                //m_MouseLook.LookRotation ((new GameObject()).transform, Camera.main.transform);
                float invV = (invertVertical) ? -1 : 1;
                float invH = (invertHorizontal) ? -1 : 1;
                float axisX = CrossPlatformInputManager.GetAxis("Mouse X") * invH * 2f; // * Time.deltaTime * 100f;
                float axisY = CrossPlatformInputManager.GetAxis("Mouse Y") * invV * 2f; // * Time.deltaTime * 100f;
                euler.y = (euler.y + axisX) % 360;
                euler.x = (euler.x - axisY) % 360;
                euler.z = 0;

                /*
                float aboveHorizonClamp = 360f - rotationXClamp;

                if (euler.x > rotationXClamp && euler.x < 180f)
                    euler.x = rotationXClamp;
                else if (euler.x < aboveHorizonClamp && euler.x > 180f)
                    euler.x = aboveHorizonClamp;
                    */

                Camera.main.transform.localEulerAngles = euler;

                /*
                euler.x = transform.localEulerAngles.x;
                euler.z = transform.localEulerAngles.z;
                transform.localEulerAngles = euler;
                */
            }

            // VR RATCHETED STICK MODE ***********************************************************************

            // comment this out
            //float turnRate = RotationRatchet / 2f;
            //if (InputMap.RThumbstickRight())
            //    turnAngleY = -turnRate * invH;
            //else if (InputMap.RThumbstickLeft())
            //    turnAngleY = turnRate * invH;
            //if (InputMap.RThumbstickUp())
            //    turnAngleX = -turnRate * invV;
            //else if (InputMap.RThumbstickDown())
            //    turnAngleX = turnRate * invV;
        }

        private void RatchetView()
        {
            Vector3 euler;

            // Use game controller shoulder buttons or Q/E keys to ratchet rotation
            float EulerRotation = 0f;
            if (leftPaddlePressed)
                EulerRotation = -RotationRatchet;
            if (rightPaddlePressed)
                EulerRotation = RotationRatchet;

            euler = transform.localEulerAngles;
            euler.y = (euler.y + EulerRotation) % 360;
            transform.localEulerAngles = euler;

            leftPaddlePressed = false;
            rightPaddlePressed = false;
        }

        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed * (m_IsWalking ? 1f : m_RunstepLenghten))) * Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }

        private void PlayFootStepAudio()
        {
            /*
            if (!m_CharacterController.isGrounded || m_FootstepSounds.Length == 0)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
            */
        }

        private void PlayJumpSound()
        {
            //AudioSource.PlayClipAtPoint(m_JumpSound, transform.position);
        }

        private void PlayLandingSound()
        {
            //AudioSource.PlayClipAtPoint(m_LandSound, transform.position);
            m_NextStep = m_StepCycle + .5f;
        }

        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");
            bool isWalking = !(horizontal == 0f && vertical == 0f);

            if (anim != null)
            {
                if (isWalking && !animStarted)
                {
                    animStarted = true;
                    anim.Play("walk");
                }
                else if (!isWalking && animStarted)
                {
                    animStarted = false;
                    anim.Play("idle");
                }
            }

            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);

            if (m_IsWalking == true && InputMap.hasController())
            {
                m_IsWalking = (InputMap.LTrigger() > .5f) ? false : true;
            }

            // set the desired speed to be walking or running
            speed = m_IsWalking ? m_RunSpeed : m_WalkSpeed;// m_WalkSpeed : m_RunSpeed;

            if (InputMap.hasController())
                speed *= InputMap.LeftThumbstick().magnitude;

            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Enable this if you want collisions to disable flying mode
            // if (controllerMode == ControllerMode.Flying)
            //     controllerMode = ControllerMode.Walking;

            Rigidbody body = hit.collider.attachedRigidbody;

            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
                return;

            if (body == null || body.isKinematic)
                return;

            body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
        }

        private void Reset()
        {
            transform.tag = "Player";
        }
    }
}
