using UnityEngine;
using Voxel.World;

namespace Voxel.Characters.AI
{
    public class SpiderWander : Wander
    {
        private Rigidbody rb;
        private Vector3 currentWanderPoint;

        public override void Enter()
        {
            rb = GetComponent<Rigidbody>();
            currentWanderPoint = GetRandomPoint();
        }

        public override void Tick()
        {
            Rotation();
            Movement();
        }

        private void Rotation()
        {
            Vector3 directionToWanderPoint = (currentWanderPoint - transform.position).normalized;
            bool isFacingWanderPoint = Vector3.Dot(transform.forward, directionToWanderPoint) <= -0.9f;
            if (!isFacingWanderPoint)
            {
                transform.rotation = Quaternion.LookRotation(directionToWanderPoint, Vector3.up);
            }
        }

        private void Movement()
        {
            float distanceToWanderPoint = (currentWanderPoint - transform.position).magnitude;
            if (distanceToWanderPoint > 0.1f)
            {
                rb.MovePosition(currentWanderPoint);
            }
            else if (distanceToWanderPoint <= 0.1f)
            {
                currentWanderPoint = GetRandomPoint();
            }
        }

        public override void Exit()
        {
        }

        private static Vector3 GetRandomPoint()
        {
            Vector2 randomPoint = Random.insideUnitCircle * 5;
            Vector3 rayStart = new Vector3(randomPoint.x, WorldManager.Instance.MaxWorldHeight / 2, randomPoint.y);
            Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, WorldManager.Instance.MaxWorldHeight);
            if (hit.collider == null)
            {
                return GetRandomPoint();
            }
            Debug.Log(hit.point);
            return hit.point;
        }
    }
}