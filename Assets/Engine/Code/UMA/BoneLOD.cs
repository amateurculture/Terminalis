using UnityEngine;
using UMA.PoseTools;
using UMA.CharacterSystem;

public class BoneLOD : MonoBehaviour
{
    GameObject umaPlayer;
    SkinnedMeshRenderer renderer;
    UMAExpressionPlayer expressions;
    Animator animator;
    CharacterController controller;
    DynamicCharacterAvatar avatar;
    
    void Update()
    {
        if (renderer == null)
        {
            foreach (Transform child in transform)
            {
                if (child.name == "UMARenderer")
                {
                    umaPlayer = child.gameObject;
                    renderer = umaPlayer.GetComponent<SkinnedMeshRenderer>();
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
                renderer.enabled = true;

                if (renderer.quality != SkinQuality.Bone4)
                {
                    renderer.quality = SkinQuality.Bone4;
                    expressions.enabled = true;
                    animator.enabled = true;
                    controller.enabled = true;
                }
            }
            else if (distance < 10)
            {
                avatar.enabled = true;
                renderer.enabled = true;

                if (renderer.quality != SkinQuality.Bone2)
                {
                    renderer.quality = SkinQuality.Bone2;
                    expressions.enabled = false;
                    animator.enabled = true;
                    controller.enabled = true;
                }
            }
            else if (distance < 80 && transform.tag != "Player")
            {
                avatar.enabled = true;
                renderer.enabled = true;

                if (renderer.quality != SkinQuality.Bone1)
                {
                    renderer.quality = SkinQuality.Bone1;
                    expressions.enabled = false;
                    animator.enabled = false;
                    controller.enabled = false;
                }
            }
            else if (distance >= 80 && transform.tag != "Player")
            {
                if (renderer.enabled)
                {
                    avatar.enabled = false;
                    renderer.enabled = false;
                    expressions.enabled = false;
                    animator.enabled = false;
                    controller.enabled = false;
                }
            }
        }
    }
}
