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
        [SerializeField]
        private float rayHitSpawnOffset = 2;

        public Transform SpawnPlayer(Vector3 atPosition)
        {
            bool hitRay = Physics.Raycast(atPosition, Vector3.down, out RaycastHit hitInfo, atPosition.y * rayHitSpawnOffset);
            Vector3 spawnPosition;
            if (hitRay)
            {
                spawnPosition = hitInfo.point + new Vector3(0, playerSpawnOffset, 0);
            }
            else
            {
                spawnPosition = atPosition;
            }

            GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            ActivePlayer = player.transform;
            return ActivePlayer;
        }
    }
}