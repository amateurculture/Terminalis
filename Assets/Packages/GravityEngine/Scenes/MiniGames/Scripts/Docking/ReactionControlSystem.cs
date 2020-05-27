using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spaceship RigidBody/GravityEngine controller.
/// 
/// Used in conjunction with a docking group. When in NBody mode, the user input is translated into 
/// GravityEngine changes (rotations are handled as rigidbody motions). 
/// 
/// When part of an active docking group, translations are performed with RigidBody AddForce() and 
/// movement is local (with overall gravity handled by the docking controller, which will parent this
/// object to a center of mass NBody object). 
/// 
/// IJKL/UO - perform translations of spaceship
/// WASD/QE - perform rotation of spaceship
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ReactionControlSystem : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Velocity change per keypress")]
    private float deltaV = 1f;

    [SerializeField]
    [Tooltip("Torque to be applied")]
    private float torque = 1f;

    //! required component. Detected at startup
    private Rigidbody rigidBody;

    private GravityEngine ge;

    private bool rigidBodyEnabled;

    //! NBody parent when under GE control
    private NBody nbody; 

    private void Start() {

        rigidBody = GetComponent<Rigidbody>();
        if (rigidBody.useGravity) {
            Debug.LogWarning("Turning off useGravity");
            rigidBody.useGravity = false;
        }
        ge = GravityEngine.Instance();


    }

    /// <summary>
    /// Input processing. 
    /// Translations may be under GE or Unity control. 
    /// Rotations are always via Unity RigidBody mechanics. 
    /// </summary>
    void Update() {

        // translations
        if (Input.GetKeyDown(KeyCode.I)) {
            // X-axis
            DoTranslation(Vector3.right);
        } else if (Input.GetKeyDown(KeyCode.K)) {
            // - X-axis
            DoTranslation(Vector3.left);
        } else if (Input.GetKeyDown(KeyCode.J)) {
            // Y-axis
            DoTranslation(Vector3.up);
        } else if (Input.GetKeyDown(KeyCode.L)) {
            // - Y-axis
            DoTranslation(Vector3.down);
        } else if (Input.GetKeyDown(KeyCode.U)) {
            // Z-axis
            DoTranslation(Vector3.forward);
        } else if (Input.GetKeyDown(KeyCode.O)) {
            // - Z-axis
            DoTranslation(Vector3.back);
        }

        // Rotations
        if (Input.GetKeyDown(KeyCode.W)) {
            // X-axis
            DoRotation(Vector3.right);
        } else if (Input.GetKeyDown(KeyCode.S)) {
            // - X-axis
            DoRotation(Vector3.left);
        } else if (Input.GetKeyDown(KeyCode.A)) {
            // Y-axis
            DoRotation(Vector3.up);
        } else if (Input.GetKeyDown(KeyCode.D)) {
            // - Y-axis
            DoRotation(Vector3.down);
        } else if (Input.GetKeyDown(KeyCode.Q)) {
            // Z-axis
            DoRotation(Vector3.forward);
        } else if (Input.GetKeyDown(KeyCode.E)) {
            // - Z-axis
            DoRotation(Vector3.back);
        }
    }

    /// <summary>
    /// Set the NBody to be used for GE application of RCS inputs. Used when object is not active in a 
    /// docking group. 
    /// </summary>
    /// <param name="n"></param>
    public void SetNBody(NBody n) {
        nbody = n;
    }

    /// <summary>
    /// Enable translation motion as Unity RigidBody physics. When enabled, there is local Unity physics
    /// motion with respect to a center of mass NBody parent.
    /// </summary>
    /// <param name="b"></param>
    public void SetRigidBodyEnabled(bool b) {
        rigidBodyEnabled = b;
    }

    private void DoTranslation(Vector3 axis) {
        if (rigidBodyEnabled) {
            rigidBody.AddRelativeForce(deltaV * axis, ForceMode.VelocityChange);
        } else {
            // for a massless (wrt e.g. Earth) ship, applyimpulse will just do a change in velocity
            ge.ApplyImpulse(nbody, GravityScaler.ScaleVelSceneToPhys(deltaV * axis));
        }
    }

    private void DoRotation(Vector3 axis) {
        Debug.Log("rotate " + axis);
        rigidBody.AddRelativeTorque(torque * axis);
    }

}
