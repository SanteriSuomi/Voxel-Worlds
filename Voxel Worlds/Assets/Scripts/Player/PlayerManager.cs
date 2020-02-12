using UnityEngine;
using Voxel.Utility;
using Voxel.vWorld;

namespace Voxel.Player
{
    public class PlayerManager : Singleton<PlayerManager>
    {
        [SerializeField]
        private GameObject playerPrefab = default;
        public Transform ActivePlayer { get; private set; }

        public void SpawnPlayer()
        {
            Debug.Log("spawn");
            int worldMaxHeight = World.Instance.MaxWorldHeight;
            Vector3 spawnRaycastPosition = new Vector3(0, worldMaxHeight, 0);
            Physics.Raycast(spawnRaycastPosition, Vector3.down, out RaycastHit hitInfo, worldMaxHeight);
            Vector3 spawnPosition = hitInfo.point + new Vector3(0, 0.5f, 0);
            ActivePlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity).transform;
        }
    }
}