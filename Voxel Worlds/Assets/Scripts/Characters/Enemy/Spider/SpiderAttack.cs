using UnityEngine;
using Voxel.Characters.Enemy;
using Voxel.Characters.Interfaces;

namespace Voxel.Characters.AI
{
    public class SpiderAttack : Attack
    {
        private SpiderBase spiderBaseState;
        public Transform Target { get; set; }

        private void OnEnable() => spiderBaseState = (SpiderBase)baseState;

        public override void Enter()
        {
        }

        public override void Tick()
        {
            Vector3 targetHeading = Target.position - transform.position;
            Vector3 targetDirection = targetHeading.normalized;
            Rotate(targetDirection);
            float distanceToTarget = Move(targetHeading, targetDirection);
            Damage(distanceToTarget);
        }

        private void Rotate(Vector3 targetDirection)
        {
            Vector3 targetDirectionForRotation = targetDirection;
            targetDirectionForRotation.y = 0;
            spiderBaseState.TryRotate(Quaternion.LookRotation(targetDirectionForRotation, Vector3.up));
        }

        private float Move(Vector3 targetHeading, Vector3 targetDirection)
        {
            float distanceToTarget = targetHeading.magnitude;
            TryMoveTowardsTarget(distanceToTarget, targetDirection);
            return distanceToTarget;
        }

        private void TryMoveTowardsTarget(float distance, Vector3 direction)
        {
            if (distance >= Spider.MaxDistanceFromTarget)
            {
                spiderBaseState.RigidBody.MovePosition(transform.position + (direction * Spider.AttackMoveSpeedMultiplier * Time.deltaTime));
            }
        }

        private void Damage(float distanceToTarget)
        {
            if (distanceToTarget <= Spider.MinDistanceForDamage
                && Target.TryGetComponent(out IDamageable damageable))
            {
                damageable.Damage(Spider.BaseDamageAmount * Time.deltaTime);
            }
        }

        public override void Exit()
        {
        }
    }
}