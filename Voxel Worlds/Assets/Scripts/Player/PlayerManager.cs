using UnityEngine;
using Voxel.Utility;

namespace Voxel.Player
{
    public class PlayerManager : Singleton<PlayerManager>
    {
        [SerializeField]
        private GameObject playerPrefab = default;
        public Transform ActivePlayer { get; private set; }

        [SerializeField]
        private float playerSpawnOffset = 1;

        public Transform SpawnPlayer(int maxWorldHeight)
        {
            Vector3 spawnRaycastPosition = new Vector3(0, maxWorldHeight, 0);
            Physics.Raycast(spawnRaycastPosition, Vector3.down, out RaycastHit hitInfo, maxWorldHeight);
            Vector3 spawnPosition = hitInfo.point + new Vector3(0, playerSpawnOffset, 0);
            return ActivePlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity).transform;
        }
    }
}