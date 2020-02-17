using UnityEngine;
using UMA.PoseTools;

namespace UMA
{
	/// <summary>
	/// Auxillary slot which adds a CapsuleCollider and Rigidbody to a newly created character.
	/// </summary>
	public class AgentSlotScript : MonoBehaviour
	{
        public void OnDnaApplied(UMAData umaData)
		{
			var rigid = umaData.gameObject.GetComponent<Rigidbody>();
			if (rigid == null)
			{
				rigid = umaData.gameObject.AddComponent<Rigidbody>();
			}
			rigid.constraints = RigidbodyConstraints.FreezeRotation;
			rigid.mass = umaData.characterMass;
            rigid.interpolation = RigidbodyInterpolation.Extrapolate;
            rigid.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

			CapsuleCollider capsule = umaData.gameObject.GetComponent<CapsuleCollider>();
			BoxCollider box = umaData.gameObject.GetComponent<BoxCollider>();

            if (umaData.umaRecipe.raceData.umaTarget == RaceData.UMATarget.Humanoid)
			{
				if (capsule == null)
				{
					capsule = umaData.gameObject.AddComponent<CapsuleCollider>();
				}
				if( box != null )
				{
					Destroy(box);
				}

				capsule.radius = umaData.characterRadius;
				capsule.height = umaData.characterHeight;
				capsule.center = new Vector3(0, capsule.height / 2, 0);
			}
			else
			{
				if (box == null)
				{
					box = umaData.gameObject.AddComponent<BoxCollider>();
				}
				if(capsule != null)
				{
					Destroy(capsule);
				}

				//with skycar this capsule collider makes no sense so we need the bounds to figure out what the size of the box collider should be
				//we will assume that renderer 0 is the base renderer
				var umaRenderer = umaData.GetRenderer(0);
				if (umaRenderer != null)
				{
					box.size = umaRenderer.bounds.size;
					box.center = umaRenderer.bounds.center;
				}
			}
            
            if (umaData.gameObject.GetComponent<Agent>().enabled == true)
                return;

            umaData.gameObject.GetComponent<Agent>().enabled = true;

            // Build Invector / UMA Hybrid... IT'S ALIVE! 
            /*
            var hips = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips);
            var spine = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Spine);
            var upperChest = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.UpperChest);
            var neck = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Neck);
            var head = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);

            var rightShoulder = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightShoulder);
            var rightUpperArm = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightUpperArm);
            var rightLowerArm = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightLowerArm);
            var rightHand = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand);
            
            var leftShoulder = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.LeftShoulder);
            var leftUpperArm = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var leftLowerArm = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var leftHand = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.LeftHand);

            var leftUpperLeg = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            var leftLowerLeg = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            var leftFoot = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.LeftFoot);

            var rightUpperLeg = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightUpperLeg);
            var rightLowerLeg = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightLowerLeg);
            var rightFoot = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightFoot);
            */

            // ==============================================================

            var rightLowerArm = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightLowerArm);
            var leftLowerArm = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var leftLowerLeg = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            var rightLowerLeg = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightLowerLeg);
            
            var hitBox1 = Instantiate(new GameObject());
            var hitBox2 = Instantiate(new GameObject());
            var hitBox3 = Instantiate(new GameObject());
            var hitBox4 = Instantiate(new GameObject());

            hitBox1.transform.parent = rightLowerArm.transform;
            hitBox2.transform.parent = leftLowerArm.transform;
            hitBox3.transform.parent = rightLowerLeg.transform;
            hitBox4.transform.parent = leftLowerLeg.transform;

            var collider1 = hitBox1.gameObject.AddComponent<BoxCollider>();
            var collider2 = hitBox2.gameObject.AddComponent<BoxCollider>();
            var collider3 = hitBox3.gameObject.AddComponent<BoxCollider>();
            var collider4 = hitBox4.gameObject.AddComponent<BoxCollider>();

            collider1.center = new Vector3(-.23f, 0, 0);
            collider1.size = new Vector3(.25f, .1f, .1f);
            collider2.center = new Vector3(-.23f, 0, 0);
            collider2.size = new Vector3(.25f, .1f, .1f);
            collider3.center = new Vector3(-.36f, 0, -.025f);
            collider3.size = new Vector3(.45f, .1f, .1f);
            collider4.center = new Vector3(-.36f, 0, -.025f);
            collider4.size = new Vector3(.45f, .1f, .1f);

            // ==============================================================

            //umaData.gameObject.GetComponent<vThirdPersonController>().enabled = true;
            //umaData.gameObject.GetComponent<vShooterMeleeInput>().enabled = true;
            //umaData.gameObject.GetComponent<vAmmoManager>().enabled = true;
            //umaData.gameObject.GetComponent<vShooterManager>().enabled = true;
            //umaData.gameObject.GetComponent<vMeleeManager>().enabled = true;
            //umaData.gameObject.GetComponent<vHeadTrack>().enabled = true;
            //umaData.gameObject.GetComponent<vRagdoll>().enabled = true;

            // Enable this for foot fall sound effects; currently not working.
            //var feet = umaData.gameObject.GetComponent<vFootStep>();
            //var leftFoot = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.LeftFoot);
            //var rightFoot = umaData.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightFoot);
            //var leftTrigger = leftFoot.gameObject.AddComponent<vFootStepTrigger>();
            //var rightTrigger = rightFoot.gameObject.AddComponent<vFootStepTrigger>();
            //var leftCollider = leftFoot.gameObject.AddComponent<SphereCollider>();
            //var rightCollider = rightFoot.gameObject.AddComponent<SphereCollider>();
            //leftCollider.radius = .1f;
            //rightCollider.radius = .1f;
            //feet.leftFootTrigger = leftTrigger;
            //feet.rightFootTrigger = rightTrigger;
            //feet.enabled = true;

            //umaData.gameObject.GetComponent<vItemManager>().enabled = true;
            //umaData.gameObject.GetComponent<vWeaponHolderManager>().enabled = true;
            //umaData.gameObject.GetComponent<vGenericAction>().enabled = true;
            //umaData.gameObject.GetComponent<vLockOnShooter>().enabled = true;
            //umaData.gameObject.GetComponent<vDrawHideShooterWeapons>().enabled = true;
            
            // umaData.gameObject.GetComponent<vLadderAction>().enabled = true;
            
            //umaData.gameObject.GetComponent<vFreeClimb>().enabled = true;
            //umaData.gameObject.GetComponent<vSwimming>().enabled = true;
            
            //umaData.transform.Find("ShooterAttachments").gameObject.SetActive(true);
            //umaData.transform.Find("ShooterAttachments").GetComponent<vBodySnappingControl>().LoadBones();

            //umaData.transform.Find("ThrowManager").gameObject.SetActive(true);
            //umaData.transform.Find("InventoryCheckItem").gameObject.SetActive(true);

            var expressions = umaData.gameObject.GetComponent<UMAExpressionPlayer>();
            if (expressions != null)
            {
                expressions.enableBlinking = true;
                expressions.enableSaccades = true;
            }

            //umaData.gameObject.GetComponent<FlyInvectorSwitch>().enabled = true;
            //umaData.gameObject.GetComponent<BasicBehaviour>().enabled = true;
        }
    }
}
