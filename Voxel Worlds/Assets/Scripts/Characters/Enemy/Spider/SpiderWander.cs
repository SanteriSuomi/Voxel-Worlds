using UnityEngine;
using Voxel.Characters.Enemy;

namespace Voxel.Characters.AI
{
    public class SpiderWander : Wander
    {
        [SerializeField]
        private Rigidbody rb = default;
        [SerializeField]
        private Transform hasObjectsAheadStart = default;
        private Vector3 currentDirection;

        private float moveTime;
        private const float maxMoveTime = 4;
        private float currentMaxMoveTime;

        private readonly Collider[] objsAheadResults = new Collider[5];
        private const float objsAheadRadius = 0.5f;

        public override void Enter() => GetRandomDirection();

        public override void Tick()
        {
            Rotation();
            Movement();
        }

        private void Rotation()
        {
            Quaternion lookRotation = Quaternion.LookRotation(currentDirection, Vector3.up);
            if (Quaternion.Angle(transform.rotation, lookRotation) > Spider.MinAngleForRotation)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Spider.RotationSpeedMultiplier * Time.deltaTime);
            }
        }

        private void Movement()
        {
            int numberOfObjsInFront = Physics.OverlapSphereNonAlloc(hasObjectsAheadStart.position, objsAheadRadius, objsAheadResults);
            if (numberOfObjsInFront <= 0)
            {
                currentDirection = -transform.forward;
                transform.rotation = Quaternion.LookRotation(-transform.forward, Vector3.up);
                return;
            }

            moveTime += Time.deltaTime;
            if (moveTime <= currentMaxMoveTime)
            {
                rb.MovePosition(transform.position + (currentDirection * Spider.MoveSpeedMultiplier * Time.deltaTime));
            }
            else
            {
                moveTime = 0;
                GetRandomDirection();
                currentMaxMoveTime = Random.Range(1, maxMoveTime);
            }
        }

        public override void Exit()
        {
        }

        private void GetRandomDirection() 
            => currentDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));

        #if UNITY_EDITOR
        private void OnDrawGizmos() => Gizmos.DrawWireSphere(hasObjectsAheadStart.position, objsAheadRadius);
        #endif
    }
}