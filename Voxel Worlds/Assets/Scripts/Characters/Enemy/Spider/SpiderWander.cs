using UnityEngine;
using Voxel.Characters.Enemy;

namespace Voxel.Characters.AI
{
    public class SpiderWander : Wander
    {
        private SpiderBase spiderBaseState;
        private Vector3 currentDirection;

        private float moveTime;
        private const float minMoveTime = 2.5f;
        private const float maxMoveTime = 6.5f;
        private float currentMaxMoveTime;

        [SerializeField]
        private Transform objectsAheadStart = default;
        private readonly Collider[] aheadResults = new Collider[3];
        private const float aheadRadius = 0.55f;

        private void OnEnable() => spiderBaseState = (SpiderBase)baseState;

        public override void Enter() => GetRandomDirection();

        public override void Tick()
        {
            // Check if there are no chunks where we're going to right now.
            if (CheckEmptyAhead())
            {
                return;
            }

            Quaternion rotationToDirection = Quaternion.LookRotation(currentDirection, Vector3.up);
            spiderBaseState.TryRotate(rotationToDirection);
            Movement();
        }

        private bool CheckEmptyAhead()
        {
            int objsAheadAmount = Physics.OverlapSphereNonAlloc(objectsAheadStart.position, aheadRadius, aheadResults, spiderBaseState.DetectionLayerMask);
            if (objsAheadAmount <= 0)
            {
                currentDirection = -transform.forward;
                transform.rotation = Quaternion.LookRotation(-transform.forward, Vector3.up);
                return true;
            }

            return false;
        }

        private void Movement() => TryMovePosition();

        private void TryMovePosition()
        {
            moveTime += Time.deltaTime;
            if (moveTime <= currentMaxMoveTime)
            {
                spiderBaseState.RigidBody.MovePosition(transform.position + (currentDirection * Spider.MoveSpeedMultiplier * Time.deltaTime));
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
            Gizmos.DrawRay(transform.position, transform.forward * SpiderBase.InfrontDistance);
        }
        #endif
    }
}