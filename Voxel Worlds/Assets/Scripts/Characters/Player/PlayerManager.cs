using System.Linq;
using UnityEngine;
using Voxel.Characters.Saving;
using Voxel.Items.Inventory;
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
        public Player Player { get; set; }

        public CharacterController CharacterController { get; private set; }

        [SerializeField]
        private Canvas mainUICanvas = default;

        [SerializeField]
        private float playerSpawnOffset = 1;
        [SerializeField]
        private string fluidGameObjectIdentifier = "Fluid";
        [SerializeField]
        private float rayHitSpawnOffset = 2;
        [SerializeField]
        private float cameraPlaneRenderDistance = 0.1f;
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

        public void Save()
        {
            SaveManager.Instance.Save(new CharacterData(ActivePlayer.position, ActivePlayer.rotation),
                                      SaveManager.Instance.BuildFilePath(playerSaveFileName));
        }

        public void Load()
        {
            (bool loaded, CharacterData playerData) = SaveManager.Instance.Load<CharacterData>(SaveManager.Instance.BuildFilePath(playerSaveFileName));
            PlayerLoaded = loaded;
            if (PlayerLoaded)
            {
                InitialPosition = playerData.Position;
                InitialRotation = playerData.Rotation;
            }
        }

        // TODO: improve player respawning
        public void RespawnAndResetPlayer()
        {
            var chunks = WorldManager.Instance.GetAllChunks();
            for (int i = 0; i < chunks.Count; i++)
            {
                Chunk chunk = chunks.ElementAt(i).Value;
                Vector3 chunkPos = chunk.BlockGameObject.transform.position;
                float distance = (ActivePlayer.position - chunkPos).magnitude;
                if (distance >= 25 && distance <= 50)
                {
                    Player.Health = Player.StartingHealth;
                    Vector3 spawnPos = chunkPos;
                    spawnPos.y = (float)WorldManager.Instance.MaxWorldHeight / 2;
                    ActivePlayer.position = spawnPos;
                }
            }
        }

        public Transform SpawnPlayer()
        {
            if (PlayerLoaded)
            {
                return InitializeAndSpawnPlayer(InitialPosition, InitialRotation);
            }

            return InitializeAndSpawnPlayer(GetPosition(), Quaternion.identity);
        }

        private Transform InitializeAndSpawnPlayer(Vector3 position, Quaternion rotation)
        {
            GameObject player = Instantiate(playerPrefab, position, rotation);
            ActivePlayer = player.transform;
            Player = ActivePlayer.GetComponent<Player>();
            CharacterController = ActivePlayer.GetComponent<CharacterController>();
            InitializeUI();
            InventoryManager.Instance.Load();
            return ActivePlayer;
        }

        private Vector3 GetPosition()
        {
            int searchSize = WorldManager.Instance.ChunkSize * WorldManager.Instance.Radius / 4;
            Vector2 randomCoord = Random.insideUnitCircle * searchSize;
            InitialPosition = new Vector3(randomCoord.x, WorldManager.Instance.MaxWorldHeight, randomCoord.y);

            Physics.Raycast(InitialPosition, Vector3.down, out RaycastHit hitInfo, InitialPosition.y * rayHitSpawnOffset);
            while (hitInfo.collider != null
                   && hitInfo.collider.name.Contains(fluidGameObjectIdentifier))
            {
                Physics.Raycast(InitialPosition, Vector3.down, out hitInfo, InitialPosition.y * rayHitSpawnOffset);
            }

            return hitInfo.point + new Vector3(0, playerSpawnOffset, 0);
        }

        private void InitializeUI()
        {
            mainUICanvas.renderMode = RenderMode.ScreenSpaceCamera;
            mainUICanvas.worldCamera = ReferenceManager.Instance.MainCamera;
            mainUICanvas.planeDistance = cameraPlaneRenderDistance;
        }
    }
}