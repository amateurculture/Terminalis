/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

using UnityEngine;
using Opsive.UltimateCharacterController.Character.MovementTypes;
using Opsive.UltimateCharacterController.Utility;
using Opsive.UltimateCharacterController.ThirdPersonController.Camera.ViewTypes;

namespace Opsive.UltimateCharacterController.FirstPersonController.Character.MovementTypes
{
    /// <summary>
    /// With the Combat movement type the character can strafe and move backwards, and is always facing in the direction of the camera.
    /// </summary>
    public class Walking : ThirdPerson
    {
        [Tooltip("The minimum yaw angle (in degrees).")]
        [SerializeField] protected float m_MinYawLimit = -180;
        [Tooltip("The maximum yaw angle (in degrees).")]
        [SerializeField] protected float m_MaxYawLimit = 180;
        [Tooltip("The speed in which the camera should rotate towards the yaw limit when out of bounds.")]
        [Range(0, 1)] [SerializeField] protected float m_YawLimitLerpSpeed = 0.7f;

        public float MinYawLimit { get { return m_MinYawLimit; } set { m_MinYawLimit = value; } }
        public float MaxYawLimit { get { return m_MaxYawLimit; } set { m_MaxYawLimit = value; } }
        public float YawLimitLerpSpeed { get { return m_YawLimitLerpSpeed; } set { m_YawLimitLerpSpeed = value; } }

        /// <summary>
        /// Rotates the camera according to the horizontal and vertical movement values.
        /// </summary>
        /// <param name="horizontalMovement">-1 to 1 value specifying the amount of horizontal movement.</param>
        /// <param name="verticalMovement">-1 to 1 value specifying the amount of vertical movement.</param>
        /// <param name="immediatePosition">Should the camera be positioned immediately?</param>
        /// <returns>The updated rotation.</returns>
        public override Quaternion Rotate(float horizontalMovement, float verticalMovement, bool immediatePosition)
        {
            // Update the rotation. The yaw may have a limit.
            if (Mathf.Abs(m_MinYawLimit - m_MaxYawLimit) < 360)
            {
                // Determine the new rotation with the updated yaw.
                var targetRotation = MathUtility.TransformQuaternion(m_CharacterRotation, Quaternion.Euler(m_Pitch, m_Yaw, 0));
                var diff = MathUtility.InverseTransformQuaternion(Quaternion.LookRotation(Vector3.forward, m_CharacterLocomotion.Up), targetRotation * Quaternion.Inverse(m_CharacterTransform.rotation));
                // The rotation shouldn't extend beyond the min and max yaw limit.
                var targetYaw = MathUtility.ClampAngle(diff.eulerAngles.y, horizontalMovement, m_MinYawLimit, m_MaxYawLimit);
                m_Yaw += Mathf.Lerp(0, Mathf.DeltaAngle(diff.eulerAngles.y, targetYaw), m_YawLimitLerpSpeed);
            }
            else
            {
                m_Yaw += horizontalMovement;
            }

            // Return the rotation.
            return base.Rotate(horizontalMovement, verticalMovement, immediatePosition);
        }
    }

    /// <summary>
    /// With the Combat movement type the character can strafe and move backwards, and is always facing in the direction of the camera.
    /// </summary>
    public class Combat : MovementType
    {
        public override bool FirstPersonPerspective { get { return true; } }

        /// <summary>
        /// Returns the delta yaw rotation of the character.
        /// </summary>
        /// <param name="characterHorizontalMovement">The character's horizontal movement.</param>
        /// <param name="characterForwardMovement">The character's forward movement.</param>
        /// <param name="cameraHorizontalMovement">The camera's horizontal movement.</param>
        /// <param name="cameraVerticalMovement">The camera's vertical movement.</param>
        /// <returns>The delta yaw rotation of the character.</returns>
        public override float GetDeltaYawRotation(float characterHorizontalMovement, float characterForwardMovement, float cameraHorizontalMovement, float cameraVerticalMovement)
        {
#if UNITY_EDITOR
            if (m_LookSource == null) {
                Debug.LogError("Error: There is no look source attached to the character. Ensure the character has a look source attached (such as a camera).");
                return 0;
            }
#endif
            var lookRotation = Quaternion.LookRotation(m_LookSource.LookDirection(true), m_CharacterLocomotion.Up);
            // Convert to a local character rotation and then only return the relative y rotation.
            return MathUtility.ClampInnerAngle(MathUtility.InverseTransformQuaternion(m_Transform.rotation, lookRotation).eulerAngles.y);
        }

        /// <summary>
        /// Gets the controller's input vector relative to the movement type.
        /// </summary>
        /// <param name="inputVector">The current input vector.</param>
        /// <returns>The updated input vector.</returns>
        public override Vector2 GetInputVector(Vector2 inputVector)
        {
            // No changes are necessary.
            return inputVector;
        }
    }
}