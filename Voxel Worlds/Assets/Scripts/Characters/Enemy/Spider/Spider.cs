
namespace Voxel.Characters.Enemy
{
    public class Spider : Enemy
    {
        public const float MoveSpeedMultiplier = 0.6f;

        public const float MinAngleForRotation = 15;
        public const float RotationSpeedMultiplier = 1.5f;

        private void Start()
        {
            fsm.CurrentState = wander;
            fsm.StartTick(() =>
            {
                if (transform.position.y <= 0)
                {
                    Destroy(gameObject);
                }
            });
        }

        private void OnDisable() => fsm.StopTick();
    }
}