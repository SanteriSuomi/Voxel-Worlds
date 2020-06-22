using UnityEngine;
using Voxel.Characters.Enemy;

namespace Voxel.Characters.AI
{
    public class SpiderWander : Wander
    {
        [SerializeField]
        private Rigidbody rb = default;
        private Vector3 currentDirection;

        private float moveTime;
        private const float minMoveTime = 2;
        private const float maxMoveTime = 6;
        private float currentMaxMoveTime;

        [SerializeField]
        private LayerMask detectionLayerMask = default;

        [SerializeField]
        private Transform objectsAheadStart = default;
        private readonly Collider[] aheadResults = new Collider[3];
        private const float aheadRadius = 0.55f;

        private readonly RaycastHit[] infrontResults = new RaycastHit[3];
        private const float infrontDistance = 0.45f;

        public override void Enter() => GetRandomDirection();

        public override void Tick()
        {
            // Check if there are no chunks where we're going to right now.
            if (CheckEmptyAhead())
            {
                return;
            }

            Rotation();
            Movement();
        }

        private bool CheckEmptyAhead()
        {
            int objsAheadAmount = Physics.OverlapSphereNonAlloc(transform.position + transform.forward, aheadRadius, aheadResults, detectionLayerMask);
            if (objsAheadAmount <= 0)
            {
                currentDirection = -transform.forward;
                transform.rotation = Quaternion.LookRotation(-transform.forward, Vector3.up);
                return true;
            }

            return false;
        }

        private void Rotation()
        {
            Quaternion lookRotation = Quaternion.LookRotation(currentDirection, Vector3.up);
            float rotationAngle = Quaternion.Angle(transform.rotation, lookRotation);
            if (rotationAngle > Spider.MinAngleForRotation)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Spider.RotationSpeedMultiplier * Time.deltaTime);
            }
        }

        private void Movement()
        {
            TryJump();
            TryMovePosition();
        }

        private void TryJump()
        {
            int objsInfrontAmount = Physics.RaycastNonAlloc(transform.position, transform.forward, infrontResults, infrontDistance, detectionLayerMask);
            if (objsInfrontAmount > 0)
            {
                rb.AddForce(Vector3.up * Spider.JumpMultiplier, ForceMode.Impulse);
            }
        }

        private void TryMovePosition()
        {
            moveTime += Time.deltaTime;
            if (moveTime <= currentMaxMoveTime)
            {
                rb.MovePosition(transform.position + (currentDirection * Spider.MoveSpeedMultiplier * Time.deltaTime));
            }
            else
            {
                moveTime = 0;
                GetRandomDirection();
                currentMaxMoveTime = Random.Range(minMoveTime, maxMoveTime);
            }
        }

        public override void Exit()
        {
        }

        private void GetRandomDirection()
            => currentDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));

        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(objectsAheadStart.position, aheadRadius);
            Gizmos.DrawRay(transform.position, transform.forward * infrontDistance);
        }
        #endif
    }
}