using UnityEngine;
using Voxel.Characters.Saving;
using Voxel.Saving;
using Voxel.Utility;
using Voxel.World;

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
        [SerializeField]
        private string playerSaveFileName = "PlayerData.dat";

        public bool PlayerLoaded { get; private set; }

        /// <summary>
        /// Contains the loaded position if one exists, otherwise contains a default raycast position to determine initial player position.
        /// </summary>
        public Vector3 InitialPosition { get; private set; }

        /// <summary>
        /// Contains the loaded rotation if one exists, otherwise contains a default identity rotation.
        /// </summary>
        public Quaternion InitialRotation { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            InitialPosition = new Vector3(0, WorldManager.Instance.MaxWorldHeight, 0);
            InitialRotation = Quaternion.identity;
        }

        public void LoadPlayer()
        {
            (bool loaded, CharacterData playerData) = SaveManager.Instance.Load<CharacterData>(SaveManager.Instance.BuildFilePath(playerSaveFileName));
            PlayerLoaded = loaded;
            if (loaded)
            {
                InitialPosition = playerData.Position;
                InitialRotation = playerData.Rotation;
            }
        }

        public Transform SpawnPlayer()
        {
            if (PlayerLoaded)
            {
                return InstantiatePlayer(InitialPosition, InitialRotation);
            }

            return InstantiatePlayer(GetRayHit(), Quaternion.identity);
        }

        private Vector3 GetRayHit()
        {
            InitialPosition = new Vector3(0, WorldManager.Instance.MaxWorldHeight, 0);
            Debug.Log(InitialPosition);
            return Physics.Raycast(InitialPosition, Vector3.down, out RaycastHit hitInfo, InitialPosition.y * rayHitSpawnOffset)
                   ? hitInfo.point + new Vector3(0, playerSpawnOffset, 0)
                   : InitialPosition;
        }

        private Transform InstantiatePlayer(Vector3 position, Quaternion rotation)
        {
            GameObject player = Instantiate(playerPrefab, position, rotation);
            ActivePlayer = player.transform;
            return ActivePlayer;
        }

        public void SavePlayer()
        {
            SaveManager.Instance.Save(new CharacterData(ActivePlayer.position, ActivePlayer.rotation),
                                      SaveManager.Instance.BuildFilePath(playerSaveFileName));
        }
    }
}