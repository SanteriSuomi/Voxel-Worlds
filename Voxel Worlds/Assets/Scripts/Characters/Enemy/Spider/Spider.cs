using UnityEngine;

namespace Voxel.Characters.Enemy
{
    public class Spider : Enemy
    {
        [SerializeField]
        private LayerMask playerInRangeLayerMask = default;
        private readonly Collider[] playerInRangeResults = new Collider[3];

        private const float playerInRangeRadius = 2;

        public const float MoveSpeedMultiplier = 0.6f;
        public const float MinAngleForRotation = 5;
        public const float RotationSpeedMultiplier = 1.5f;
        public const float JumpMultiplier = 0.35f;

        private void Start()
        {
            fsm.CurrentState = wander;
            fsm.StartTick(OnSpiderPreTick);
        }

        private void OnSpiderPreTick()
        {
            if (CheckPlayerInRange())
            {
                fsm.CurrentState = attack;
            }
            else
            {
                fsm.CurrentState = wander;
            }

            CheckOutOfMap();
        }

        private bool CheckPlayerInRange()
        {
            int playerInRangeAmount = Physics.OverlapSphereNonAlloc(transform.position, playerInRangeRadius, playerInRangeResults, playerInRangeLayerMask);
            return playerInRangeAmount >= 1;
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