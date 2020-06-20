namespace Voxel.Characters.AI
{
    public abstract class Attack : State
    {
        public abstract override void Enter();
        public abstract override void Exit();
        public abstract override void Tick();
    }
}