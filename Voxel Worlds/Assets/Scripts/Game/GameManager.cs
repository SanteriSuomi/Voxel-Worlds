using Voxel.Utility;

namespace Voxel.Game
{
    public class GameManager : Singleton<GameManager>
    {
        public bool IsGameRunning { get; private set; } = true;

        private void OnEnable()
        {
            IsGameRunning = true;
        }

        private void OnDisable()
        {
            IsGameRunning = false;
        }
    }
}