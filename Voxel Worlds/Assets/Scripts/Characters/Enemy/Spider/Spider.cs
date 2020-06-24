using UnityEngine;
using Voxel.Characters.AI;
using Voxel.Characters.Interfaces;

namespace Voxel.Characters.Enemy
{
    public class Spider : Enemy, IDamageable
    {
        [SerializeField]
        private LayerMask playerInRangeLayerMask = default;
        private readonly Collider[] playerInRangeResults = new Collider[1];

        #region Spider Variables
        private const float playerInRangeRadius = 2.6f;

        public const float BaseDamageAmount = 10;
        public const float MaxDistanceFromTarget = 1;
        public const float MinDistanceForDamage = 1.1f;
        public const float MaxDamageInterval = 5;

        public const float MoveSpeedMultiplier = 1;
        public const float AttackMoveSpeedMultiplier = 1.5f;

        public const float MinAngleForRotation = 3;
        public const float RotationSpeedMultiplier = 1.8f;
        public const float JumpMultiplier = 0.36f;
        #endregion

        private SpiderAttack spiderAttack;
        private SpiderBase spiderBase;

        public void Damage(float damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                RemoveEnemy();
            }
        }

        private void Start()
        {
            fsm.CurrentState = wander;
            spiderAttack = (SpiderAttack)attack;
            spiderBase = (SpiderBase)baseState;
            fsm.StartTick(() =>
            {
                spiderBase.TryJump();
                CheckState();
                CheckOutOfMap();
            });
        }

        private void CheckState()
        {
            (Collider player, bool inRange) = TryGetPlayerInRange();
            if (inRange)
            {
                spiderAttack.Target = player.transform;
                fsm.CurrentState = attack;
            }
            else
            {
                fsm.CurrentState = wander;
            }
        }

        private (Collider player, bool inRange) TryGetPlayerInRange()
        {
            int amount = Physics.OverlapSphereNonAlloc(transform.position, playerInRangeRadius, playerInRangeResults, playerInRangeLayerMask);
            return (playerInRangeResults[0], amount > 0);
        }

        private void CheckOutOfMap()
        {
            if (transform.position.y <= 0)
            {
                Destroy(gameObject);
            }
        }

        private void OnDisable() => fsm.StopTick();

        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, playerInRangeRadius);
        }
        #endif
    }
}