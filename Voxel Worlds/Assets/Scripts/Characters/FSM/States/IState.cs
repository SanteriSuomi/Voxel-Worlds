namespace Voxel.Characters.AI
{
    public interface IState
    {
        void Enter();
        void Tick();
        void Exit();
    }
}