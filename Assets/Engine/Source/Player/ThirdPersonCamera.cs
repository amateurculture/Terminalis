using AC_System;
using UnityEngine;

namespace Controllers
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] public Transform target;
        [SerializeField] public float distance = 10.0f;

        private GameObject player;
        private Vector3 lerpTarget;
        private Vector3 startPos;
        private Vector3 endPos;
        private Vector3 startLerp;
        private Vector3 endLerp;
        private float baseTargetHeight = 1.35f;
        private LayerMask layerMask;
        private bool isTouching;
        private RaycastHit hit;
        private float smoothRadius = .5f;

        void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player");
            layerMask = new LayerMask();
            layerMask = ~((1 << LayerMask.NameToLayer("Ignore Raycast")) | 
                (1 << LayerMask.NameToLayer("TransparentFX")));

            hit = new RaycastHit();
        }

        public PlayerController.ControllerPOV UpdateCameraForScrollwheel(PlayerController.ControllerPOV controllerPOV, GameObject corpse)
        {
            PlayerController.ControllerPOV testControllerPOV = controllerPOV;
            Vector3 adjustedPosition = target.localPosition;

            var delta = Input.GetAxis("Mouse ScrollWheel");
            if (delta < 0)
            {
                if (testControllerPOV == PlayerController.ControllerPOV.First)
                {
                    testControllerPOV = PlayerController.ControllerPOV.Third;
                    distance = 1f;
                    target.localPosition = new Vector3(target.localPosition.x, baseTargetHeight, target.localPosition.z);
                }
                else
                    distance += (distance < 100f) ? ((distance / 5f) < 1 ? 1 : (distance / 5f)) : 0;
            }
            else if (delta > 0)
                distance -= (distance > 0f) ? ((distance / 5f) < 1 ? 1 : (distance / 5f)) : 0;
            
            Vector3 distance1 = target.transform.position - transform.position;

            if (corpse != null && testControllerPOV == PlayerController.ControllerPOV.Third)
                if (distance1.magnitude < .35f)
                    corpse.SetActive(false);
                else
                    corpse.SetActive(true);
            
            return testControllerPOV;
        }

        void OnDrawGizmosSelected()
        {
            //Gizmos.color = Color.red;
            //Gizmos.DrawWireSphere(lerpTarget, smoothRadius);
            //Gizmos.DrawWireSphere(target.position, smoothRadius);
        }

        Vector3 GetClosestLerp(Vector3[] lerpChoices)
        {
            Vector3 bestTarget = Vector3.zero;
            float closestDistanceSqr = Mathf.Infinity;
            Vector3 currentPosition = target.position;

            foreach (Vector3 potentialTarget in lerpChoices)
            {
                Vector3 directionToTarget = potentialTarget - currentPosition;

                float dSqrToTarget = directionToTarget.sqrMagnitude;
                if (dSqrToTarget < closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    bestTarget = potentialTarget;
                }
            }
            return bestTarget;
        }

        void LateUpdate()
        {
            if (!target)
                return;

            //var currentRotationAngle = transform.eulerAngles.y;
            //var currentRotationX = transform.eulerAngles.x;
            //if (currentRotationX > 90 && currentRotationX < 290) currentRotationX = 290;
            //var currentRotation = Quaternion.Euler(currentRotationX, currentRotationAngle, 0);

            //target.position = new Vector3(target.position.x, 6f, target.position.z);

            lerpTarget = target.position;
            lerpTarget -= transform.rotation * Vector3.forward * distance;

            Vector3 heading = lerpTarget - target.position;
            float distance1 = heading.magnitude;
            Vector3 direction = heading / distance1;

            Vector3[] choice = { lerpTarget, Vector3.positiveInfinity, Vector3.positiveInfinity, Vector3.positiveInfinity };

            if (Physics.SphereCast(target.position, smoothRadius, direction.normalized * distance1, out hit, distance1, layerMask))
                choice[0] = Vector3.MoveTowards(hit.point, hit.point + hit.normal, smoothRadius);
            if (Physics.SphereCast(target.position, .25f, direction.normalized * distance1, out hit, distance1, layerMask))
                choice[1] = Vector3.MoveTowards(hit.point, hit.point + hit.normal, smoothRadius);
            if (Physics.Raycast(target.position, direction.normalized * distance1, out hit, smoothRadius, layerMask))
                choice[2] = Vector3.MoveTowards(hit.point, hit.point + hit.normal, smoothRadius);

            lerpTarget = GetClosestLerp(choice);
            
            //if (Vector3.Distance(transform.position, lerpTarget) > smoothRadius) // .25f
            //    transform.position = Vector3.MoveTowards(transform.position, lerpTarget, Time.deltaTime * 6f);
            //else
                transform.position = lerpTarget;
        }
    }
}
