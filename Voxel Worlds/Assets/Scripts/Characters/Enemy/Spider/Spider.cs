
namespace Voxel.Characters.Enemy
{
    public class Spider : Enemy
    {
        private void Start()
        {
            fsm.CurrentState = wander;
            fsm.StartTick();
        }

        private void OnDisable() => fsm.StopTick();
    }
}