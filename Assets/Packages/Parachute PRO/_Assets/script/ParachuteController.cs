using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Controls the parachute states (open / drop).
/// Only realistic visual control (not logical control).
/// </summary>
public class ParachuteController : MonoBehaviour
{
    //[SerializeField] LayerMask colliderLayerMask;

    // Private fields
    Animator animator;
    Rigidbody rBody;
    Cloth cloth;
    SkinnedMeshRenderer skRend1, skRend2;
    bool opened;
    bool dropped;
    List<CapsuleCollider> colliderList;


    #region MONO
    private void Awake()
    {
        // Cache components
        rBody = transform.GetComponent<Rigidbody>();
        animator = transform.GetComponent<Animator>();
        cloth = transform.Find("mesh").Find("mesh_Stage2").GetComponent<Cloth>();
        skRend1 = transform.Find("mesh").Find("mesh_Stage1").GetComponent<SkinnedMeshRenderer>();
        skRend2 = transform.Find("mesh").Find("mesh_Stage2").GetComponent<SkinnedMeshRenderer>();

        colliderList = new List<CapsuleCollider>();

        // Init settings
        Init();
    }
    private void OnTriggerEnter(Collider other)
    {
        //if (1 << other.gameObject.layer != colliderLayerMask) return;

        if (!(other is CapsuleCollider))
            return;

        colliderList.Add(other as CapsuleCollider);
    }
    private void OnTriggerExit(Collider other)
    {
        if (colliderList.Contains(other as CapsuleCollider))
            colliderList.Remove(other as CapsuleCollider);
    }
    #endregion


    #region PRIVATE
    void Init()
    {
        opened = false;
        dropped = false;
        skRend1.enabled = false;
        skRend2.enabled = false;
        cloth.useTethers = true;
        cloth.useGravity = true;
        cloth.worldVelocityScale = 0.5f;
        cloth.worldAccelerationScale = 1f;
        cloth.stretchingStiffness = 1f;
        cloth.bendingStiffness = 1f;
        cloth.friction = 1f;
        cloth.damping = 0.7f;
        cloth.clothSolverFrequency = 200f;
        cloth.externalAcceleration = new Vector3(0f, 70f, 0f);
        cloth.randomAcceleration = new Vector3(25f, 25f, 25f);
    }
    IEnumerator IE_Opening()
    {
        skRend1.enabled = true;
        animator.Play("Opening");
        yield return new WaitForSeconds(1f);
        skRend1.enabled = false;
        skRend2.enabled = true;
        yield return new WaitForSeconds(1f);
        cloth.damping = 0.4f;
    }
    void UpdateClothColliders()
    {
        cloth.capsuleColliders = colliderList.ToArray();
    }
    #endregion


    #region PUBLIC
    /// <summary>
    /// Opening animation
    /// </summary>
    public void Open()
    {
        if (opened)
            return;

        opened = true;
        StartCoroutine(IE_Opening());
    }

    /// <summary>
    /// Dropping parachute to the ground
    /// </summary>
    public void Drop()
    {
        if (dropped)
            return;

        dropped = true;
        cloth.stretchingStiffness = 1f;
        cloth.bendingStiffness = 0.7f;
        cloth.worldVelocityScale = 0f;
        cloth.worldAccelerationScale = 0f;
        cloth.damping = 0f;
        cloth.externalAcceleration = new Vector3(0f, -10f, 0f);
        cloth.randomAcceleration = Vector3.zero;
        transform.parent = null;
        rBody.isKinematic = false;
        rBody.AddForce(-transform.forward * 10f);
        rBody.AddRelativeTorque(-1000000f, 0f, 0f);
        UpdateClothColliders();
    }
    #endregion
}