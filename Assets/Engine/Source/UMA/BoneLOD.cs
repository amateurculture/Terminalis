using UnityEngine;
using UMA.PoseTools;
using UMA.CharacterSystem;

public class BoneLOD : MonoBehaviour
{
    GameObject umaPlayer;
    SkinnedMeshRenderer skinRenderer;
    UMAExpressionPlayer expressions;
    Animator animator;
    CharacterController controller;
    DynamicCharacterAvatar avatar;
    
    void Update()
    {
        if (skinRenderer == null)
        {
            foreach (Transform child in transform)
            {
                if (child.name == "UMARenderer")
                {
                    umaPlayer = child.gameObject;
                    skinRenderer = umaPlayer.GetComponent<SkinnedMeshRenderer>();
                    expressions = transform.GetComponent<UMAExpressionPlayer>();
                    animator = transform.GetComponent<Animator>();
                    controller = transform.GetComponent<CharacterController>();
                    avatar = transform.GetComponent<DynamicCharacterAvatar>();
                    break;
                }
            }
        }
        else
        {
            float distance = Vector3.Distance(Camera.main.transform.position, transform.position);

            if (distance < 5)
            {
                avatar.enabled = true;
                skinRenderer.enabled = true;

                if (skinRenderer.quality != SkinQuality.Bone4)
                {
                    skinRenderer.quality = SkinQuality.Bone4;
                    expressions.enabled = true;
                    animator.enabled = true;
                    controller.enabled = true;
                }
            }
            else if (distance < 10)
            {
                avatar.enabled = true;
                skinRenderer.enabled = true;

                if (skinRenderer.quality != SkinQuality.Bone2)
                {
                    skinRenderer.quality = SkinQuality.Bone2;
                    expressions.enabled = false;
                    animator.enabled = true;
                    controller.enabled = true;
                }
            }
            else if (distance < 80 && transform.tag != "Player")
            {
                avatar.enabled = true;
                skinRenderer.enabled = true;

                if (skinRenderer.quality != SkinQuality.Bone1)
                {
                    skinRenderer.quality = SkinQuality.Bone1;
                    expressions.enabled = false;
                    animator.enabled = false;
                    controller.enabled = false;
                }
            }
            else if (distance >= 80 && transform.tag != "Player")
            {
                if (skinRenderer.enabled)
                {
                    avatar.enabled = false;
                    skinRenderer.enabled = false;
                    expressions.enabled = false;
                    animator.enabled = false;
                    controller.enabled = false;
                }
            }
        }
    }
}
