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

        public Transform SpawnPlayer(Vector3 atPosition)
        {
            Physics.Raycast(atPosition, Vector3.down, out RaycastHit hitInfo, atPosition.y * 2);
            Vector3 spawnPosition = hitInfo.point + new Vector3(0, playerSpawnOffset, 0);
            return ActivePlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity).transform;
        }
    }
}